// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Scoring
{
    public static class DivaHitJudgementEvaluator
    {
        public const double PERFECT_WINDOW = 32;
        public const double GREAT_WINDOW = 50;
        public const double GOOD_WINDOW = 80;
        public const double OK_WINDOW = 120;

        /// <summary>ProjectDIVA strip COOL window.</summary>
        public const double HOLD_PERFECT_WINDOW = 50;

        /// <summary>ProjectDIVA strip FINE window.</summary>
        public const double HOLD_GREAT_WINDOW = 100;

        /// <summary>ProjectDIVA strip SAFE window.</summary>
        public const double HOLD_GOOD_WINDOW = 200;

        /// <summary>ProjectDIVA strip SAD window.</summary>
        public const double HOLD_OK_WINDOW = 300;

        /// <summary>ProjectDIVA <c>gameini.delay</c> after Start/End before forced miss.</summary>
        public const double HOLD_MISS_TIMEOUT = 350;

        public static HitResult GetResultFor(double timeOffset)
        {
            var abs = Math.Abs(timeOffset);

            if (abs <= PERFECT_WINDOW) return HitResult.Perfect;
            if (abs <= GREAT_WINDOW) return HitResult.Great;
            if (abs <= GOOD_WINDOW) return HitResult.Good;
            if (abs <= OK_WINDOW) return HitResult.Ok;

            return HitResult.None;
        }

        /// <summary>Hold head/release timing mapped from ProjectDIVA COOL/FINE/SAFE/SAD.</summary>
        public static HitResult GetHoldResultFor(double timeOffset)
        {
            var abs = Math.Abs(timeOffset);

            if (abs <= HOLD_PERFECT_WINDOW) return HitResult.Perfect;
            if (abs <= HOLD_GREAT_WINDOW) return HitResult.Great;
            if (abs <= HOLD_GOOD_WINDOW) return HitResult.Good;
            if (abs <= HOLD_OK_WINDOW) return HitResult.Ok;

            return HitResult.None;
        }

        public static bool IsWithinOkWindow(double timeOffset) => Math.Abs(timeOffset) <= OK_WINDOW;

        public static bool ShouldMiss(double timeOffset) => timeOffset > OK_WINDOW;

        public static bool ShouldMissHold(double timeOffset) => timeOffset > HOLD_MISS_TIMEOUT;

        public static HitResult GetPressResult(bool validPress, double timeOffset)
        {
            var timingResult = GetResultFor(timeOffset);

            if (timingResult == HitResult.None)
                return HitResult.None;

            return validPress ? timingResult : HitResult.Meh;
        }

        public static HitResult GetHoldPressResult(bool validPress, double timeOffset)
        {
            var timingResult = GetHoldResultFor(timeOffset);

            if (timingResult == HitResult.None)
                return HitResult.None;

            return validPress ? timingResult : HitResult.Meh;
        }

        /// <summary>ProjectDIVA scores press and release separately; we keep the worse of both.</summary>
        public static HitResult CombineHoldResults(HitResult head, HitResult release)
            => (HitResult)Math.Min((int)head, (int)release);

        public static DivaJudgementResult.DivaMehSource GetMehSourceFor(HitResult result) => result switch
        {
            HitResult.Perfect => DivaJudgementResult.DivaMehSource.PerfectWindowWrongPress,
            HitResult.Great => DivaJudgementResult.DivaMehSource.GreatWindowWrongPress,
            HitResult.Good => DivaJudgementResult.DivaMehSource.GoodWindowWrongPress,
            HitResult.Ok => DivaJudgementResult.DivaMehSource.OkWindowWrongPress,
            _ => DivaJudgementResult.DivaMehSource.None
        };
    }
}
