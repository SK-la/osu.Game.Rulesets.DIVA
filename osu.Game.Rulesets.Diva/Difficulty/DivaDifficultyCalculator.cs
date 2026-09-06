// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Diva.Difficulty
{
    public partial class DivaDifficultyCalculator : DifficultyCalculator
    {
        public DivaDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills)
            => new DifficultyAttributes(mods, ResolveNativeStarRating(beatmap.BeatmapInfo));

        /// <summary>
        /// ProjectDIVA has no algorithmic SR — panel stars should match chart <c>Hard</c>
        /// (stored as OD, and as <c>★N</c> in <see cref="IBeatmapInfo.DifficultyName"/>).
        /// </summary>
        public static double ResolveNativeStarRating(IBeatmapInfo beatmapInfo)
        {
            if (tryParseBlackStar(beatmapInfo.DifficultyName, out double fromName))
                return fromName;

            double od = beatmapInfo.Difficulty.OverallDifficulty;
            return od > 0 ? od : 0;
        }

        private static bool tryParseBlackStar(string? difficultyName, out double stars)
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

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, Mod[] mods) => Enumerable.Empty<DifficultyHitObject>();

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods) => [];
    }
}
