using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Scoring
{
    public partial class DivaHitWindows : HitWindows
    {
        public override bool IsHitResultAllowed(HitResult result)
        {
            switch (result)
            {
                case HitResult.Perfect:
                case HitResult.Great:
                case HitResult.Good:
                case HitResult.Ok:
                case HitResult.Meh:
                case HitResult.Miss:
                    return true;
            }

            return false;
        }

        public override double WindowFor(HitResult result) => result switch
        {
            HitResult.Perfect => DivaHitJudgementEvaluator.COOL_WINDOW,
            HitResult.Great => DivaHitJudgementEvaluator.FINE_WINDOW,
            HitResult.Good => DivaHitJudgementEvaluator.SAFE_WINDOW,
            HitResult.Ok => DivaHitJudgementEvaluator.SAD_WINDOW,
            // Align with ShouldMiss (timeOffset > OK_WINDOW). Required for JudgementResult.TimeOffset
            // clamping and HitErrorMeter (which skips rulesets with Miss window == 0).
            HitResult.Miss => DivaHitJudgementEvaluator.SAD_WINDOW,
            // Meh/Worst is a special penalty result and should not be obtainable from timing alone.
            HitResult.Meh => 0,
            _ => 0
        };

        public override void SetDifficulty(double difficulty)
        {
            // Diva uses fixed hit windows, ignore difficulty.
        }
    }
}
