// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using osu.Game.Beatmaps;

namespace osu.Game.Rulesets.Diva.Difficulty
{
    /// <summary>
    /// Resolves ProjectDIVA chart <c>Hard</c> (black stars) for Realm persistence / carousel sort.
    /// Panel display uses <see cref="DivaDifficultyCalculator"/> algorithmic stars via the difficulty cache instead.
    /// </summary>
    public static class DivaNativeStarRating
    {
        /// <summary>
        /// Star rating written to <see cref="IBeatmapInfo.StarRating"/> on decode / external sync.
        /// </summary>
        public static double FromHard(int hard) => hard > 0 ? hard : 0;

        /// <summary>
        /// Prefer <c>★N</c> in <see cref="IBeatmapInfo.DifficultyName"/>; fall back to OverallDifficulty.
        /// </summary>
        public static double Resolve(IBeatmapInfo beatmapInfo)
        {
            if (TryParseBlackStar(beatmapInfo.DifficultyName, out double fromName))
                return fromName;

            double od = beatmapInfo.Difficulty.OverallDifficulty;
            return od > 0 ? od : 0;
        }

        public static bool TryParseBlackStar(string? difficultyName, out double stars)
        {
            stars = 0;

            if (string.IsNullOrEmpty(difficultyName) || difficultyName[0] != '★')
                return false;

            int end = 1;

            while (end < difficultyName.Length && char.IsDigit(difficultyName[end]))
                end++;

            if (end == 1)
                return false;

            return double.TryParse(difficultyName.AsSpan(1, end - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out stars)
                   && stars > 0;
        }
    }
}
