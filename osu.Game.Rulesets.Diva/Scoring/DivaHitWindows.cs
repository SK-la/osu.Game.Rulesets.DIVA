using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Scoring
{
    public class DivaHitWindows : HitWindows
    {
        // Fixed windows for this ruleset.
        private const double perfect_window = 22.5;
        private const double great_window = 45.0;
        private const double good_window = 90.0;
        private const double ok_window = 135.0;

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
            HitResult.Perfect => perfect_window,
            HitResult.Great => great_window,
            HitResult.Good => good_window,
            HitResult.Ok => ok_window,
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
