#include "protection_sim.h"

#include <string.h>

static uint8_t ProtectionSim_EvaluateHigh(const ProtectionHighFaultConfig *config,
                                          ProtectionFaultState *state,
                                          uint16_t value)
{
    if (state->second_active == 0U)
    {
        if (value >= config->second_threshold)
        {
            ++state->second_assert_count;
            if (state->second_assert_count >= config->filter_ticks)
            {
                state->second_active = 1U;
                state->second_assert_count = config->filter_ticks;
                state->second_recover_count = 0U;
            }
        }
        else
        {
            state->second_assert_count = 0U;
        }
    }
    else if (value <= config->first_threshold)
    {
        ++state->second_recover_count;
        if (state->second_recover_count >= config->filter_ticks)
        {
            state->second_active = 0U;
            state->second_recover_count = config->filter_ticks;
        }
    }
    else
    {
        state->second_recover_count = 0U;
    }

    if (state->third_active == 0U)
    {
        if (value >= config->third_threshold)
        {
            ++state->third_assert_count;
            if (state->third_assert_count >= config->filter_ticks)
            {
                state->third_active = 1U;
                state->third_assert_count = config->filter_ticks;
                state->third_recover_count = 0U;
            }
        }
        else
        {
            state->third_assert_count = 0U;
        }
    }
    else if (value <= config->recover_threshold)
    {
        ++state->third_recover_count;
        if (state->third_recover_count >= config->filter_ticks)
        {
            state->third_active = 0U;
            state->third_recover_count = config->filter_ticks;
        }
    }
    else
    {
        state->third_recover_count = 0U;
    }

    if (state->third_active != 0U)
    {
        return 3U;
    }
    if (state->second_active != 0U)
    {
        return 2U;
    }
    return 0U;
}

static uint8_t ProtectionSim_EvaluateLow(const ProtectionLowFaultConfig *config,
                                         ProtectionFaultState *state,
                                         uint16_t value)
{
    if (state->second_active == 0U)
    {
        if (value <= config->second_threshold)
        {
            ++state->second_assert_count;
            if (state->second_assert_count >= config->filter_ticks)
            {
                state->second_active = 1U;
                state->second_assert_count = config->filter_ticks;
                state->second_recover_count = 0U;
            }
        }
        else
        {
            state->second_assert_count = 0U;
        }
    }
    else if (value >= config->first_threshold)
    {
        ++state->second_recover_count;
        if (state->second_recover_count >= config->filter_ticks)
        {
            state->second_active = 0U;
            state->second_recover_count = config->filter_ticks;
        }
    }
    else
    {
        state->second_recover_count = 0U;
    }

    if (state->third_active == 0U)
    {
        if (value <= config->third_threshold)
        {
            ++state->third_assert_count;
            if (state->third_assert_count >= config->filter_ticks)
            {
                state->third_active = 1U;
                state->third_assert_count = config->filter_ticks;
                state->third_recover_count = 0U;
            }
        }
        else
        {
            state->third_assert_count = 0U;
        }
    }
    else if (value >= config->recover_threshold)
    {
        ++state->third_recover_count;
        if (state->third_recover_count >= config->filter_ticks)
        {
            state->third_active = 0U;
            state->third_recover_count = config->filter_ticks;
        }
    }
    else
    {
        state->third_recover_count = 0U;
    }

    if (state->third_active != 0U)
    {
        return 3U;
    }
    if (state->second_active != 0U)
    {
        return 2U;
    }
    return 0U;
}

static void ProtectionSim_ApplySeverity(uint32_t fault_mask,
                                        uint8_t severity,
                                        AppSimOutputSnapshot *output)
{
    if (severity >= 1U)
    {
        output->fault_first |= fault_mask;
    }
    if (severity >= 2U)
    {
        output->fault_second |= fault_mask;
    }
    if (severity >= 3U)
    {
        output->fault_third |= fault_mask;
    }
}

void ProtectionSim_LoadBaselineConfig(ProtectionSimConfig *config)
{
    if (config == NULL)
    {
        return;
    }

    config->cell_ovp = (ProtectionHighFaultConfig){4200U, 4300U, 4400U, 4100U, 3U};
    config->cell_uvp = (ProtectionLowFaultConfig){3000U, 2900U, 2800U, 3150U, 3U};
    config->bat_ovp = (ProtectionHighFaultConfig){59000U, 60000U, 61000U, 58000U, 3U};
    config->bat_uvp = (ProtectionLowFaultConfig){42000U, 41000U, 40000U, 43500U, 3U};
    config->charge_ocp = (ProtectionHighFaultConfig){10000U, 12000U, 15000U, 8000U, 2U};
    config->discharge_ocp = (ProtectionHighFaultConfig){20000U, 25000U, 30000U, 18000U, 2U};
    config->charge_otp = (ProtectionHighFaultConfig){450U, 500U, 550U, 420U, 2U};
    config->discharge_utp = (ProtectionLowFaultConfig){50U, 0U, 0U, 80U, 2U};
    config->mos_otp = (ProtectionHighFaultConfig){800U, 900U, 1000U, 700U, 2U};
    config->vdelta_ovp = (ProtectionHighFaultConfig){100U, 150U, 220U, 80U, 3U};
    config->soc_low = (ProtectionLowFaultConfig){100U, 80U, 50U, 120U, 3U};
}

void ProtectionSim_Reset(ProtectionSimState *state)
{
    if (state == NULL)
    {
        return;
    }

    memset(state, 0, sizeof(*state));
}

void ProtectionSim_Step(const ProtectionSimConfig *config,
                        ProtectionSimState *state,
                        const AppSimInputSnapshot *input,
                        AppSimOutputSnapshot *output)
{
    uint8_t severity;

    if ((config == NULL) || (state == NULL) || (input == NULL) || (output == NULL))
    {
        return;
    }

    memset(output, 0, sizeof(*output));

    severity = ProtectionSim_EvaluateHigh(&config->cell_ovp, &state->cell_ovp, input->cell_max_mv);
    ProtectionSim_ApplySeverity(PROTECTION_FAULT_CELL_OVP, severity, output);

    severity = ProtectionSim_EvaluateLow(&config->cell_uvp, &state->cell_uvp, input->cell_min_mv);
    ProtectionSim_ApplySeverity(PROTECTION_FAULT_CELL_UVP, severity, output);

    severity = ProtectionSim_EvaluateHigh(&config->bat_ovp, &state->bat_ovp, input->pack_mv);
    ProtectionSim_ApplySeverity(PROTECTION_FAULT_BAT_OVP, severity, output);

    severity = ProtectionSim_EvaluateLow(&config->bat_uvp, &state->bat_uvp, input->pack_mv);
    ProtectionSim_ApplySeverity(PROTECTION_FAULT_BAT_UVP, severity, output);

    severity = ProtectionSim_EvaluateHigh(&config->charge_ocp,
                                          &state->charge_ocp,
                                          (uint16_t)((input->charge_current_ma > 0) ? input->charge_current_ma : 0));
    ProtectionSim_ApplySeverity(PROTECTION_FAULT_CHG_OCP, severity, output);

    severity = ProtectionSim_EvaluateHigh(&config->discharge_ocp,
                                          &state->discharge_ocp,
                                          (uint16_t)((input->discharge_current_ma > 0) ? input->discharge_current_ma : 0));
    ProtectionSim_ApplySeverity(PROTECTION_FAULT_DSG_OCP, severity, output);

    severity = ProtectionSim_EvaluateHigh(&config->charge_otp,
                                          &state->charge_otp,
                                          (uint16_t)((input->temp_chg_max_c_x10 > 0) ? input->temp_chg_max_c_x10 : 0));
    ProtectionSim_ApplySeverity(PROTECTION_FAULT_CHG_OTP, severity, output);

    severity = ProtectionSim_EvaluateLow(&config->discharge_utp,
                                         &state->discharge_utp,
                                         (uint16_t)((input->temp_dsg_min_c_x10 > 0) ? input->temp_dsg_min_c_x10 : 0));
    ProtectionSim_ApplySeverity(PROTECTION_FAULT_DSG_UTP, severity, output);

    severity = ProtectionSim_EvaluateHigh(&config->mos_otp,
                                          &state->mos_otp,
                                          (uint16_t)((input->temp_mos_c_x10 > 0) ? input->temp_mos_c_x10 : 0));
    ProtectionSim_ApplySeverity(PROTECTION_FAULT_MOS_OTP, severity, output);

    severity = ProtectionSim_EvaluateHigh(&config->vdelta_ovp, &state->vdelta_ovp, input->cell_delta_mv);
    ProtectionSim_ApplySeverity(PROTECTION_FAULT_VDELTA_OVP, severity, output);

    severity = ProtectionSim_EvaluateLow(&config->soc_low, &state->soc_low, input->soc_pct_x10);
    ProtectionSim_ApplySeverity(PROTECTION_FAULT_SOC_LOW, severity, output);

    output->charge_mos_off = (output->fault_first &
                              (PROTECTION_FAULT_CELL_OVP | PROTECTION_FAULT_BAT_OVP |
                               PROTECTION_FAULT_CHG_OCP | PROTECTION_FAULT_CHG_OTP |
                               PROTECTION_FAULT_MOS_OTP | PROTECTION_FAULT_VDELTA_OVP)) != 0U;
    output->discharge_mos_off = (output->fault_first &
                                 (PROTECTION_FAULT_CELL_UVP | PROTECTION_FAULT_BAT_UVP |
                                  PROTECTION_FAULT_DSG_OCP | PROTECTION_FAULT_DSG_UTP |
                                  PROTECTION_FAULT_MOS_OTP | PROTECTION_FAULT_VDELTA_OVP |
                                  PROTECTION_FAULT_SOC_LOW)) != 0U;
}
