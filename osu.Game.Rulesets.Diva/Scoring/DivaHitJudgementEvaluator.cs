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

        public static HitResult GetResultFor(double timeOffset)
        {
            var abs = Math.Abs(timeOffset);

            if (abs <= PERFECT_WINDOW) return HitResult.Perfect;
            if (abs <= GREAT_WINDOW) return HitResult.Great;
            if (abs <= GOOD_WINDOW) return HitResult.Good;
            if (abs <= OK_WINDOW) return HitResult.Ok;

            return HitResult.None;
        }

        public static bool IsWithinOkWindow(double timeOffset) => Math.Abs(timeOffset) <= OK_WINDOW;

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

