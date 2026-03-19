#include "app_sim_snapshot.h"
#include "low_power_sim.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#include <direct.h>
#define HOST_SIM_MKDIR(path) _mkdir(path)
#else
#include <sys/stat.h>
#include <sys/types.h>
#define HOST_SIM_MKDIR(path) mkdir((path), 0755)
#endif

#define HOST_SIM_MAX_LINE 512

typedef struct
{
    const char *input_path;
    const char *log_path;
    const char *snapshot_path;
} ReplayOptions;

typedef struct
{
    uint32_t steps;
    uint32_t to_sleep_steps;
    uint32_t wake_steps;
    uint32_t deep_sleep_steps;
    uint32_t normal_l2_steps;
    uint32_t normal_l3_steps;
    uint32_t first_to_sleep_step;
    uint32_t first_wake_step;
    uint32_t first_deep_sleep_step;
} ReplaySummary;

static void HostSim_PrintUsage(const char *program)
{
    fprintf(stderr, "Usage: %s [--input path] [--log path] [--snapshot path]\n", program);
}

static int HostSim_ParseArgs(int argc, char **argv, ReplayOptions *options)
{
    int i;

    options->input_path = "host_sim/scenarios/low_power/low_power_normal.csv";
    options->log_path = "artifacts/host-sim/low-power-replay.log";
    options->snapshot_path = "artifacts/host-sim/low-power-replay.jsonl";

    for (i = 1; i < argc; ++i)
    {
        if ((strcmp(argv[i], "--input") == 0) && (i + 1 < argc))
        {
            options->input_path = argv[++i];
        }
        else if ((strcmp(argv[i], "--log") == 0) && (i + 1 < argc))
        {
            options->log_path = argv[++i];
        }
        else if ((strcmp(argv[i], "--snapshot") == 0) && (i + 1 < argc))
        {
            options->snapshot_path = argv[++i];
        }
        else
        {
            HostSim_PrintUsage(argv[0]);
            return 0;
        }
    }

    return 1;
}

static void HostSim_EnsureParentDir(const char *path)
{
    char buffer[260];
    char *cursor;

    if (path == NULL)
    {
        return;
    }

    strncpy(buffer, path, sizeof(buffer) - 1U);
    buffer[sizeof(buffer) - 1U] = '\0';

    for (cursor = buffer; *cursor != '\0'; ++cursor)
    {
        if ((*cursor == '\\') || (*cursor == '/'))
        {
            char saved = *cursor;
            *cursor = '\0';
            if (buffer[0] != '\0')
            {
                HOST_SIM_MKDIR(buffer);
            }
            *cursor = saved;
        }
    }
}

static int HostSim_IsBlank(const char *line)
{
    while (*line != '\0')
    {
        if ((*line != ' ') && (*line != '\t') && (*line != '\r') && (*line != '\n'))
        {
            return 0;
        }
        ++line;
    }
    return 1;
}

static int HostSim_ParseLine(const char *line, AppSimInputSnapshot *input)
{
    unsigned int tick_ms;
    unsigned int cell_min_mv;
    int charge_current_ma;
    int discharge_current_ma;
    unsigned int ext_wake_event;
    unsigned int force_sleep_level;

    if ((line[0] == '#') || HostSim_IsBlank(line) || (strncmp(line, "tick_ms", 7U) == 0))
    {
        return 0;
    }

    if (sscanf(line,
               " %u , %u , %d , %d , %u , %u",
               &tick_ms,
               &cell_min_mv,
               &charge_current_ma,
               &discharge_current_ma,
               &ext_wake_event,
               &force_sleep_level) != 6)
    {
        return -1;
    }

    memset(input, 0, sizeof(*input));
    input->tick_ms = tick_ms;
    input->cell_min_mv = (uint16_t)cell_min_mv;
    input->charge_current_ma = charge_current_ma;
    input->discharge_current_ma = discharge_current_ma;
    input->ext_wake_event = (uint8_t)ext_wake_event;
    input->force_sleep_level = (uint8_t)force_sleep_level;
    return 1;
}

static const char *HostSim_ResultName(const AppSimOutputSnapshot *output)
{
    if ((output->power_state_flags & LOW_POWER_FLAG_DEEP_SLEEP) != 0U)
    {
        return "deep_sleep";
    }
    if ((output->power_state_flags & LOW_POWER_FLAG_WAKE_EVENT) != 0U)
    {
        return "wake";
    }
    if ((output->power_state_flags & LOW_POWER_FLAG_TO_SLEEP) != 0U)
    {
        return "to_sleep";
    }
    return "active";
}

static void HostSim_UpdateSummary(ReplaySummary *summary, const AppSimOutputSnapshot *output)
{
    ++summary->steps;

    if ((output->power_state_flags & LOW_POWER_FLAG_TO_SLEEP) != 0U)
    {
        ++summary->to_sleep_steps;
        if (summary->first_to_sleep_step == 0U)
        {
            summary->first_to_sleep_step = summary->steps;
        }
    }
    if ((output->power_state_flags & LOW_POWER_FLAG_WAKE_EVENT) != 0U)
    {
        ++summary->wake_steps;
        if (summary->first_wake_step == 0U)
        {
            summary->first_wake_step = summary->steps;
        }
    }
    if ((output->power_state_flags & LOW_POWER_FLAG_DEEP_SLEEP) != 0U)
    {
        ++summary->deep_sleep_steps;
        if (summary->first_deep_sleep_step == 0U)
        {
            summary->first_deep_sleep_step = summary->steps;
        }
    }
    if (output->power_mode == LOW_POWER_MODE_NORMAL_L2)
    {
        ++summary->normal_l2_steps;
    }
    else if (output->power_mode == LOW_POWER_MODE_NORMAL_L3)
    {
        ++summary->normal_l3_steps;
    }
}

int main(int argc, char **argv)
{
    ReplayOptions options;
    FILE *input_file;
    FILE *log_file;
    FILE *snapshot_file;
    char line[HOST_SIM_MAX_LINE];
    int line_index = 0;
    int failures = 0;
    ReplaySummary summary;
    LowPowerSimConfig config;
    LowPowerSimState state;
    AppSimInputSnapshot input;
    AppSimOutputSnapshot output;
    AppSimTraceSnapshot trace;
    char json_line[1024];

    if (!HostSim_ParseArgs(argc, argv, &options))
    {
        return 1;
    }

    input_file = fopen(options.input_path, "r");
    if (input_file == NULL)
    {
        fprintf(stderr, "failed to open input scenario: %s\n", options.input_path);
        return 1;
    }

    HostSim_EnsureParentDir(options.log_path);
    HostSim_EnsureParentDir(options.snapshot_path);

    log_file = fopen(options.log_path, "w");
    snapshot_file = fopen(options.snapshot_path, "w");
    if ((log_file == NULL) || (snapshot_file == NULL))
    {
        fprintf(stderr, "failed to open output files\n");
        fclose(input_file);
        if (log_file != NULL)
        {
            fclose(log_file);
        }
        if (snapshot_file != NULL)
        {
            fclose(snapshot_file);
        }
        return 1;
    }

    memset(&summary, 0, sizeof(summary));
    LowPowerSim_LoadBaselineConfig(&config);
    LowPowerSim_Reset(&state);

    while (fgets(line, sizeof(line), input_file) != NULL)
    {
        int parse_status = HostSim_ParseLine(line, &input);
        ++line_index;
        if (parse_status == 0)
        {
            continue;
        }
        if (parse_status < 0)
        {
            fprintf(stderr, "invalid scenario line %d: %s", line_index, line);
            ++failures;
            break;
        }

        memset(&output, 0, sizeof(output));
        LowPowerSim_Step(&config, &state, &input, &output);
        HostSim_UpdateSummary(&summary, &output);

        AppSimSnapshot_Init(&trace);
        trace.cycle = (uint32_t)summary.steps;
        trace.step = (uint32_t)summary.steps;
        trace.input = input;
        trace.output = output;
        AppSimSnapshot_SetText(trace.source, sizeof(trace.source), "low_power_replay");
        AppSimSnapshot_SetText(trace.result, sizeof(trace.result), HostSim_ResultName(&output));

        AppSimSnapshot_ToJsonLine(&trace, json_line, sizeof(json_line));
        fputs(json_line, snapshot_file);

        fprintf(log_file,
                "[low-power] step=%lu tick_ms=%lu cell_min_mv=%u chg=%ld dsg=%ld wake=%u force=%u mode=%u flags=0x%08lX\n",
                (unsigned long)summary.steps,
                (unsigned long)input.tick_ms,
                input.cell_min_mv,
                (long)input.charge_current_ma,
                (long)input.discharge_current_ma,
                input.ext_wake_event,
                input.force_sleep_level,
                output.power_mode,
                (unsigned long)output.power_state_flags);
    }

    fprintf(log_file,
            "[summary] steps=%lu to_sleep_steps=%lu wake_steps=%lu deep_sleep_steps=%lu normal_l2_steps=%lu normal_l3_steps=%lu "
            "first_to_sleep_step=%lu first_wake_step=%lu first_deep_sleep_step=%lu\n",
            (unsigned long)summary.steps,
            (unsigned long)summary.to_sleep_steps,
            (unsigned long)summary.wake_steps,
            (unsigned long)summary.deep_sleep_steps,
            (unsigned long)summary.normal_l2_steps,
            (unsigned long)summary.normal_l3_steps,
            (unsigned long)summary.first_to_sleep_step,
            (unsigned long)summary.first_wake_step,
            (unsigned long)summary.first_deep_sleep_step);

    fclose(input_file);
    fclose(log_file);
    fclose(snapshot_file);

    return failures == 0 ? 0 : 1;
}
