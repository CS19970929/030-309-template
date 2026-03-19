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
    config->idle_current_threshold_ma = 150U;
    config->idle_blend_delay_ticks = 3U;
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

    if (abs_current_ma <= config->idle_current_threshold_ma)
    {
        ++state->idle_ticks;
    }
    else
    {
        state->idle_ticks = 0U;
    }

    ocv_soc_pct_x10 = input->soc_pct_x10;
    if ((ocv_soc_pct_x10 <= 1000U) &&
        (state->idle_ticks >= config->idle_blend_delay_ticks))
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
