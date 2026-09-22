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

        private static readonly Regex key_suffix = new Regex(
            @"-k(\d+)$",
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
        ///     Grid coordinates may be fractional (high-precision charts / future editor).
        /// </summary>
        public static Vector2 ToPlayfieldPosition(float gridX, float gridY)
        {
            float x = DivaChartConstants.ORIGIN_X + gridX * DivaChartConstants.DELTA_X + 12;
            float y = DivaChartConstants.ORIGIN_Y + gridY * DivaChartConstants.DELTA_Y + 12;
            return new Vector2(x, y);
        }

        /// <summary>
        ///     Inverse of <see cref="ToPlayfieldPosition"/>: playfield pixels → grid cell (may be fractional).
        /// </summary>
        public static Vector2 ToGridPosition(Vector2 playfield)
        {
            float x = (playfield.X - DivaChartConstants.ORIGIN_X - 12) / DivaChartConstants.DELTA_X;
            float y = (playfield.Y - DivaChartConstants.ORIGIN_Y - 12) / DivaChartConstants.DELTA_Y;
            return new Vector2(x, y);
        }

        /// <summary>
        ///     Snap a playfield-space point onto the ProjectDIVA note grid.
        /// </summary>
        public static Vector2 SnapToGrid(Vector2 playfield, bool integerCells = true)
        {
            Vector2 grid = ToGridPosition(playfield);

            if (integerCells)
                grid = new Vector2(MathF.Round(grid.X), MathF.Round(grid.Y));

            return ToPlayfieldPosition(grid.X, grid.Y);
        }

        /// <summary>Obsolete name kept for call sites; prefers playfield space.</summary>
        public static Vector2 ToOsuPosition(float gridX, float gridY) => ToPlayfieldPosition(gridX, gridY);

        /// <summary>
        ///     ProjectDIVA <c>nowDistance</c>: how far a note flies, scaled by the BPM in play
        ///     (<c>DISTANCE * 120 / bpm</c>).
        /// </summary>
        public static float ApproachDistance(double bpm)
        {
            if (bpm <= 0)
                bpm = DivaChartConstants.BASE_BPM;

            return (float)(DivaChartConstants.DISTANCE * DivaChartConstants.BASE_BPM / bpm);
        }

        /// <summary>
        ///     The flight vector a note actually uses: ProjectDIVA keeps only the direction of
        ///     <c>(_tailx, _taily) - note</c> and derives the length from the BPM, so anything that stores a
        ///     freely dragged vector has to be normalised to it to match what the game shows.
        /// </summary>
        public static Vector2 NormaliseApproachOrigin(Vector2 approach, double bpm)
        {
            if (approach.LengthSquared < 0.0001f)
                return new Vector2(ApproachDistance(bpm), 0);

            return approach.Normalized() * ApproachDistance(bpm);
        }

        /// <summary>
        ///     Relative approach start (note-local), matching ProjectDIVA
        ///     <c>note + (rhythm-note).unit() * (DISTANCE * 120 / bpm)</c>.
        /// </summary>
        public static Vector2 ComputeApproachOrigin(Vector2 notePos, int tailX, int tailY, double bpm)
            => NormaliseApproachOrigin(new Vector2(tailX, tailY) - notePos, bpm);

        public static Vector2 ComputeApproachOrigin(DivaChartNote note, double bpm)
        {
            Vector2 notePos = ToPlayfieldPosition(note.X, note.Y);
            return ComputeApproachOrigin(notePos, note.TailX, note.TailY, bpm);
        }

        public static string EncodeSampleFileName(DivaAction action, bool isHold, double durationMs = 0, Vector2? approachOrigin = null, int? wavKey = null)
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

            // The key goes before the approach suffix: the approach regex is anchored to the end of the name.
            if (wavKey is { } key)
                core = string.Create(CultureInfo.InvariantCulture, $"{core}-k{key}");

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
            out Vector2? approachOrigin,
            out int? wavKey)
        {
            action = DivaAction.Circle;
            isHold = false;
            durationMs = 0;
            approachOrigin = null;
            wavKey = null;

            foreach (HitSampleInfo sample in original.Samples)
            {
                foreach (string name in sample.LookupNames.Append(sample.Name))
                {
                    string leaf = Path.GetFileName(name);
                    if (string.IsNullOrEmpty(leaf))
                        continue;

                    if (tryParseSampleLeaf(leaf, out action, out isHold, out durationMs, out approachOrigin, out wavKey))
                        return true;
                }
            }

            return false;
        }

        /// <summary>Back-compat overload without the approach and key.</summary>
        public static bool TryParseFromHitObject(HitObject original, out DivaAction action, out bool isHold, out double durationMs)
            => TryParseFromHitObject(original, out action, out isHold, out durationMs, out _, out _);

        /// <summary>Overload for callers that only care about the approach vector.</summary>
        public static bool TryParseFromHitObject(HitObject original, out DivaAction action, out bool isHold, out double durationMs, out Vector2? approachOrigin)
            => TryParseFromHitObject(original, out action, out isHold, out durationMs, out approachOrigin, out _);

        private static bool tryParseSampleLeaf(
            string leaf,
            out DivaAction action,
            out bool isHold,
            out double durationMs,
            out Vector2? approachOrigin,
            out int? wavKey)
        {
            action = DivaAction.Circle;
            isHold = false;
            durationMs = 0;
            approachOrigin = null;
            wavKey = null;

            string payload = leaf;
            Match approachMatch = approach_suffix.Match(leaf);

            if (approachMatch.Success
                && float.TryParse(approachMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float ax)
                && float.TryParse(approachMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float ay))
            {
                approachOrigin = new Vector2(ax, ay);
                payload = leaf[..approachMatch.Index];
            }

            Match keyMatch = key_suffix.Match(payload);

            if (keyMatch.Success && int.TryParse(keyMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedKey))
            {
                wavKey = parsedKey;
                payload = payload[..keyMatch.Index];
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
