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
        private const float star_spacing = 8f;

        /// <summary>
        ///     Star count floor at 100% density. The pre-fix effect spawned on a timer, so a short strip held as many
        ///     stars as a long one; without this floor a short hold would carry only a couple of stars, i.e. far
        ///     fewer than that look. Bodies longer than the floor implies keep the constant-density spacing.
        /// </summary>
        private const int min_stars = 14;

        /// <summary>Cap for extremely long bodies; a longer strip then thins out instead of growing without bound.</summary>
        private const int max_stars = 200;

        /// <summary>ProjectDIVA <c>AddStarParticle</c> draws each strip star at <c>rand_size * 10</c> px.</summary>
        private const float star_max_size = 10f;

        /// <summary>Floor on the random size so a rolled-away particle stays visible.</summary>
        private const float star_min_size = 2.5f;

        /// <summary>ProjectDIVA scatters each strip star within ±6px of the strip.</summary>
        private const float star_jitter = 6f;

        /// <summary>
        ///     Closest a star may get to the body's far end. The strip head is pinned on the fixed target for the whole
        ///     hold, so without this the head-most star parks on the resting note for the entire body.
        /// </summary>
        private const float star_head_min_offset = 4f;

        private const double flicker_min = 55;
        private const double flicker_max = 120;

        private readonly Vector2 startPos;
        private readonly Color4 colour;
        private readonly double durationMs;

        private SmoothPath outerPath = null!;
        private SmoothPath innerPath = null!;
        private Container starLayer = null!;
        private Texture? starTexture;
        private float durationRatio;

        private float bodyTail;
        private float bodyHead = 1f;
        private float bodyLength;

        private readonly List<StripStar> stars = new List<StripStar>();
        private readonly Vector2[] pathPoints = new Vector2[segments + 1];
        private readonly float[] pathLengths = new float[segments + 1];

        /// <summary>Path shape, synced from the ruleset setting by the owning drawable.</summary>
        public DivaNoteFlightCurve Curve = DivaNoteFlightCurve.DivaNative;

        /// <summary>Lateral multiplier; 1.0 is the curve's ProjectDIVA-matching baseline.</summary>
        public float Amplitude = 1f;

        /// <summary>Star density multiplier (1.0 = <see cref="star_spacing" />); 0 hides the stars.</summary>
        public float StarDensity = 1f;

        /// <summary>
        ///     Half-extent of the fixed target the head is pinned to, synced from the note size by the owning drawable.
        ///     Stars are held clear of it so none is ever left sitting on the resting note.
        /// </summary>
        public float TargetHalfExtent = 20f;

        public HoldStripPiece(Vector2 startPos, Color4 colour, double durationMs, double approachDurationMs)
        {
            this.startPos = startPos;
            this.colour = colour;
            this.durationMs = Math.Max(durationMs, 1);
            double approachDurationMs1 = Math.Max(approachDurationMs, 1);
            durationRatio = (float)(this.durationMs / approachDurationMs1);

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
        /// <param name="timeOffsetFromStart">0 at note time → negative before, positive after.</param>
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

            for (int i = 0; i <= segments; i++)
            {
                float t = tail + (head - tail) * (i / (float)segments);
                Vector2 pos = sampleCurve(t);
                pathPoints[i] = pos;
                outerPath.AddVertex(pos);
                innerPath.AddVertex(pos);

                if (i > 0)
                    bodyLength += Vector2.Distance(pathPoints[i - 1], pos);

                pathLengths[i] = bodyLength;
            }

            // Keep path local origin at note centre (0,0); do not reassign OriginPosition each frame.
            outerPath.Position = Vector2.Zero;
            innerPath.Position = Vector2.Zero;
            outerPath.OriginPosition = outerPath.PositionInBoundingBox(Vector2.Zero);
            innerPath.OriginPosition = innerPath.PositionInBoundingBox(Vector2.Zero);
        }

        /// <summary>Position <paramref name="distance" /> along the body, measured from its far (tail) end.</summary>
        private Vector2 positionAtDistance(float distance)
        {
            if (distance <= 0)
                return pathPoints[0];

            if (distance >= bodyLength)
                return pathPoints[segments];

            int i = 1;

            while (i < segments && pathLengths[i] < distance)
                i++;

            float segmentStart = pathLengths[i - 1];
            float segmentLength = pathLengths[i] - segmentStart;
            float f = segmentLength > 0 ? (distance - segmentStart) / segmentLength : 0;

            return Vector2.Lerp(pathPoints[i - 1], pathPoints[i], f);
        }

        private Vector2 sampleCurve(float t)
        {
            // t=0 far, t=1 note. Allow t<0 to extend past the far end (tail behind head).
            return DivaFlightPath.Sample(Curve, startPos, 1f - t, Amplitude);
        }

        /// <summary>
        ///     Body length actually available to stars: the target's own extent plus a star's size, scatter and head
        ///     offset is reserved at the far end so no star lands on the fixed target.
        /// </summary>
        private float starSpanLength => bodyLength - targetClearance;

        /// <summary>Keep-out radius at the far end of the body; see <see cref="TargetHalfExtent" />.</summary>
        private float targetClearance => TargetHalfExtent + star_max_size * 0.5f + star_jitter + star_head_min_offset;

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

            float span = starSpanLength;
            int count = stars.Count;

            for (int i = 0; i < count; i++)
            {
                StripStar star = stars[i];

                // Even slot by arc length over the span clear of the target: density holds as the strip grows/shrinks.
                star.Sprite.Position = positionAtDistance((i + 0.5f) / count * span) + star.Jitter;

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
            float span = starSpanLength;

            if (density <= 0 || span <= 0 || !float.IsFinite(span))
                return 0;

            int byLength = (int)MathF.Round(span * density / star_spacing);
            int floor = (int)MathF.Round(min_stars * density);

            // Never pack tighter than half the nominal spacing: the floor above is meant for short-but-long-enough
            // bodies, and a body barely longer than the keep-out would otherwise collapse into a single bright blob.
            int maxBySpacing = Math.Max(1, (int)MathF.Round(span / (star_spacing * 0.5f)));

            return Math.Clamp(Math.Max(byLength, floor), 0, Math.Min(max_stars, maxBySpacing));
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
