#include "app_runtime_monitor.h"

#include <string.h>

#include "main.h"

#ifndef APP_RUNTIME_MONITOR_LOOP_DIVIDER
#define APP_RUNTIME_MONITOR_LOOP_DIVIDER 128U
#endif

#ifndef APP_RUNTIME_MONITOR_LOG_ENABLE
#define APP_RUNTIME_MONITOR_LOG_ENABLE 0
#endif

extern UINT8 SeriesNum;

AppStateSnapshot g_app_runtime_monitor_snapshot;
char g_app_runtime_monitor_json[APP_RUNTIME_MONITOR_JSON_MAX_LEN];
char g_app_runtime_monitor_reason[APP_RUNTIME_MONITOR_REASON_MAX_LEN];
uint32_t g_app_runtime_monitor_update_count = 0U;
uint32_t g_app_runtime_monitor_loop_count = 0U;
uint32_t g_app_runtime_monitor_last_status = 0U;
uint32_t g_app_runtime_monitor_last_fault_flags = 0U;
int g_app_runtime_monitor_json_length = 0;

static uint32_t AppRuntimeMonitor_ReadFaultFlags(void)
{
    return (uint32_t)g_stCellInfoReport.unMdlFault_First.all |
           ((uint32_t)g_stCellInfoReport.unMdlFault_Second.all << 16);
}

static int32_t AppRuntimeMonitor_EncodeCurrentMa(void)
{
    if (g_stCellInfoReport.u16Ichg > 0U)
    {
        return (int32_t)g_stCellInfoReport.u16Ichg * 100;
    }

    if (g_stCellInfoReport.u16IDischg > 0U)
    {
        return -((int32_t)g_stCellInfoReport.u16IDischg * 100);
    }

    return 0;
}

static int32_t AppRuntimeMonitor_ConvertTempCx10(uint16_t raw_temp)
{
    if (raw_temp == 0U)
    {
        return 0;
    }

    return (int32_t)raw_temp - 400;
}

static void AppRuntimeMonitor_Capture(const char *reason)
{
    AppSnapshot_Init(&g_app_runtime_monitor_snapshot);

    g_app_runtime_monitor_snapshot.cycle = g_app_runtime_monitor_loop_count;
    g_app_runtime_monitor_snapshot.step = g_app_runtime_monitor_update_count + 1U;
    g_app_runtime_monitor_snapshot.series_num = (uint32_t)SeriesNum;
    g_app_runtime_monitor_snapshot.pack_voltage_mv = (uint32_t)g_stCellInfoReport.u16VCellTotle * 10U;
    g_app_runtime_monitor_snapshot.pack_current_ma = AppRuntimeMonitor_EncodeCurrentMa();
    g_app_runtime_monitor_snapshot.temp_max_c_x10 = AppRuntimeMonitor_ConvertTempCx10(g_stCellInfoReport.u16TempMax);
    g_app_runtime_monitor_snapshot.temp_min_c_x10 = AppRuntimeMonitor_ConvertTempCx10(g_stCellInfoReport.u16TempMin);
    g_app_runtime_monitor_snapshot.soc_pct = (uint32_t)g_stCellInfoReport.SocElement.u16Soc;
    g_app_runtime_monitor_snapshot.soh_pct = (uint32_t)g_stCellInfoReport.SocElement.u16Soh;
    g_app_runtime_monitor_snapshot.fault_flags = AppRuntimeMonitor_ReadFaultFlags();
    g_app_runtime_monitor_snapshot.system_status = SystemStatus.all;
    AppSnapshot_SetText(g_app_runtime_monitor_snapshot.source,
                        sizeof(g_app_runtime_monitor_snapshot.source),
                        "mcu_runtime");
    AppSnapshot_SetText(g_app_runtime_monitor_snapshot.result,
                        sizeof(g_app_runtime_monitor_snapshot.result),
                        (reason != NULL) ? reason : "periodic");

    AppSnapshot_SetText(g_app_runtime_monitor_reason,
                        sizeof(g_app_runtime_monitor_reason),
                        (reason != NULL) ? reason : "periodic");

    g_app_runtime_monitor_json_length =
        AppSnapshot_ToJsonLine(&g_app_runtime_monitor_snapshot,
                               g_app_runtime_monitor_json,
                               sizeof(g_app_runtime_monitor_json));
    ++g_app_runtime_monitor_update_count;
    g_app_runtime_monitor_last_status = g_app_runtime_monitor_snapshot.system_status;
    g_app_runtime_monitor_last_fault_flags = g_app_runtime_monitor_snapshot.fault_flags;

#if APP_RUNTIME_MONITOR_LOG_ENABLE
    if (g_app_runtime_monitor_json_length > 0)
    {
        APP_LOG_INFO("runtime-snapshot %s", g_app_runtime_monitor_json);
    }
#endif
}

void AppRuntimeMonitor_Init(void)
{
    memset(g_app_runtime_monitor_json, 0, sizeof(g_app_runtime_monitor_json));
    memset(g_app_runtime_monitor_reason, 0, sizeof(g_app_runtime_monitor_reason));
    g_app_runtime_monitor_update_count = 0U;
    g_app_runtime_monitor_loop_count = 0U;
    g_app_runtime_monitor_last_status = 0U;
    g_app_runtime_monitor_last_fault_flags = 0U;
    g_app_runtime_monitor_json_length = 0;
    AppRuntimeMonitor_Capture("startup");
}

void AppRuntimeMonitor_ForceCapture(const char *reason)
{
    AppRuntimeMonitor_Capture(reason);
}

void AppRuntimeMonitor_RunTask(void)
{
    AppRuntimeMonitor_Capture("periodic");
}

void AppRuntimeMonitor_Tick(void)
{
    uint32_t current_status;
    uint32_t current_fault_flags;

    ++g_app_runtime_monitor_loop_count;
    current_status = SystemStatus.all;
    current_fault_flags = AppRuntimeMonitor_ReadFaultFlags();

    if ((current_status != g_app_runtime_monitor_last_status) ||
        (current_fault_flags != g_app_runtime_monitor_last_fault_flags))
    {
        AppRuntimeMonitor_Capture("state_change");
        return;
    }

    if ((g_app_runtime_monitor_loop_count % APP_RUNTIME_MONITOR_LOOP_DIVIDER) == 0U)
    {
        AppRuntimeMonitor_Capture("periodic");
    }
}
