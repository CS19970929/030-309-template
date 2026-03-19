#ifndef SOC_SIM_H
#define SOC_SIM_H

#include <stdint.h>

#include "app_sim_snapshot.h"

enum
{
    SOC_SIM_FLAG_CHARGE = 1U << 0,
    SOC_SIM_FLAG_DISCHARGE = 1U << 1,
    SOC_SIM_FLAG_OCV_CORRECTED = 1U << 2,
    SOC_SIM_FLAG_CLAMPED_EMPTY = 1U << 3,
    SOC_SIM_FLAG_CLAMPED_FULL = 1U << 4
};

typedef struct
{
    uint32_t capacity_as_x10;
    uint16_t initial_soc_pct_x10;
    uint16_t ocv_blend_step_pct_x10;
    uint16_t idle_current_threshold_ma;
    uint16_t idle_blend_delay_ticks;
} SocSimConfig;

typedef struct
{
    uint32_t remaining_capacity_as_x10;
    uint16_t soc_est_pct_x10;
    uint32_t last_tick_ms;
    uint16_t idle_ticks;
    uint32_t ocv_correction_count;
    uint32_t clamp_empty_count;
    uint32_t clamp_full_count;
} SocSimState;

void SocSim_LoadBaselineConfig(SocSimConfig *config);
void SocSim_Reset(const SocSimConfig *config, SocSimState *state);
void SocSim_Step(const SocSimConfig *config,
                 SocSimState *state,
                 const AppSimInputSnapshot *input,
                 AppSimOutputSnapshot *output);

#endif
