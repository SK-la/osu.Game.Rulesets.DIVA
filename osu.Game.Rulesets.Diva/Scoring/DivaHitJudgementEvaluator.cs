// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Scoring
{
    public static class DivaHitJudgementEvaluator
    {
        public const double COOL_WINDOW = 50;
        public const double FINE_WINDOW = 100;
        public const double SAFE_WINDOW = 200;
        public const double SAD_WINDOW = 300;

        public const double HOLD_COOL_WINDOW = 50;
        public const double HOLD_FINE_WINDOW = 100;
        public const double HOLD_SAFE_WINDOW = 200;
        public const double HOLD_SAD_WINDOW = 300;

        /// <summary>ProjectDIVA <c>gameini.delay</c> after Start/End before forced miss.</summary>
        public const double HOLD_MISS_TIMEOUT = 350;

        public static HitResult GetResultFor(double timeOffset)
        {
            var abs = Math.Abs(timeOffset);

            if (abs <= COOL_WINDOW) return HitResult.Perfect;
            if (abs <= FINE_WINDOW) return HitResult.Great;
            if (abs <= SAFE_WINDOW) return HitResult.Good;
            if (abs <= SAD_WINDOW) return HitResult.Ok;

            return HitResult.None;
        }

        /// <summary>Hold head/release timing mapped from ProjectDIVA COOL/FINE/SAFE/SAD.</summary>
        public static HitResult GetHoldResultFor(double timeOffset)
        {
            var abs = Math.Abs(timeOffset);

            if (abs <= HOLD_COOL_WINDOW) return HitResult.Perfect;
            if (abs <= HOLD_FINE_WINDOW) return HitResult.Great;
            if (abs <= HOLD_SAFE_WINDOW) return HitResult.Good;
            if (abs <= HOLD_SAD_WINDOW) return HitResult.Ok;

            return HitResult.None;
        }

        public static bool IsWithinOkWindow(double timeOffset) => Math.Abs(timeOffset) <= SAD_WINDOW;

        public static bool ShouldMiss(double timeOffset) => timeOffset > SAD_WINDOW;

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
            HitResult.Perfect => DivaJudgementResult.DivaMehSource.CoolWindowWrongPress,
            HitResult.Great => DivaJudgementResult.DivaMehSource.FineWindowWrongPress,
            HitResult.Good => DivaJudgementResult.DivaMehSource.SafeWindowWrongPress,
            HitResult.Ok => DivaJudgementResult.DivaMehSource.SadWindowWrongPress,
            _ => DivaJudgementResult.DivaMehSource.None
        };
    }
}
