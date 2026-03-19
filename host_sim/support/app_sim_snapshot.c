#include "app_sim_snapshot.h"

#include <stdio.h>
#include <string.h>

void AppSimSnapshot_Init(AppSimTraceSnapshot *snapshot)
{
    if (snapshot == NULL)
    {
        return;
    }

    memset(snapshot, 0, sizeof(*snapshot));
    AppSimSnapshot_SetText(snapshot->source, sizeof(snapshot->source), "unknown");
    AppSimSnapshot_SetText(snapshot->result, sizeof(snapshot->result), "unknown");
}

void AppSimSnapshot_SetText(char *dst, size_t capacity, const char *src)
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

int AppSimSnapshot_ToJsonLine(const AppSimTraceSnapshot *snapshot, char *buffer, size_t capacity)
{
    if ((snapshot == NULL) || (buffer == NULL) || (capacity == 0U))
    {
        return 0;
    }

    return snprintf(
        buffer,
        capacity,
        "{\"source\":\"%s\",\"cycle\":%lu,\"step\":%lu,\"result\":\"%s\","
        "\"tick_ms\":%lu,\"cell_max_mv\":%u,\"cell_min_mv\":%u,\"cell_delta_mv\":%u,"
        "\"pack_mv\":%u,\"soc_pct_x10\":%u,"
        "\"charge_current_ma\":%ld,\"discharge_current_ma\":%ld,"
        "\"temp_chg_max_c_x10\":%d,\"temp_dsg_min_c_x10\":%d,\"temp_mos_c_x10\":%d,"
        "\"charger_present\":%u,\"load_present\":%u,"
        "\"fault_first\":%lu,\"fault_second\":%lu,\"fault_third\":%lu,"
        "\"charge_mos_off\":%u,\"discharge_mos_off\":%u,"
        "\"soc_est_pct_x10\":%u,\"soc_ocv_pct_x10\":%u,"
        "\"soc_error_pct_x10\":%d,\"soc_dsg_cycle_acc_pct_x10\":%u,"
        "\"soc_cycle_times_x100\":%lu,\"soc_state_flags\":%lu}\n",
        snapshot->source,
        (unsigned long)snapshot->cycle,
        (unsigned long)snapshot->step,
        snapshot->result,
        (unsigned long)snapshot->input.tick_ms,
        snapshot->input.cell_max_mv,
        snapshot->input.cell_min_mv,
        snapshot->input.cell_delta_mv,
        snapshot->input.pack_mv,
        snapshot->input.soc_pct_x10,
        (long)snapshot->input.charge_current_ma,
        (long)snapshot->input.discharge_current_ma,
        (int)snapshot->input.temp_chg_max_c_x10,
        (int)snapshot->input.temp_dsg_min_c_x10,
        (int)snapshot->input.temp_mos_c_x10,
        snapshot->input.charger_present,
        snapshot->input.load_present,
        (unsigned long)snapshot->output.fault_first,
        (unsigned long)snapshot->output.fault_second,
        (unsigned long)snapshot->output.fault_third,
        snapshot->output.charge_mos_off,
        snapshot->output.discharge_mos_off,
        snapshot->output.soc_est_pct_x10,
        snapshot->output.soc_ocv_pct_x10,
        (int)snapshot->output.soc_error_pct_x10,
        snapshot->output.soc_dsg_cycle_acc_pct_x10,
        (unsigned long)snapshot->output.soc_cycle_times_x100,
        (unsigned long)snapshot->output.soc_state_flags);
}
