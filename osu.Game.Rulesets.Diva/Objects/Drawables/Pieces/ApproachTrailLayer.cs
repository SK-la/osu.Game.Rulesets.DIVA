// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Diva.Graphics;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.Objects.Drawables.Pieces
{
    /// <summary>
    ///     Lightweight star / block trail left behind the flying approach piece.
    /// </summary>
    public partial class ApproachTrailLayer : Container
    {
        private Texture? starTexture;
        private Texture? blockTexture;
        private double emitClock;
        private readonly Color4 trailColour;

        public ApproachTrailLayer(Color4 trailColour)
        {
            this.trailColour = trailColour;
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            starTexture = DivaProjectDivaAtlas.GetParticleStar(textures)
                          ?? textures.Get(DivaProjectDivaAtlas.PARTICLE_5STAR)
                          ?? textures.Get(DivaProjectDivaAtlas.PARTICLE_STAR2);
            blockTexture = DivaProjectDivaAtlas.GetBlock(textures);
        }

        public void EmitAt(Vector2 localPosition, double elapsedFrameTime)
        {
            emitClock += elapsedFrameTime;
            if (emitClock < 28)
                return;

            emitClock = 0;

            Texture? tex = (Children.Count % 3 == 0 ? blockTexture : null) ?? starTexture;
            if (tex == null)
                return;

            var particle = new Sprite
            {
                Texture = tex,
                Size = new Vector2(tex == blockTexture ? 6 : 12),
                Position = localPosition,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Colour = trailColour,
                Blending = BlendingParameters.Additive,
                Alpha = 0.85f,
            };

            Add(particle);
            particle.FadeOut(280).ScaleTo(0.25f, 280).Expire();
        }
    }
}
