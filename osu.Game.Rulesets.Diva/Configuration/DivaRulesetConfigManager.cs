// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Text.Json;
using osu.Game.Configuration;
using osu.Game.Rulesets.Configuration;
using osu.Game.Rulesets.Diva.Objects.Drawables.Pieces;

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
#if DIVA_EZ2LAZER
            return Get<bool>(DivaRulesetSettings.ImportToRealm);
#else
            return true;
#endif
        }

        public void PersistImportToRealm(bool value)
        {
#if DIVA_EZ2LAZER
            SetValue(DivaRulesetSettings.ImportToRealm, value);
#else
            SetValue(DivaRulesetSettings.ImportToRealm, true);
#endif
        }

        protected override void InitialiseDefaults()
        {
            base.InitialiseDefaults();

            SetDefault(DivaRulesetSettings.UseXBoxButtons, false);
            SetDefault(DivaRulesetSettings.EnableVisualBursts, true);
            SetDefault(DivaRulesetSettings.EnableBuiltinHitSounds, true);
            SetDefault(DivaRulesetSettings.NoteSize, 40.0, 10.0, 64.0);
            // Fraction of the max contain-fit for this beatmap's logical field (≤1 keeps content on-screen on any aspect).
            SetDefault(DivaRulesetSettings.PlayfieldScale, 0.92, 0.5, 1.0, 0.01);
            // Multiplier on PD note_standing×MsPerFrame(BPM); 1.0 = ProjectDIVA baseline.
            SetDefault(DivaRulesetSettings.ApproachPreemptScale, 1.0, 0.75, 1.25, 0.05);
            SetDefault(DivaRulesetSettings.HitExplosionAlpha, 1.0, 0.0, 1.0, 0.05);
            // PD Strict (on): wrong key within window consumes the note; Standard (off): ignore wrong key.
            SetDefault(DivaRulesetSettings.JudgementLock, true);
            SetDefault(DivaRulesetSettings.InputOffset, 0.0, -200.0, 200);
            SetDefault(DivaRulesetSettings.DivaRootPath, string.Empty);
            SetDefault(DivaRulesetSettings.DivaLibraryPaths, "[]");
            SetDefault(DivaRulesetSettings.ImportToRealm, true);
            // ProjectDIVA native: off-field clipping + NOTE_BLOWUP pop on the target note.
            SetDefault(DivaRulesetSettings.NoteAppearance, DivaNoteAppearance.DivaNative);
            SetDefault(DivaRulesetSettings.FlightCurve, DivaNoteFlightCurve.DivaNative);
            // Percent of the curve's baseline lateral offset (ProjectDIVA's own amplitude is 100%); 0 is a straight line.
            SetDefault(DivaRulesetSettings.FlightAmplitude, 100.0, 0.0, 200.0, 5.0);
            // Percent of the default hold-body star spacing (100 = ProjectDIVA-ish density); 0 hides the stars.
            SetDefault(DivaRulesetSettings.HoldStarDensity, 100.0, 0.0, 200.0, 5.0);
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
        PlayfieldScale,
        ApproachPreemptScale,
        HitExplosionAlpha,
        JudgementLock,
        InputOffset,
        DivaRootPath,
        DivaLibraryPaths,
        ImportToRealm,
        NoteAppearance,
        FlightCurve,
        FlightAmplitude,
        HoldStarDensity
    }
}
