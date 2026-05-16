// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Scoring
{
    public partial class DivaHealthProcessor : LegacyDrainingHealthProcessor
    {
        public DivaHealthProcessor(double drainStartTime)
            : base(drainStartTime)
        {
        }

        protected override double ComputeDrainRate()
        {
            return 0;
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

                case HitResult.Good:
                    return 0.005;

                case HitResult.Great:
                    return 0.01;

                case HitResult.Perfect:
                    return 0.02;

                default:
                    return 0;
            }
        }
    }
}
