// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    public sealed class DivaSongFolder
    {
        public required string FolderPath { get; init; }
        public required IReadOnlyList<string> ChartPaths { get; init; }
    }

    public static class DivaLibraryScanner
    {
        public static IReadOnlyList<DivaSongFolder> Scan(IEnumerable<string> rootPaths)
        {
            var songs = new List<DivaSongFolder>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string root in rootPaths)
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                    continue;

                collectSongs(root, songs, seen);
            }

            return songs;
        }

        private static void collectSongs(string directory, List<DivaSongFolder> songs, HashSet<string> seen)
        {
            string[] charts;

            try
            {
                charts = Directory.GetFiles(directory, "*.diva", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return;
            }

            if (charts.Length > 0)
            {
                string full = Path.GetFullPath(directory);

                if (seen.Add(full))
                {
                    songs.Add(new DivaSongFolder
                    {
                        FolderPath = full,
                        ChartPaths = charts.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToArray()
                    });
                }

                return;
            }

            string[] children;

            try
            {
                children = Directory.GetDirectories(directory);
            }
            catch
            {
                return;
            }

            foreach (string child in children)
                collectSongs(child, songs, seen);
        }
    }

    public static class DivaSongSetPackager
    {
        public static string PackageSong(DivaSongFolder song, string stagingRoot)
        {
            string setId = StableIdFromPath(song.FolderPath);
            string outputDir = Path.Combine(stagingRoot, setId);

            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);

            Directory.CreateDirectory(outputDir);

            var copiedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string chartPath in song.ChartPaths)
            {
                DivaChart chart = DivaChartFileParser.Parse(chartPath);
                string safeDifficulty = sanitiseFileName(DivaChartConstants.LEVEL_NAMES[Math.Clamp(chart.Metadata.Level - 1, 0, 4)]);
                string osuName = $"{sanitiseFileName(chart.Metadata.Title)} [{safeDifficulty}].osu";
                string osuPath = Path.Combine(outputDir, osuName);
                DivaToOsuExporter.WriteToFile(chart, osuPath);

                copyRelative(song.FolderPath, chart.ResolvePrimaryAudioRelativePath(), outputDir, copiedFiles);
                copyRelative(song.FolderPath, chart.ResolveBackgroundRelativePath(), outputDir, copiedFiles);

                // Keep referenced wav/resource files when present so audio lookup can succeed.
                foreach (string relative in chart.WavFiles.Values.Concat(chart.ResourceFiles.Values))
                    copyRelative(song.FolderPath, relative, outputDir, copiedFiles);
            }

            // Marker for re-import / dedupe identity.
            File.WriteAllText(Path.Combine(outputDir, "diva-source.txt"), song.FolderPath, Encoding.UTF8);
            return outputDir;
        }

        public static string StableIdFromPath(string path)
        {
            byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToLowerInvariant()));
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }

        private static void copyRelative(string songFolder, string? relative, string outputDir, HashSet<string> copied)
        {
            if (string.IsNullOrWhiteSpace(relative))
                return;

            string source = Path.IsPathRooted(relative)
                ? relative
                : Path.Combine(songFolder, relative.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(source))
                return;

            string fileName = Path.GetFileName(source);
            if (!copied.Add(fileName))
                return;

            File.Copy(source, Path.Combine(outputDir, fileName), true);
        }

        private static string sanitiseFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "chart" : name.Trim();
        }
    }
}
