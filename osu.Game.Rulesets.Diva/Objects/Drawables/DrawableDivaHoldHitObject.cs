// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            // Keep strip visible after head hit so the remaining body can finish.
            if (state == ArmedState.Hit)
            {
                this.Delay(holdDuration).FadeOut(120).Expire();
                return;
            }

            base.UpdateHitStateTransforms(state);
        }
    }
}
