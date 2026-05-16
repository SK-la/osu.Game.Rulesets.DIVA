// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Judgements;

namespace osu.Game.Rulesets.Diva.Judgements
{
    public class DivaJudgementResult : JudgementResult
    {
        /// <summary>
        /// The <see cref="DivaHitObject"/> that was judged.
        /// </summary>
        public new DivaHitObject HitObject => (DivaHitObject)base.HitObject;

        /// <summary>
        /// The judgement which this <see cref="DivaJudgementResult"/> applies for.
        /// </summary>
        public new Judgement Judgement => base.Judgement;

        /// <summary>
        /// Invoked when this judgement result is applied.
        /// </summary>
        public event Action<DivaJudgementResult> Applied;

        /// <summary>
        /// Invoked when this judgement result is reverted.
        /// </summary>
        public event Action<DivaJudgementResult> Reverted;

        public DivaJudgementResult(DivaHitObject hitObject, Judgement judgement)
            : base(hitObject, judgement)
        {
        }

        /// <summary>
        /// Notify listeners that this result has been applied.
        /// </summary>
        public void NotifyApplied()
        {
            Applied?.Invoke(this);
        }

        /// <summary>
        /// Notify listeners that this result has been reverted.
        /// </summary>
        public void NotifyReverted()
        {
            Reverted?.Invoke(this);
        }
    }
}
