// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Diva.Audio;
using osu.Game.Rulesets.Diva.Graphics;
using osu.Game.Rulesets.Diva.Objects.Drawables.Pieces;
using osu.Game.Rulesets.Diva.Scoring;
using osu.Game.Rulesets.Diva.UI;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    /// <summary>
    /// ProjectDIVA strip: press at head, keep held, release at tail.
    /// </summary>
    public partial class DrawableDivaHoldHitObject : DrawableDivaHitObject
    {
        private HoldStripPiece? strip;

        /// <summary>
        ///     ProjectDIVA flies both ends of a strip, so the far end gets its own piece riding the body's far end.
        ///     It keeps flying after the head press and lands on the fixed target as the hold is released.
        /// </summary>
        private ApproachPiece? tailPiece;

        private readonly double holdDuration;

        /// <summary>
        ///     A strip's flying head has no trail in ProjectDIVA; its particle stream is the body's stars.
        /// </summary>
        protected override bool UseApproachTrail => false;

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
        private void load(TextureStore textures)
        {
            Color4 colour = DivaProjectDivaAtlas.GetUnitColor(ValidAction);
            strip = new HoldStripPiece(HitObject.ApproachPieceOriginPosition, colour, holdDuration, TimePreempt)
            {
                Depth = 4,
            };
            AddInternal(strip);

            tailPiece = new ApproachPiece
            {
                Depth = 0,
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                StartPos = HitObject.ApproachPieceOriginPosition,
                Texture = textures.Get($"{GetTextureLocation()}{GetTextureAction()}Move"),
            };
            AddInternal(tailPiece);

            ApproachPreemptScale.BindValueChanged(_ => strip?.SetApproachDuration(TimePreempt), true);
            HoldStarDensity.BindValueChanged(v =>
            {
                strip?.StarDensity = (float)(v.NewValue / 100.0);
            }, true);
            NoteSize.BindValueChanged(v =>
            {
                strip?.TargetHalfExtent = (float)v.NewValue * 0.5f;
            }, true);

            ApplyFlightSettings();
        }

        protected override void OnFlightSettingsChanged(DivaNoteFlightCurve curve, float amplitude)
        {
            if (strip == null)
                return;

            strip.Curve = curve;
            strip.Amplitude = amplitude;

            if (tailPiece != null)
            {
                tailPiece.Curve = curve;
                tailPiece.Amplitude = amplitude;
            }
        }

        protected override void OnApproachUpdate(float blend)
        {
            if (strip == null || tailPiece == null)
                return;

            // Re-read the flight vector instead of the one captured at load: the editor edits it in place, and a
            // body that kept the load-time value would keep drawing itself along a vector the chart no longer has.
            Vector2 approach = HitObject.ApproachPieceOriginPosition;
            strip.StartPos = approach;
            tailPiece.StartPos = approach;

            double offset = Time.Current - HitObject.StartTime;
            strip.UpdateStrip(blend, offset);

            tailPiece.UpdatePos(strip.TailBlend);

            bool nativeAppearance = NoteAppearanceMode.Value == DivaNoteAppearance.DivaNative;

            // ProjectDIVA culls flying pieces per piece, so the body follows its own head: without this the strip
            // slides in from outside the field while its head is still culled, i.e. the head looks delayed.
            strip.DrawRangeVisible = !nativeAppearance || isWithinDrawRange(ApproachPiece.Position);
            tailPiece.Alpha = !nativeAppearance || isWithinDrawRange(tailPiece.Position) ? 1 : 0;
        }

        private bool isWithinDrawRange(Vector2 localPosition)
            => DivaPlayfieldSize.IsInsideDrawRange(HitObject.Position + localPosition, LogicalPlayfieldSize, (float)NoteSize.Value);

        public bool IsHolding { get; private set; }

        public bool IsHoldingAction(DivaAction action) => IsHolding && !Judged && ComputeValidPress(action);

        /// <summary>A started strip is already consumed by its head press; ProjectDIVA skips it entirely.</summary>
        public override bool AcceptsPressNow => !Judged && !IsHolding;

        public override bool TryHandlePress(DivaAction action)
        {
            if (Judged || IsHolding || IsGameplayRewinding)
                return false;

            if (!AcceptsInput(action))
                return false;

            // PD strips do not participate in wrong-key cancel; ignore mismatched head presses.
            if (!ComputeValidPress(action))
                return false;

            pendingHeadPressValid = true;
            pendingRelease = false;
            UpdateResult(true);
            return IsHolding || Judged;
        }

        public override void TryHandleRelease(DivaAction action)
        {
            if (!IsHolding || Judged || IsGameplayRewinding)
                return;

            if (!ComputeValidPress(action))
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

            if (!IsHolding)
            {
                // A release only counts while the head is held; anything parked here would be replayed as a release
                // at the next press.
                pendingRelease = false;

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

                IsHolding = true;
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

        protected override void ResetTransientState()
        {
            base.ResetTransientState();

            IsHolding = false;
            pendingRelease = false;
            pendingHeadPressValid = null;
            headResult = HitResult.None;

            strip?.ResetVisualState();
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
            // ProjectDIVA: after press, the head is hidden; fixed target, strip and flying tail remain.
            ApproachPiece.FadeOut(60);
            ApproachHand.FadeOut(60);
            ApproachTrail?.FadeOut(60);
        }
    }
}
