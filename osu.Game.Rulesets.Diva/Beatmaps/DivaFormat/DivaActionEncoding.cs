// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Rulesets.Objects;
using osuTK;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    /// <summary>
    ///     Encodes <see cref="DivaAction" /> into legacy hit-sample filenames so Mode:0 .osu imports survive conversion.
    /// </summary>
    public static class DivaActionEncoding
    {
        public const string ACTION_PREFIX = "diva-action-";
        public const string HOLD_PREFIX = "diva-hold-";
        public const string NATIVE_TAG = "diva-native";

        public static DivaAction FromUnitType(int typeOrKey)
        {
            int unit = typeOrKey % DivaChartConstants.NOTE_TYPE_COUNT;
            if (typeOrKey >= DivaChartConstants.NOTE_TYPE_COUNT)
                unit = (typeOrKey - DivaChartConstants.NOTE_TYPE_COUNT) % DivaChartConstants.NOTE_TYPE_COUNT;

            return unit switch
            {
                0 => DivaAction.Circle,
                1 => DivaAction.Square,
                2 => DivaAction.Cross,
                3 => DivaAction.Triangle,
                4 => DivaAction.Right,
                5 => DivaAction.Left,
                6 => DivaAction.Down,
                7 => DivaAction.Up,
                _ => DivaAction.Circle
            };
        }

        public static DivaAction ResolveAction(DivaChartNote note)
        {
            int source = note.Key >= 0 && note.Key < DivaChartConstants.NOTE_TYPE_COUNT
                ? note.Key
                : note.Type;
            return FromUnitType(source);
        }

        public static Vector2 ToOsuPosition(int gridX, int gridY)
        {
            float x = DivaChartConstants.ORIGIN_X + gridX * DivaChartConstants.DELTA_X + 12;
            float y = DivaChartConstants.ORIGIN_Y + gridY * DivaChartConstants.DELTA_Y + 12;
            return new Vector2(Math.Clamp(x, 0, 512), Math.Clamp(y, 0, 384));
        }

        public static string EncodeSampleFileName(DivaAction action, bool isHold, double durationMs = 0)
        {
            int id = (int)action;
            if (!isHold)
                return ACTION_PREFIX + id.ToString(CultureInfo.InvariantCulture);

            int duration = Math.Max(0, (int)Math.Round(durationMs));
            return HOLD_PREFIX + id.ToString(CultureInfo.InvariantCulture) + "-" + duration.ToString(CultureInfo.InvariantCulture);
        }

        public static bool TryParseFromHitObject(HitObject original, out DivaAction action, out bool isHold, out double durationMs)
        {
            action = DivaAction.Circle;
            isHold = false;
            durationMs = 0;

            foreach (HitSampleInfo sample in original.Samples)
            {
                foreach (string name in sample.LookupNames.Append(sample.Name))
                {
                    string leaf = Path.GetFileName(name);
                    if (string.IsNullOrEmpty(leaf))
                        continue;

                    if (leaf.StartsWith(HOLD_PREFIX, StringComparison.OrdinalIgnoreCase))
                    {
                        string payload = leaf[HOLD_PREFIX.Length..];
                        string[] parts = payload.Split('-', 2);

                        if (parts.Length == 2
                            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int holdAction)
                            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out durationMs)
                            && Enum.IsDefined(typeof(DivaAction), holdAction))
                        {
                            action = (DivaAction)holdAction;
                            isHold = true;
                            return true;
                        }
                    }

                    if (leaf.StartsWith(ACTION_PREFIX, StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(leaf[ACTION_PREFIX.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int actionId)
                        && Enum.IsDefined(typeof(DivaAction), actionId))
                    {
                        action = (DivaAction)actionId;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
