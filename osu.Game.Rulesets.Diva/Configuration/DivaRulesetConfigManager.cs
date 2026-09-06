// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Text.Json;
using osu.Game.Configuration;
using osu.Game.Rulesets.Configuration;

namespace osu.Game.Rulesets.Diva.Configuration
{
    public partial class DivaRulesetConfigManager : RulesetConfigManager<DivaRulesetSettings>
    {
        public DivaRulesetConfigManager(SettingsStore? settings, RulesetInfo ruleset, int? variant = null)
            : base(settings, ruleset, variant)
        {
        }

        public static IReadOnlyList<string> ParseLibraryPaths(string? rawPaths, string? legacyRootPath = null)
        {
            var paths = new List<string>();

            if (!string.IsNullOrWhiteSpace(rawPaths))
            {
                string trimmed = rawPaths.Trim();

                try
                {
                    if (trimmed.StartsWith('['))
                    {
                        var deserialised = JsonSerializer.Deserialize<List<string>>(trimmed);

                        if (deserialised != null)
                            paths.AddRange(deserialised);
                    }
                    else
                        paths.Add(trimmed);
                }
                catch (JsonException)
                {
                    paths.Add(trimmed);
                }
            }

            if (paths.Count == 0 && !string.IsNullOrWhiteSpace(legacyRootPath))
                paths.Add(legacyRootPath);

            return normalisePaths(paths);
        }

        public static string SerialiseLibraryPaths(IEnumerable<string> paths) => JsonSerializer.Serialize(normalisePaths(paths));

        public IReadOnlyList<string> GetLibraryPaths() => ParseLibraryPaths(Get<string>(DivaRulesetSettings.DivaLibraryPaths), Get<string>(DivaRulesetSettings.DivaRootPath));

        public void PersistLibraryPaths(IReadOnlyList<string> paths)
        {
            IReadOnlyList<string> normalised = ParseLibraryPaths(SerialiseLibraryPaths(paths));
            SetValue(DivaRulesetSettings.DivaLibraryPaths, SerialiseLibraryPaths(normalised));
            SetValue(DivaRulesetSettings.DivaRootPath, normalised.Count > 0 ? normalised[0] : string.Empty);
        }

        public bool GetImportToRealm()
        {
#if NET8_0
            return true;
#else
            return Get<bool>(DivaRulesetSettings.ImportToRealm);
#endif
        }

        public void PersistImportToRealm(bool value)
        {
#if NET8_0
            SetValue(DivaRulesetSettings.ImportToRealm, true);
#else
            SetValue(DivaRulesetSettings.ImportToRealm, value);
#endif
        }

        protected override void InitialiseDefaults()
        {
            base.InitialiseDefaults();

            SetDefault(DivaRulesetSettings.UseXBoxButtons, false);
            SetDefault(DivaRulesetSettings.EnableVisualBursts, true);
            SetDefault(DivaRulesetSettings.EnableBuiltinHitSounds, true);
            SetDefault(DivaRulesetSettings.NoteSize, 40.0, 10.0, 64.0, 1.0);
            // Multiplier on PD note_standing×MsPerFrame(BPM); 1.0 = ProjectDIVA baseline.
            SetDefault(DivaRulesetSettings.ApproachPreemptScale, 1.0, 0.75, 1.25, 0.05);
            SetDefault(DivaRulesetSettings.HitExplosionAlpha, 1.0, 0.0, 1.0, 0.05);
            // PD Strict (on): wrong key within window consumes the note; Standard (off): ignore wrong key.
            SetDefault(DivaRulesetSettings.JudgementLock, true);
            SetDefault(DivaRulesetSettings.InputOffset, 0.0, -200.0, 200.0, 1.0);
            SetDefault(DivaRulesetSettings.DivaRootPath, string.Empty);
            SetDefault(DivaRulesetSettings.DivaLibraryPaths, "[]");
            SetDefault(DivaRulesetSettings.ImportToRealm, true);
        }

        private static List<string> normalisePaths(IEnumerable<string> paths)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();

            foreach (string path in paths)
            {
                string trimmed = path.Trim();

                if (string.IsNullOrEmpty(trimmed) || !seen.Add(trimmed))
                    continue;

                result.Add(trimmed);
            }

            return result;
        }
    }

    public enum DivaRulesetSettings
    {
        UseXBoxButtons,
        EnableVisualBursts,
        EnableBuiltinHitSounds,
        NoteSize,
        ApproachPreemptScale,
        HitExplosionAlpha,
        JudgementLock,
        InputOffset,
        DivaRootPath,
        DivaLibraryPaths,
        ImportToRealm
    }
}
