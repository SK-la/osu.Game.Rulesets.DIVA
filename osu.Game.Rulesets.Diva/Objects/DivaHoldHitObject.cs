// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Diva.Scoring;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Rulesets.Diva.Objects
{
    /// <summary>
    ///     Hold / long-press note. Hit SE via <see cref="Audio.DivaHitSamplePlayer"/> (head keydown + tail release).
    ///     Judged like ProjectDIVA strips: press at <see cref="HitObject.StartTime"/>, release at <see cref="EndTime"/>.
    /// </summary>
    public partial class DivaHoldHitObject : DivaHitObject, IHasDuration
    {
        public double EndTime
        {
            get => StartTime + Duration;
            set => Duration = value - StartTime;
        }

        public double Duration { get; set; }

        public override double MaximumJudgementOffset => DivaHitJudgementEvaluator.HOLD_MISS_TIMEOUT;
    }
}
