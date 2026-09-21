// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Game.Rulesets.Diva.Audio;
using osu.Game.Rulesets.Diva.Edit.Blueprints.Components;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Diva.Edit.Blueprints
{
    public partial class DivaHoldPlacementBlueprint : HitObjectPlacementBlueprint
    {
        public new DivaHoldHitObject HitObject => (DivaHoldHitObject)base.HitObject;

        private readonly DivaNotePiece piece;

        [Resolved]
        private DivaHitObjectComposer? composer { get; set; }

        [Resolved]
        private IBeatSnapProvider? beatSnapProvider { get; set; }

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        public override bool ReplacesExistingObject(HitObject existing)
            => composer?.ReplaceOnSameTime == true && base.ReplacesExistingObject(existing);

        protected override bool IsValidForPlacement =>
            base.IsValidForPlacement && (PlacementActive == PlacementState.Waiting || Precision.DefinitelyBigger(HitObject.Duration, 0));

        public DivaHoldPlacementBlueprint()
            : base(new DivaHoldHitObject())
        {
            HitObject.Samples.Clear();
            HitObject.Samples.Add(DivaHitSampleInfo.Normal);
            Child = piece = new DivaNotePiece { Alpha = 0.55f };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            BeginPlacement();
        }

        protected override void Update()
        {
            base.Update();
            piece.UpdateFrom(HitObject, textures);

            if (PlacementActive == PlacementState.Active)
                updateDurationFromClock();
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left)
                return false;

            if (PlacementActive == PlacementState.Active)
            {
                updateDurationFromClock();
                EndPlacement(true);
                return true;
            }

            BeginPlacement(true);
            return true;
        }

        public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
        {
            var result = composer?.FindSnappedPositionAndTime(screenSpacePosition) ?? new SnapResult(screenSpacePosition, fallbackTime);

            if (PlacementActive != PlacementState.Active)
            {
                base.UpdateTimeAndPosition(result.ScreenSpacePosition, result.Time ?? fallbackTime);

                if (composer != null)
                {
                    HitObject.Position = composer.Playfield.ToLocalSpace(result.ScreenSpacePosition);
                    composer.ApplyPlacementDefaults(HitObject);
                }
                else
                    HitObject.Position = ToLocalSpace(result.ScreenSpacePosition);
            }

            return result;
        }

        private void updateDurationFromClock()
        {
            if (beatSnapProvider == null)
                return;

            double snapped = beatSnapProvider.SnapTime(EditorClock.CurrentTime);
            double minDuration = beatSnapProvider.GetBeatLengthAtTime(HitObject.StartTime);
            HitObject.Duration = Math.Max(minDuration, snapped - HitObject.StartTime);
        }
    }
}
