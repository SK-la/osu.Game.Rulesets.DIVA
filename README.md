# osu!DIVA

A try to recreate Hatsune Miku: Project DIVA as a custom mode for osu!

Compatible with **official osu! lazer** and forks (e.g. Ez2Lazer) — drop the DLL into the `rulesets` folder.

## Installation

1. Get the dll from:
    - [Releases](https://github.com/Artemis-chan/osu-DIVA/releases)
    - If that does not work, [GitHub Actions](https://nightly.link/Artemis-chan/osu-DIVA/workflows/debug/master/release.zip)
    - or [compile it yourself](#build)
2. Place it in osu! user ruleset folder
    1. Open osu!Lazer
    2. In setting press "Open osu! folder"
    3. Copy the dll into **rulesets** folder
3. Restart osu!

## Build

### Dependencies

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Official NuGet: `ppy.osu.Game`, `ppy.osu.Game.Rulesets.Osu` (versions in [Dependencies.props](Dependencies.props))

### Steps

1. Clone the repo
2. `dotnet publish osu.Game.Rulesets.Diva/osu.Game.Rulesets.Diva.csproj -c Release`
3. Output: `osu.Game.Rulesets.Diva/bin/Release/net8.0/publish/osu.Game.Rulesets.Diva.dll`

### Release

- **Tag format**: `yyyy.mdd.0` (e.g. `2026.524.0`, same as osu! lazer releases)
- Push a tag to trigger [release.yml](.github/workflows/release.yml) — the DLL gets proper assembly version metadata
- [update-deps.yml](.github/workflows/update-deps.yml) **约每 3 天**查询 nuget.org 上 `ppy.osu.Game`、`ppy.osu.Game.Rulesets.Osu` 最新稳定版；有更新则改 [Dependencies.props](Dependencies.props)、开 PR，**无冲突且可合并时自动 squash 合并**（Actions 里可 `workflow_dispatch` 立即跑一次）
