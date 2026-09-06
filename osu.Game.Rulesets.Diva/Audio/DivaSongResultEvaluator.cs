// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Audio
{
    /// <summary>
    /// Maps a finished play to ProjectDIVA <c>SONGRESULT_*</c> grades
    /// (<c>Gui.cpp</c> ScoreMenu thresholds).
    /// </summary>
    public static class DivaSongResultEvaluator
    {
        public static DivaSongResult Evaluate(
            bool failedOrNotCleared,
            long totalScore,
            long maximumTotalScore,
            IReadOnlyDictionary<HitResult, int> statistics)
        {
            if (failedOrNotCleared)
                return DivaSongResult.Mistake;

            if (isPerfectClear(statistics))
                return DivaSongResult.Perfect;

            double ratio = maximumTotalScore > 0 ? (double)totalScore / maximumTotalScore : 0;

            if (ratio >= DivaHitSampleInfo.RESULT_GREAT_RATIO)
                return DivaSongResult.Great;

            if (ratio >= DivaHitSampleInfo.RESULT_STANDARD_RATIO)
                return DivaSongResult.Standard;

            return DivaSongResult.Cheap;
        }

        /// <summary>
        /// ProjectDIVA Perfect: every note was COOL/FINE only (combo never broke).
        /// Mapped: only <see cref="HitResult.Perfect"/> / <see cref="HitResult.Great"/>.
        /// </summary>
        private static bool isPerfectClear(IReadOnlyDictionary<HitResult, int> statistics)
        {
            int coolFine = count(statistics, HitResult.Perfect) + count(statistics, HitResult.Great);
            int total = coolFine
                        + count(statistics, HitResult.Good)
                        + count(statistics, HitResult.Meh)
                        + count(statistics, HitResult.Miss);

            return total > 0 && coolFine == total;
        }

        private static int count(IReadOnlyDictionary<HitResult, int> statistics, HitResult result)
            => statistics.TryGetValue(result, out int c) ? c : 0;
    }
}
