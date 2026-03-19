#include "low_power_sim.h"

#include <string.h>

void LowPowerSim_LoadBaselineConfig(LowPowerSimConfig *config)
{
    if (config == NULL)
    {
        return;
    }

    config->sleep_vlow_mv = 3100U;
    config->current_chg_threshold_ma = 200U;
    config->current_dsg_threshold_ma = 200U;
    config->sleep_time_normal_ticks = 6U;
    config->sleep_time_vlow_ticks = 4U;
    config->deep_sleep_cell_mv = 2600U;
    config->deep_sleep_delay_ticks = 60U;
}

void LowPowerSim_Reset(LowPowerSimState *state)
{
    if (state == NULL)
    {
        return;
    }

    memset(state, 0, sizeof(*state));
    state->mode = LOW_POWER_MODE_ACTIVE;
}

static void LowPowerSim_ResetQuietCounters(LowPowerSimState *state)
{
    state->normal_quiet_ticks = 0U;
    state->low_v_quiet_ticks = 0U;
}

void LowPowerSim_Step(const LowPowerSimConfig *config,
                      LowPowerSimState *state,
                      const AppSimInputSnapshot *input,
                      AppSimOutputSnapshot *output)
{
    uint32_t chg_ma;
    uint32_t dsg_ma;
    int quiet_enough;

    if ((config == NULL) || (state == NULL) || (input == NULL) || (output == NULL))
    {
        return;
    }

    chg_ma = (uint32_t)((input->charge_current_ma >= 0) ? input->charge_current_ma : 0);
    dsg_ma = (uint32_t)((input->discharge_current_ma >= 0) ? input->discharge_current_ma : 0);
    quiet_enough = (chg_ma <= config->current_chg_threshold_ma) &&
                   (dsg_ma <= config->current_dsg_threshold_ma);

    if (input->ext_wake_event != 0U)
    {
        state->mode = LOW_POWER_MODE_ACTIVE;
        LowPowerSim_ResetQuietCounters(state);
        state->deep_sleep_ticks = 0U;
        output->power_state_flags |= LOW_POWER_FLAG_WAKE_EVENT | LOW_POWER_FLAG_RESET_BY_WAKE;
    }

    if (input->force_sleep_level != 0U)
    {
        output->power_state_flags |= LOW_POWER_FLAG_FORCE_REQUEST | LOW_POWER_FLAG_TO_SLEEP;
        if (input->force_sleep_level >= 3U)
        {
            state->mode = LOW_POWER_MODE_DEEP;
            output->power_state_flags |= LOW_POWER_FLAG_DEEP_SLEEP;
        }
        else if (input->force_sleep_level == 2U)
        {
            state->mode = LOW_POWER_MODE_NORMAL_L3;
        }
        else
        {
            state->mode = LOW_POWER_MODE_NORMAL_L2;
        }
    }
    else if (!quiet_enough)
    {
        state->mode = LOW_POWER_MODE_ACTIVE;
        LowPowerSim_ResetQuietCounters(state);
        state->deep_sleep_ticks = 0U;
        output->power_state_flags |= LOW_POWER_FLAG_RESET_BY_CURRENT;
    }
    else if (input->cell_min_mv < config->sleep_vlow_mv)
    {
        output->power_state_flags |= LOW_POWER_FLAG_ELIGIBLE_LOW_V;
        state->normal_quiet_ticks = 0U;
        ++state->low_v_quiet_ticks;
        if (state->low_v_quiet_ticks > config->sleep_time_vlow_ticks)
        {
            state->mode = LOW_POWER_MODE_NORMAL_L3;
            output->power_state_flags |= LOW_POWER_FLAG_TO_SLEEP;
        }
        else
        {
            state->mode = LOW_POWER_MODE_ACTIVE;
        }
    }
    else
    {
        output->power_state_flags |= LOW_POWER_FLAG_ELIGIBLE_NORMAL;
        state->low_v_quiet_ticks = 0U;
        ++state->normal_quiet_ticks;
        if (state->normal_quiet_ticks > config->sleep_time_normal_ticks)
        {
            state->mode = LOW_POWER_MODE_NORMAL_L2;
            output->power_state_flags |= LOW_POWER_FLAG_TO_SLEEP;
        }
        else
        {
            state->mode = LOW_POWER_MODE_ACTIVE;
        }
    }

    if ((input->cell_min_mv < config->deep_sleep_cell_mv) && (chg_ma == 0U))
    {
        ++state->deep_sleep_ticks;
        if (state->deep_sleep_ticks >= config->deep_sleep_delay_ticks)
        {
            state->mode = LOW_POWER_MODE_DEEP;
            output->power_state_flags |= LOW_POWER_FLAG_DEEP_SLEEP | LOW_POWER_FLAG_TO_SLEEP;
        }
    }
    else if (input->force_sleep_level < 3U)
    {
        state->deep_sleep_ticks = 0U;
    }

    output->power_mode = state->mode;
}
