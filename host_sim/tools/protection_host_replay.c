#include "app_sim_snapshot.h"
#include "protection_sim.h"

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
} ReplayOptions;

static void HostSim_PrintUsage(const char *program)
{
    fprintf(stderr,
            "Usage: %s [--input path] [--log path] [--snapshot path] [--quiet-console]\n",
            program);
}

static int HostSim_ParseArgs(int argc, char **argv, ReplayOptions *options)
{
    int i;

    options->input_path = "host_sim/scenarios/protection/protection_basic.csv";
    options->log_path = "artifacts/host-sim/protection-replay.log";
    options->snapshot_path = "artifacts/host-sim/protection-replay.jsonl";
    options->quiet_console = 0;

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
    unsigned int cell_max_mv;
    unsigned int cell_min_mv;
    unsigned int pack_mv;
    int charge_current_ma;
    int discharge_current_ma;
    int temp_chg_max_c_x10;
    int temp_dsg_min_c_x10;
    int temp_mos_c_x10;
    unsigned int charger_present;
    unsigned int load_present;

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
               " %u , %u , %u , %u , %d , %d , %d , %d , %d , %u , %u",
               &tick_ms,
               &cell_max_mv,
               &cell_min_mv,
               &pack_mv,
               &charge_current_ma,
               &discharge_current_ma,
               &temp_chg_max_c_x10,
               &temp_dsg_min_c_x10,
               &temp_mos_c_x10,
               &charger_present,
               &load_present) != 11)
    {
        return -1;
    }

    memset(input, 0, sizeof(*input));
    input->tick_ms = tick_ms;
    input->cell_max_mv = (uint16_t)cell_max_mv;
    input->cell_min_mv = (uint16_t)cell_min_mv;
    input->pack_mv = (uint16_t)pack_mv;
    input->charge_current_ma = charge_current_ma;
    input->discharge_current_ma = discharge_current_ma;
    input->temp_chg_max_c_x10 = (int16_t)temp_chg_max_c_x10;
    input->temp_dsg_min_c_x10 = (int16_t)temp_dsg_min_c_x10;
    input->temp_mos_c_x10 = (int16_t)temp_mos_c_x10;
    input->charger_present = (uint8_t)charger_present;
    input->load_present = (uint8_t)load_present;

    return 1;
}

static const char *HostSim_ResultName(const AppSimOutputSnapshot *output)
{
    if (output->fault_third != 0U)
    {
        return "fault_third";
    }
    if (output->fault_second != 0U)
    {
        return "fault_second";
    }
    if (output->fault_first != 0U)
    {
        return "fault_first";
    }
    return "normal";
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
    ProtectionSimConfig config;
    ProtectionSimState state;
    AppSimInputSnapshot input;
    AppSimOutputSnapshot output;
    AppSimTraceSnapshot trace;
    char json_line[512];

    if (!HostSim_ParseArgs(argc, argv, &options))
    {
        return EXIT_FAILURE;
    }

    input_file = fopen(options.input_path, "r");
    if (input_file == NULL)
    {
        fprintf(stderr, "Failed to open input: %s\n", options.input_path);
        return EXIT_FAILURE;
    }

    HostSim_EnsureParentDir(options.log_path);
    HostSim_EnsureParentDir(options.snapshot_path);

    log_file = fopen(options.log_path, "w");
    snapshot_file = fopen(options.snapshot_path, "w");
    if ((log_file == NULL) || (snapshot_file == NULL))
    {
        fprintf(stderr, "Failed to open output files.\n");
        fclose(input_file);
        if (log_file != NULL)
        {
            fclose(log_file);
        }
        if (snapshot_file != NULL)
        {
            fclose(snapshot_file);
        }
        return EXIT_FAILURE;
    }

    ProtectionSim_LoadBaselineConfig(&config);
    ProtectionSim_Reset(&state);

    while (fgets(line, sizeof(line), input_file) != NULL)
    {
        int parse_result;

        ++line_index;
        parse_result = HostSim_ParseLine(line, &input);
        if (parse_result == 0)
        {
            continue;
        }
        if (parse_result < 0)
        {
            ++failures;
            fprintf(log_file, "line=%d parse_error text=%s", line_index, line);
            continue;
        }

        ++step;
        ProtectionSim_Step(&config, &state, &input, &output);

        AppSimSnapshot_Init(&trace);
        trace.cycle = (uint32_t)step;
        trace.step = (uint32_t)line_index;
        trace.input = input;
        trace.output = output;
        AppSimSnapshot_SetText(trace.source, sizeof(trace.source), "host_protection_replay");
        AppSimSnapshot_SetText(trace.result, sizeof(trace.result), HostSim_ResultName(&output));

        if (AppSimSnapshot_ToJsonLine(&trace, json_line, sizeof(json_line)) > 0)
        {
            fputs(json_line, snapshot_file);
        }

        fprintf(log_file,
                "step=%d tick=%lu result=%s fault1=0x%08lX fault2=0x%08lX fault3=0x%08lX chg_mos=%u dsg_mos=%u\n",
                step,
                (unsigned long)input.tick_ms,
                HostSim_ResultName(&output),
                (unsigned long)output.fault_first,
                (unsigned long)output.fault_second,
                (unsigned long)output.fault_third,
                output.charge_mos_off,
                output.discharge_mos_off);

        if (!options.quiet_console)
        {
            printf("step=%d tick=%lu result=%s fault1=0x%08lX fault2=0x%08lX fault3=0x%08lX\n",
                   step,
                   (unsigned long)input.tick_ms,
                   HostSim_ResultName(&output),
                   (unsigned long)output.fault_first,
                   (unsigned long)output.fault_second,
                   (unsigned long)output.fault_third);
        }
    }

    fprintf(log_file, "summary steps=%d failures=%d\n", step, failures);
    if (!options.quiet_console)
    {
        printf("Protection replay finished: steps=%d failures=%d\n", step, failures);
    }

    fclose(snapshot_file);
    fclose(log_file);
    fclose(input_file);

    return (failures == 0) ? EXIT_SUCCESS : EXIT_FAILURE;
}
