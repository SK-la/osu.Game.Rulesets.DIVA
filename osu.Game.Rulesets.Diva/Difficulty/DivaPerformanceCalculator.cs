// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Diva.Difficulty
{
    /// <summary>
    /// Simple baseline PP from DIVA star rating and DIVA judgement statistics
    /// (no osu!standard performance mapping).
    /// </summary>
    public class DivaPerformanceCalculator : PerformanceCalculator
    {
        private static readonly HitResult[] judgement_results =
        [
            HitResult.Perfect,
            HitResult.Great,
            HitResult.Good,
            HitResult.Ok,
            HitResult.Meh,
            HitResult.Miss
        ];

        public DivaPerformanceCalculator()
            : base(new DivaRuleset())
        {
        }

        protected override PerformanceAttributes CreatePerformanceAttributes(ScoreInfo score, DifficultyAttributes attributes)
        {
            if (!score.Passed)
                return new PerformanceAttributes();

            double starRating = Math.Max(attributes.StarRating, 0);
            int observedJudgements = countJudgements(score.Statistics);
            int expectedJudgements = countJudgements(score.MaximumStatistics);

            if (expectedJudgements <= 0)
                expectedJudgements = Math.Max(attributes.MaxCombo, observedJudgements);

            expectedJudgements = Math.Max(expectedJudgements, 1);

            int unplayed = Math.Max(expectedJudgements - observedJudgements, 0);
            int missCount = get(score.Statistics, HitResult.Miss);
            int effectiveMissCount = Math.Min(missCount + unplayed, expectedJudgements);
            int maxCombo = Math.Max(attributes.MaxCombo, 1);
            int combo = Math.Clamp(score.MaxCombo, 0, maxCombo);

            double completion = Math.Clamp((double)observedJudgements / expectedJudgements, 0, 1);
            double judgementQuality = computeJudgementQuality(score);
            double accuracy = Math.Min(Math.Clamp(score.Accuracy, 0, 1), judgementQuality);
            double missRate = (double)effectiveMissCount / expectedJudgements;

            double basePp = Math.Pow(starRating, 2.2) * 9.0;
            double accuracyFactor = Math.Pow(accuracy, 3.2);
            double missFactor = Math.Pow(Math.Max(1 - missRate, 0), 1.5)
                                * (0.92 + 0.08 * Math.Exp(-effectiveMissCount / 5.0));
            double comboFactor = Math.Pow((double)combo / maxCombo, 0.8);
            double completionFactor = Math.Pow(completion, 1.5);
            double lengthBonus = 1 + 0.08 * Math.Min(Math.Sqrt(expectedJudgements / 1000.0), 1);

            double total = basePp * accuracyFactor * missFactor * comboFactor * completionFactor * lengthBonus;

            if (!double.IsFinite(total) || total < 0)
                total = 0;

            return new PerformanceAttributes { Total = total };
        }

        private static double computeJudgementQuality(ScoreInfo score)
        {
            int perfect = get(score.Statistics, HitResult.Perfect);
            int great = get(score.Statistics, HitResult.Great);
            int good = get(score.Statistics, HitResult.Good);
            int ok = get(score.Statistics, HitResult.Ok);
            int meh = get(score.Statistics, HitResult.Meh);
            int miss = get(score.Statistics, HitResult.Miss);

            int total = perfect + great + good + ok + meh + miss;

            if (total <= 0)
                return 0;

            double weighted = perfect * 1.0 + great * 0.9 + good * 0.45 + ok * 0.25 + meh * 0.1;
            return Math.Clamp(weighted / total, 0, 1);
        }

        private static int get(IReadOnlyDictionary<HitResult, int> source, HitResult result)
            => source.GetValueOrDefault(result, 0);

        private static int countJudgements(IReadOnlyDictionary<HitResult, int> source)
        {
            int total = 0;

            foreach (HitResult result in judgement_results)
                total += get(source, result);

            return total;
        }
    }
}
