#include "app_state_snapshot.h"

#include <stdio.h>
#include <string.h>

void AppSnapshot_Init(AppStateSnapshot *snapshot)
{
    if (snapshot == NULL)
    {
        return;
    }

    memset(snapshot, 0, sizeof(*snapshot));
    AppSnapshot_SetText(snapshot->source, sizeof(snapshot->source), "unknown");
    AppSnapshot_SetText(snapshot->result, sizeof(snapshot->result), "unknown");
}

void AppSnapshot_SetText(char *dst, size_t capacity, const char *src)
{
    if ((dst == NULL) || (capacity == 0U))
    {
        return;
    }

    if (src == NULL)
    {
        dst[0] = '\0';
        return;
    }

    strncpy(dst, src, capacity - 1U);
    dst[capacity - 1U] = '\0';
}

void AppSnapshot_SetFrameHex(AppStateSnapshot *snapshot, const uint8_t *frame, uint16_t frame_len)
{
    uint16_t i;
    size_t offset;

    if (snapshot == NULL)
    {
        return;
    }

    snapshot->frame_hex[0] = '\0';
    if ((frame == NULL) || (frame_len == 0U))
    {
        return;
    }

    offset = 0U;
    for (i = 0U; i < frame_len; ++i)
    {
        int written;
        written = snprintf(&snapshot->frame_hex[offset],
                           sizeof(snapshot->frame_hex) - offset,
                           (i == 0U) ? "%02X" : " %02X",
                           frame[i]);
        if ((written <= 0) || ((size_t)written >= (sizeof(snapshot->frame_hex) - offset)))
        {
            break;
        }
        offset += (size_t)written;
    }
}

int AppSnapshot_ToJsonLine(const AppStateSnapshot *snapshot, char *buffer, size_t capacity)
{
    if ((snapshot == NULL) || (buffer == NULL) || (capacity == 0U))
    {
        return 0;
    }

    return snprintf(
        buffer,
        capacity,
        "{\"source\":\"%s\",\"cycle\":%lu,\"step\":%lu,\"result\":\"%s\","
        "\"parser_length\":%lu,\"expected_length\":%lu,\"series_num\":%lu,"
        "\"pack_voltage_mv\":%lu,\"pack_current_ma\":%ld,\"temp_max_c_x10\":%ld,"
        "\"temp_min_c_x10\":%ld,\"soc_pct\":%lu,\"soh_pct\":%lu,"
        "\"fault_flags\":%lu,\"system_status\":%lu,\"frame\":\"%s\"}\n",
        snapshot->source,
        (unsigned long)snapshot->cycle,
        (unsigned long)snapshot->step,
        snapshot->result,
        (unsigned long)snapshot->parser_length,
        (unsigned long)snapshot->expected_length,
        (unsigned long)snapshot->series_num,
        (unsigned long)snapshot->pack_voltage_mv,
        (long)snapshot->pack_current_ma,
        (long)snapshot->temp_max_c_x10,
        (long)snapshot->temp_min_c_x10,
        (unsigned long)snapshot->soc_pct,
        (unsigned long)snapshot->soh_pct,
        (unsigned long)snapshot->fault_flags,
        (unsigned long)snapshot->system_status,
        snapshot->frame_hex);
}
