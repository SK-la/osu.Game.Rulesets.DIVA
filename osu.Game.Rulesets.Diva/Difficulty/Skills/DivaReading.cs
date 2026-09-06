// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Diva.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Diva.Difficulty.Skills
{
    /// <summary>
    /// Approach / 飞入 reading: concurrent approaching notes and direction spread.
    /// Does not use hit-position spacing as aim.
    /// </summary>
    public class DivaReading : StrainDecaySkill
    {
        protected override double SkillMultiplier => 0.5;
        protected override double StrainDecayBase => 0.35;

        public DivaReading(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var diva = (DivaDifficultyHitObject)current;
            // A handful of simultaneous approach pieces is normal DIVA presentation.
            // Only clutter beyond that baseline contributes reading strain.
            double density = Math.Log(1 + Math.Max(diva.ApproachDensity - 6, 0));
            double directionSpread = Math.Max(diva.ApproachDirectionSpread - 1, 0);
            return density * (1 + 0.08 * directionSpread);
        }
    }
}
