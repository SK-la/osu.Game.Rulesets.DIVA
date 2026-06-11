# osu!DIVA

A try to recreate Hatsune Miku: Project DIVA as a custom mode for osu!

Compatible with **official osu! lazer** and forks (e.g. Ez2Lazer) — drop the DLL into the `rulesets` folder.

## Installation

1. Get the DLL from [Releases](https://github.com/SK-la/osu.Game.Rulesets.DIVA/releases) or build it yourself (see below)
2. Open osu! lazer → Settings → **Open osu! folder**
3. Copy **only** `osu.Game.Rulesets.Diva.dll` into the **rulesets** folder
4. Restart the game

Do **not** copy `osu.Game.dll`, `osu.Framework.dll`, or other dependencies into `rulesets/` — the client already loads those.

## Build

### Dependencies

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Compile-time NuGet: `ppy.osu.Game`, `ppy.osu.Game.Rulesets.Osu` (versions in [Dependencies.props](Dependencies.props))
- Runtime: the client's already-loaded `osu.Game` / `osu.Framework` assemblies (via `RulesetStore.AssemblyResolve`)

### Steps

```bash
dotnet publish osu.Game.Rulesets.Diva/osu.Game.Rulesets.Diva.csproj -c Release
```

Output: `osu.Game.Rulesets.Diva/bin/Release/net8.0/publish/osu.Game.Rulesets.Diva.dll`

### Release

- **Tag format**: `yyyy.mdd.0` (e.g. `2026.524.0`, same as osu! lazer releases)
- Push a tag to trigger [release.yml](.github/workflows/release.yml)
- [update-deps.yml](.github/workflows/update-deps.yml) periodically bumps `ppy.osu.Game` on nuget.org

## Troubleshooting

| Symptom | Common cause |
|---------|--------------|
| Ruleset missing / load failure | DIVA built against a much newer `ppy.osu.Game` than the client supports |
| Config errors | Leftover `osu.Game*.dll` in `rulesets/` from an old publish folder copy |
| ReflectionTypeLoadException | Missing `ppy.osu.Framework.SourceGeneration` at build time |
