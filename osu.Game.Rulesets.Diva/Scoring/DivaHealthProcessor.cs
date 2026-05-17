// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Scoring
{
    public partial class DivaHealthProcessor : LegacyDrainingHealthProcessor
    {
        private readonly Dictionary<double, DivaJudgementResult.DivaMehSource> specialMehResults = new Dictionary<double, DivaJudgementResult.DivaMehSource>();

        public DivaHealthProcessor(double drainStartTime)
            : base(drainStartTime)
        {
        }

        protected override double ComputeDrainRate()
        {
            return 0;
        }

        protected override bool CheckDefaultFailCondition(JudgementResult result)
        {
            if (result is DivaJudgementResult { Type: HitResult.Meh } divaResult)
                specialMehResults[result.HitObject.StartTime] = divaResult.SpecialMehSource;

            return false;
        }

        public override void ApplyBeatmap(IBeatmap beatmap)
        {
            base.ApplyBeatmap(beatmap);
            specialMehResults.Clear();
        }

        protected override IEnumerable<HitObject> EnumerateTopLevelHitObjects() => Beatmap.HitObjects;

        protected override IEnumerable<HitObject> EnumerateNestedHitObjects(HitObject hitObject) => hitObject.NestedHitObjects;

        protected override double GetHealthIncreaseFor(HitObject hitObject, HitResult result)
        {
            switch (result)
            {
                case HitResult.Miss:
                    switch (hitObject)
                    {
                        default:
                            return -0.08;
                    }

                // sad
                case HitResult.Ok:
                    return -0.0625; // 1/16

                case HitResult.Great:
                    return 0.01;

                case HitResult.Perfect:
                    return 0.02;

                case HitResult.Meh:
                    if (specialMehResults.TryGetValue(hitObject.StartTime, out var mehSource))
                        return -0.08 * getMehDamageMultiplier(mehSource);

                    return -0.08;

                default:
                    return 0;
            }
        }

        private static double getMehDamageMultiplier(DivaJudgementResult.DivaMehSource mehSource)
        {
            switch (mehSource)
            {
                case DivaJudgementResult.DivaMehSource.PerfectWindowWrongPress:
                    return 0.25;

                case DivaJudgementResult.DivaMehSource.GreatWindowWrongPress:
                    return 0.5;

                case DivaJudgementResult.DivaMehSource.GoodWindowWrongPress:
                    return 0.75;

                case DivaJudgementResult.DivaMehSource.OkWindowWrongPress:
                    return 1.0;

                default:
                    return 1.0;
            }
        }
    }
}
