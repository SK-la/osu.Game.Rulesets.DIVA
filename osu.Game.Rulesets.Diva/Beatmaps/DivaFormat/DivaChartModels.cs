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

    public sealed class DivaChart
    {
        public DivaChartMetadata Metadata { get; init; } = new DivaChartMetadata();
        public int PeriodCount { get; init; }
        public int FrameCount { get; init; }
        public IReadOnlyList<DivaChartControlPoint> TimingPoints { get; init; } = [];
        public IReadOnlyList<DivaChartNote> Notes { get; init; } = [];
        public IReadOnlyDictionary<int, string> WavFiles { get; init; } = new Dictionary<int, string>();
        public IReadOnlyDictionary<int, string> ResourceFiles { get; init; } = new Dictionary<int, string>();
        public int ChanceTimeStart { get; init; } = -1;
        public int ChanceTimeEnd { get; init; } = -1;

        public string? ResolvePrimaryAudioRelativePath()
        {
            foreach ((_, string file) in WavFiles)
            {
                if (string.IsNullOrWhiteSpace(file))
                    continue;

                string ext = Path.GetExtension(file);
                if (ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".ogg", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".wav", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".flac", StringComparison.OrdinalIgnoreCase))
                    return file.Replace('\\', '/');
            }

            foreach ((_, string file) in WavFiles)
            {
                if (!string.IsNullOrWhiteSpace(file))
                    return file.Replace('\\', '/');
            }

            return null;
        }

        public string? ResolveBackgroundRelativePath()
        {
            if (!string.IsNullOrWhiteSpace(Metadata.OverviewPicture))
                return Metadata.OverviewPicture.Replace('\\', '/');

            foreach ((_, string file) in ResourceFiles)
            {
                string ext = Path.GetExtension(file);
                if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
                    return file.Replace('\\', '/');
            }

            return null;
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
        public const double SECOND = 1000.0;

        public static readonly string[] LEVEL_NAMES =
        [
            "Easy",
            "Normal",
            "Hard",
            "Extra",
            "Extreme"
        ];

        public static double MsPerFrame(double bpm)
        {
            if (bpm <= 0)
                bpm = 120;

            return 60.0 * SECOND / (bpm * (NOTE_PER_PERIOD / (double)TIME_PER_PERIOD));
        }
    }
}
