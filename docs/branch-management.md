# 分支管理速览

> 本文是速查版：怎么用。分支模型与 AuthHub 保持一致（Git Flow），仅按本仓库现实做两处调整：主干沿用 `master`（CI 与徽章均基于它，不改名）；`v*` tag 推送后由 CI 自动构建产物并挂到 GitHub Release，无需手工建 Release。

## 分支模型（Git Flow）

| 分支 | 从哪开 | 用途 | 归宿 |
|---|---|---|---|
| `master` | — | **生产就绪**，任何 commit 可发布，每次合入打 tag | 永久 |
| `develop` | 初始化自 `master` | **集成主干**，跑全量 CI | 永久 |
| `feature/*` `fix/*` `chore/*` `refactor/*` `test/*` `docs/*` | `develop` | 单一职责的短命分支 | 合并即删 |
| `release/x.y.z` | `develop` | **发版冻结**：只修 bug | 双合后删除 |
| `hotfix/x.y.z` | **`master`** | **线上救火** | 双合后删除 |

命名：辅助分支用 `<type>/<短横线描述>`（全小写）；`release` / `hotfix` 用语义化版本号、不带 `v`——`v` 只出现在 tag 上。

## 全景图

```mermaid
---
config:
  gitGraph:
    mainBranchName: master
---
gitGraph
    commit id: "init"
    branch "develop"
    commit id: "bootstrap"
    branch "feature/i18n"
    commit id: "feat: Demo.I18n.Api"
    commit id: "feat: 数据国际化"
    checkout "develop"
    branch "feature/configuration"
    commit id: "feat: Options 校验"
    commit id: "feat: 环境分层加载"
    checkout "develop"
    merge "feature/i18n" id: "merge i18n"
    branch "chore/bump-actions"
    commit id: "chore: runner 固定 24.04"
    checkout "develop"
    merge "chore/bump-actions" id: "merge chore"
    merge "feature/configuration" id: "merge configuration"
    branch "release/1.1.0"
    commit id: "chore: 冻结修 bug"
    checkout "develop"
    branch "feature/etag"
    commit id: "feat: Demo.ETag.Api"
    checkout "develop"
    merge "feature/etag" id: "merge etag"
    checkout "master"
    merge "release/1.1.0" id: "merge release → master" tag: "v1.1.0"
    checkout "develop"
    merge "release/1.1.0" id: "merge release → develop"
    checkout "master"
    branch "hotfix/1.1.1"
    commit id: "fix: 端口冲突"
    checkout "develop"
    branch "feature/short-url"
    commit id: "feat: Demo.ShortUrl.Api"
    checkout "master"
    merge "hotfix/1.1.1" id: "merge hotfix → master" tag: "v1.1.1"
    checkout "develop"
    merge "hotfix/1.1.1" id: "merge hotfix → develop"
    merge "feature/short-url" id: "merge short-url"
```

图上可以看出多任务并行的三条规则：

- **`develop` 是集散地**：任意多个 `feature/*`、`fix/*`、`chore/*` 同时开出、各自独立合入，互不等待；
- **冻结只冻结 `release` 分支**：冻结期 `develop` 上照常并行开发（图中 `feature/etag`），它不属于本版本，也不会进 `release`；
- **救火与日常并行**：`hotfix` 基于 `master` 进行时，`develop` 上的开发（图中 `feature/short-url`）不受影响，靠双合把修复带回集成主干。

## 怎么用

### 1. 开发一个功能

```bash
git switch develop && git pull --ff-only
git switch -c feature/<短横线描述>
# …开发；提交信息遵循 Conventional Commits（feat/fix/chore…）
# PR → develop，合并方式 Merge commit，CI 绿后合并，分支自动删除
```

### 2. 发布一个版本

```bash
# 1) develop CI 全绿后，切出 release 分支进入冻结
git switch develop && git pull --ff-only
git switch -c release/1.1.0 && git push -u origin release/1.1.0
#    冻结期只做一件事：修 bug

# 2) PR release/1.1.0 → master（Merge commit）

# 3) 打 tag 前核对，然后打 annotated tag
git switch master && git pull --ff-only
git rev-parse HEAD            # 必须等于下一行输出，否则停下排查
git rev-parse origin/master
git tag -a v1.1.0 -m "v1.1.0: <一句话说明>" && git push origin v1.1.0
#    push tag 后 CI 自动发布全部 Api 项目并把 zip 挂到该 GitHub Release（release.yml），无需手工操作

# 4) PR release/1.1.0 → develop（把修复带回集成主干）
# 5) 删除 release 分支
```

### 3. 线上救火（hotfix）

```bash
git switch master && git pull --ff-only       # 必须基于 master，修的才是线上那份
git switch -c hotfix/1.1.1
# 修复；PR → master（Merge commit），核对后打 v1.1.1（同发布第 3 步）
# 双合：PR hotfix/1.1.1 → develop；若此刻存在 release 分支，也要把它合进 release
```

## 平台保护（Rulesets）

| 目标 | 规则 |
|---|---|
| `master` | 禁删除、禁 force push、仅限 PR 合入；必需检查：构建与测试 |
| `develop` | 同上；必需检查：构建与测试 |
| `v*` tag | 禁删除、禁改写 |

## 红线

- 所有合并一律 **Merge commit，不用 squash**
- `v*` tag 不可删、不可移动（ruleset 强制）
- `release` 冻结期不合新功能
- `hotfix` 必须双合 `master` + `develop`，否则下个 release 会把 bug 带回线上
- 发版后不开 `master → develop` 回灌 PR——「master 领先 develop」是 Git Flow 的正常形态
