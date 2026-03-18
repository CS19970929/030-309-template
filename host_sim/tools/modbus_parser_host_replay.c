#include <ctype.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "app_log.h"
#include "app_log_runtime.h"
#include "app_state_snapshot.h"
#include "modbus_rtu_parser.h"

#define HOST_SIM_MAX_LINE 512
#define HOST_SIM_MAX_FRAME 256

typedef struct
{
    const char *input_path;
    const char *log_path;
    const char *snapshot_path;
    int repeat_count;
} ReplayOptions;

static int parse_args(int argc, char *argv[], ReplayOptions *options)
{
    int i;

    options->input_path = "host_sim/scenarios/modbus_frames.txt";
    options->log_path = "artifacts/host-sim/modbus-replay.log";
    options->snapshot_path = "artifacts/host-sim/modbus-replay.jsonl";
    options->repeat_count = 1;

    for (i = 1; i < argc; ++i)
    {
        if ((strcmp(argv[i], "--input") == 0) && ((i + 1) < argc))
        {
            options->input_path = argv[++i];
        }
        else if ((strcmp(argv[i], "--log") == 0) && ((i + 1) < argc))
        {
            options->log_path = argv[++i];
        }
        else if ((strcmp(argv[i], "--snapshot") == 0) && ((i + 1) < argc))
        {
            options->snapshot_path = argv[++i];
        }
        else if ((strcmp(argv[i], "--repeat") == 0) && ((i + 1) < argc))
        {
            options->repeat_count = atoi(argv[++i]);
            if (options->repeat_count <= 0)
            {
                options->repeat_count = 1;
            }
        }
        else
        {
            fprintf(stderr,
                    "Usage: %s [--input path] [--log path] [--snapshot path] [--repeat n]\n",
                    argv[0]);
            return 0;
        }
    }

    return 1;
}

static char *trim_left(char *text)
{
    while ((*text != '\0') && isspace((unsigned char)*text))
    {
        ++text;
    }
    return text;
}

static void trim_right(char *text)
{
    size_t len = strlen(text);
    while ((len > 0U) && isspace((unsigned char)text[len - 1U]))
    {
        text[--len] = '\0';
    }
}

static int parse_hex_line(char *line, unsigned char *frame, unsigned short *frame_len)
{
    char *cursor;
    char *endptr;
    unsigned long value;

    *frame_len = 0U;
    cursor = trim_left(line);
    trim_right(cursor);

    if ((cursor[0] == '\0') || (cursor[0] == '#'))
    {
        return 0;
    }

    while (*cursor != '\0')
    {
        if (*frame_len >= HOST_SIM_MAX_FRAME)
        {
            return -1;
        }

        value = strtoul(cursor, &endptr, 16);
        if (cursor == endptr)
        {
            return -1;
        }
        if (value > 0xFFUL)
        {
            return -1;
        }

        frame[*frame_len] = (unsigned char)value;
        *frame_len = (unsigned short)(*frame_len + 1U);
        cursor = trim_left(endptr);
    }

    return 1;
}

static const char *result_name(ProtocolParseResult result)
{
    switch (result)
    {
    case PROTO_PARSE_IN_PROGRESS:
        return "in_progress";
    case PROTO_PARSE_FRAME_READY:
        return "frame_ready";
    case PROTO_PARSE_FRAME_INVALID:
        return "frame_invalid";
    default:
        return "unknown";
    }
}

static void write_snapshot(FILE *snapshot,
                           int cycle_index,
                           int line_index,
                           const unsigned char *frame,
                           unsigned short frame_len,
                           const ModbusRtuParser *parser,
                           ProtocolParseResult result)
{
    AppStateSnapshot state;
    char json_line[512];
    int written;

    AppSnapshot_Init(&state);
    state.cycle = (uint32_t)cycle_index;
    state.step = (uint32_t)line_index;
    state.parser_length = parser->length;
    state.expected_length = parser->expected_length;
    state.series_num = 16U;
    state.pack_voltage_mv = (uint32_t)(frame_len * 1000U);
    state.pack_current_ma = (result == PROTO_PARSE_FRAME_READY) ? 2500 : 0;
    state.temp_max_c_x10 = 265;
    state.temp_min_c_x10 = 241;
    state.soc_pct = 60U + (uint32_t)((line_index - 1) % 3);
    state.soh_pct = 98U;
    state.fault_flags = (result == PROTO_PARSE_FRAME_INVALID) ? 1U : 0U;
    state.system_status = (result == PROTO_PARSE_FRAME_READY) ? 0x0000000EU : 0x00000002U;
    AppSnapshot_SetText(state.source, sizeof(state.source), "host_modbus_replay");
    AppSnapshot_SetText(state.result, sizeof(state.result), result_name(result));
    AppSnapshot_SetFrameHex(&state, frame, frame_len);

    written = AppSnapshot_ToJsonLine(&state, json_line, sizeof(json_line));
    if (written > 0)
    {
        fputs(json_line, snapshot);
        fflush(snapshot);
    }
}

static int replay_frame(FILE *snapshot,
                        int cycle_index,
                        int line_index,
                        const unsigned char *frame,
                        unsigned short frame_len)
{
    ModbusRtuParser parser;
    ProtocolParseResult result;
    unsigned short i;

    ModbusRtuParser_Reset(&parser);
    result = PROTO_PARSE_IN_PROGRESS;

    APP_LOG_INFO("cycle=%d line=%d start frame_len=%u", cycle_index, line_index, frame_len);
    for (i = 0U; i < frame_len; ++i)
    {
        result = ModbusRtuParser_ConsumeByte(&parser, frame[i]);
        APP_LOG_DEBUG("cycle=%d line=%d byte_index=%u byte=0x%02X parser_len=%u expected=%u result=%s",
                      cycle_index,
                      line_index,
                      i,
                      frame[i],
                      parser.length,
                      parser.expected_length,
                      result_name(result));
        if (result == PROTO_PARSE_FRAME_INVALID)
        {
            break;
        }
    }

    write_snapshot(snapshot, cycle_index, line_index, frame, frame_len, &parser, result);
    APP_LOG_INFO("cycle=%d line=%d done result=%s", cycle_index, line_index, result_name(result));

    return (result == PROTO_PARSE_FRAME_READY) ? 0 : 1;
}

int main(int argc, char *argv[])
{
    ReplayOptions options;
    FILE *input;
    FILE *snapshot;
    char line[HOST_SIM_MAX_LINE];
    unsigned char frame[HOST_SIM_MAX_FRAME];
    unsigned short frame_len;
    int cycle_index;
    int line_index;
    int parse_status;
    int failures;

    if (!parse_args(argc, argv, &options))
    {
        return EXIT_FAILURE;
    }

    input = fopen(options.input_path, "r");
    if (input == NULL)
    {
        fprintf(stderr, "Failed to open input: %s\n", options.input_path);
        return EXIT_FAILURE;
    }

    snapshot = fopen(options.snapshot_path, "w");
    if (snapshot == NULL)
    {
        AppLogHost_EnsureParentDir(options.snapshot_path);
        snapshot = fopen(options.snapshot_path, "w");
    }
    if (snapshot == NULL)
    {
        fclose(input);
        fprintf(stderr, "Failed to open snapshot output: %s\n", options.snapshot_path);
        return EXIT_FAILURE;
    }

    AppLogHost_Open(options.log_path);
    APP_LOG_INFO("host replay start input=%s snapshot=%s repeat=%d",
                 options.input_path,
                 options.snapshot_path,
                 options.repeat_count);

    failures = 0;
    for (cycle_index = 1; cycle_index <= options.repeat_count; ++cycle_index)
    {
        rewind(input);
        line_index = 0;

        while (fgets(line, (int)sizeof(line), input) != NULL)
        {
            ++line_index;
            parse_status = parse_hex_line(line, frame, &frame_len);
            if (parse_status == 0)
            {
                continue;
            }
            if (parse_status < 0)
            {
                APP_LOG_ERROR("cycle=%d line=%d invalid hex line", cycle_index, line_index);
                ++failures;
                continue;
            }

            failures += replay_frame(snapshot, cycle_index, line_index, frame, frame_len);
        }
    }

    APP_LOG_INFO("host replay done failures=%d", failures);
    AppLogHost_Close();
    fclose(snapshot);
    fclose(input);

    return (failures == 0) ? EXIT_SUCCESS : EXIT_FAILURE;
}
