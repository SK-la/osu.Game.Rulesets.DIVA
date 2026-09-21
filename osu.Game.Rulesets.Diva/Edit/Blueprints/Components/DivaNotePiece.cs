// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Diva.Objects;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit.Blueprints.Components
{
    public partial class DivaNotePiece : CompositeDrawable
    {
        private readonly Sprite sprite;

        public DivaNotePiece()
        {
            Origin = Anchor.Centre;
            Size = new Vector2(DivaEditorNoteVisual.NoteSize);

            InternalChild = sprite = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            };
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            sprite.Texture = textures.Get(DivaEditorNoteVisual.StatTextureName(DivaAction.Circle));
        }

        public void UpdateFrom(DivaHitObject hitObject, TextureStore textures)
        {
            Position = hitObject.Position;
            sprite.Texture = textures.Get(DivaEditorNoteVisual.StatTextureName(hitObject.ValidAction));
        }
    }
}
