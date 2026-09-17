# osu!DIVA

A try to recreate Hatsune Miku: Project DIVA as a custom mode for osu!

兼容 **Ez2Lazer** 与 **官方 osu! lazer**——两者都已是 net10，按客户端选对应 DLL 放进 `rulesets` 文件夹即可。

## Installation

1. Get the DLL from [Releases](https://github.com/SK-la/osu.Game.Rulesets.DIVA/releases) or build it yourself (see below)
2. Open the client → Settings → **Open osu! folder**
3. Pick the DLL for your client and copy **only that one** into the **rulesets** folder:
   - Ez2Lazer → `osu.Game.Rulesets.Diva.Ez2Lazer.dll`
   - 官方 lazer → `osu.Game.Rulesets.Diva.Lazer.dll`
   - 文件名不影响加载，不用改名（客户端只按 `osu.Game.Rulesets.*.dll` 扫目录，再认程序集名）
4. Restart the game

Do **not** copy both variants in at once — 两者程序集名相同，会各自注册一次 DIVA 规则集而冲突。也不要拷贝 `osu.Game.dll`、`osu.Framework.dll` 等依赖进 `rulesets/`，客户端已自带。

## Variants

两个变体同为 `net10.0`，差别只在编译期引用的 Game 包，用 Configuration 切换：

| 变体 | Configuration | 编译期 Game 包 | 产物 DLL |
|---|---|---|---|
| Ez2Lazer | `Debug` / `Release` / `VisualTests` | `ez2lazer.Game` | `osu.Game.Rulesets.Diva.dll`（Release 资产里叫 `…Diva.Ez2Lazer.dll`） |
| 官方 lazer | `DebugLazer` / `ReleaseLazer` | `ppy.osu.Game` | `osu.Game.Rulesets.Diva.dll`（Release 资产里叫 `…Diva.Lazer.dll`） |

两者程序集名都是 `osu.Game.Rulesets.Diva`（`<AssemblyName>` 固定），所以本地 publish 产物同名、只是落在不同 `bin/<Configuration>/`；Release 资产靠文件名后缀区分变体，`rulesets/` 目录里**只能放一个**。

官方变体缺少 Ez2Lazer 专有 API（`osu.Game.Beatmaps.ExternalLibraries`、`BeatmapSetHostingKind.External`），因此该变体下：

- 外链目录同步不可用，目录只能整体导入 Realm（`ImportToRealm` 被强制为开启且不可关闭）

代码中以 `DIVA_EZ2LAZER` 符号区分（非 `*Lazer` 配置定义），相关分支见 `DivaExternalLibrarySynchronizer`、`DivaSettingsSubsection` 等。

## Build

### Dependencies

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Compile-time NuGet（版本见 [Directory.Packages.props](Directory.Packages.props)）：
  - Ez2Lazer：`ez2lazer.Game`、`ppy.osu.Game.Rulesets.Osu`
  - 官方 lazer：`ppy.osu.Game`、`ppy.osu.Game.Rulesets.Osu`
- Runtime: the client's already-loaded `osu.Game` / `osu.Framework` assemblies (via `RulesetStore.AssemblyResolve`)

### Steps

```bash
dotnet publish osu.Game.Rulesets.Diva/osu.Game.Rulesets.Diva.csproj -c Release -f net10.0
dotnet publish osu.Game.Rulesets.Diva/osu.Game.Rulesets.Diva.csproj -c ReleaseLazer -f net10.0
```

Outputs:

- `osu.Game.Rulesets.Diva/bin/Release/net10.0/publish/osu.Game.Rulesets.Diva.dll`
- `osu.Game.Rulesets.Diva/bin/ReleaseLazer/net10.0/publish/osu.Game.Rulesets.Diva.dll`

要一次性发布并拷贝到本机两个客户端，用 [Deploy-Rulesets.ps1](Deploy-Rulesets.ps1)。

### Release

- **Tag format**: `yyyy.mdd.0` (e.g. `2026.524.0`, same as osu! lazer releases)
- Push a tag to trigger [release.yml](.github/workflows/release.yml)；同一 Release 上传：
  - `osu.Game.Rulesets.Diva.Ez2Lazer.dll`（Ez2Lazer）
  - `osu.Game.Rulesets.Diva.Lazer.dll`（官方）
- [CI](.github/workflows/ci.yml) 构建并校验两个变体；Dependabot 每日检查 NuGet（`ppy-osu` / `ez2lazer-game` / `other-nuget`），CI 通过后自动 squash 合并到 `master`

## Troubleshooting

| Symptom | Common cause |
|---------|--------------|
| Ruleset missing / load failure | Wrong variant DLL for the client, or DIVA built against a much newer Game package than the client supports |
| 外链目录同步报 `NotSupportedException` | 装载的是官方变体；换 Ez2Lazer 变体，或改用「整体导入 Realm」 |
| Config errors | Leftover `osu.Game*.dll` in `rulesets/` from an old publish folder copy |
| ReflectionTypeLoadException | Missing `ppy.osu.Framework.SourceGeneration` at build time |
