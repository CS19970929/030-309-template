#include "soc_sim.h"

#include <string.h>

static uint32_t SocSim_ClampCapacity(uint32_t value, uint32_t limit)
{
    if (value > limit)
    {
        return limit;
    }
    return value;
}

void SocSim_LoadBaselineConfig(SocSimConfig *config)
{
    if (config == NULL)
    {
        return;
    }

    config->capacity_as_x10 = 60U * 3600U * 10U;
    config->initial_soc_pct_x10 = 800U;
    config->ocv_blend_step_pct_x10 = 15U;
    config->current_threshold_ma = 200U;
    config->transfer_enter_ticks = 3U;
    config->transfer_exit_ticks = 2U;
    config->terminal_charge_near_mv = 54000U;
    config->terminal_charge_full_mv = 54400U;
    config->terminal_discharge_near_mv = 49000U;
    config->terminal_discharge_empty_mv = 48500U;
}

void SocSim_Reset(const SocSimConfig *config, SocSimState *state)
{
    uint32_t initial_capacity;

    if ((config == NULL) || (state == NULL))
    {
        return;
    }

    memset(state, 0, sizeof(*state));
    initial_capacity = (config->capacity_as_x10 * config->initial_soc_pct_x10) / 1000U;
    state->remaining_capacity_as_x10 = initial_capacity;
    state->soc_est_pct_x10 = config->initial_soc_pct_x10;
    state->mode = SOC_SIM_MODE_TRANSFER;
}

void SocSim_SavePersistentState(const SocSimState *state, SocSimPersistedState *persisted)
{
    if ((state == NULL) || (persisted == NULL))
    {
        return;
    }

    persisted->remaining_capacity_as_x10 = state->remaining_capacity_as_x10;
    persisted->soc_est_pct_x10 = state->soc_est_pct_x10;
    persisted->ocv_correction_count = state->ocv_correction_count;
    persisted->clamp_empty_count = state->clamp_empty_count;
    persisted->clamp_full_count = state->clamp_full_count;
    persisted->terminal_correction_count = state->terminal_correction_count;
}

void SocSim_RestorePersistentState(const SocSimConfig *config,
                                   const SocSimPersistedState *persisted,
                                   SocSimState *state)
{
    if ((config == NULL) || (persisted == NULL) || (state == NULL))
    {
        return;
    }

    SocSim_Reset(config, state);
    state->remaining_capacity_as_x10 = SocSim_ClampCapacity(persisted->remaining_capacity_as_x10, config->capacity_as_x10);
    state->soc_est_pct_x10 = persisted->soc_est_pct_x10;
    state->ocv_correction_count = persisted->ocv_correction_count;
    state->clamp_empty_count = persisted->clamp_empty_count;
    state->clamp_full_count = persisted->clamp_full_count;
    state->terminal_correction_count = persisted->terminal_correction_count;
    state->mode = SOC_SIM_MODE_TRANSFER;
}

static void SocSim_ApplyTerminalCorrection(const SocSimConfig *config,
                                           SocSimState *state,
                                           const AppSimInputSnapshot *input,
                                           AppSimOutputSnapshot *output)
{
    if (state->mode == SOC_SIM_MODE_CHARGE)
    {
        if ((input->pack_mv >= config->terminal_charge_near_mv) &&
            (input->pack_mv < config->terminal_charge_full_mv) &&
            (state->soc_est_pct_x10 < 950U))
        {
            state->soc_est_pct_x10 += 10U;
            output->soc_state_flags |= SOC_SIM_FLAG_TERMINAL_CORRECTED;
        }
        else if ((input->pack_mv >= config->terminal_charge_full_mv) &&
                 (state->soc_est_pct_x10 < 1000U))
        {
            state->soc_est_pct_x10 += (state->soc_est_pct_x10 >= 950U) ? 10U : 20U;
            output->soc_state_flags |= SOC_SIM_FLAG_TERMINAL_CORRECTED;
        }
        if (state->soc_est_pct_x10 > 1000U)
        {
            state->soc_est_pct_x10 = 1000U;
        }
    }
    else if (state->mode == SOC_SIM_MODE_DISCHARGE)
    {
        if ((input->pack_mv <= config->terminal_discharge_near_mv) &&
            (input->pack_mv > config->terminal_discharge_empty_mv) &&
            (state->soc_est_pct_x10 > 50U))
        {
            state->soc_est_pct_x10 -= 10U;
            output->soc_state_flags |= SOC_SIM_FLAG_TERMINAL_CORRECTED;
        }
        else if ((input->pack_mv <= config->terminal_discharge_empty_mv) &&
                 (state->soc_est_pct_x10 > 0U))
        {
            state->soc_est_pct_x10 -= (state->soc_est_pct_x10 <= 50U) ? 10U : 20U;
            output->soc_state_flags |= SOC_SIM_FLAG_TERMINAL_CORRECTED;
        }
    }

    if ((output->soc_state_flags & SOC_SIM_FLAG_TERMINAL_CORRECTED) != 0U)
    {
        state->remaining_capacity_as_x10 = (config->capacity_as_x10 * state->soc_est_pct_x10) / 1000U;
        state->remaining_capacity_as_x10 = SocSim_ClampCapacity(state->remaining_capacity_as_x10, config->capacity_as_x10);
        ++state->terminal_correction_count;
    }
}

void SocSim_Step(const SocSimConfig *config,
                 SocSimState *state,
                 const AppSimInputSnapshot *input,
                 AppSimOutputSnapshot *output)
{
    uint32_t delta_ms;
    int32_t net_current_ma;
    uint32_t abs_current_ma;
    int32_t delta_capacity_as_x10;
    int32_t next_capacity_as_x10;
    uint16_t ocv_soc_pct_x10;

    if ((config == NULL) || (state == NULL) || (input == NULL) || (output == NULL))
    {
        return;
    }

    delta_ms = 0U;
    if (state->last_tick_ms != 0U && input->tick_ms > state->last_tick_ms)
    {
        delta_ms = input->tick_ms - state->last_tick_ms;
    }
    state->last_tick_ms = input->tick_ms;

    net_current_ma = input->charge_current_ma - input->discharge_current_ma;
    abs_current_ma = (uint32_t)((net_current_ma >= 0) ? net_current_ma : -net_current_ma);

    if (delta_ms > 0U)
    {
        delta_capacity_as_x10 = (int32_t)(((int64_t)net_current_ma * (int64_t)delta_ms) / 100U);
        next_capacity_as_x10 = (int32_t)state->remaining_capacity_as_x10 + delta_capacity_as_x10;
        if (next_capacity_as_x10 < 0)
        {
            next_capacity_as_x10 = 0;
            ++state->clamp_empty_count;
            output->soc_state_flags |= SOC_SIM_FLAG_CLAMPED_EMPTY;
        }
        if ((uint32_t)next_capacity_as_x10 > config->capacity_as_x10)
        {
            next_capacity_as_x10 = (int32_t)config->capacity_as_x10;
            ++state->clamp_full_count;
            output->soc_state_flags |= SOC_SIM_FLAG_CLAMPED_FULL;
        }
        state->remaining_capacity_as_x10 = (uint32_t)next_capacity_as_x10;
    }

    state->soc_est_pct_x10 = (uint16_t)((state->remaining_capacity_as_x10 * 1000U) / config->capacity_as_x10);

    if (net_current_ma > 0)
    {
        output->soc_state_flags |= SOC_SIM_FLAG_CHARGE;
    }
    else if (net_current_ma < 0)
    {
        output->soc_state_flags |= SOC_SIM_FLAG_DISCHARGE;
    }

    if ((uint32_t)input->charge_current_ma >= config->current_threshold_ma)
    {
        ++state->mode_state_charge_ticks;
        state->mode_state_discharge_ticks = 0U;
        state->mode_state_transfer_ticks = 0U;
        if (state->mode_state_charge_ticks >= config->transfer_enter_ticks)
        {
            state->mode_state_charge_ticks = 0U;
            state->mode = SOC_SIM_MODE_CHARGE;
        }
    }
    else if ((uint32_t)input->discharge_current_ma >= config->current_threshold_ma)
    {
        ++state->mode_state_discharge_ticks;
        state->mode_state_charge_ticks = 0U;
        state->mode_state_transfer_ticks = 0U;
        if (state->mode_state_discharge_ticks >= config->transfer_enter_ticks)
        {
            state->mode_state_discharge_ticks = 0U;
            state->mode = SOC_SIM_MODE_DISCHARGE;
        }
    }
    else
    {
        ++state->mode_state_transfer_ticks;
        state->mode_state_charge_ticks = 0U;
        state->mode_state_discharge_ticks = 0U;
        if (state->mode_state_transfer_ticks >= config->transfer_exit_ticks)
        {
            state->mode_state_transfer_ticks = 0U;
            state->mode = SOC_SIM_MODE_TRANSFER;
        }
    }

    SocSim_ApplyTerminalCorrection(config, state, input, output);

    if ((state->mode == SOC_SIM_MODE_TRANSFER) &&
        (abs_current_ma <= config->current_threshold_ma))
    {
        ++state->idle_ticks;
    }
    else
    {
        state->idle_ticks = 0U;
    }

    ocv_soc_pct_x10 = input->soc_pct_x10;
    if ((ocv_soc_pct_x10 <= 1000U) &&
        (state->idle_ticks >= config->transfer_enter_ticks))
    {
        int32_t diff = (int32_t)ocv_soc_pct_x10 - (int32_t)state->soc_est_pct_x10;
        if (diff != 0)
        {
            int32_t step = (diff > 0) ? (int32_t)config->ocv_blend_step_pct_x10 : -(int32_t)config->ocv_blend_step_pct_x10;
            if ((diff > 0 && step > diff) || (diff < 0 && step < diff))
            {
                step = diff;
            }
            state->soc_est_pct_x10 = (uint16_t)((int32_t)state->soc_est_pct_x10 + step);
            state->remaining_capacity_as_x10 = (config->capacity_as_x10 * state->soc_est_pct_x10) / 1000U;
            state->remaining_capacity_as_x10 = SocSim_ClampCapacity(state->remaining_capacity_as_x10, config->capacity_as_x10);
            ++state->ocv_correction_count;
            output->soc_state_flags |= SOC_SIM_FLAG_OCV_CORRECTED;
        }
    }

    output->soc_est_pct_x10 = state->soc_est_pct_x10;
    output->soc_ocv_pct_x10 = ocv_soc_pct_x10;
    output->soc_error_pct_x10 = (int16_t)((int32_t)state->soc_est_pct_x10 - (int32_t)ocv_soc_pct_x10);
}
