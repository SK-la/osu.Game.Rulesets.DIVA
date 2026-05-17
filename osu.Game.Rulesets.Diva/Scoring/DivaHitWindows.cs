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
            HitResult.Perfect => DivaHitJudgementEvaluator.PERFECT_WINDOW,
            HitResult.Great => DivaHitJudgementEvaluator.GREAT_WINDOW,
            HitResult.Good => DivaHitJudgementEvaluator.GOOD_WINDOW,
            HitResult.Ok => DivaHitJudgementEvaluator.OK_WINDOW,
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
