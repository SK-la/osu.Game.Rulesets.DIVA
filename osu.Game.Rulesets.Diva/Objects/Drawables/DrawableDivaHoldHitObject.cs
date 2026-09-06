// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Diva.Audio;
using osu.Game.Rulesets.Diva.Graphics;
using osu.Game.Rulesets.Diva.Objects.Drawables.Pieces;
using osu.Game.Rulesets.Diva.Scoring;
using osu.Game.Rulesets.Diva.UI;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    /// <summary>
    /// ProjectDIVA strip: press at head, keep held, release at tail.
    /// </summary>
    public partial class DrawableDivaHoldHitObject : DrawableDivaHitObject
    {
        private HoldStripPiece? strip;
        private readonly double holdDuration;

        private bool holding;
        private bool pendingRelease;
        private bool? pendingHeadPressValid;
        private HitResult headResult = HitResult.None;

        [Resolved(canBeNull: true)]
        private DivaHitSamplePlayer? hitSamplePlayer { get; set; }

        [Resolved(canBeNull: true)]
        private DivaPlayfield? playfield { get; set; }

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

        public override bool OnPressed(KeyBindingPressEvent<DivaAction> e)
        {
            if (Judged || holding)
                return false;

            if (!AcceptsInput(e.Action))
                return false;

            // PD strips do not participate in wrong-key cancel; ignore mismatched head presses.
            if (!ComputeValidPress(e.Action))
                return false;

            pendingHeadPressValid = true;
            pendingRelease = false;
            UpdateResult(true);
            return holding || Judged;
        }

        public override void OnReleased(KeyBindingReleaseEvent<DivaAction> e)
        {
            if (!holding || Judged)
                return;

            if (!ComputeValidPress(e.Action))
                return;

            pendingRelease = true;
            UpdateResult(true);
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            timeOffset += InputOffset.Value;

            // Framework: timeOffset = Time.Current - EndTime (+ InputOffset).
            double startOffset = timeOffset + holdDuration;
            double endOffset = timeOffset;

            if (!holding)
            {
                if (!userTriggered)
                {
                    if (DivaHitJudgementEvaluator.ShouldMissHold(startOffset))
                        ApplyResult((r, _) => r.Type = HitResult.Miss);

                    return;
                }

                if (pendingHeadPressValid == null)
                    return;

                bool validPress = pendingHeadPressValid.Value;
                pendingHeadPressValid = null;

                HitResult result = DivaHitJudgementEvaluator.GetHoldPressResult(validPress, startOffset);

                if (result == HitResult.None)
                    return;

                // Wrong-key Meh path retained for safety; OnPressed already filters invalid presses.
                if (!validPress)
                    return;

                holding = true;
                headResult = result;
                hideFlyingPieces();
                // ProjectDIVA: head press also plays AddEffectNotePress (release plays again).
                playfield?.ShowHitFeedback(this, result);
                return;
            }

            if (pendingRelease)
            {
                pendingRelease = false;

                HitResult releaseResult = DivaHitJudgementEvaluator.GetHoldResultFor(endOffset);
                if (releaseResult == HitResult.None)
                    releaseResult = HitResult.Miss;

                // ProjectDIVA: strip release always PlayHit(1) when a matching hold is released.
                hitSamplePlayer?.PlayReleaseHit();

                ApplyResult((r, _) => r.Type = DivaHitJudgementEvaluator.CombineHoldResults(headResult, releaseResult));
                return;
            }

            if (!userTriggered && DivaHitJudgementEvaluator.ShouldMissHold(endOffset))
                ApplyResult((r, _) => r.Type = DivaHitJudgementEvaluator.CombineHoldResults(headResult, HitResult.Miss));
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            switch (state)
            {
                case ArmedState.Hit:
                    // Target + strip clear once the hold is fully resolved.
                    this.FadeOut(80).Expire();
                    break;

                default:
                    base.UpdateHitStateTransforms(state);
                    break;
            }
        }

        private void hideFlyingPieces()
        {
            // ProjectDIVA: after press, rhythm head is hidden; fixed target + strip remain.
            ApproachPiece.FadeOut(60);
            ApproachHand.FadeOut(60);
            ApproachTrail?.FadeOut(60);
        }
    }
}
