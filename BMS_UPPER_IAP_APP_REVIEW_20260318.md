# 上位机 / IAP / APP 项目梳理与优化建议

## 1. 当前结构梳理

### 1.1 上位机 `BMS upper/CommomUpper_32Series`

- 技术栈：`.NET Framework 4.8` + `WinForms`
- 主入口：`Program.cs`，启动 `Form1`
- 主要职责：
  - 参数读写、状态显示、日志记录
  - 通过串口执行 BMS 协议通信
  - 通过固定寄存器地址触发 IAP 在线升级
- 关键文件职责：
  - `Form1.cs`：主界面、升级流程、部分通信与业务逻辑
  - `SerialPort.cs`：大量协议寄存器、收发、解析相关逻辑
  - `Verify.cs`：参数合法性验证
  - `Log_Version.cs`：CSV 日志与产品信息相关逻辑
  - `Lang.cs`、`Language.ini`：中英文切换

### 1.2 APP `Code/Source`

- 当前主程序是 BMS 运行态业务固件
- `main.c` 负责初始化、任务调度、通信轮询、SOC/日志/休眠等核心功能
- `Comm.c` 已经引入了较新的通信分层：
  - 环形缓冲
  - ASCII / Modbus RTU 双协议解析
  - RS485 收发切换
- `Sci_Upper.c` 仍保留了与上位机升级、寄存器协议强相关的旧接口定义

### 1.3 IAP `Code/Source/iap`

- `iap/main.c` 启动后先检查 `FLASH_ADDR_UPDATE_FLAG`
- 若标志为 `FLASH_TO_APP_VALUE`，则直接跳转 APP
- 否则进入升级通信循环，等待上位机写入升级数据
- 升级协议关键寄存器：
  - `0xFFFD`：连接/进入升级
  - `0xFFFE`：升级数据分块写入
  - `0xFFFF`：升级完成

## 2. 现在的联动关系

整体流程如下：

1. 上位机通过串口发送升级连接命令到 `0xFFFD`
2. APP 侧收到升级请求后，通过 `IapRequest_ArmLegacyFlag()` 写 `FLASH_ADDR_UPDATE_FLAG`
3. APP 复位，IAP 启动
4. IAP 根据 Flash 标志停留在升级态
5. 上位机持续向 `0xFFFE` 分块发送固件
6. 上位机写 `0xFFFF` 通知升级完成
7. IAP 写回 `FLASH_TO_APP_VALUE`，再复位并跳转 APP

这个链路是跑通的，但现在存在“协议分散、状态标志双轨并存、上位机逻辑过度集中”三个核心维护问题。

## 3. 主要问题判断

### 3.1 上位机代码耦合过重

- `Form1.cs` 和 `SerialPort.cs` 体量很大，UI、通信、协议编码、升级流程、数据转换混在一起
- 多处直接 `serialPort1.DiscardInBuffer/DiscardOutBuffer/Write(...)`，发送行为分散，难以统一超时、重试、日志与异常处理
- `CheckForIllegalCrossThreadCalls = false` 说明当前通过关闭跨线程检查规避 UI 线程问题，这会掩盖真实并发缺陷

### 3.2 升级协议缺少统一定义源

- 上位机、APP、IAP 都各自保存了一份升级寄存器/标志定义
- `FLASH_ADDR_UPDATE_FLAG`、`FLASH_TO_APP_VALUE` 等关键常量在 APP 与 IAP 分别维护
- 一旦地址、块大小、握手机制调整，三端容易出现隐性不一致

### 3.3 Boot 标志机制并存，语义不够统一

- 目前同时存在：
  - Flash 升级标志 `FLASH_ADDR_UPDATE_FLAG`
  - RTC Backup 的 `BootFlag_*`
- 其中睡眠/唤醒路径使用 `BootFlag`
- 升级路径仍兼容旧板卡，保留 Flash 标志握手
- 这在兼容期是合理的，但长期看会让“复位后到底进哪里”缺少单一真相源

### 3.4 编码与可维护性风险

- 多个源码文件注释已经出现乱码迹象，说明编码历史不统一
- 这会持续影响后续 diff、审查、跨工具编辑和自动化处理

### 3.5 工程边界不清晰

- 上位机目录里曾混入大量 `bin/obj/.vs/*.csv/*.exe`
- 说明“源码、产物、测试日志、客户定制版本”还没有被明确分层管理

## 4. 优化建议

### 4.1 第一优先级：统一协议定义

建议建立一份“升级协议与寄存器映射主文档”，并逐步把以下内容收敛为单一来源：

- 升级寄存器地址
- 分包大小
- 校验方式
- 升级状态码
- 超时与重试策略
- 版本查询命令

落地方式建议：

- 固件侧：抽出 `iap/app` 共用头文件，例如 `upgrade_proto.h`
- 上位机侧：新增 `UpgradeProtocol.cs`，把 `0xFFFD/0xFFFE/0xFFFF`、包大小、状态码集中定义
- 文档侧：保留一份协议说明，避免口口相传

### 4.2 第二优先级：拆分上位机分层

建议把 `CommomUpper_32Series` 至少拆成 4 层：

- `UI`：窗体与控件事件
- `Transport`：串口打开、发送、接收、超时、重连
- `Protocol`：寄存器封包、解包、校验
- `Service`：升级服务、参数服务、日志服务

最先改的不是全部重构，而是先把“统一发送入口”抽出来，例如：

- `SendCommand(...)`
- `ReadRegisters(...)`
- `WriteRegisters(...)`
- `UpgradeService.Connect/SendChunk/Complete`

这样可以先止住复制粘贴继续扩散。

### 4.3 第三优先级：重做升级状态机

建议把升级从“散落在按钮事件和串口收发里的流程”重构成显式状态机：

- `Idle`
- `ConnectBoot`
- `SwitchToIap`
- `Transfer`
- `Verify`
- `Complete`
- `RebootWait`
- `Done`
- `Failed`

收益：

- 超时、重试、断线恢复更清晰
- 可以记录每一步失败原因
- 便于以后支持版本校验、CRC 校验、断点续传

### 4.4 第四优先级：统一 Boot 决策

建议中期目标是把“进入 APP / 进入 IAP / 休眠唤醒原因”收敛为统一启动决策层。

推荐路线：

- 短期：保留 Flash 升级标志，继续兼容已出货板
- 中期：新增统一 `BootReason` 抽象，内部兼容 `BootFlag` 与旧 Flash 标志
- 长期：新板只保留一套可靠的启动原因机制，旧机制仅做兼容读取

### 4.5 第五优先级：补最小闭环验证能力

建议至少补这几类测试/验证：

- 上位机对升级包分块、末包长度、校验的单元测试
- APP 收到升级请求后的复位与标志写入验证
- IAP 对非法包、超时、半包的容错验证
- 上位机与固件的版本查询、升级前后版本一致性校验

### 4.6 第六优先级：整理仓库与交付物

建议目录边界明确化：

- `BMS upper/CommomUpper_32Series` 只保留源码、资源、工程文件
- 构建产物统一忽略
- 客户定制版 EXE、测试日志、现场 CSV 单独放到发布或归档目录，不进入源码仓库

## 5. 建议执行顺序

### 阶段一：本周可做

- 清理上位机 Git 纳管范围
- 固化升级协议文档
- 提炼上位机统一串口发送接口
- 统一 `iap/app` 的升级常量定义

### 阶段二：下一轮重构

- 把升级流程抽成状态机
- 把上位机协议层从窗体中拆出
- 建版本查询与升级结果校验

### 阶段三：平台化

- 收敛 BootReason 机制
- 建立协议变更流程
- 增加最小自动化测试

## 6. 本次 Git 纳管建议

建议纳入：

- `BMS upper/.gitignore`
- `BMS upper/CommomUpper_32Series/*.cs`
- `BMS upper/CommomUpper_32Series/*.resx`
- `BMS upper/CommomUpper_32Series/*.csproj`
- `BMS upper/CommomUpper_32Series/*.sln`
- `BMS upper/CommomUpper_32Series/app.config`
- `BMS upper/CommomUpper_32Series/Language.ini`
- `BMS upper/CommomUpper_32Series/Readme.txt`
- `BMS upper/CommomUpper_32Series/Properties/*`
- 图标资源文件

建议忽略：

- `.vs/`
- `.vscode/`
- `bin/`
- `obj/`
- `packages/`
- `*.user`
- `*.csv`
- `*.exe`
- `*.dll`
- `*.pdb`
- 各类现场日志与测试产物
