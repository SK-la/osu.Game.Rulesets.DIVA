// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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

        /// <summary>Spacing between twinkle stars in path units; live star count follows body length.</summary>
        private const float star_spacing = 38f;

        /// <summary>Cap for extremely long bodies (a longer strip then thins out instead of unbounded sprite growth).</summary>
        private const int max_stars = 48;

        private const double star_period_min = 620;
        private const double star_period_max = 980;

        private readonly Vector2 startPos;
        private readonly Color4 colour;
        private readonly double durationMs;
        private readonly double approachDurationMs;

        private SmoothPath outerPath = null!;
        private SmoothPath innerPath = null!;
        private Container starLayer = null!;
        private Texture? starTexture;
        private float durationRatio;

        private float bodyTail;
        private float bodyHead = 1f;
        private float bodyLength;

        private readonly List<TwinkleStar> stars = new List<TwinkleStar>();

        /// <summary>Path shape, synced from the ruleset setting by the owning drawable.</summary>
        public DivaNoteFlightCurve Curve = DivaNoteFlightCurve.DivaNative;

        /// <summary>Lateral multiplier; 1.0 is the curve's ProjectDIVA-matching baseline.</summary>
        public float Amplitude = 1f;

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
            float head;
            float remain = 1f;

            if (timeOffsetFromStart > 0)
            {
                // Head pinned on note; shrink remaining body toward far side.
                head = 1f;
                remain = (float)Math.Clamp(1.0 - timeOffsetFromStart / durationMs, 0, 1);
                visibleRatio *= remain;
            }
            else
            {
                head = Math.Clamp(blend, 0f, 1f);
            }

            bodyTail = head - visibleRatio;
            bodyHead = head;
            rebuildPath(bodyTail, bodyHead);

            Alpha = remain <= 0 ? 0 : 0.45f + 0.55f * Math.Max(remain, 0.2f);

            updateStars();
        }

        private void rebuildPath(float tail, float head)
        {
            outerPath.ClearVertices();
            innerPath.ClearVertices();
            bodyLength = 0;

            if (head - tail < 0.001f)
                return;

            Vector2 previous = sampleCurve(tail);

            for (int i = 0; i <= segments; i++)
            {
                float t = tail + (head - tail) * (i / (float)segments);
                Vector2 pos = sampleCurve(t);
                outerPath.AddVertex(pos);
                innerPath.AddVertex(pos);

                if (i > 0)
                    bodyLength += Vector2.Distance(previous, pos);

                previous = pos;
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
            return DivaFlightPath.Sample(Curve, startPos, 1f - t, Amplitude);
        }

        /// <summary>
        ///     Keeps a fixed number of stars on the body — one per <see cref="star_spacing" /> of length — and blinks
        ///     each in place on its own period, so the strip's total star density stays constant instead of
        ///     accumulating with frame rate.
        /// </summary>
        private void updateStars()
        {
            int desired = bodyLength > 0 && float.IsFinite(bodyLength)
                ? Math.Clamp((int)MathF.Round(bodyLength / star_spacing), 0, max_stars)
                : 0;

            while (stars.Count > desired)
            {
                TwinkleStar removed = stars[^1];
                stars.RemoveAt(stars.Count - 1);
                removed.Sprite.Expire();
            }

            while (stars.Count < desired && starTexture != null)
                stars.Add(createStar());

            float span = bodyHead - bodyTail;

            for (int i = 0; i < stars.Count; i++)
            {
                TwinkleStar star = stars[i];

                // Even slot along the visible body: density stays constant as the strip grows/shrinks.
                float slot = (i + 0.5f) / stars.Count;
                star.Sprite.Position = sampleCurve(bodyTail + slot * span) + star.Jitter;

                double phase = (Time.Current / star.Period + star.Phase) % 1.0;
                if (phase < 0)
                    phase += 1;

                float blink = (float)(0.5 - 0.5 * Math.Cos(2 * Math.PI * phase));

                star.Sprite.Alpha = 0.12f + 0.83f * blink;
                star.Sprite.Scale = new Vector2(0.7f + 0.45f * blink);
            }
        }

        private TwinkleStar createStar()
        {
            var sprite = new Sprite
            {
                Texture = starTexture,
                Size = new Vector2(9 + Random.Shared.NextSingle() * 7),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Colour = colour,
                Blending = BlendingParameters.Additive,
                Alpha = 0f,
            };

            starLayer.Add(sprite);

            return new TwinkleStar
            {
                Sprite = sprite,
                Jitter = new Vector2((Random.Shared.NextSingle() - 0.5f) * 14, (Random.Shared.NextSingle() - 0.5f) * 14),
                Phase = Random.Shared.NextSingle(),
                Period = star_period_min + Random.Shared.NextDouble() * (star_period_max - star_period_min),
            };
        }

        private sealed class TwinkleStar
        {
            public Sprite Sprite = null!;
            public Vector2 Jitter;
            public float Phase;
            public double Period;
        }
    }
}
