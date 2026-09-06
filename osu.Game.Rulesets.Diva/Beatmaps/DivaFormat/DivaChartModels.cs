// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    public sealed class DivaChartMetadata
    {
        public string EditorVersion { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Creator { get; init; } = string.Empty;
        public string Artist { get; init; } = string.Empty;
        public string Style { get; init; } = string.Empty;
        public string OverviewPicture { get; init; } = string.Empty;
        public int Level { get; init; }
        public int Hard { get; init; }
        public double Bpm { get; init; }
        public string SourcePath { get; init; } = string.Empty;
        public string SongFolder { get; init; } = string.Empty;
    }

    public sealed class DivaChartNote
    {
        public bool IsHold => Type >= DivaChartConstants.NOTE_TYPE_COUNT;
        public int FrameIndex { get; init; }
        public double StartTimeMs { get; init; }
        public int Type { get; init; }
        public int GridX { get; init; }
        public int GridY { get; init; }
        public int TailX { get; init; }
        public int TailY { get; init; }
        public int Key { get; init; }
        public double DurationMs { get; init; }
    }

    public sealed class DivaChartControlPoint
    {
        public int FrameIndex { get; init; }
        public double TimeMs { get; init; }
        public double Bpm { get; init; }
    }

    public sealed class DivaBgmEvent
    {
        public int Sequence { get; init; }
        public int FrameIndex { get; init; }
        public double TimeMs { get; init; }
        public int Slot { get; init; }
        public int WavId { get; init; }

        /// <summary>
        /// Source seek declared by a negative BGS position, in milliseconds.
        /// ProjectDIVA stores the final value globally per WAV id.
        /// </summary>
        public double? DeclaredSourceOffsetMs { get; init; }
    }

    public sealed class DivaResourceEvent
    {
        public int Sequence { get; init; }
        public int FrameIndex { get; init; }
        public double TimeMs { get; init; }
        public int ResourceId { get; init; }

        /// <summary>
        /// Source seek declared by a negative Resource position, in milliseconds.
        /// ProjectDIVA stores only the final declared value in <c>videoEngine.m_pTime</c>.
        /// </summary>
        public double? DeclaredSourceOffsetMs { get; init; }
    }

    public sealed class DivaChart
    {
        public DivaChartMetadata Metadata { get; init; } = new DivaChartMetadata();
        public int PeriodCount { get; init; }
        public int FrameCount { get; init; }
        public IReadOnlyList<DivaChartControlPoint> TimingPoints { get; init; } = [];
        public IReadOnlyList<DivaChartNote> Notes { get; init; } = [];
        public IReadOnlyDictionary<int, string> WavFiles { get; init; } = new Dictionary<int, string>();
        public IReadOnlyDictionary<int, string> ResourceFiles { get; init; } = new Dictionary<int, string>();
        public IReadOnlyList<DivaBgmEvent> BgmEvents { get; init; } = [];
        public IReadOnlyList<DivaResourceEvent> ResourceEvents { get; init; } = [];
        /// <summary>Frame index; -1 if unset.</summary>
        public int ChanceTimeStart { get; init; } = -1;

        /// <summary>Frame index; -1 if unset.</summary>
        public int ChanceTimeEnd { get; init; } = -1;

        /// <summary>Start time in ms for Chance Time / Kiai; negative if unset.</summary>
        public double ChanceTimeStartMs { get; init; } = -1;

        /// <summary>
        /// Exclusive end time in ms (frame <c>ChanceTimeEnd + 1</c>), matching ProjectDIVA.
        /// Negative if unset.
        /// </summary>
        public double ChanceTimeEndMs { get; init; } = -1;

        public bool HasChanceTime => ChanceTimeStart >= 0 && ChanceTimeEnd >= ChanceTimeStart;

        public string? ResolvePrimaryAudioRelativePath() => DivaPlaybackTimeline.Create(this).AudioRelativePath;

        public string? ResolveBackgroundRelativePath()
        {
            if (!string.IsNullOrWhiteSpace(Metadata.OverviewPicture))
            {
                string? overview = ResolveRelativePath(Metadata.OverviewPicture);
                if (overview != null)
                    return overview;
            }

            foreach ((_, string file) in ResourceFiles)
            {
                string ext = Path.GetExtension(file);
                if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
                {
                    string? resolved = ResolveRelativePath(file);
                    if (resolved != null)
                        return resolved;
                }
            }

            return null;
        }

        internal string? ResolveRelativePath(string relative)
        {
            string normalised = relative.Replace('\\', '/');

            if (string.IsNullOrWhiteSpace(Metadata.SongFolder))
                return normalised;

            return DivaChartTextEncoding.ResolveExistingRelativePath(Metadata.SongFolder, normalised) ?? normalised;
        }
    }

    public static class DivaChartConstants
    {
        public const int NOTE_TYPE_COUNT = 8;
        public const int NOTE_PER_PERIOD = 192;
        public const int TIME_PER_PERIOD = 4;
        public const int ORIGIN_X = 25;
        public const int ORIGIN_Y = 52;
        public const int DELTA_X = 12;
        public const int DELTA_Y = 12;
        public const int WIDTH = 480;
        public const int HEIGHT = 272;
        /// <summary>ProjectDIVA approach flight distance base (scaled by 120/BPM).</summary>
        public const int DISTANCE = 500;
        public const double BASE_BPM = 120;
        public const double SECOND = 1000.0;

        public static readonly string[] LEVEL_NAMES =
        [
            "Easy",
            "Normal",
            "Hard",
            "Extra",
            "Extreme"
        ];

        public static string LevelName(int level)
        {
            int index = Math.Clamp(level - 1, 0, LEVEL_NAMES.Length - 1);
            return LEVEL_NAMES[index];
        }

        /// <summary>
        /// Formats panel difficulty name like BMS black stars: <c>★{hard} {Level}</c>.
        /// When <paramref name="hard"/> is not positive, returns the level name only.
        /// </summary>
        public static string FormatDifficultyName(int level, int hard)
        {
            string label = LevelName(level);
            return hard > 0 ? $"★{hard} {label}" : label;
        }

        public static double MsPerFrame(double bpm)
        {
            if (bpm <= 0)
                bpm = 120;

            return 60.0 * SECOND / (bpm * (NOTE_PER_PERIOD / (double)TIME_PER_PERIOD));
        }

        /// <summary>
        /// ProjectDIVA <c>note_standing * singleTime</c>: how long a note approaches before hit.
        /// At 120 BPM this is 2000ms; at 150 BPM, 1600ms.
        /// </summary>
        public static double StandingPreemptMs(double bpm) => NOTE_PER_PERIOD * MsPerFrame(bpm);
    }
}
