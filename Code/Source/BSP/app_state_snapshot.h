#ifndef APP_STATE_SNAPSHOT_H
#define APP_STATE_SNAPSHOT_H

#include <stddef.h>
#include <stdint.h>

#define APP_SNAPSHOT_SOURCE_MAX_LEN 32U
#define APP_SNAPSHOT_RESULT_MAX_LEN 32U
#define APP_SNAPSHOT_FRAME_MAX_LEN 192U

typedef struct
{
    uint32_t cycle;
    uint32_t step;
    uint32_t parser_length;
    uint32_t expected_length;
    uint32_t series_num;
    uint32_t pack_voltage_mv;
    int32_t pack_current_ma;
    int32_t temp_max_c_x10;
    int32_t temp_min_c_x10;
    uint32_t soc_pct;
    uint32_t soh_pct;
    uint32_t fault_flags;
    uint32_t system_status;
    char source[APP_SNAPSHOT_SOURCE_MAX_LEN];
    char result[APP_SNAPSHOT_RESULT_MAX_LEN];
    char frame_hex[APP_SNAPSHOT_FRAME_MAX_LEN];
} AppStateSnapshot;

void AppSnapshot_Init(AppStateSnapshot *snapshot);
void AppSnapshot_SetText(char *dst, size_t capacity, const char *src);
void AppSnapshot_SetFrameHex(AppStateSnapshot *snapshot, const uint8_t *frame, uint16_t frame_len);
int AppSnapshot_ToJsonLine(const AppStateSnapshot *snapshot, char *buffer, size_t capacity);

#endif
