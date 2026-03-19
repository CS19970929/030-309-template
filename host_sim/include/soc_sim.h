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
    SOC_SIM_FLAG_CLAMPED_FULL = 1U << 4,
    SOC_SIM_FLAG_POWER_RESTORE = 1U << 5,
    SOC_SIM_FLAG_EEPROM_RESTORED = 1U << 6,
    SOC_SIM_FLAG_TERMINAL_CORRECTED = 1U << 7
};

enum
{
    SOC_SIM_MODE_TRANSFER = 0,
    SOC_SIM_MODE_CHARGE = 1,
    SOC_SIM_MODE_DISCHARGE = 2
};

typedef struct
{
    uint32_t capacity_as_x10;
    uint16_t initial_soc_pct_x10;
    uint16_t ocv_blend_step_pct_x10;
    uint16_t current_threshold_ma;
    uint16_t transfer_enter_ticks;
    uint16_t transfer_exit_ticks;
    uint16_t terminal_charge_near_mv;
    uint16_t terminal_charge_full_mv;
    uint16_t terminal_discharge_near_mv;
    uint16_t terminal_discharge_empty_mv;
} SocSimConfig;

typedef struct
{
    uint32_t remaining_capacity_as_x10;
    uint16_t soc_est_pct_x10;
    uint32_t ocv_correction_count;
    uint32_t clamp_empty_count;
    uint32_t clamp_full_count;
    uint32_t terminal_correction_count;
} SocSimPersistedState;

typedef struct
{
    uint32_t remaining_capacity_as_x10;
    uint16_t soc_est_pct_x10;
    uint32_t last_tick_ms;
    uint16_t idle_ticks;
    uint16_t mode_state_charge_ticks;
    uint16_t mode_state_discharge_ticks;
    uint16_t mode_state_transfer_ticks;
    uint8_t mode;
    uint32_t ocv_correction_count;
    uint32_t clamp_empty_count;
    uint32_t clamp_full_count;
    uint32_t terminal_correction_count;
} SocSimState;

void SocSim_LoadBaselineConfig(SocSimConfig *config);
void SocSim_Reset(const SocSimConfig *config, SocSimState *state);
void SocSim_SavePersistentState(const SocSimState *state, SocSimPersistedState *persisted);
void SocSim_RestorePersistentState(const SocSimConfig *config,
                                   const SocSimPersistedState *persisted,
                                   SocSimState *state);
void SocSim_Step(const SocSimConfig *config,
                 SocSimState *state,
                 const AppSimInputSnapshot *input,
                 AppSimOutputSnapshot *output);

#endif
