// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Diva.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Diva.Difficulty.Skills
{
    /// <summary>Input occupancy, overlap and release pressure from press-and-hold strips.</summary>
    public class DivaHold : StrainDecaySkill
    {
        protected override double SkillMultiplier => 0.5;
        protected override double StrainDecayBase => 0.4;

        public DivaHold(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var diva = (DivaDifficultyHitObject)current;
            double occupancy = 0.35 * Math.Log(1 + diva.ActiveHoldCount);
            double startingDuration = diva.StartingHoldCount == 0
                ? 0
                : 0.3 * diva.StartingHoldCount * Math.Min(Math.Log(1 + diva.LongestStartingHoldDuration / 250), 2);
            double release = 0.65 * diva.ReleaseCount;
            double conflict = diva.HasReleaseConflict ? 0.8 : 0;

            return occupancy + startingDuration + release + conflict;
        }
    }
}
