// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Scoring
{
    public partial class DivaScoreProcessor : ScoreProcessor
    {
        private const double combo_base = 4;

        public DivaScoreProcessor()
            : base(new DivaRuleset())
        {
        }

        protected override double ComputeTotalScore(double comboProgress, double accuracyProgress, double bonusPortion)
        {
            // return 1000000 * Accuracy.Value;

            return 150000 * comboProgress
                   + 850000 * Math.Pow(Accuracy.Value, 2 + 2 * Accuracy.Value) * accuracyProgress
                   + bonusPortion;
        }

        public override int GetBaseScoreForResult(HitResult result)
        {
            switch (result)
            {
                case HitResult.Perfect:
                    return 320;

                case HitResult.Great:
                    return 300;

                case HitResult.Good:
                    return 0;

                case HitResult.Ok:
                    return 0;

                case HitResult.Meh:
                    return 0;

                case HitResult.Miss:
                    return 0;
            }

            return base.GetBaseScoreForResult(result);
        }

        protected override double GetComboScoreChange(JudgementResult result)
        {
            return getBaseComboScoreForResult(result.Type) * Math.Min(Math.Max(0.5, Math.Log(result.ComboAfterJudgement, combo_base)), Math.Log(400, combo_base));
        }

        private int getBaseComboScoreForResult(HitResult result)
        {
            switch (result)
            {
                case HitResult.Perfect:
                    return 300;

                case HitResult.Great:
                    return 200;

                case HitResult.Good:
                case HitResult.Ok:
                case HitResult.Meh:
                    return 0;
            }

            return GetBaseScoreForResult(result);
        }

        // protected override IEnumerable<HitObject> EnumerateHitObjects(IBeatmap beatmap)
        // {
        //     return base.EnumerateHitObjects(beatmap).Order(JudgementOrderComparer.DEFAULT);
        // }

        protected override JudgementResult CreateResult(HitObject hitObject, Judgement judgement) => new DivaJudgementResult((DivaHitObject)hitObject, judgement);
    }
}
