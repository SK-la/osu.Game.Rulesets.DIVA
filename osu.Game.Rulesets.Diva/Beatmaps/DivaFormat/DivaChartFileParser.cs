// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    /// <summary>
    ///     Parses ProjectDIVA PC <c>.diva</c> text charts (see <c>NoteMap::SetNew</c>).
    /// </summary>
    public static class DivaChartFileParser
    {
        public static DivaChart Parse(string path)
        {
            using var reader = DivaChartTextEncoding.OpenReader(path);
            return Parse(reader, path, Path.GetDirectoryName(path) ?? string.Empty);
        }

        public static DivaChart Parse(TextReader reader, string sourcePath, string songFolder)
        {
            string editorVer = readLineRequired(reader);
            string title = readLineRequired(reader);
            string creator = readLineRequired(reader);
            string artist = readLineRequired(reader);
            string style = readLineRequired(reader);
            string overview = readLineRequired(reader);

            int level = readIntLine(reader);
            int hard = readIntLine(reader);
            double headerBpm = readDoubleLine(reader);
            int periodCount = readIntLine(reader);
            int frameCount = periodCount * DivaChartConstants.NOTE_PER_PERIOD;

            double[] frameBpms = new double[Math.Max(frameCount, 1)];

            while (true)
            {
                if (!tryReadIntToken(reader, out int pos))
                    break;
                if (pos == -1)
                    break;

                double bpm = readDoubleToken(reader);
                if (string.CompareOrdinal(editorVer, "1.0.4.7") <= 0)
                    bpm = snapLegacyBpm(bpm);

                if (pos >= 0 && pos < frameBpms.Length)
                    frameBpms[pos] = bpm;
            }

            // Resource events (ignored for gameplay import, but must be consumed).
            while (true)
            {
                if (!tryReadIntToken(reader, out int pos))
                    break;
                if (pos == -1)
                    break;

                _ = readIntToken(reader);
            }

            // BGS events.
            while (true)
            {
                if (!tryReadIntToken(reader, out int pos))
                    break;
                if (pos == -1)
                    break;

                _ = readIntToken(reader);
                _ = readIntToken(reader);
            }

            var notes = new List<DivaChartNote>();
            var rawNotes = new List<(int pos, int type, int x, int y, int tx, int ty, int key, double duration)>();

            while (true)
            {
                if (!tryReadIntToken(reader, out int pos))
                    break;
                if (pos == -1)
                    break;

                int type = readIntToken(reader);
                int x = readIntToken(reader);
                int y = readIntToken(reader);
                int tx = readIntToken(reader);
                int ty = readIntToken(reader);
                int key = readIntToken(reader);
                double duration = 0;

                if (type >= DivaChartConstants.NOTE_TYPE_COUNT)
                    duration = readDoubleToken(reader);

                rawNotes.Add((pos, type, x, y, tx, ty, key, duration));
            }

            var wav = new Dictionary<int, string>();

            while (true)
            {
                if (!tryReadIntToken(reader, out int id))
                    break;
                if (id == -1)
                    break;

                string file = readRestOfLine(reader).Trim();
                wav[id] = file;
            }

            var resources = new Dictionary<int, string>();

            while (true)
            {
                if (!tryReadIntToken(reader, out int id))
                    break;
                if (id == -1)
                    break;

                string file = readRestOfLine(reader).Trim();
                resources[id] = file;
            }

            int chanceStart = -1;
            int chanceEnd = -1;

            if (string.CompareOrdinal(editorVer, "1.0.1.0") >= 0)
            {
                if (tryReadIntToken(reader, out chanceStart))
                    tryReadIntToken(reader, out chanceEnd);
            }

            double[] frameTimes = buildFrameTimes(frameBpms, headerBpm, frameCount);

            var timingPoints = new List<DivaChartControlPoint>();

            for (int i = 0; i < frameBpms.Length; i++)
            {
                if (frameBpms[i] > 0)
                {
                    timingPoints.Add(new DivaChartControlPoint
                    {
                        FrameIndex = i,
                        TimeMs = frameTimes[i],
                        Bpm = frameBpms[i]
                    });
                }
            }

            if (timingPoints.Count == 0)
            {
                timingPoints.Add(new DivaChartControlPoint
                {
                    FrameIndex = 0,
                    TimeMs = 0,
                    Bpm = headerBpm > 0 ? headerBpm : 120
                });
            }

            foreach ((int pos, int type, int x, int y, int tx, int ty, int key, double duration) raw in rawNotes)
            {
                int clamped = Math.Clamp(raw.pos, 0, Math.Max(frameTimes.Length - 1, 0));
                double start = frameTimes.Length == 0 ? 0 : frameTimes[clamped];
                notes.Add(new DivaChartNote
                {
                    FrameIndex = raw.pos,
                    StartTimeMs = start,
                    Type = raw.type,
                    GridX = raw.x,
                    GridY = raw.y,
                    TailX = raw.tx,
                    TailY = raw.ty,
                    Key = raw.key,
                    DurationMs = raw.duration
                });
            }

            double chanceStartMs = -1;
            double chanceEndMs = -1;

            if (chanceStart >= 0 && frameTimes.Length > 0)
            {
                chanceStartMs = frameTimes[Math.Clamp(chanceStart, 0, frameTimes.Length - 1)];

                // ProjectDIVA ends Chance Time when notePos == _chanceTimeEnd + 1 (exclusive end).
                int endFrame = chanceEnd >= 0 ? chanceEnd + 1 : chanceStart + 1;
                if (endFrame < frameTimes.Length)
                    chanceEndMs = frameTimes[endFrame];
                else
                {
                    double last = frameTimes[^1];
                    double bpm = headerBpm > 0 ? headerBpm : 120;
                    chanceEndMs = last + DivaChartConstants.MsPerFrame(bpm);
                }
            }

            return new DivaChart
            {
                Metadata = new DivaChartMetadata
                {
                    EditorVersion = editorVer,
                    Title = title,
                    Creator = creator,
                    Artist = artist,
                    Style = style,
                    OverviewPicture = overview,
                    Level = level,
                    Hard = hard,
                    Bpm = headerBpm,
                    SourcePath = sourcePath,
                    SongFolder = songFolder
                },
                PeriodCount = periodCount,
                FrameCount = frameCount,
                TimingPoints = timingPoints,
                Notes = notes,
                WavFiles = wav,
                ResourceFiles = resources,
                ChanceTimeStart = chanceStart,
                ChanceTimeEnd = chanceEnd,
                ChanceTimeStartMs = chanceStartMs,
                ChanceTimeEndMs = chanceEndMs
            };
        }

        public static DivaChartMetadata ReadMetadata(string path)
        {
            using var reader = DivaChartTextEncoding.OpenReader(path);
            string editorVer = readLineRequired(reader);
            string title = readLineRequired(reader);
            string creator = readLineRequired(reader);
            string artist = readLineRequired(reader);
            string style = readLineRequired(reader);
            string overview = readLineRequired(reader);
            int level = readIntLine(reader);
            int hard = readIntLine(reader);
            double bpm = readDoubleLine(reader);

            return new DivaChartMetadata
            {
                EditorVersion = editorVer,
                Title = title,
                Creator = creator,
                Artist = artist,
                Style = style,
                OverviewPicture = overview,
                Level = level,
                Hard = hard,
                Bpm = bpm,
                SourcePath = path,
                SongFolder = Path.GetDirectoryName(path) ?? string.Empty
            };
        }

        private static double[] buildFrameTimes(double[] frameBpms, double headerBpm, int frameCount)
        {
            double[] times = new double[Math.Max(frameCount, 0)];
            if (frameCount <= 0)
                return times;

            double lastTime = 0;
            int lastBpmIndex = 0;
            double singleTime = DivaChartConstants.MsPerFrame(headerBpm > 0 ? headerBpm : 120);

            for (int i = 0; i < frameCount; i++)
            {
                times[i] = lastTime + (i - lastBpmIndex) * singleTime;

                if (frameBpms[i] > 0)
                {
                    lastTime = times[i];
                    lastBpmIndex = i;
                    singleTime = DivaChartConstants.MsPerFrame(frameBpms[i]);
                }
            }

            return times;
        }

        private static double snapLegacyBpm(double bpm)
        {
            double digit = bpm - Math.Floor(bpm);
            if (digit > 0.8)
                return Math.Ceiling(bpm);
            if (digit < 0.2)
                return Math.Floor(bpm);

            return bpm;
        }

        private static string readLineRequired(TextReader reader)
        {
            string? line = reader.ReadLine();
            if (line == null)
                throw new InvalidDataException("Unexpected end of .diva file.");

            return trimTail(line);
        }

        private static int readIntLine(TextReader reader) => int.Parse(readLineRequired(reader), CultureInfo.InvariantCulture);

        private static double readDoubleLine(TextReader reader) => double.Parse(readLineRequired(reader), CultureInfo.InvariantCulture);

        private static bool tryReadIntToken(TextReader reader, out int value)
        {
            value = 0;
            skipSpaces(reader);
            int ch = reader.Peek();
            if (ch < 0)
                return false;

            var sb = new StringBuilder();

            if (ch == '-' || ch == '+')
            {
                sb.Append((char)reader.Read());
                ch = reader.Peek();
            }

            if (ch < 0 || !char.IsDigit((char)ch))
                return false;

            while (true)
            {
                ch = reader.Peek();
                if (ch < 0 || !char.IsDigit((char)ch))
                    break;

                sb.Append((char)reader.Read());
            }

            return int.TryParse(sb.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static int readIntToken(TextReader reader)
        {
            if (!tryReadIntToken(reader, out int value))
                throw new InvalidDataException("Expected integer token in .diva file.");

            return value;
        }

        private static double readDoubleToken(TextReader reader)
        {
            skipSpaces(reader);
            var sb = new StringBuilder();
            int ch = reader.Peek();

            if (ch == '-' || ch == '+')
            {
                sb.Append((char)reader.Read());
                ch = reader.Peek();
            }

            bool seenDot = false;

            while (true)
            {
                ch = reader.Peek();
                if (ch < 0)
                    break;

                char c = (char)ch;

                if (char.IsDigit(c))
                {
                    sb.Append((char)reader.Read());
                    continue;
                }

                if (c == '.' && !seenDot)
                {
                    seenDot = true;
                    sb.Append((char)reader.Read());
                    continue;
                }

                break;
            }

            if (sb.Length == 0)
                throw new InvalidDataException("Expected floating token in .diva file.");

            return double.Parse(sb.ToString(), CultureInfo.InvariantCulture);
        }

        private static string readRestOfLine(TextReader reader)
        {
            skipSpaces(reader);
            string? line = reader.ReadLine();
            return line == null ? string.Empty : trimTail(line);
        }

        private static void skipSpaces(TextReader reader)
        {
            while (true)
            {
                int ch = reader.Peek();
                if (ch < 0)
                    return;

                char c = (char)ch;

                if (c == ' ' || c == '\t' || c == '\r')
                {
                    reader.Read();
                    continue;
                }

                if (c == '\n')
                {
                    reader.Read();
                    continue;
                }

                return;
            }
        }

        private static string trimTail(string value) => value.TrimEnd('\0', '\r', '\n').Trim();
    }
}
