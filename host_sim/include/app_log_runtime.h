#ifndef APP_LOG_RUNTIME_H
#define APP_LOG_RUNTIME_H

#include <stdarg.h>

void AppLogHost_Open(const char *log_path);
void AppLogHost_Close(void);
void AppLogHost_Printf(const char *fmt, ...);
void AppLogHost_VPrintf(const char *fmt, va_list args);
int AppLogHost_EnsureParentDir(const char *file_path);

#endif
