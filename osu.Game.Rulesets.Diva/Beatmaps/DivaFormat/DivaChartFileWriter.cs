// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    /// <summary>
    ///     Writes ProjectDIVA <c>.diva</c> text charts, mirroring the section and field order read by
    ///     <see cref="DivaChartFileParser" /> (<c>NoteMap::SetNew</c>).
    /// </summary>
    public static class DivaChartFileWriter
    {
        private const string newline = "\r\n";

        /// <param name="highPrecisionCoordinates">
        ///     false (default): note x/y are rounded, so ProjectDIVA's own <c>fscanf("%d")</c> can still
        ///     read the chart. true: fractional x/y are written as-is (ProjectDIVA cannot read those).
        ///     Integral values are written without a decimal point in either mode.
        /// </param>
        public static string ExportToString(DivaChart chart, bool highPrecisionCoordinates = false)
        {
            var sb = new StringBuilder();
            DivaChartMetadata meta = chart.Metadata;

            sb.Append(meta.EditorVersion).Append(newline);
            sb.Append(meta.Title).Append(newline);
            sb.Append(meta.Creator).Append(newline);
            sb.Append(meta.Artist).Append(newline);
            sb.Append(meta.Style).Append(newline);
            sb.Append(meta.OverviewPicture).Append(newline);
            sb.Append(formatInt(meta.Level)).Append(newline);
            sb.Append(formatInt(meta.Hard)).Append(newline);
            sb.Append(formatDouble(meta.Bpm)).Append(newline);
            sb.Append(formatInt(chart.PeriodCount)).Append(newline);

            foreach (DivaChartControlPoint timing in chart.TimingPoints.OrderBy(t => t.FrameIndex).ThenBy(t => t.TimeMs))
                sb.Append(formatInt(timing.FrameIndex)).Append(' ').Append(formatDouble(timing.Bpm)).Append(newline);

            appendTerminator(sb);

            // Negative position is a declared source seek, not a frame index.
            foreach (DivaResourceEvent resource in chart.ResourceEvents)
                appendEvent(sb, resource.DeclaredSourceOffsetMs, resource.FrameIndex, resource.ResourceId);

            appendTerminator(sb);

            foreach (DivaBgmEvent bgm in chart.BgmEvents)
            {
                sb.Append(formatEventPosition(bgm.DeclaredSourceOffsetMs, bgm.FrameIndex))
                  .Append(' ').Append(formatInt(bgm.Slot))
                  .Append(' ').Append(formatInt(bgm.WavId))
                  .Append(newline);
            }

            appendTerminator(sb);

            foreach (DivaChartNote note in chart.Notes)
            {
                sb.Append(formatInt(note.FrameIndex)).Append(' ')
                  .Append(formatInt(note.Type)).Append(' ')
                  .Append(formatCoordinate(note.X, highPrecisionCoordinates)).Append(' ')
                  .Append(formatCoordinate(note.Y, highPrecisionCoordinates)).Append(' ')
                  .Append(formatInt(note.TailX)).Append(' ')
                  .Append(formatInt(note.TailY)).Append(' ')
                  .Append(formatInt(note.Key));

                if (note.Type >= DivaChartConstants.NOTE_TYPE_COUNT)
                    sb.Append(' ').Append(formatInt(toFrameLength(note, chart)));

                sb.Append(newline);
            }

            appendTerminator(sb);

            foreach ((int id, string file) in chart.WavFiles.OrderBy(pair => pair.Key))
                sb.Append(formatInt(id)).Append(' ').Append(file).Append(newline);

            appendTerminator(sb);

            foreach ((int id, string file) in chart.ResourceFiles.OrderBy(pair => pair.Key))
                sb.Append(formatInt(id)).Append(' ').Append(file).Append(newline);

            appendTerminator(sb);

            if (string.CompareOrdinal(meta.EditorVersion, "1.0.1.0") >= 0)
                sb.Append(formatInt(chart.ChanceTimeStart)).Append(' ').Append(formatInt(chart.ChanceTimeEnd)).Append(newline);

            return sb.ToString();
        }

        public static void WriteToFile(DivaChart chart, string destinationPath, bool highPrecisionCoordinates = false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            DivaChartTextEncoding.WriteFile(destinationPath, ExportToString(chart, highPrecisionCoordinates), resolveWriteEncoding(chart));
        }

        /// <summary>
        ///     Reuses the source chart's encoding when it is still on disk: re-encoding CJK metadata into
        ///     a different code page could make the written file unreadable by the same detection pass.
        /// </summary>
        private static Encoding resolveWriteEncoding(DivaChart chart)
        {
            string source = chart.Metadata.SourcePath;

            if (!string.IsNullOrWhiteSpace(source) && File.Exists(source))
            {
                try
                {
                    return DivaChartTextEncoding.DetectBestEncoding(source);
                }
                catch
                {
                    // Unreadable source; UTF-8 is the safe default.
                }
            }

            return new UTF8Encoding(false);
        }

        private static void appendEvent(StringBuilder sb, double? declaredSourceOffsetMs, int frameIndex, int value)
            => sb.Append(formatEventPosition(declaredSourceOffsetMs, frameIndex)).Append(' ').Append(formatInt(value)).Append(newline);

        private static void appendTerminator(StringBuilder sb) => sb.Append("-1").Append(newline);

        private static string formatEventPosition(double? declaredSourceOffsetMs, int frameIndex)
            => declaredSourceOffsetMs is { } seek ? formatInt(-(int)Math.Round(seek)) : formatInt(frameIndex);

        private static string formatCoordinate(float value, bool highPrecisionCoordinates)
            => highPrecisionCoordinates
                ? value.ToString("R", CultureInfo.InvariantCulture)
                : formatInt((int)MathF.Round(value, MidpointRounding.AwayFromZero));

        private static string formatInt(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string formatDouble(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>
        ///     Inverts the parser's frame → ms duration conversion (ProjectDIVA stores hold length in chart frames).
        /// </summary>
        private static int toFrameLength(DivaChartNote note, DivaChart chart)
        {
            if (note.DurationMs <= 0)
                return 0;

            double frames = note.DurationMs / DivaChartConstants.MsPerFrame(activeBpmAt(chart, note.FrameIndex));
            return Math.Max(1, (int)Math.Round(frames, MidpointRounding.AwayFromZero));
        }

        /// <summary>Last timing point at or before <paramref name="frameIndex"/>, else the header BPM.</summary>
        private static double activeBpmAt(DivaChart chart, int frameIndex)
        {
            double bpm = chart.Metadata.Bpm > 0 ? chart.Metadata.Bpm : DivaChartConstants.BASE_BPM;
            int lastFrame = int.MinValue;

            foreach (DivaChartControlPoint timing in chart.TimingPoints)
            {
                if (timing.Bpm <= 0 || timing.FrameIndex > frameIndex || timing.FrameIndex < lastFrame)
                    continue;

                lastFrame = timing.FrameIndex;
                bpm = timing.Bpm;
            }

            return bpm;
        }
    }
}
