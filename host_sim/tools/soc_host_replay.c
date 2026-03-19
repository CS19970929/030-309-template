#include "app_sim_snapshot.h"
#include "soc_sim.h"

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
    int quiet_console;
    uint32_t repeat_count;
} ReplayOptions;

typedef struct
{
    AppSimInputSnapshot input;
    uint8_t restore_event;
} SocReplayRow;

typedef struct
{
    uint32_t steps;
    uint32_t ocv_corrected_steps;
    uint32_t clamped_empty_steps;
    uint32_t clamped_full_steps;
    uint16_t min_soc_est_pct_x10;
    uint16_t max_soc_est_pct_x10;
    int32_t max_abs_error_pct_x10;
    uint16_t final_soc_est_pct_x10;
    uint32_t restore_count;
} ReplaySummary;

static void HostSim_PrintUsage(const char *program)
{
    fprintf(stderr,
            "Usage: %s [--input path] [--log path] [--snapshot path] [--quiet-console]\n",
            program);
}

static int HostSim_ParseArgs(int argc, char **argv, ReplayOptions *options)
{
    int i;

    options->input_path = "host_sim/scenarios/soc/soc_mixed_cycle.csv";
    options->log_path = "artifacts/host-sim/soc-replay.log";
    options->snapshot_path = "artifacts/host-sim/soc-replay.jsonl";
    options->quiet_console = 0;
    options->repeat_count = 1U;

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
        else if (strcmp(argv[i], "--quiet-console") == 0)
        {
            options->quiet_console = 1;
        }
        else if ((strcmp(argv[i], "--repeat") == 0) && (i + 1 < argc))
        {
            options->repeat_count = (uint32_t)strtoul(argv[++i], NULL, 10);
            if (options->repeat_count == 0U)
            {
                options->repeat_count = 1U;
            }
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

static int HostSim_ParseLine(const char *line, SocReplayRow *row)
{
    unsigned int tick_ms;
    unsigned int pack_mv;
    unsigned int soc_pct_x10;
    int charge_current_ma;
    int discharge_current_ma;
    unsigned int charger_present;
    unsigned int load_present;
    unsigned int restore_event = 0U;

    if (line[0] == '#')
    {
        return 0;
    }
    if (HostSim_IsBlank(line))
    {
        return 0;
    }
    if (strncmp(line, "tick_ms", 7U) == 0)
    {
        return 0;
    }

    if (sscanf(line,
               " %u , %u , %u , %d , %d , %u , %u , %u",
               &tick_ms,
               &pack_mv,
               &soc_pct_x10,
               &charge_current_ma,
               &discharge_current_ma,
               &charger_present,
               &load_present,
               &restore_event) < 7)
    {
        return -1;
    }

    memset(row, 0, sizeof(*row));
    row->input.tick_ms = tick_ms;
    row->input.pack_mv = (uint16_t)pack_mv;
    row->input.soc_pct_x10 = (uint16_t)soc_pct_x10;
    row->input.charge_current_ma = charge_current_ma;
    row->input.discharge_current_ma = discharge_current_ma;
    row->input.charger_present = (uint8_t)charger_present;
    row->input.load_present = (uint8_t)load_present;
    row->restore_event = (uint8_t)restore_event;
    return 1;
}

static const char *HostSim_ResultName(const AppSimOutputSnapshot *output)
{
    if ((output->soc_state_flags & SOC_SIM_FLAG_OCV_CORRECTED) != 0U)
    {
        return "ocv_corrected";
    }
    if ((output->soc_state_flags & SOC_SIM_FLAG_CLAMPED_EMPTY) != 0U)
    {
        return "clamped_empty";
    }
    if ((output->soc_state_flags & SOC_SIM_FLAG_CLAMPED_FULL) != 0U)
    {
        return "clamped_full";
    }
    if ((output->soc_state_flags & SOC_SIM_FLAG_CHARGE) != 0U)
    {
        return "charge";
    }
    if ((output->soc_state_flags & SOC_SIM_FLAG_DISCHARGE) != 0U)
    {
        return "discharge";
    }
    return "idle";
}

static void HostSim_UpdateSummary(ReplaySummary *summary, const AppSimOutputSnapshot *output)
{
    int32_t abs_error;

    ++summary->steps;
    if ((output->soc_state_flags & SOC_SIM_FLAG_OCV_CORRECTED) != 0U)
    {
        ++summary->ocv_corrected_steps;
    }
    if ((output->soc_state_flags & SOC_SIM_FLAG_CLAMPED_EMPTY) != 0U)
    {
        ++summary->clamped_empty_steps;
    }
    if ((output->soc_state_flags & SOC_SIM_FLAG_CLAMPED_FULL) != 0U)
    {
        ++summary->clamped_full_steps;
    }
    if ((output->soc_state_flags & SOC_SIM_FLAG_POWER_RESTORE) != 0U)
    {
        ++summary->restore_count;
    }
    if (output->soc_est_pct_x10 < summary->min_soc_est_pct_x10)
    {
        summary->min_soc_est_pct_x10 = output->soc_est_pct_x10;
    }
    if (output->soc_est_pct_x10 > summary->max_soc_est_pct_x10)
    {
        summary->max_soc_est_pct_x10 = output->soc_est_pct_x10;
    }
    abs_error = (output->soc_error_pct_x10 >= 0) ? output->soc_error_pct_x10 : -output->soc_error_pct_x10;
    if (abs_error > summary->max_abs_error_pct_x10)
    {
        summary->max_abs_error_pct_x10 = abs_error;
    }
    summary->final_soc_est_pct_x10 = output->soc_est_pct_x10;
}

int main(int argc, char **argv)
{
    ReplayOptions options;
    FILE *input_file;
    FILE *log_file;
    FILE *snapshot_file;
    char line[HOST_SIM_MAX_LINE];
    int line_index = 0;
    int step = 0;
    int failures = 0;
    uint32_t repeat_index;
    uint32_t cycle_tick_offset = 0U;
    uint32_t last_cycle_tick = 0U;
    ReplaySummary summary;
    SocSimConfig config;
    SocSimState state;
    SocSimPersistedState persisted;
    AppSimInputSnapshot input;
    SocReplayRow row;
    AppSimOutputSnapshot output;
    AppSimTraceSnapshot trace;
    char json_line[512];

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
    summary.min_soc_est_pct_x10 = 1000U;

    SocSim_LoadBaselineConfig(&config);
    SocSim_Reset(&config, &state);
    SocSim_SavePersistentState(&state, &persisted);

    for (repeat_index = 0U; repeat_index < options.repeat_count; ++repeat_index)
    {
        rewind(input_file);
        line_index = 0;

        while (fgets(line, sizeof(line), input_file) != NULL)
        {
            int parse_status = HostSim_ParseLine(line, &row);
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

            input = row.input;
            input.tick_ms += cycle_tick_offset;
            last_cycle_tick = input.tick_ms;

            memset(&output, 0, sizeof(output));
            if (row.restore_event != 0U)
            {
                SocSim_RestorePersistentState(&config, &persisted, &state);
                state.last_tick_ms = input.tick_ms;
                output.soc_state_flags |= SOC_SIM_FLAG_POWER_RESTORE | SOC_SIM_FLAG_EEPROM_RESTORED;
            }
            SocSim_Step(&config, &state, &input, &output);
            SocSim_SavePersistentState(&state, &persisted);
            HostSim_UpdateSummary(&summary, &output);

            AppSimSnapshot_Init(&trace);
            trace.cycle = repeat_index + 1U;
            trace.step = (uint32_t)(step + 1);
            trace.input = input;
            trace.output = output;
            AppSimSnapshot_SetText(trace.source, sizeof(trace.source), "soc_replay");
            AppSimSnapshot_SetText(trace.result, sizeof(trace.result), HostSim_ResultName(&output));

            AppSimSnapshot_ToJsonLine(&trace, json_line, sizeof(json_line));
            fputs(json_line, snapshot_file);

            fprintf(log_file,
                    "[soc] cycle=%lu step=%d tick_ms=%lu pack_mv=%u ref_soc=%u est_soc=%u error=%d restore=%u flags=0x%08lX\n",
                    (unsigned long)(repeat_index + 1U),
                    step + 1,
                    (unsigned long)input.tick_ms,
                    input.pack_mv,
                    input.soc_pct_x10,
                    output.soc_est_pct_x10,
                    (int)output.soc_error_pct_x10,
                    (unsigned int)row.restore_event,
                    (unsigned long)output.soc_state_flags);

            if (!options.quiet_console)
            {
                printf("[soc] cycle=%lu step=%d est_soc=%u ref_soc=%u restore=%u flags=0x%08lX\n",
                       (unsigned long)(repeat_index + 1U),
                       step + 1,
                       output.soc_est_pct_x10,
                       input.soc_pct_x10,
                       (unsigned int)row.restore_event,
                       (unsigned long)output.soc_state_flags);
            }

            ++step;
        }

        if (failures != 0)
        {
            break;
        }
        cycle_tick_offset = last_cycle_tick + 1000U;
    }

    fprintf(log_file,
            "[summary] steps=%lu ocv_corrected_steps=%lu clamped_empty_steps=%lu clamped_full_steps=%lu restore_count=%lu "
            "min_soc_est_pct_x10=%u max_soc_est_pct_x10=%u max_abs_error_pct_x10=%ld final_soc_est_pct_x10=%u\n",
            (unsigned long)summary.steps,
            (unsigned long)summary.ocv_corrected_steps,
            (unsigned long)summary.clamped_empty_steps,
            (unsigned long)summary.clamped_full_steps,
            (unsigned long)summary.restore_count,
            summary.min_soc_est_pct_x10,
            summary.max_soc_est_pct_x10,
            (long)summary.max_abs_error_pct_x10,
            summary.final_soc_est_pct_x10);

    fclose(input_file);
    fclose(log_file);
    fclose(snapshot_file);

    return failures == 0 ? 0 : 1;
}
