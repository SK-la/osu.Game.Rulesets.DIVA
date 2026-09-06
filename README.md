# osu!DIVA

A try to recreate Hatsune Miku: Project DIVA as a custom mode for osu!

Compatible with **official osu! lazer** (net8) and forks such as **Ez2Lazer** (net10) — drop the matching DLL into the `rulesets` folder.

## Installation

1. Get the DLL from [Releases](https://github.com/SK-la/osu.Game.Rulesets.DIVA/releases) or build it yourself (see below)
2. Open osu! lazer → Settings → **Open osu! folder**
3. Copy **only** the ruleset DLL into the **rulesets** folder:
   - Official lazer → `osu.Game.Rulesets.Diva.net8.0.dll`（可重命名为 `osu.Game.Rulesets.Diva.dll`）
   - Ez2Lazer → `osu.Game.Rulesets.Diva.net10.0.dll`（可重命名为 `osu.Game.Rulesets.Diva.dll`）
4. Restart the game

Do **not** copy `osu.Game.dll`, `osu.Framework.dll`, or other dependencies into `rulesets/` — the client already loads those.

## Build

### Dependencies

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) **and** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Compile-time NuGet（版本见 [Directory.Packages.props](Directory.Packages.props)）：
  - **net8**：`ppy.osu.Game`、`ppy.osu.Game.Rulesets.Osu`
  - **net10**：`ez2lazer.Game`、`ppy.osu.Game.Rulesets.Osu`
- Runtime: the client's already-loaded `osu.Game` / `osu.Framework` assemblies (via `RulesetStore.AssemblyResolve`)

### Steps

```bash
dotnet publish osu.Game.Rulesets.Diva/osu.Game.Rulesets.Diva.csproj -c Release -f net8.0
dotnet publish osu.Game.Rulesets.Diva/osu.Game.Rulesets.Diva.csproj -c Release -f net10.0
```

Outputs:

- `osu.Game.Rulesets.Diva/bin/Release/net8.0/publish/osu.Game.Rulesets.Diva.dll`
- `osu.Game.Rulesets.Diva/bin/Release/net10.0/publish/osu.Game.Rulesets.Diva.dll`

### Release

- **Tag format**: `yyyy.mdd.0` (e.g. `2026.524.0`, same as osu! lazer releases)
- Push a tag to trigger [release.yml](.github/workflows/release.yml)；同一 Release 上传：
  - `osu.Game.Rulesets.Diva.net8.0.dll`
  - `osu.Game.Rulesets.Diva.net10.0.dll`
- Dependabot 每日检查 NuGet（`ppy-osu` / `ez2lazer-game` / `other-nuget`）；[CI](.github/workflows/ci.yml) 通过后自动 squash 合并到 `master`

## Troubleshooting

| Symptom | Common cause |
|---------|--------------|
| Ruleset missing / load failure | Wrong TFM DLL for the client, or DIVA built against a much newer Game package than the client supports |
| Config errors | Leftover `osu.Game*.dll` in `rulesets/` from an old publish folder copy |
| ReflectionTypeLoadException | Missing `ppy.osu.Framework.SourceGeneration` at build time |
