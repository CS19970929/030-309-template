#ifndef APP_RUNTIME_MONITOR_H
#define APP_RUNTIME_MONITOR_H

#include <stdint.h>

#include "app_state_snapshot.h"

#define APP_RUNTIME_MONITOR_REASON_MAX_LEN 32U
#define APP_RUNTIME_MONITOR_JSON_MAX_LEN 512U

#ifdef APP_RUNTIME_MONITOR_ENABLE
extern AppStateSnapshot g_app_runtime_monitor_snapshot;
extern char g_app_runtime_monitor_json[APP_RUNTIME_MONITOR_JSON_MAX_LEN];
extern char g_app_runtime_monitor_reason[APP_RUNTIME_MONITOR_REASON_MAX_LEN];
extern uint32_t g_app_runtime_monitor_update_count;
extern uint32_t g_app_runtime_monitor_loop_count;
extern uint32_t g_app_runtime_monitor_last_status;
extern uint32_t g_app_runtime_monitor_last_fault_flags;
extern int g_app_runtime_monitor_json_length;

void AppRuntimeMonitor_Init(void);
void AppRuntimeMonitor_Tick(void);
void AppRuntimeMonitor_ForceCapture(const char *reason);
void AppRuntimeMonitor_RunTask(void);
#else
static inline void AppRuntimeMonitor_Init(void) {}
static inline void AppRuntimeMonitor_Tick(void) {}
static inline void AppRuntimeMonitor_ForceCapture(const char *reason)
{
    (void)reason;
}
static inline void AppRuntimeMonitor_RunTask(void) {}
#endif

#endif
