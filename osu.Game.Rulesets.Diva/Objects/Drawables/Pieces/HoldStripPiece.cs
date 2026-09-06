// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Diva.Graphics;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.Objects.Drawables.Pieces
{
    /// <summary>
    ///     Hold body: head tracks the flying approach piece; strip extends toward the far end.
    /// </summary>
    public partial class HoldStripPiece : CompositeDrawable
    {
        private const float min_visible_ratio = 0.08f;
        private const int segments = 32;

        private readonly Vector2 startPos;
        private readonly Color4 colour;
        private readonly double durationMs;
        private readonly double approachDurationMs;

        private SmoothPath outerPath = null!;
        private SmoothPath innerPath = null!;
        private Container starLayer = null!;
        private double starTimer;
        private Texture? starTexture;
        private float durationRatio;

        public HoldStripPiece(Vector2 startPos, Color4 colour, double durationMs, double approachDurationMs)
        {
            this.startPos = startPos;
            this.colour = colour;
            this.durationMs = Math.Max(durationMs, 1);
            this.approachDurationMs = Math.Max(approachDurationMs, 1);
            durationRatio = (float)(this.durationMs / this.approachDurationMs);

            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Masking = false;
        }

        public void SetApproachDuration(double approachDurationMs)
        {
            durationRatio = (float)(durationMs / Math.Max(approachDurationMs, 1));
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            starTexture = DivaProjectDivaAtlas.GetParticleStar(textures) ?? textures.Get(DivaProjectDivaAtlas.PARTICLE_5STAR);

            InternalChildren =
            [
                outerPath = new SmoothPath
                {
                    PathRadius = 8f,
                    Colour = Color4.White,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.TopLeft,
                    Position = Vector2.Zero,
                },
                innerPath = new SmoothPath
                {
                    PathRadius = 5.5f,
                    Colour = colour,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.TopLeft,
                    Position = Vector2.Zero,
                },
                starLayer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = false,
                }
            ];

            rebuildPath(0, 1);
        }

        /// <param name="blend">0 at spawn → 1 at note time (approach complete).</param>
        public void UpdateStrip(float blend, double timeOffsetFromStart)
        {
            float visibleRatio = Math.Max(durationRatio, min_visible_ratio);
            float tHead;
            float remain = 1f;

            if (timeOffsetFromStart > 0)
            {
                // Head pinned on note; shrink remaining body toward far side.
                tHead = 1f;
                remain = (float)Math.Clamp(1.0 - timeOffsetFromStart / durationMs, 0, 1);
                visibleRatio *= remain;
            }
            else
            {
                tHead = Math.Clamp(blend, 0f, 1f);
            }

            float tTail = tHead - visibleRatio;
            rebuildPath(tTail, tHead);

            Alpha = remain <= 0 ? 0 : 0.45f + 0.55f * Math.Max(remain, 0.2f);

            starTimer += 16;
            if (starTimer > 40 && starTexture != null && remain > 0.05f)
            {
                starTimer = 0;
                spawnStar(tTail, tHead);
            }
        }

        private void rebuildPath(float tTail, float tHead)
        {
            outerPath.ClearVertices();
            innerPath.ClearVertices();

            if (tHead - tTail < 0.001f)
                return;

            for (int i = 0; i <= segments; i++)
            {
                float t = tTail + (tHead - tTail) * (i / (float)segments);
                Vector2 pos = sampleCurve(t);
                outerPath.AddVertex(pos);
                innerPath.AddVertex(pos);
            }

            // Keep path local origin at note centre (0,0); do not reassign OriginPosition each frame.
            outerPath.Position = Vector2.Zero;
            innerPath.Position = Vector2.Zero;
            outerPath.OriginPosition = outerPath.PositionInBoundingBox(Vector2.Zero);
            innerPath.OriginPosition = innerPath.PositionInBoundingBox(Vector2.Zero);
        }

        private Vector2 sampleCurve(float t)
        {
            // t=0 far, t=1 note. Allow t<0 to extend past the far end (tail behind head).
            return Extensions.CubicInterpolate(startPos, Vector2.Zero, t, 150);
        }

        private void spawnStar(float tTail, float tHead)
        {
            float span = tHead - tTail;
            if (span <= 0)
                return;

            float t = tTail + Random.Shared.NextSingle() * span;
            Vector2 pos = sampleCurve(t);
            pos += new Vector2((Random.Shared.NextSingle() - 0.5f) * 18, (Random.Shared.NextSingle() - 0.5f) * 18);

            var star = new Sprite
            {
                Texture = starTexture,
                Size = new Vector2(10 + Random.Shared.NextSingle() * 8),
                Position = pos,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Colour = colour,
                Blending = BlendingParameters.Additive,
                Alpha = 0.9f,
            };

            starLayer.Add(star);
            star.FadeOut(350).ScaleTo(0.2f, 350).Expire();
        }
    }
}
