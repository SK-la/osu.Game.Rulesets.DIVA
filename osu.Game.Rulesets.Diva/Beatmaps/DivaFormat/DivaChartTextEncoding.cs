// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    /// <summary>
    /// ProjectDIVA <c>.diva</c> charts are typically saved in the system ANSI code page
    /// (see original <c>Ansi2UTF8(CP_ACP)</c>). On modern .NET, <see cref="Encoding.Default"/> is UTF-8,
    /// so reading with Default corrupts CJK filenames (spaces are fine; the bytes were mis-decoded).
    /// </summary>
    public static class DivaChartTextEncoding
    {
        private static readonly object provider_lock = new object();
        private static bool providerRegistered;
        private static int systemAnsiCodePage = 1252;

        private static readonly Regex referenced_file_pattern = new Regex(
            @"^\s*\d+\s+(.+\.(?:mp3|ogg|wav|flac|jpg|jpeg|png|bmp|mpg|mpeg|avi|mp4|wmv))\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        static DivaChartTextEncoding() => ensureCodePages();

        public static string DecodeFile(string path)
        {
            ensureCodePages();

            byte[] bytes = File.ReadAllBytes(path);
            string songFolder = Path.GetDirectoryName(path) ?? string.Empty;

            Encoding? best = null;
            string? bestText = null;
            int bestScore = int.MinValue;

            foreach (Encoding encoding in getCandidates(bytes))
            {
                string text;

                try
                {
                    text = encoding.GetString(bytes);
                }
                catch
                {
                    continue;
                }

                int score = scoreDecodedText(text, songFolder, bytes, encoding);

                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = encoding;
                bestText = text;
            }

            return bestText ?? Encoding.UTF8.GetString(bytes);
        }

        public static TextReader OpenReader(string path) => new StringReader(DecodeFile(path));

        /// <summary>
        /// Detects the best encoding for a .diva file (same scoring as <see cref="DecodeFile"/>).
        /// </summary>
        public static Encoding DetectBestEncoding(string path)
        {
            ensureCodePages();

            byte[] bytes = File.ReadAllBytes(path);
            string songFolder = Path.GetDirectoryName(path) ?? string.Empty;

            Encoding? best = null;
            int bestScore = int.MinValue;

            foreach (Encoding encoding in getCandidates(bytes))
            {
                string text;

                try
                {
                    text = encoding.GetString(bytes);
                }
                catch
                {
                    continue;
                }

                int score = scoreDecodedText(text, songFolder, bytes, encoding);

                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = encoding;
            }

            return best ?? Encoding.UTF8;
        }

        public static void WriteFile(string path, string text, Encoding encoding)
        {
            ensureCodePages();
            byte[] bytes = encoding.GetBytes(text);
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// Resolves a chart-relative path against <paramref name="contentRoot"/>, tolerating
        /// separator differences and falling back to a unique same-extension filename match.
        /// </summary>
        public static string? ResolveExistingRelativePath(string contentRoot, string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(contentRoot) || string.IsNullOrWhiteSpace(relativePath))
                return null;

            string normalisedRelative = relativePath.Replace('\\', '/').Trim();
            string full = Path.GetFullPath(Path.Combine(contentRoot, normalisedRelative.Replace('/', Path.DirectorySeparatorChar)));

            if (File.Exists(full))
                return Path.GetRelativePath(contentRoot, full).Replace('\\', '/');

            string fileName = Path.GetFileName(normalisedRelative);
            string direct = Path.Combine(contentRoot, fileName);

            if (File.Exists(direct))
                return fileName;

            // ProjectDIVA layout: media often lives under RES/ or WAV/ while charts store bare names.
            foreach (string folder in new[] { "RES", "res", "WAV", "wav" })
            {
                string nested = Path.Combine(contentRoot, folder, fileName);

                if (File.Exists(nested))
                    return Path.Combine(folder, fileName).Replace('\\', '/');
            }

            string extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension) || !Directory.Exists(contentRoot))
                return null;

            string asciiHint = extractAsciiHint(fileName);
            string[] candidates;

            try
            {
                candidates = Directory.GetFiles(contentRoot, "*" + extension, SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return null;
            }

            IEnumerable<string> matches = candidates;

            if (!string.IsNullOrEmpty(asciiHint))
            {
                string[] hinted = candidates
                    .Where(f => Path.GetFileName(f).Contains(asciiHint, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (hinted.Length > 0)
                    matches = hinted;
            }

            string[] matchList = matches.ToArray();

            if (matchList.Length == 1)
                return Path.GetFileName(matchList[0]);

            return null;
        }

        private static void ensureCodePages()
        {
            lock (provider_lock)
            {
                if (providerRegistered)
                    return;
            }

            lock (provider_lock)
            {
                if (providerRegistered)
                    return;

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                try
                {
                    systemAnsiCodePage = Encoding.GetEncoding(0).CodePage;
                }
                catch
                {
                    systemAnsiCodePage = 1252;
                }

                providerRegistered = true;
            }
        }

        private static IEnumerable<Encoding> getCandidates(byte[] bytes)
        {
            // UTF-8 with BOM is authoritative when present.
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                yield return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
                yield break;
            }

            // Prefer system ANSI (CP_ACP) — matches ProjectDIVA Ansi2UTF8.
            Encoding? ansi = tryGetEncoding(0);
            if (ansi != null)
                yield return ansi;

            // Common community encodings for CJK charts.
            Encoding? gbk = tryGetEncoding(936);
            if (gbk != null)
                yield return gbk;

            Encoding? shiftJis = tryGetEncoding(932);
            if (shiftJis != null)
                yield return shiftJis;

            yield return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
        }

        private static Encoding? tryGetEncoding(int codePage)
        {
            try
            {
                return Encoding.GetEncoding(codePage);
            }
            catch
            {
                return null;
            }
        }

        private static int scoreDecodedText(string text, string songFolder, byte[] bytes, Encoding encoding)
        {
            int score = 0;

            if (!text.Contains('\uFFFD'))
                score += 20;

            // Valid UTF-8 payload (no invalid sequences) gets a small bonus when not ANSI.
            if (encoding.CodePage is 65001 or 1200)
            {
                try
                {
                    _ = new UTF8Encoding(false, true).GetString(bytes);
                    score += 5;
                }
                catch
                {
                    score -= 40;
                }
            }

            if (encoding.CodePage == systemAnsiCodePage)
                score += 15; // prefer ACP when tied — original game behaviour

            if (string.IsNullOrEmpty(songFolder) || !Directory.Exists(songFolder))
                return score;

            int existing = 0;
            int referenced = 0;

            using var reader = new StringReader(text);

            // Skip header lines until we can harvest path-looking references cheaply via regex on whole text.
            foreach (Match match in referenced_file_pattern.Matches(text))
            {
                referenced++;
                string relative = match.Groups[1].Value.Trim().TrimEnd('\0', '\r', '\n');
                string? resolved = ResolveExistingRelativePath(songFolder, relative);

                if (resolved != null)
                    existing++;
            }

            // Also check overview picture (6th text line).
            string? overview = tryReadOverviewRelative(text);

            if (!string.IsNullOrWhiteSpace(overview))
            {
                referenced++;

                if (ResolveExistingRelativePath(songFolder, overview) != null)
                    existing++;
            }

            if (referenced > 0)
                score += existing * 50 - (referenced - existing) * 10;

            return score;
        }

        private static string? tryReadOverviewRelative(string text)
        {
            using var reader = new StringReader(text);

            for (int i = 0; i < 6; i++)
            {
                string? line = reader.ReadLine();
                if (line == null)
                    return null;

                if (i == 5)
                    return line.Trim().TrimEnd('\0');
            }

            return null;
        }

        private static string extractAsciiHint(string fileName)
        {
            var sb = new StringBuilder();

            foreach (char c in Path.GetFileNameWithoutExtension(fileName))
            {
                if (c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                    sb.Append(c);
                else if (sb.Length >= 4)
                    break;
                else
                    sb.Clear();
            }

            return sb.Length >= 4 ? sb.ToString() : string.Empty;
        }
    }
}
