# Git 提交变更记录规范

## 目标

统一仓库内的 `git commit` 记录方式，让 `GitLens`、`git log`、`PR` 审查和后续问题追踪都能直接看到“改了什么、影响什么、怎么验证、如何回退”。

截图里那种“按模块分段展示的变更记录”，本质上来自：

- `git commit` 标题
- `git commit` 正文

只要提交正文写成结构化段落，`GitLens` 在历史视图中就会直接按原文展示。

## 推荐格式

```text
<type>: <简短标题>

变更说明:
- 改了什么
- 为什么要改

影响范围:
- 影响到的模块、脚本、文档或构建入口
- 是否涉及配置、仿真、烧录、产物路径

验证记录:
- 执行过的命令
- 观察到的结果

风险与回退:
- 已知风险；如果没有则写“无新增已知风险”
- 回退方式，例如“revert 当前提交”
```

## 示例

```text
tooling: 统一 Codex 提交变更记录模板

变更说明:
- 新增仓库级 commit template，统一提交正文结构
- 在 AGENTS.md 中约束 Codex 必须补充结构化提交说明

影响范围:
- git 提交流程
- Codex 协作规范
- docs 文档资产

验证记录:
- git config --local commit.template .gitmessage-codex.txt
- git config --get commit.template 返回 .gitmessage-codex.txt

风险与回退:
- 无新增已知风险
- 如需回退，可删除模板配置并回退对应提交
```

## 仓库落地方式

当前仓库采用两层约束：

1. `AGENTS.md`

- 约束 Codex 以后提交时必须写结构化正文。

2. `.gitmessage-codex.txt`

- 作为本仓库的提交模板文件，供 `git commit` 直接复用。

## 配置命令

在当前仓库执行：

```bash
git config --local commit.template .gitmessage-codex.txt
```

配置完成后，可用下面命令确认：

```bash
git config --get commit.template
```

期望输出：

```text
.gitmessage-codex.txt
```

## 使用建议

- 简单提交也保留四段，只是每段内容可以更短。
- 如果涉及多模块，优先按模块或子系统写在“变更说明”里。
- 如果没有执行完整回归，必须在“验证记录”里明确写出未验证项。
- 如果修改了安全、存储、升级、保护策略，必须在“风险与回退”里写清边界与回退方案。
