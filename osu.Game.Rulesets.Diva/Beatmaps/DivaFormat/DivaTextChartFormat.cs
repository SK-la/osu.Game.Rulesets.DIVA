// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    public sealed class DivaTextChartFormat : IDivaChartFormat
    {
        public static DivaTextChartFormat Instance { get; } = new DivaTextChartFormat();

        public string FormatName => ".diva";

        public bool CanHandle(string path, Stream? peekStream = null)
        {
            if (path.EndsWith(".diva", StringComparison.OrdinalIgnoreCase))
                return true;

            if (peekStream == null)
                return false;

            using var reader = new StreamReader(peekStream, leaveOpen: true);
            string? first = reader.ReadLine()?.Trim();
            return !string.IsNullOrEmpty(first) && first.StartsWith("1.", StringComparison.Ordinal);
        }

        public DivaChartMetadata ReadMetadata(string path) => DivaChartFileParser.ReadMetadata(path);

        public DivaChart Decode(string path) => DivaChartFileParser.Parse(path);
    }

    public static class DivaChartFormatRegistry
    {
        public static IReadOnlyList<IDivaChartFormat> All => formats;
        private static readonly IDivaChartFormat[] formats = [DivaTextChartFormat.Instance];

        public static IDivaChartFormat? FindForPath(string path) => formats.FirstOrDefault(f => f.CanHandle(path));
    }
}
