// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Diva.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Diva.Difficulty.Skills
{
    /// <summary>Minimum key-switch cost between consecutive logical input events.</summary>
    public class DivaPattern : StrainDecaySkill
    {
        protected override double SkillMultiplier => 0.6;
        protected override double StrainDecayBase => 0.35;

        public DivaPattern(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (current.Index == 0 || current.Previous(0) is not DivaDifficultyHitObject previous)
                return 0;

            var diva = (DivaDifficultyHitObject)current;
            double minimum = double.MaxValue;

            foreach (int previousOption in previous.InputOptions)
            {
                foreach (int currentOption in diva.InputOptions)
                    minimum = Math.Min(minimum, maskTransitionCost(previousOption, currentOption));
            }

            double chordLoad = 0.25 * Math.Max(diva.PressCount - 1, 0);
            double releaseConflict = diva.HasReleaseConflict ? 0.35 : 0;
            return (minimum == double.MaxValue ? 0 : minimum) + chordLoad + releaseConflict;
        }

        private static double maskTransitionCost(int previousMask, int currentMask)
        {
            if (previousMask == 0 || currentMask == 0)
                return 0;

            double total = 0;
            int currentCount = 0;

            for (int currentAction = 0; currentAction < 4; currentAction++)
            {
                if ((currentMask & (1 << currentAction)) == 0)
                    continue;

                double best = double.MaxValue;

                for (int previousAction = 0; previousAction < 4; previousAction++)
                {
                    if ((previousMask & (1 << previousAction)) != 0)
                        best = Math.Min(best, actionPairCost(previousAction, currentAction));
                }

                total += best;
                currentCount++;
            }

            return currentCount == 0 ? 0 : total / currentCount;
        }

        private static double actionPairCost(int from, int to)
        {
            if (from == to)
                return 0.05;

            return isOpposite(from, to) ? 1.0 : 0.55;
        }

        private static bool isOpposite(int a, int b) => ((DivaLogicalAction)a, (DivaLogicalAction)b) switch
        {
            (DivaLogicalAction.Square, DivaLogicalAction.Circle) or (DivaLogicalAction.Circle, DivaLogicalAction.Square) => true,
            (DivaLogicalAction.Triangle, DivaLogicalAction.Cross) or (DivaLogicalAction.Cross, DivaLogicalAction.Triangle) => true,
            _ => false
        };
    }
}
