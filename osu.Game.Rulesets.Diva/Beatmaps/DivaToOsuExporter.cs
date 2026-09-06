// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osuTK;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    /// <summary>
    ///     Writes a Mode:0 .osu that preserves DIVA button data via sample filenames.
    /// </summary>
    public static class DivaToOsuExporter
    {
        public static string ExportToString(DivaChart chart)
        {
            var sb = new StringBuilder();
            DivaPlaybackTimeline timeline = DivaPlaybackTimeline.Create(chart);
            string audio = Path.GetFileName(timeline.AudioRelativePath ?? "audio.mp3");
            string? background = chart.ResolveBackgroundRelativePath();
            string difficulty = DivaChartConstants.FormatDifficultyName(chart.Metadata.Level, chart.Metadata.Hard);
            double bpm = chart.Metadata.Bpm > 0 ? chart.Metadata.Bpm : 120;
            double beatLength = 60000.0 / bpm;

            sb.AppendLine("osu file format v14");
            sb.AppendLine();
            sb.AppendLine("[General]");
            sb.AppendLine($"AudioFilename: {audio}");
            sb.AppendLine("AudioLeadIn: 0");
            sb.AppendLine("PreviewTime: -1");
            sb.AppendLine("Countdown: 0");
            sb.AppendLine("SampleSet: Soft");
            sb.AppendLine("StackLeniency: 0.7");
            sb.AppendLine("Mode: 0");
            sb.AppendLine("LetterboxInBreaks: 0");
            sb.AppendLine("WidescreenStoryboard: 1");
            sb.AppendLine();
            sb.AppendLine("[Editor]");
            sb.AppendLine("DistanceSpacing: 1");
            sb.AppendLine("BeatDivisor: 4");
            sb.AppendLine("GridSize: 4");
            sb.AppendLine("TimelineZoom: 1");
            sb.AppendLine();
            sb.AppendLine("[Metadata]");
            sb.AppendLine($"Title:{chart.Metadata.Title}");
            sb.AppendLine($"TitleUnicode:{chart.Metadata.Title}");
            sb.AppendLine($"Artist:{chart.Metadata.Artist}");
            sb.AppendLine($"ArtistUnicode:{chart.Metadata.Artist}");
            sb.AppendLine($"Creator:{chart.Metadata.Creator}");
            sb.AppendLine($"Version:{difficulty}");
            sb.AppendLine("Source:ProjectDIVA");
            sb.AppendLine($"Tags:{DivaActionEncoding.NATIVE_TAG} diva-import {chart.Metadata.Style}".Trim());
            sb.AppendLine("BeatmapID:0");
            sb.AppendLine("BeatmapSetID:-1");
            sb.AppendLine();
            sb.AppendLine("[Difficulty]");
            sb.AppendLine("HPDrainRate:5");
            sb.AppendLine("CircleSize:4");
            sb.AppendLine($"OverallDifficulty:{Math.Clamp(chart.Metadata.Hard, 1, 10).ToString(CultureInfo.InvariantCulture)}");
            sb.AppendLine("ApproachRate:8");
            sb.AppendLine("SliderMultiplier:1.4");
            sb.AppendLine("SliderTickRate:1");
            sb.AppendLine();
            sb.AppendLine("[Events]");
            sb.AppendLine("//Background and Video events");
            if (!string.IsNullOrWhiteSpace(background))
                sb.AppendLine($"0,0,\"{Path.GetFileName(background)}\",0,0");
            sb.AppendLine();
            sb.AppendLine("[TimingPoints]");

            foreach (DivaChartControlPoint point in chart.TimingPoints.OrderBy(p => p.TimeMs))
            {
                double bl = 60000.0 / (point.Bpm > 0 ? point.Bpm : bpm);
                sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"{timeline.ToPlaybackTime(point.TimeMs):0.###},{bl:0.###},4,2,0,100,1,0"));
            }

            if (chart.TimingPoints.Count == 0)
            {
                sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"{timeline.ToPlaybackTime(0):0.###},{beatLength:0.###},4,2,0,100,1,0"));
            }

            if (chart.HasChanceTime)
            {
                sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"{timeline.ToPlaybackTime(chart.ChanceTimeStartMs):0.###},-100,4,2,0,100,0,1"));
                sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"{timeline.ToPlaybackTime(chart.ChanceTimeEndMs):0.###},-100,4,2,0,100,0,0"));
            }

            sb.AppendLine();
            sb.AppendLine("[HitObjects]");

            foreach (DivaChartNote note in chart.Notes.OrderBy(n => n.StartTimeMs).ThenBy(n => n.FrameIndex))
            {
                Vector2 pos = DivaActionEncoding.ToPlayfieldPosition(note.GridX, note.GridY);
                DivaAction action = DivaActionEncoding.ResolveAction(note);
                double noteBpm = resolveBpmAt(chart, note.StartTimeMs, bpm);
                Vector2 approach = DivaActionEncoding.ComputeApproachOrigin(note, noteBpm);
                string sample = DivaActionEncoding.EncodeSampleFileName(action, note.IsHold, note.DurationMs, approach);
                int x = (int)Math.Round(pos.X);
                int y = (int)Math.Round(pos.Y);
                int time = (int)Math.Round(timeline.ToPlaybackTime(note.StartTimeMs));

                // type 1 = circle
                sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"{x},{y},{time},1,0,0:0:0:0:{sample}"));
            }

            return sb.ToString();
        }

        public static void WriteToFile(DivaChart chart, string destinationPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.WriteAllText(destinationPath, ExportToString(chart), new UTF8Encoding(false));
        }

        private static double resolveBpmAt(DivaChart chart, double timeMs, double fallback)
        {
            double current = fallback;

            foreach (DivaChartControlPoint point in chart.TimingPoints.OrderBy(p => p.TimeMs))
            {
                if (point.TimeMs > timeMs)
                    break;

                if (point.Bpm > 0)
                    current = point.Bpm;
            }

            return current;
        }
    }
}
