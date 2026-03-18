#ifndef PROTECTION_SIM_H
#define PROTECTION_SIM_H

#include <stdint.h>

#include "app_sim_snapshot.h"

enum
{
    PROTECTION_FAULT_CELL_OVP = 1U << 0,
    PROTECTION_FAULT_CELL_UVP = 1U << 1,
    PROTECTION_FAULT_CHG_OCP = 1U << 2,
    PROTECTION_FAULT_DSG_OCP = 1U << 3,
    PROTECTION_FAULT_CHG_OTP = 1U << 4,
    PROTECTION_FAULT_DSG_UTP = 1U << 5,
    PROTECTION_FAULT_MOS_OTP = 1U << 6,
    PROTECTION_FAULT_BAT_OVP = 1U << 7,
    PROTECTION_FAULT_BAT_UVP = 1U << 8,
    PROTECTION_FAULT_VDELTA_OVP = 1U << 9,
    PROTECTION_FAULT_SOC_LOW = 1U << 10
};

typedef struct
{
    uint16_t first_threshold;
    uint16_t second_threshold;
    uint16_t third_threshold;
    uint16_t recover_threshold;
    uint16_t filter_ticks;
} ProtectionHighFaultConfig;

typedef struct
{
    uint16_t first_threshold;
    uint16_t second_threshold;
    uint16_t third_threshold;
    uint16_t recover_threshold;
    uint16_t filter_ticks;
} ProtectionLowFaultConfig;

typedef struct
{
    ProtectionHighFaultConfig cell_ovp;
    ProtectionLowFaultConfig cell_uvp;
    ProtectionHighFaultConfig bat_ovp;
    ProtectionLowFaultConfig bat_uvp;
    ProtectionHighFaultConfig charge_ocp;
    ProtectionHighFaultConfig discharge_ocp;
    ProtectionHighFaultConfig charge_otp;
    ProtectionLowFaultConfig discharge_utp;
    ProtectionHighFaultConfig mos_otp;
    ProtectionHighFaultConfig vdelta_ovp;
    ProtectionLowFaultConfig soc_low;
} ProtectionSimConfig;

typedef struct
{
    uint8_t second_active;
    uint8_t third_active;
    uint16_t second_assert_count;
    uint16_t second_recover_count;
    uint16_t third_assert_count;
    uint16_t third_recover_count;
} ProtectionFaultState;

typedef struct
{
    ProtectionFaultState cell_ovp;
    ProtectionFaultState cell_uvp;
    ProtectionFaultState bat_ovp;
    ProtectionFaultState bat_uvp;
    ProtectionFaultState charge_ocp;
    ProtectionFaultState discharge_ocp;
    ProtectionFaultState charge_otp;
    ProtectionFaultState discharge_utp;
    ProtectionFaultState mos_otp;
    ProtectionFaultState vdelta_ovp;
    ProtectionFaultState soc_low;
} ProtectionSimState;

void ProtectionSim_LoadBaselineConfig(ProtectionSimConfig *config);
void ProtectionSim_Reset(ProtectionSimState *state);
void ProtectionSim_Step(const ProtectionSimConfig *config,
                        ProtectionSimState *state,
                        const AppSimInputSnapshot *input,
                        AppSimOutputSnapshot *output);

#endif
