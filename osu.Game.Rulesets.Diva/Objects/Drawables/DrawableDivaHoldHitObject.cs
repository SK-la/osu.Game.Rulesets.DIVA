// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Diva.Graphics;
using osu.Game.Rulesets.Diva.Objects.Drawables.Pieces;
using osu.Game.Rulesets.Objects.Drawables;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    public partial class DrawableDivaHoldHitObject : DrawableDivaHitObject
    {
        private HoldStripPiece? strip;
        private readonly double holdDuration;

        public DrawableDivaHoldHitObject(DivaHoldHitObject hitObject)
            : base(hitObject)
        {
            holdDuration = hitObject.Duration;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Color4 colour = DivaProjectDivaAtlas.GetUnitColor(ValidAction);
            strip = new HoldStripPiece(HitObject.ApproachPieceOriginPosition, colour, holdDuration, TimePreempt)
            {
                Depth = 4,
            };
            AddInternal(strip);

            ApproachPreemptScale.BindValueChanged(_ => strip?.SetApproachDuration(TimePreempt), true);
        }

        protected override void OnApproachUpdate(float blend)
        {
            double offset = Time.Current - HitObject.StartTime;
            strip?.UpdateStrip(blend, offset);
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            // Framework passes Time.Current - GetEndTime() for IHasDuration objects.
            // DIVA holds are scored at the head (StartTime), same as taps — convert back.
            base.CheckForResult(userTriggered, timeOffset + holdDuration);
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            if (state == ArmedState.Hit)
            {
                // ProjectDIVA: after head press, hide the flying rhythm piece immediately.
                // Keep the fixed target + shrinking strip until EndTime, then clear.
                // Lifetime is anchored to EndTime (not HitTime + Duration) so a late head
                // press cannot leave the note stuck on the target after the strip finishes.
                ApproachPiece.FadeOut(60);
                ApproachHand.FadeOut(60);
                ApproachTrail?.FadeOut(60);

                double remainingToEnd = Math.Max(0, ((DivaHoldHitObject)HitObject).EndTime - Time.Current);
                this.Delay(remainingToEnd).FadeOut(80).Expire();
                return;
            }

            base.UpdateHitStateTransforms(state);
        }
    }
}
