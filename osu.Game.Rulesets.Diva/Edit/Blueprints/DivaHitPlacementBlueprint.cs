// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Diva.Audio;
using osu.Game.Rulesets.Diva.Edit.Blueprints.Components;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Diva.Edit.Blueprints
{
    public partial class DivaHitPlacementBlueprint : DivaPlacementBlueprint
    {
        public new DivaHitObject HitObject => (DivaHitObject)base.HitObject;

        private readonly DivaNotePiece piece;

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        public DivaHitPlacementBlueprint()
            : base(new DivaHitObject())
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
        }

        public override bool ReplacesExistingObject(HitObject existing)
            => Composer?.ReplaceOnSameTime == true && base.ReplacesExistingObject(existing);

        private protected override bool HandlePlacementMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left)
                return false;

            EndPlacement(true);
            return true;
        }

        public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
        {
            var result = Composer?.FindSnappedPositionAndTime(screenSpacePosition) ?? new SnapResult(screenSpacePosition, fallbackTime);

            base.UpdateTimeAndPosition(result.ScreenSpacePosition, result.Time ?? fallbackTime);

            if (Composer != null)
            {
                HitObject.Position = Composer.Playfield.ToLocalSpace(result.ScreenSpacePosition);
                Composer.ApplyPlacementDefaults(HitObject);
            }
            else
                HitObject.Position = ToLocalSpace(result.ScreenSpacePosition);

            return result;
        }
    }
}
