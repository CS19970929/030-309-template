## 变更摘要

- 说明本次改动解决了什么问题
- 说明改动范围是 `tooling / build / protocol / protection / soc / sleep / board`

## 风险分级

- [ ] 仅文档或脚本
- [ ] 仅 PC 仿真相关
- [ ] 涉及 MCU 构建
- [ ] 涉及保护逻辑
- [ ] 涉及 `SOC`
- [ ] 涉及低功耗策略
- [ ] 涉及 Flash / 存储 / 升级
- [ ] 涉及量产参数或校准常量

## 本地验证

- [ ] `task build`
- [ ] `task sim-modbus`
- [ ] `task sim-replay`
- [ ] `task sim-protection-suite-summary`
- [ ] `task sim-soc-suite-summary`
- [ ] `task sim-low-power-suite-summary`
- [ ] `task codex-overview`
- [ ] 其他：请写明

## 关键产物

- `artifacts/host-sim/protection-suite-summary.json`
- `artifacts/host-sim/protection-suite-summary.md`
- `artifacts/host-sim/soc-suite-summary.json`
- `artifacts/host-sim/soc-suite-summary.md`
- `artifacts/host-sim/low-power-suite-summary.json`
- `artifacts/host-sim/low-power-suite-summary.md`
- `artifacts/codex-overview.json`
- `artifacts/codex-overview.md`
- `artifacts/map-summary.json`

## 对 Codex 的审查要求

如需 Codex 做第一轮审查，请在评论中添加：

```text
@codex review
```

如果想限制审查范围，建议这样写：

```text
@codex review the protection logic and cross-platform build impact
```

```text
@codex review for flash layout, calibration, and storage risks
```

## 备注

- 未经确认，不直接修改量产参数、保护阈值、Flash 布局、校准常量。
- 涉及安全、存储、升级、保护策略时，优先要求 Codex 审查。
