#ifndef APP_LOG_H
#define APP_LOG_H

#include <stdio.h>

#ifndef APP_LOG_LEVEL
#define APP_LOG_LEVEL 3
#endif

#define APP_LOG_LEVEL_ERROR 0
#define APP_LOG_LEVEL_WARN 1
#define APP_LOG_LEVEL_INFO 2
#define APP_LOG_LEVEL_DEBUG 3

#ifdef BMS_HOST_SIM
#define APP_LOG_STREAM stderr
#else
#define APP_LOG_STREAM stdout
#endif

#define APP_LOG_PRINTF(...)                    \
    do                                        \
    {                                         \
        fprintf(APP_LOG_STREAM, __VA_ARGS__); \
        fflush(APP_LOG_STREAM);               \
    } while (0)

#define APP_LOG_ERROR(fmt, ...)                                                         \
    do                                                                                  \
    {                                                                                   \
        if (APP_LOG_LEVEL >= APP_LOG_LEVEL_ERROR)                                       \
        {                                                                               \
            APP_LOG_PRINTF("[ERR] [%s:%s:%d] " fmt "\n", __FILE__, __func__, __LINE__, ##__VA_ARGS__); \
        }                                                                               \
    } while (0)

#define APP_LOG_WARN(fmt, ...)                                                          \
    do                                                                                  \
    {                                                                                   \
        if (APP_LOG_LEVEL >= APP_LOG_LEVEL_WARN)                                        \
        {                                                                               \
            APP_LOG_PRINTF("[WRN] [%s:%s:%d] " fmt "\n", __FILE__, __func__, __LINE__, ##__VA_ARGS__); \
        }                                                                               \
    } while (0)

#define APP_LOG_INFO(fmt, ...)                                                          \
    do                                                                                  \
    {                                                                                   \
        if (APP_LOG_LEVEL >= APP_LOG_LEVEL_INFO)                                        \
        {                                                                               \
            APP_LOG_PRINTF("[INF] [%s:%s:%d] " fmt "\n", __FILE__, __func__, __LINE__, ##__VA_ARGS__); \
        }                                                                               \
    } while (0)

#define APP_LOG_DEBUG(fmt, ...)                                                         \
    do                                                                                  \
    {                                                                                   \
        if (APP_LOG_LEVEL >= APP_LOG_LEVEL_DEBUG)                                       \
        {                                                                               \
            APP_LOG_PRINTF("[DBG] [%s:%s:%d] " fmt "\n", __FILE__, __func__, __LINE__, ##__VA_ARGS__); \
        }                                                                               \
    } while (0)

#endif
