using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Scoring
{
    public class DivaHitWindows : DefaultHitWindows
    {
        public override bool IsHitResultAllowed(HitResult result)
        {
            switch (result)
            {
                case HitResult.Perfect:
                case HitResult.Great:
                case HitResult.Good:
                case HitResult.Ok:
                // case HitResult.Meh: //  TODO: 不允许直接判定 Meh，必须通过特殊条件进行判定，对齐 Wrong 的多类型判定方式。
                case HitResult.Miss:
                    return true;
            }

            return false;
        }
    }
}
