// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Diva.Difficulty
{
    /// <summary>
    /// Baseline PP for the osu!-projected DIVA map (same objects as <see cref="DivaDifficulty"/>).
    /// Maps DIVA judgements onto osu hit results so <see cref="OsuPerformanceCalculator"/> can run.
    /// </summary>
    public class DivaPerformanceCalculator : PerformanceCalculator
    {
        private readonly OsuPerformanceCalculator osuCalculator = new OsuPerformanceCalculator();

        public DivaPerformanceCalculator()
            : base(new DivaRuleset())
        {
        }

        protected override PerformanceAttributes CreatePerformanceAttributes(ScoreInfo score, DifficultyAttributes attributes)
        {
            if (attributes is not OsuDifficultyAttributes)
                return new PerformanceAttributes();

            return osuCalculator.Calculate(createOsuCompatibleScore(score), attributes);
        }

        private static ScoreInfo createOsuCompatibleScore(ScoreInfo score)
        {
            var clone = score.DeepClone();
            clone.Statistics = mapHitStatistics(score.Statistics);
            clone.MaximumStatistics = mapHitStatistics(score.MaximumStatistics);
            return clone;
        }

        /// <summary>
        /// DIVA: Perfect/Great/Good/Ok/Meh/Miss → osu circle: Great/Ok/Meh/Miss.
        /// </summary>
        private static Dictionary<HitResult, int> mapHitStatistics(IReadOnlyDictionary<HitResult, int> source)
        {
            int perfect = get(source, HitResult.Perfect);
            int great = get(source, HitResult.Great);
            int good = get(source, HitResult.Good);
            int ok = get(source, HitResult.Ok);
            int meh = get(source, HitResult.Meh);
            int miss = get(source, HitResult.Miss);

            return new Dictionary<HitResult, int>
            {
                [HitResult.Great] = perfect + great,
                [HitResult.Ok] = good,
                [HitResult.Meh] = ok + meh,
                [HitResult.Miss] = miss,
            };
        }

        private static int get(IReadOnlyDictionary<HitResult, int> source, HitResult result)
            => source.GetValueOrDefault(result, 0);
    }
}
