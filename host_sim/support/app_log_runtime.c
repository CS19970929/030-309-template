#include "app_log_runtime.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#include <direct.h>
#define HOST_SIM_MKDIR(path) _mkdir(path)
#else
#include <sys/stat.h>
#define HOST_SIM_MKDIR(path) mkdir(path, 0777)
#endif

static FILE *g_app_log_file = NULL;

int AppLogHost_EnsureParentDir(const char *file_path)
{
    char buffer[512];
    size_t len;
    size_t i;

    if (file_path == NULL)
    {
        return 0;
    }

    len = strlen(file_path);
    if ((len == 0U) || (len >= sizeof(buffer)))
    {
        return 0;
    }

    memcpy(buffer, file_path, len + 1U);
    for (i = 0U; i < len; ++i)
    {
        if ((buffer[i] == '\\') || (buffer[i] == '/'))
        {
            char saved = buffer[i];
            buffer[i] = '\0';
            if (strlen(buffer) > 0U)
            {
                HOST_SIM_MKDIR(buffer);
            }
            buffer[i] = saved;
        }
    }

    return 1;
}

void AppLogHost_Open(const char *log_path)
{
    if (g_app_log_file != NULL)
    {
        fclose(g_app_log_file);
        g_app_log_file = NULL;
    }

    if ((log_path == NULL) || (log_path[0] == '\0'))
    {
        return;
    }

    AppLogHost_EnsureParentDir(log_path);
    g_app_log_file = fopen(log_path, "w");
}

void AppLogHost_Close(void)
{
    if (g_app_log_file != NULL)
    {
        fclose(g_app_log_file);
        g_app_log_file = NULL;
    }
}

void AppLogHost_VPrintf(const char *fmt, va_list args)
{
    va_list file_args;

    va_copy(file_args, args);
    vfprintf(stderr, fmt, args);
    fflush(stderr);

    if (g_app_log_file != NULL)
    {
        vfprintf(g_app_log_file, fmt, file_args);
        fflush(g_app_log_file);
    }

    va_end(file_args);
}

void AppLogHost_Printf(const char *fmt, ...)
{
    va_list args;

    va_start(args, fmt);
    AppLogHost_VPrintf(fmt, args);
    va_end(args);
}
