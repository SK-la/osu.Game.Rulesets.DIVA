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

        /// <summary>Path units per star at 100% density.</summary>
        private const float star_spacing = 34f;

        /// <summary>Cap for extremely long bodies; a longer strip then thins out instead of growing without bound.</summary>
        private const int max_stars = 48;

        /// <summary>ProjectDIVA <c>AddStarParticle</c> draws each strip star at <c>rand_size * 10</c> px.</summary>
        private const float star_max_size = 10f;

        /// <summary>Floor on the random size so a rolled-away particle stays visible.</summary>
        private const float star_min_size = 2.5f;

        /// <summary>ProjectDIVA scatters each strip star within ±6px of the strip.</summary>
        private const float star_jitter = 6f;

        /// <summary>
        ///     Keep-out radius around the fixed target: the strip head is pinned on the target for the whole hold, so
        ///     without this the head-most star sits on the resting note for the entire body.
        /// </summary>
        private const float target_clearance = 22f;

        private const double flicker_min = 55;
        private const double flicker_max = 120;

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

        private readonly List<StripStar> stars = new List<StripStar>();

        /// <summary>Path shape, synced from the ruleset setting by the owning drawable.</summary>
        public DivaNoteFlightCurve Curve = DivaNoteFlightCurve.DivaNative;

        /// <summary>Lateral multiplier; 1.0 is the curve's ProjectDIVA-matching baseline.</summary>
        public float Amplitude = 1f;

        /// <summary>Star density multiplier (1.0 = <see cref="star_spacing" />); 0 hides the stars.</summary>
        public float StarDensity = 1f;

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
        ///     Keeps one star per <see cref="star_spacing" /> of body length and blinks each in place, so the total
        ///     star density stays constant while the strip grows and shrinks.
        /// </summary>
        private void updateStars()
        {
            int desired = desiredStarCount();

            while (stars.Count > desired)
            {
                StripStar removed = stars[^1];
                stars.RemoveAt(stars.Count - 1);
                removed.Sprite.Expire();
            }

            while (stars.Count < desired)
            {
                StripStar? created = createStar();

                if (created == null)
                    break;

                stars.Add(created);
            }

            float span = bodyHead - bodyTail;

            for (int i = 0; i < stars.Count; i++)
            {
                StripStar star = stars[i];

                // Even slot along the visible body: density does not change as the strip grows or shrinks.
                float slot = (i + 0.5f) / stars.Count;
                Vector2 position = sampleCurve(bodyTail + slot * span) + star.Jitter;

                // Never park a star on the fixed target note.
                if (position.LengthSquared <= target_clearance * target_clearance)
                {
                    star.Sprite.Alpha = 0;
                    continue;
                }

                star.Sprite.Position = position;

                if (Time.Current >= star.NextFlickerTime)
                {
                    // ProjectDIVA re-rolls a strip star's alpha (ParticleComet state bit 1) rather than fading it.
                    star.NextFlickerTime = Time.Current + star.FlickerPeriod;
                    star.Alpha = 0.08f + 0.92f * Random.Shared.NextSingle();
                }

                star.Sprite.Alpha = star.Alpha;
            }
        }

        private int desiredStarCount()
        {
            float density = Math.Clamp(StarDensity, 0f, 2f);

            if (density <= 0 || bodyLength <= 0 || !float.IsFinite(bodyLength))
                return 0;

            return Math.Clamp((int)MathF.Round(bodyLength * density / star_spacing), 0, max_stars);
        }

        private StripStar? createStar()
        {
            if (starTexture == null)
                return null;

            // ProjectDIVA AddStarParticle: a random colour averaged with the unit colour, so strip stars vary per particle.
            var starColour = new Color4(
                (Random.Shared.NextSingle() + colour.R) * 0.5f,
                (Random.Shared.NextSingle() + colour.G) * 0.5f,
                (Random.Shared.NextSingle() + colour.B) * 0.5f,
                1f);

            var sprite = new Sprite
            {
                Texture = starTexture,
                Size = new Vector2(MathF.Max(star_min_size, Random.Shared.NextSingle() * star_max_size)),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Colour = starColour,
                Blending = BlendingParameters.Additive,
                Alpha = 0f,
            };

            starLayer.Add(sprite);

            return new StripStar
            {
                Sprite = sprite,
                Jitter = new Vector2(
                    (Random.Shared.NextSingle() - 0.5f) * 2 * star_jitter,
                    (Random.Shared.NextSingle() - 0.5f) * 2 * star_jitter),
                FlickerPeriod = flicker_min + Random.Shared.NextDouble() * (flicker_max - flicker_min),
                NextFlickerTime = Time.Current,
            };
        }

        private sealed class StripStar
        {
            public Sprite Sprite = null!;
            public Vector2 Jitter;
            public double NextFlickerTime;
            public double FlickerPeriod;
            public float Alpha;
        }
    }
}
