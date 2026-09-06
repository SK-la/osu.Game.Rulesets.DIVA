// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using osu.Framework.Logging;
using osu.Game.Rulesets.Diva.Localization;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    /// <summary>
    /// Manual tool: ensure each song folder has playable audio under <c>WAV/</c>
    /// (extract from <c>RES/</c> video when needed) and rewrite all <c>.diva</c> wav tables.
    /// </summary>
    public static class DivaAudioTrackSeparator
    {
        public readonly struct SeparationResult
        {
            public SeparationResult(int songsScanned, int songsUpdated, int chartsRewritten, int songsSkipped, string summary)
            {
                SongsScanned = songsScanned;
                SongsUpdated = songsUpdated;
                ChartsRewritten = chartsRewritten;
                SongsSkipped = songsSkipped;
                Summary = summary;
            }

            public int SongsScanned { get; }
            public int SongsUpdated { get; }
            public int ChartsRewritten { get; }
            public int SongsSkipped { get; }
            public string Summary { get; }
        }

        public static SeparationResult Separate(IReadOnlyList<string> libraryRoots, Action<string>? reportStatus = null)
        {
            IReadOnlyList<DivaSongFolder> songs = DivaLibraryScanner.Scan(libraryRoots);
            int updated = 0;
            int rewritten = 0;
            int skipped = 0;

            reportStatus?.Invoke(DivaStrings.Separate_Scanning(songs.Count));

            foreach (DivaSongFolder song in songs)
            {
                reportStatus?.Invoke(DivaStrings.Separate_Processing(Path.GetFileName(song.FolderPath)));

                try
                {
                    string? audioRelative = ensureSongAudio(song.FolderPath);

                    if (audioRelative == null)
                    {
                        skipped++;
                        Logger.Log($"[DIVA] Separate audio skipped (no WAV/RES audio): {song.FolderPath}", level: LogLevel.Important);
                        continue;
                    }

                    int chartCount = 0;

                    foreach (string chartPath in song.ChartPaths)
                    {
                        if (DivaChartWavRewriter.EnsurePrimaryWavAssociation(chartPath, audioRelative))
                            chartCount++;
                    }

                    if (chartCount > 0)
                    {
                        updated++;
                        rewritten += chartCount;
                    }
                    else
                    {
                        // Audio exists and charts already pointed at it.
                        updated++;
                    }
                }
                catch (Exception ex)
                {
                    skipped++;
                    Logger.Log($"[DIVA] Separate audio failed for '{song.FolderPath}': {ex.Message}", level: LogLevel.Important);
                }
            }

            string summary = DivaStrings.Separate_Summary(songs.Count, updated, rewritten, skipped);
            Logger.Log($"[DIVA] {summary}");
            reportStatus?.Invoke(summary);

            return new SeparationResult(songs.Count, updated, rewritten, skipped, summary);
        }

        private static string? ensureSongAudio(string songFolder)
        {
            string? wavDir = findChildDirectory(songFolder, DivaVideoAudioExtractor.WAV_FOLDER);
            string? existing = pickExistingAudio(wavDir);

            if (existing != null)
                return toChartRelative(songFolder, existing);

            string? resDir = findChildDirectory(songFolder, DivaVideoAudioExtractor.RES_FOLDER);
            string? video = pickPrimaryVideo(resDir);

            if (video == null)
                return null;

            return DivaVideoAudioExtractor.ExtractToWavFolder(songFolder, video);
        }

        private static string? pickExistingAudio(string? wavDir)
        {
            if (wavDir == null || !Directory.Exists(wavDir))
                return null;

            string[] files;

            try
            {
                files = Directory.GetFiles(wavDir);
            }
            catch
            {
                return null;
            }

            return files
                   .Where(DivaVideoAudioExtractor.IsAudioExtension)
                   .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                   .FirstOrDefault();
        }

        private static string? pickPrimaryVideo(string? resDir)
        {
            if (resDir == null || !Directory.Exists(resDir))
                return null;

            string[] files;

            try
            {
                files = Directory.GetFiles(resDir)
                                .Where(DivaVideoAudioExtractor.IsVideoExtension)
                                .ToArray();
            }
            catch
            {
                return null;
            }

            if (files.Length == 0)
                return null;

            // Prefer common BGM/PV names, else largest file.
            string[] preferred =
            [
                "pv", "bgm", "movie", "video", "op", "main"
            ];

            foreach (string hint in preferred)
            {
                string? match = files.FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).Contains(hint, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return match;
            }

            return files.OrderByDescending(f => new FileInfo(f).Length).First();
        }

        private static string? findChildDirectory(string parent, string name)
        {
            if (!Directory.Exists(parent))
                return null;

            try
            {
                return Directory.GetDirectories(parent)
                                .FirstOrDefault(d => string.Equals(Path.GetFileName(d), name, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private static string toChartRelative(string songFolder, string fullPath)
        {
            string relative = Path.GetRelativePath(songFolder, fullPath);
            return relative.Replace('/', '\\');
        }
    }

    /// <summary>
    /// Rewrites the wav definition block of a <c>.diva</c> chart so id 0 points at the given audio path.
    /// </summary>
    public static class DivaChartWavRewriter
    {
        /// <returns>True if the file was modified.</returns>
        public static bool EnsurePrimaryWavAssociation(string chartPath, string wavRelativeBackslash)
        {
            Encoding encoding = DivaChartTextEncoding.DetectBestEncoding(chartPath);
            byte[] bytes = File.ReadAllBytes(chartPath);
            string text = encoding.GetString(bytes);

            string normalisedTarget = wavRelativeBackslash.Replace('/', '\\');

            if (!tryLocateWavSection(text, out int wavStart, out int wavEnd, out Dictionary<int, string> existing))
                throw new InvalidDataException($"Could not locate wav section in '{chartPath}'.");

            bool alreadyOk = existing.Any(kvp =>
                string.Equals(kvp.Value.Replace('/', '\\'), normalisedTarget, StringComparison.OrdinalIgnoreCase)
                && DivaVideoAudioExtractor.IsAudioExtension(kvp.Value));

            if (alreadyOk && existing.ContainsKey(0)
                          && string.Equals(existing[0].Replace('/', '\\'), normalisedTarget, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var merged = new SortedDictionary<int, string>(existing);

            // Drop entries that already point at the same file under another id.
            foreach (int id in merged.Keys.ToArray())
            {
                if (id == 0)
                    continue;

                if (string.Equals(merged[id].Replace('/', '\\'), normalisedTarget, StringComparison.OrdinalIgnoreCase))
                    merged.Remove(id);
            }

            merged[0] = normalisedTarget;

            var sb = new StringBuilder();
            sb.Append(text.AsSpan(0, wavStart));

            foreach ((int id, string file) in merged)
            {
                sb.Append(id.ToString(CultureInfo.InvariantCulture));
                sb.Append(' ');
                sb.Append(file.Replace('/', '\\'));
                sb.Append('\n');
            }

            sb.Append("-1\n");
            sb.Append(text.AsSpan(wavEnd));

            string rewritten = sb.ToString();
            if (string.Equals(rewritten, text, StringComparison.Ordinal))
                return false;

            DivaChartTextEncoding.WriteFile(chartPath, rewritten, encoding);
            Logger.Log($"[DIVA] Updated wav association in {Path.GetFileName(chartPath)} → {normalisedTarget}");
            return true;
        }

        private static bool tryLocateWavSection(string text, out int wavStart, out int wavEnd, out Dictionary<int, string> existing)
        {
            wavStart = 0;
            wavEnd = 0;
            existing = new Dictionary<int, string>();

            var reader = new TrackingStringReader(text);

            try
            {
                // Header
                for (int i = 0; i < 6; i++)
                    readLineRequired(reader);

                _ = readIntLine(reader); // level
                _ = readIntLine(reader); // hard
                _ = readDoubleLine(reader); // bpm
                _ = readIntLine(reader); // period count

                skipIntThenDoubleUntilMinusOne(reader); // bpm events
                skipTwoIntsUntilMinusOne(reader); // resource events
                skipThreeIntsUntilMinusOne(reader); // BGS
                skipNotesUntilMinusOne(reader);

                wavStart = reader.Position;
                existing = readWavEntries(reader);
                wavEnd = reader.Position;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[DIVA] Wav section locate failed: {ex.Message}", level: LogLevel.Important);
                return false;
            }
        }

        private static Dictionary<int, string> readWavEntries(TrackingStringReader reader)
        {
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

            return wav;
        }

        private static void skipIntThenDoubleUntilMinusOne(TrackingStringReader reader)
        {
            while (true)
            {
                if (!tryReadIntToken(reader, out int pos))
                    break;
                if (pos == -1)
                    break;

                _ = readDoubleToken(reader);
            }
        }

        private static void skipTwoIntsUntilMinusOne(TrackingStringReader reader)
        {
            while (true)
            {
                if (!tryReadIntToken(reader, out int pos))
                    break;
                if (pos == -1)
                    break;

                _ = readIntToken(reader);
            }
        }

        private static void skipThreeIntsUntilMinusOne(TrackingStringReader reader)
        {
            while (true)
            {
                if (!tryReadIntToken(reader, out int pos))
                    break;
                if (pos == -1)
                    break;

                _ = readIntToken(reader);
                _ = readIntToken(reader);
            }
        }

        private static void skipNotesUntilMinusOne(TrackingStringReader reader)
        {
            while (true)
            {
                if (!tryReadIntToken(reader, out int pos))
                    break;
                if (pos == -1)
                    break;

                int type = readIntToken(reader);
                _ = readIntToken(reader);
                _ = readIntToken(reader);
                _ = readIntToken(reader);
                _ = readIntToken(reader);
                _ = readIntToken(reader);

                if (type >= DivaChartConstants.NOTE_TYPE_COUNT)
                    _ = readDoubleToken(reader);
            }
        }

        private static string readLineRequired(TrackingStringReader reader)
        {
            string? line = reader.ReadLine();
            if (line == null)
                throw new InvalidDataException("Unexpected end of .diva file.");

            return line;
        }

        private static int readIntLine(TrackingStringReader reader)
            => int.Parse(readLineRequired(reader).Trim(), CultureInfo.InvariantCulture);

        private static double readDoubleLine(TrackingStringReader reader)
            => double.Parse(readLineRequired(reader).Trim(), CultureInfo.InvariantCulture);

        private static bool tryReadIntToken(TrackingStringReader reader, out int value)
        {
            value = 0;
            skipSpaces(reader);
            int ch = reader.Peek();
            if (ch < 0)
                return false;

            var sb = new StringBuilder();

            if (ch is '-' or '+')
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

        private static int readIntToken(TrackingStringReader reader)
        {
            if (!tryReadIntToken(reader, out int value))
                throw new InvalidDataException("Expected integer token in .diva file.");

            return value;
        }

        private static double readDoubleToken(TrackingStringReader reader)
        {
            skipSpaces(reader);
            var sb = new StringBuilder();
            int ch = reader.Peek();

            if (ch is '-' or '+')
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

        private static string readRestOfLine(TrackingStringReader reader)
        {
            skipSpaces(reader);
            string? line = reader.ReadLine();
            return line == null ? string.Empty : line.TrimEnd('\0', '\r', '\n').Trim();
        }

        private static void skipSpaces(TrackingStringReader reader)
        {
            while (true)
            {
                int ch = reader.Peek();
                if (ch < 0)
                    return;

                char c = (char)ch;

                if (c is ' ' or '\t' or '\r' or '\n')
                {
                    reader.Read();
                    continue;
                }

                return;
            }
        }

        private sealed class TrackingStringReader
        {
            private readonly string source;
            private int position;

            public TrackingStringReader(string source)
            {
                this.source = source;
            }

            public int Position => position;

            public int Peek() => position >= source.Length ? -1 : source[position];

            public int Read()
            {
                if (position >= source.Length)
                    return -1;

                return source[position++];
            }

            public string? ReadLine()
            {
                if (position >= source.Length)
                    return null;

                int start = position;

                while (position < source.Length)
                {
                    char c = source[position];

                    if (c == '\r')
                    {
                        string line = source[start..position];
                        position++;

                        if (position < source.Length && source[position] == '\n')
                            position++;

                        return line;
                    }

                    if (c == '\n')
                    {
                        string line = source[start..position];
                        position++;
                        return line;
                    }

                    position++;
                }

                return source[start..position];
            }
        }
    }
}
