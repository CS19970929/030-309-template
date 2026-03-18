#include "app_state_snapshot.h"
#include "app_log_runtime.h"
#include "Time_Triggered.h"

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

#define APP_HOST_SERIES_NUM 16U
#define APP_HOST_SYSTEM_STATUS_IDLE 0x10U

typedef struct
{
    const char *name;
    uint16_t start_delay;
    uint16_t period;
    uint32_t run_count;
    uint32_t status_tag;
} HostTaskState;

static HostTaskState g_task_states[] = {
    {"App_AFEGet", 0U, 200U, 0U, 0x01U},
    {"App_WarnCtrl", 8U, 10U, 0U, 0x02U},
    {"App_AnlogCal", 2U, 10U, 0U, 0x03U},
    {"App_SOC", 5U, 100U, 0U, 0x04U},
    {"App_LogRecord", 6U, 1000U, 0U, 0x05U},
    {"AppRuntimeMonitor_RunTask", 11U, 1000U, 0U, 0x06U},
};

static AppStateSnapshot g_snapshot;
static char g_json_buffer[512];
static FILE *g_log_file = NULL;
static FILE *g_snapshot_file = NULL;
static uint32_t g_tick_count = 0U;
static uint32_t g_step_count = 0U;
static uint32_t g_pack_voltage_mv = 51200U;
static int32_t g_pack_current_ma = 0;
static int32_t g_temp_max_c_x10 = 255;
static int32_t g_temp_min_c_x10 = 230;
static uint32_t g_soc_pct = 800U;
static uint32_t g_soh_pct = 980U;
static int g_quiet_console = 0;

static void HostSim_ResetScheduler(void)
{
    uint32_t i;

    memset(SCH_task_G, 0, sizeof(SCH_task_G));
    for (i = 0; i < (sizeof(g_task_states) / sizeof(g_task_states[0])); ++i)
    {
        g_task_states[i].run_count = 0U;
    }
}

static void HostSim_WriteLogLine(const char *task_name, uint32_t run_count)
{
    if (!g_quiet_console)
    {
        printf("tick=%lu task=%s run=%lu voltage=%lu current=%ld soc=%lu\n",
               (unsigned long)g_tick_count,
               task_name,
               (unsigned long)run_count,
               (unsigned long)g_pack_voltage_mv,
               (long)g_pack_current_ma,
               (unsigned long)g_soc_pct);
        fflush(stdout);
    }

    if (g_log_file == NULL)
    {
        return;
    }

    fprintf(g_log_file,
            "tick=%lu task=%s run=%lu voltage=%lu current=%ld soc=%lu\n",
            (unsigned long)g_tick_count,
            task_name,
            (unsigned long)run_count,
            (unsigned long)g_pack_voltage_mv,
            (long)g_pack_current_ma,
            (unsigned long)g_soc_pct);
}

static void HostSim_WriteSnapshot(HostTaskState *task_state)
{
    int json_len;

    if (g_snapshot_file == NULL)
    {
        return;
    }

    AppSnapshot_Init(&g_snapshot);
    g_snapshot.cycle = g_tick_count;
    g_snapshot.step = ++g_step_count;
    g_snapshot.series_num = APP_HOST_SERIES_NUM;
    g_snapshot.pack_voltage_mv = g_pack_voltage_mv;
    g_snapshot.pack_current_ma = g_pack_current_ma;
    g_snapshot.temp_max_c_x10 = g_temp_max_c_x10;
    g_snapshot.temp_min_c_x10 = g_temp_min_c_x10;
    g_snapshot.soc_pct = g_soc_pct;
    g_snapshot.soh_pct = g_soh_pct;
    g_snapshot.system_status = APP_HOST_SYSTEM_STATUS_IDLE | task_state->status_tag;
    AppSnapshot_SetText(g_snapshot.source, sizeof(g_snapshot.source), "host_scheduler");
    AppSnapshot_SetText(g_snapshot.result, sizeof(g_snapshot.result), task_state->name);

    json_len = AppSnapshot_ToJsonLine(&g_snapshot, g_json_buffer, sizeof(g_json_buffer));
    if (json_len > 0)
    {
        fputs(g_json_buffer, g_snapshot_file);
    }

    HostSim_WriteLogLine(task_state->name, task_state->run_count);
}

static void HostSim_SimulateMeasurements(uint32_t task_slot)
{
    g_pack_voltage_mv = 51200U + ((g_tick_count + task_slot) % 23U) * 10U;
    g_pack_current_ma = (int32_t)(((g_tick_count / 5U) + task_slot) % 17U) * 100 - 800;
    g_temp_max_c_x10 = 250 + (int32_t)((g_tick_count + task_slot) % 12U);
    g_temp_min_c_x10 = 220 + (int32_t)((g_tick_count + task_slot) % 8U);
    g_soc_pct = 760U + ((g_tick_count / 10U) % 40U);
    g_soh_pct = 980U;
}

static void HostTask_RunSlot0(void)
{
    HostTaskState *task_state = &g_task_states[0];
    ++task_state->run_count;
    HostSim_SimulateMeasurements(0U);
    HostSim_WriteSnapshot(task_state);
}

static void HostTask_RunSlot1(void)
{
    HostTaskState *task_state = &g_task_states[1];
    ++task_state->run_count;
    HostSim_SimulateMeasurements(1U);
    HostSim_WriteSnapshot(task_state);
}

static void HostTask_RunSlot2(void)
{
    HostTaskState *task_state = &g_task_states[2];
    ++task_state->run_count;
    HostSim_SimulateMeasurements(2U);
    HostSim_WriteSnapshot(task_state);
}

static void HostTask_RunSlot3(void)
{
    HostTaskState *task_state = &g_task_states[3];
    ++task_state->run_count;
    HostSim_SimulateMeasurements(3U);
    HostSim_WriteSnapshot(task_state);
}

static void HostTask_RunSlot4(void)
{
    HostTaskState *task_state = &g_task_states[4];
    ++task_state->run_count;
    HostSim_SimulateMeasurements(4U);
    HostSim_WriteSnapshot(task_state);
}

static void HostTask_RunSlot5(void)
{
    HostTaskState *task_state = &g_task_states[5];
    ++task_state->run_count;
    HostSim_SimulateMeasurements(5U);
    HostSim_WriteSnapshot(task_state);
}

static void HostSim_RegisterTasks(void)
{
    SCH_Add_Task(HostTask_RunSlot0, g_task_states[0].start_delay, g_task_states[0].period);
    SCH_Add_Task(HostTask_RunSlot1, g_task_states[1].start_delay, g_task_states[1].period);
    SCH_Add_Task(HostTask_RunSlot2, g_task_states[2].start_delay, g_task_states[2].period);
    SCH_Add_Task(HostTask_RunSlot3, g_task_states[3].start_delay, g_task_states[3].period);
    SCH_Add_Task(HostTask_RunSlot4, g_task_states[4].start_delay, g_task_states[4].period);
    SCH_Add_Task(HostTask_RunSlot5, g_task_states[5].start_delay, g_task_states[5].period);
}

static void HostSim_PrintUsage(const char *program)
{
    fprintf(stderr,
            "Usage: %s --ticks <count> --log <path> --snapshot <path> [--quiet-console]\n",
            program);
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

int main(int argc, char **argv)
{
    int i;
    uint32_t ticks = 1000U;
    const char *log_path = NULL;
    const char *snapshot_path = NULL;

    for (i = 1; i < argc; ++i)
    {
        if ((strcmp(argv[i], "--ticks") == 0) && (i + 1 < argc))
        {
            ticks = (uint32_t)strtoul(argv[++i], NULL, 10);
        }
        else if ((strcmp(argv[i], "--log") == 0) && (i + 1 < argc))
        {
            log_path = argv[++i];
        }
        else if ((strcmp(argv[i], "--snapshot") == 0) && (i + 1 < argc))
        {
            snapshot_path = argv[++i];
        }
        else if (strcmp(argv[i], "--quiet-console") == 0)
        {
            g_quiet_console = 1;
        }
        else
        {
            HostSim_PrintUsage(argv[0]);
            return 1;
        }
    }

    if ((log_path == NULL) || (snapshot_path == NULL))
    {
        HostSim_PrintUsage(argv[0]);
        return 1;
    }

    HostSim_EnsureParentDir(log_path);
    HostSim_EnsureParentDir(snapshot_path);

    g_log_file = fopen(log_path, "w");
    if (g_log_file == NULL)
    {
        fprintf(stderr, "cannot open log file: %s\n", log_path);
        return 2;
    }

    g_snapshot_file = fopen(snapshot_path, "w");
    if (g_snapshot_file == NULL)
    {
        fprintf(stderr, "cannot open snapshot file: %s\n", snapshot_path);
        fclose(g_log_file);
        g_log_file = NULL;
        return 3;
    }

    HostSim_ResetScheduler();
    HostSim_RegisterTasks();

    for (g_tick_count = 0U; g_tick_count < ticks; ++g_tick_count)
    {
        SCH_Update();
        SCH_Dispatch_Tasks();
    }

    fprintf(g_log_file,
            "summary: ticks=%lu afe=%lu warn=%lu analog=%lu soc=%lu log=%lu runtime=%lu\n",
            (unsigned long)ticks,
            (unsigned long)g_task_states[0].run_count,
            (unsigned long)g_task_states[1].run_count,
            (unsigned long)g_task_states[2].run_count,
            (unsigned long)g_task_states[3].run_count,
            (unsigned long)g_task_states[4].run_count,
            (unsigned long)g_task_states[5].run_count);

    fclose(g_snapshot_file);
    fclose(g_log_file);
    g_snapshot_file = NULL;
    g_log_file = NULL;

    printf("Scheduler replay finished: ticks=%lu\n", (unsigned long)ticks);
    return 0;
}
