#include "app_runtime_monitor.h"

#include <stdio.h>
#include <string.h>

#include "main.h"

#ifndef APP_RUNTIME_MONITOR_LOOP_DIVIDER
#define APP_RUNTIME_MONITOR_LOOP_DIVIDER 128U
#endif

#ifndef APP_RUNTIME_MONITOR_LOG_ENABLE
#define APP_RUNTIME_MONITOR_LOG_ENABLE 0
#endif

#ifndef APP_RUNTIME_MONITOR_ENABLE

void AppRuntimeMonitor_Init(void) {}
void AppRuntimeMonitor_Tick(void) {}
void AppRuntimeMonitor_ForceCapture(const char *reason)
{
    (void)reason;
}
void AppRuntimeMonitor_RunTask(void) {}

#else

extern UINT8 SeriesNum;

char g_app_runtime_monitor_json[APP_RUNTIME_MONITOR_JSON_MAX_LEN];
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
    AppStateSnapshot snapshot;

    AppSnapshot_Init(&snapshot);

    snapshot.cycle = g_app_runtime_monitor_loop_count;
    snapshot.step = g_app_runtime_monitor_update_count + 1U;
    snapshot.series_num = (uint32_t)SeriesNum;
    snapshot.pack_voltage_mv = (uint32_t)g_stCellInfoReport.u16VCellTotle * 10U;
    snapshot.pack_current_ma = AppRuntimeMonitor_EncodeCurrentMa();
    snapshot.temp_max_c_x10 = AppRuntimeMonitor_ConvertTempCx10(g_stCellInfoReport.u16TempMax);
    snapshot.temp_min_c_x10 = AppRuntimeMonitor_ConvertTempCx10(g_stCellInfoReport.u16TempMin);
    snapshot.soc_pct = (uint32_t)g_stCellInfoReport.SocElement.u16Soc;
    snapshot.soh_pct = (uint32_t)g_stCellInfoReport.SocElement.u16Soh;
    snapshot.fault_flags = AppRuntimeMonitor_ReadFaultFlags();
    snapshot.system_status = SystemStatus.all;
    AppSnapshot_SetText(snapshot.source,
                        sizeof(snapshot.source),
                        "mcu_runtime");
    AppSnapshot_SetText(snapshot.result,
                        sizeof(snapshot.result),
                        (reason != NULL) ? reason : "periodic");

    g_app_runtime_monitor_json_length = snprintf(
        g_app_runtime_monitor_json,
        sizeof(g_app_runtime_monitor_json),
        "{\"src\":\"mcu\",\"step\":%lu,\"reason\":\"%s\",\"v\":%lu,\"i\":%ld,"
        "\"tmax\":%ld,\"tmin\":%ld,\"soc\":%lu,\"soh\":%lu,\"fault\":%lu,\"status\":%lu}\n",
        (unsigned long)snapshot.step,
        (reason != NULL) ? reason : "periodic",
        (unsigned long)snapshot.pack_voltage_mv,
        (long)snapshot.pack_current_ma,
        (long)snapshot.temp_max_c_x10,
        (long)snapshot.temp_min_c_x10,
        (unsigned long)snapshot.soc_pct,
        (unsigned long)snapshot.soh_pct,
        (unsigned long)snapshot.fault_flags,
        (unsigned long)snapshot.system_status);
    ++g_app_runtime_monitor_update_count;
    g_app_runtime_monitor_last_status = snapshot.system_status;
    g_app_runtime_monitor_last_fault_flags = snapshot.fault_flags;

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

#endif
