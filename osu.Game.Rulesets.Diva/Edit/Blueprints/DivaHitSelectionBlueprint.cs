// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Diva.Edit.Blueprints.Components;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit.Blueprints
{
    public partial class DivaHitSelectionBlueprint : HitObjectSelectionBlueprint<DivaHitObject>
    {
        private readonly DivaNotePiece piece;
        private readonly DivaApproachHandle approachHandle;

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        protected override bool AlwaysShowWhenSelected => true;

        public DivaHitSelectionBlueprint(DivaHitObject hitObject)
            : base(hitObject)
        {
            InternalChildren = new Drawable[]
            {
                piece = new DivaNotePiece(),
                approachHandle = new DivaApproachHandle(hitObject)
                {
                    Alpha = 0
                }
            };
        }

        protected override void Update()
        {
            base.Update();

            piece.UpdateFrom(HitObject, textures);
            approachHandle.Alpha = IsSelected ? 1 : 0;
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
            => piece.ReceivePositionalInputAt(screenSpacePos) || (IsSelected && approachHandle.ReceivePositionalInputAt(screenSpacePos));

        public override Quad SelectionQuad => piece.ScreenSpaceDrawQuad;
    }
}
