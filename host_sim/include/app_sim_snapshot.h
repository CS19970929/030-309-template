#ifndef APP_SIM_SNAPSHOT_H
#define APP_SIM_SNAPSHOT_H

#include <stddef.h>
#include <stdint.h>

#define APP_SIM_TEXT_MAX_LEN 32U

typedef struct
{
    uint32_t tick_ms;
    uint16_t cell_max_mv;
    uint16_t cell_min_mv;
    uint16_t cell_delta_mv;
    uint16_t pack_mv;
    uint16_t soc_pct_x10;
    int32_t charge_current_ma;
    int32_t discharge_current_ma;
    int16_t temp_chg_max_c_x10;
    int16_t temp_dsg_min_c_x10;
    int16_t temp_mos_c_x10;
    uint8_t charger_present;
    uint8_t load_present;
} AppSimInputSnapshot;

typedef struct
{
    uint32_t fault_first;
    uint32_t fault_second;
    uint32_t fault_third;
    uint8_t charge_mos_off;
    uint8_t discharge_mos_off;
    uint16_t soc_est_pct_x10;
    uint16_t soc_ocv_pct_x10;
    int16_t soc_error_pct_x10;
    uint16_t soc_dsg_cycle_acc_pct_x10;
    uint32_t soc_cycle_times_x100;
    uint32_t soc_state_flags;
} AppSimOutputSnapshot;

typedef struct
{
    uint32_t cycle;
    uint32_t step;
    AppSimInputSnapshot input;
    AppSimOutputSnapshot output;
    char source[APP_SIM_TEXT_MAX_LEN];
    char result[APP_SIM_TEXT_MAX_LEN];
} AppSimTraceSnapshot;

void AppSimSnapshot_Init(AppSimTraceSnapshot *snapshot);
void AppSimSnapshot_SetText(char *dst, size_t capacity, const char *src);
int AppSimSnapshot_ToJsonLine(const AppSimTraceSnapshot *snapshot, char *buffer, size_t capacity);

#endif
