// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Diva.Audio;
using osu.Game.Rulesets.Diva.Edit.Blueprints.Components;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Diva.Edit.Blueprints
{
    public partial class DivaHitPlacementBlueprint : HitObjectPlacementBlueprint
    {
        public new DivaHitObject HitObject => (DivaHitObject)base.HitObject;

        private readonly DivaNotePiece piece;

        [Resolved]
        private DivaHitObjectComposer? composer { get; set; }

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        public DivaHitPlacementBlueprint()
            : base(new DivaHitObject())
        {
            HitObject.Samples.Clear();
            HitObject.Samples.Add(DivaHitSampleInfo.Normal);
            Child = piece = new DivaNotePiece();
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

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left)
                return false;

            EndPlacement(true);
            return true;
        }

        public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
        {
            var result = composer?.FindSnappedPositionAndTime(screenSpacePosition) ?? new SnapResult(screenSpacePosition, fallbackTime);

            base.UpdateTimeAndPosition(result.ScreenSpacePosition, result.Time ?? fallbackTime);

            if (composer != null)
            {
                HitObject.Position = composer.Playfield.ToLocalSpace(result.ScreenSpacePosition);
                composer.ApplyPlacementDefaults(HitObject);
            }
            else
                HitObject.Position = ToLocalSpace(result.ScreenSpacePosition);

            return result;
        }
    }
}
