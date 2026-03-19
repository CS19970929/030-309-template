#ifndef LOW_POWER_SIM_H
#define LOW_POWER_SIM_H

#include <stdint.h>

#include "app_sim_snapshot.h"

enum
{
    LOW_POWER_FLAG_ELIGIBLE_NORMAL = 1U << 0,
    LOW_POWER_FLAG_ELIGIBLE_LOW_V = 1U << 1,
    LOW_POWER_FLAG_TO_SLEEP = 1U << 2,
    LOW_POWER_FLAG_WAKE_EVENT = 1U << 3,
    LOW_POWER_FLAG_DEEP_SLEEP = 1U << 4,
    LOW_POWER_FLAG_FORCE_REQUEST = 1U << 5,
    LOW_POWER_FLAG_RESET_BY_CURRENT = 1U << 6,
    LOW_POWER_FLAG_RESET_BY_WAKE = 1U << 7
};

enum
{
    LOW_POWER_MODE_ACTIVE = 0,
    LOW_POWER_MODE_NORMAL_L2 = 1,
    LOW_POWER_MODE_NORMAL_L3 = 2,
    LOW_POWER_MODE_DEEP = 3
};

typedef struct
{
    uint16_t sleep_vlow_mv;
    uint16_t current_chg_threshold_ma;
    uint16_t current_dsg_threshold_ma;
    uint16_t sleep_time_normal_ticks;
    uint16_t sleep_time_vlow_ticks;
    uint16_t deep_sleep_cell_mv;
    uint16_t deep_sleep_delay_ticks;
} LowPowerSimConfig;

typedef struct
{
    uint16_t normal_quiet_ticks;
    uint16_t low_v_quiet_ticks;
    uint16_t deep_sleep_ticks;
    uint8_t mode;
} LowPowerSimState;

void LowPowerSim_LoadBaselineConfig(LowPowerSimConfig *config);
void LowPowerSim_Reset(LowPowerSimState *state);
void LowPowerSim_Step(const LowPowerSimConfig *config,
                      LowPowerSimState *state,
                      const AppSimInputSnapshot *input,
                      AppSimOutputSnapshot *output);

#endif
