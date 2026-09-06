// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Diva.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Diva.Difficulty.Skills
{
    /// <summary>Hit density and simultaneous presses (no aim).</summary>
    public class DivaSpeed : StrainDecaySkill
    {
        protected override double SkillMultiplier => 0.6;
        protected override double StrainDecayBase => 0.3;

        public DivaSpeed(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var diva = (DivaDifficultyHitObject)current;

            if (current.Index == 0)
                return 0;

            double delta = Math.Max(diva.DeltaTime, 35);
            double rateLoad = Math.Min(Math.Sqrt(200.0 / delta), 2.5);
            double pressLoad = diva.PressCount == 0 ? 0 : 1 + 0.7 * (diva.PressCount - 1);
            double releaseLoad = 0.55 * diva.ReleaseCount;

            return rateLoad * Math.Max(pressLoad + releaseLoad, 0.35);
        }
    }
}
