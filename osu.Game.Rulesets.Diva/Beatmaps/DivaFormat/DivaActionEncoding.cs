// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        private static readonly Regex approach_suffix = new Regex(
            @"-ax(-?\d+(?:\.\d+)?)-ay(-?\d+(?:\.\d+)?)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

        /// <summary>
        ///     ProjectDIVA <c>_type</c> is the button shape; <c>_key</c> is only a WAV table index.
        /// </summary>
        public static DivaAction ResolveAction(DivaChartNote note) => FromUnitType(note.Type);

        /// <summary>Maps <see cref="DivaAction"/> to ProjectDIVA UNIT index 0–7.</summary>
        public static int ToUnitIndex(DivaAction action) => action switch
        {
            DivaAction.Circle => 0,
            DivaAction.Square => 1,
            DivaAction.Cross => 2,
            DivaAction.Triangle => 3,
            DivaAction.Right => 4,
            DivaAction.Left => 5,
            DivaAction.Down => 6,
            DivaAction.Up => 7,
            _ => 0
        };

        /// <summary>
        ///     Grid cell → ProjectDIVA playfield pixels (480×272), matching <c>ORIGIN + grid*DELTA + 12</c>.
        /// </summary>
        public static Vector2 ToPlayfieldPosition(int gridX, int gridY)
        {
            float x = DivaChartConstants.ORIGIN_X + gridX * DivaChartConstants.DELTA_X + 12;
            float y = DivaChartConstants.ORIGIN_Y + gridY * DivaChartConstants.DELTA_Y + 12;
            return new Vector2(x, y);
        }

        /// <summary>Obsolete name kept for call sites; prefers playfield space.</summary>
        public static Vector2 ToOsuPosition(int gridX, int gridY) => ToPlayfieldPosition(gridX, gridY);

        /// <summary>
        ///     Relative approach start (note-local), matching ProjectDIVA
        ///     <c>note + (rhythm-note).unit() * (DISTANCE * 120 / bpm)</c>.
        /// </summary>
        public static Vector2 ComputeApproachOrigin(Vector2 notePos, int tailX, int tailY, double bpm)
        {
            if (bpm <= 0)
                bpm = DivaChartConstants.BASE_BPM;

            float distance = (float)(DivaChartConstants.DISTANCE * DivaChartConstants.BASE_BPM / bpm);
            Vector2 dir = new Vector2(tailX, tailY) - notePos;

            if (dir.LengthSquared < 0.0001f)
                return new Vector2(distance, 0);

            return dir.Normalized() * distance;
        }

        public static Vector2 ComputeApproachOrigin(DivaChartNote note, double bpm)
        {
            Vector2 notePos = ToPlayfieldPosition(note.GridX, note.GridY);
            return ComputeApproachOrigin(notePos, note.TailX, note.TailY, bpm);
        }

        public static string EncodeSampleFileName(DivaAction action, bool isHold, double durationMs = 0, Vector2? approachOrigin = null)
        {
            int id = (int)action;
            string core;

            if (!isHold)
                core = ACTION_PREFIX + id.ToString(CultureInfo.InvariantCulture);
            else
            {
                int duration = Math.Max(0, (int)Math.Round(durationMs));
                core = HOLD_PREFIX + id.ToString(CultureInfo.InvariantCulture) + "-" + duration.ToString(CultureInfo.InvariantCulture);
            }

            if (approachOrigin is not { } origin)
                return core;

            return string.Create(CultureInfo.InvariantCulture,
                $"{core}-ax{origin.X:0.###}-ay{origin.Y:0.###}");
        }

        public static bool TryParseFromHitObject(
            HitObject original,
            out DivaAction action,
            out bool isHold,
            out double durationMs,
            out Vector2? approachOrigin)
        {
            action = DivaAction.Circle;
            isHold = false;
            durationMs = 0;
            approachOrigin = null;

            foreach (HitSampleInfo sample in original.Samples)
            {
                foreach (string name in sample.LookupNames.Append(sample.Name))
                {
                    string leaf = Path.GetFileName(name);
                    if (string.IsNullOrEmpty(leaf))
                        continue;

                    if (tryParseSampleLeaf(leaf, out action, out isHold, out durationMs, out approachOrigin))
                        return true;
                }
            }

            return false;
        }

        /// <summary>Back-compat overload without approach.</summary>
        public static bool TryParseFromHitObject(HitObject original, out DivaAction action, out bool isHold, out double durationMs)
            => TryParseFromHitObject(original, out action, out isHold, out durationMs, out _);

        private static bool tryParseSampleLeaf(
            string leaf,
            out DivaAction action,
            out bool isHold,
            out double durationMs,
            out Vector2? approachOrigin)
        {
            action = DivaAction.Circle;
            isHold = false;
            durationMs = 0;
            approachOrigin = null;

            string payload = leaf;
            Match approachMatch = approach_suffix.Match(leaf);

            if (approachMatch.Success
                && float.TryParse(approachMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float ax)
                && float.TryParse(approachMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float ay))
            {
                approachOrigin = new Vector2(ax, ay);
                payload = leaf[..approachMatch.Index];
            }

            if (payload.StartsWith(HOLD_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                string holdPayload = payload[HOLD_PREFIX.Length..];
                string[] parts = holdPayload.Split('-', 2);

                if (parts.Length == 2
                    && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int holdAction)
                    && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out durationMs)
                    && Enum.IsDefined(typeof(DivaAction), holdAction))
                {
                    action = (DivaAction)holdAction;
                    isHold = true;
                    return true;
                }

                return false;
            }

            if (payload.StartsWith(ACTION_PREFIX, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(payload[ACTION_PREFIX.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int actionId)
                && Enum.IsDefined(typeof(DivaAction), actionId))
            {
                action = (DivaAction)actionId;
                return true;
            }

            return false;
        }
    }
}
