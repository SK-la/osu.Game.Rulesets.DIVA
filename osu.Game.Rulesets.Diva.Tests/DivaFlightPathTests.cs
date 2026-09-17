// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Diva.Objects.Drawables.Pieces;
using osuTK;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaFlightPathTests
    {
        /// <summary>|L| = 500, i.e. ProjectDIVA's 120 BPM approach distance.</summary>
        private static readonly Vector2 far = new Vector2(300, 400);

        /// <summary>ProjectDIVA peak lateral offset for the 0.15 control-point offset.</summary>
        private const float pd_peak_lateral = 0.0433f;

        /// <summary>Parameter of the peak of <c>t(1-t)(1-2t)</c>.</summary>
        private const float lateral_peak_t = 0.2113249f;

        private static Vector2 normal => new Vector2(far.Y, -far.X);

        /// <summary>Signed perpendicular distance from the straight chord (which passes through the origin).</summary>
        private static float lateralOffset(Vector2 sampled) => Vector2.Dot(sampled, normal.Normalized());

        /// <summary>Distance progressed along the chord.</summary>
        private static float alongChord(Vector2 sampled) => Vector2.Dot(sampled, far.Normalized());

        [TestCase(DivaNoteFlightCurve.DivaNative)]
        [TestCase(DivaNoteFlightCurve.Quadratic)]
        [TestCase(DivaNoteFlightCurve.CatmullRom)]
        [TestCase(DivaNoteFlightCurve.EasedSmoothStep)]
        [TestCase(DivaNoteFlightCurve.EasedExpoOut)]
        public void Endpoints_are_note_centre_and_far_point(DivaNoteFlightCurve curve)
        {
            Assert.That(DivaFlightPath.Sample(curve, far, 0f, 1f), Is.EqualTo(Vector2.Zero));

            Vector2 atFar = DivaFlightPath.Sample(curve, far, 1f, 1f);
            Assert.That(atFar.X, Is.EqualTo(far.X).Within(1e-4f));
            Assert.That(atFar.Y, Is.EqualTo(far.Y).Within(1e-4f));
        }

        [Test]
        public void DivaNative_crosses_the_chord_at_its_midpoint()
        {
            Vector2 p = DivaFlightPath.Sample(DivaNoteFlightCurve.DivaNative, far, 0.5f, 1f);

            Assert.That(lateralOffset(p), Is.EqualTo(0f).Within(1e-3f));
            Assert.That(alongChord(p), Is.EqualTo(far.Length * 0.5f).Within(1e-2f));
        }

        [Test]
        public void DivaNative_is_an_s_curve_with_project_diva_peak()
        {
            float atLowT = lateralOffset(DivaFlightPath.Sample(DivaNoteFlightCurve.DivaNative, far, lateral_peak_t, 1f));
            float atHighT = lateralOffset(DivaFlightPath.Sample(DivaNoteFlightCurve.DivaNative, far, 1f - lateral_peak_t, 1f));

            Assert.That(MathF.Abs(atLowT), Is.EqualTo(pd_peak_lateral * far.Length).Within(0.2f));
            Assert.That(MathF.Abs(atHighT), Is.EqualTo(pd_peak_lateral * far.Length).Within(0.2f));
            Assert.That(atLowT * atHighT, Is.LessThan(0f), "The two humps must sit on opposite sides: an S, not a one-sided C.");
        }

        [Test]
        public void DivaNative_eases_in_and_out_along_the_chord()
        {
            for (int i = 1; i < 10; i++)
            {
                float t = i / 10f;
                float expected = 0.75f * t + 0.75f * t * t - 0.5f * t * t * t;

                Assert.That(alongChord(DivaFlightPath.Sample(DivaNoteFlightCurve.DivaNative, far, t, 1f)) / far.Length,
                    Is.EqualTo(expected).Within(1e-3f));
            }
        }

        [Test]
        public void CatmullRom_shares_the_diva_native_lateral_profile()
        {
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;

                float native = lateralOffset(DivaFlightPath.Sample(DivaNoteFlightCurve.DivaNative, far, t, 1f));
                float spline = lateralOffset(DivaFlightPath.Sample(DivaNoteFlightCurve.CatmullRom, far, t, 1f));

                Assert.That(spline, Is.EqualTo(native).Within(1e-3f));
            }
        }

        [Test]
        public void CatmullRom_uses_smoothstep_along_the_chord()
        {
            for (int i = 1; i < 10; i++)
            {
                float t = i / 10f;

                Assert.That(alongChord(DivaFlightPath.Sample(DivaNoteFlightCurve.CatmullRom, far, t, 1f)) / far.Length,
                    Is.EqualTo(t * t * (3f - 2f * t)).Within(1e-3f));
            }
        }

        [Test]
        public void Quadratic_stays_on_one_side_and_peaks_at_the_project_diva_amplitude()
        {
            float peak = lateralOffset(DivaFlightPath.Sample(DivaNoteFlightCurve.Quadratic, far, 0.5f, 1f));
            Assert.That(peak, Is.EqualTo(pd_peak_lateral * far.Length).Within(0.2f));

            for (int i = 0; i <= 10; i++)
            {
                float lateral = lateralOffset(DivaFlightPath.Sample(DivaNoteFlightCurve.Quadratic, far, i / 10f, 1f));

                Assert.That(lateral, Is.GreaterThanOrEqualTo(-1e-3f), "A single arc must never cross the chord.");
                Assert.That(lateral, Is.LessThanOrEqualTo(peak + 1e-3f));
            }
        }

        [Test]
        public void Quadratic_runs_at_a_near_constant_speed_along_the_chord()
        {
            Vector2 previous = DivaFlightPath.Sample(DivaNoteFlightCurve.Quadratic, far, 0, 1f);
            float largestStep = 0;
            float smallestStep = float.MaxValue;

            for (int i = 1; i <= 20; i++)
            {
                Vector2 current = DivaFlightPath.Sample(DivaNoteFlightCurve.Quadratic, far, i / 20f, 1f);
                float step = Vector2.Distance(previous, current);
                previous = current;

                largestStep = MathF.Max(largestStep, step);
                smallestStep = MathF.Min(smallestStep, step);
            }

            // The lateral term is second order, so the speed only wobbles by ~1.5%.
            Assert.That(largestStep / smallestStep, Is.LessThan(1.05f));
        }

        [TestCase(DivaNoteFlightCurve.DivaNative)]
        [TestCase(DivaNoteFlightCurve.Quadratic)]
        [TestCase(DivaNoteFlightCurve.CatmullRom)]
        public void Zero_amplitude_collapses_spatial_curves_onto_the_chord(DivaNoteFlightCurve curve)
        {
            for (int i = 0; i <= 10; i++)
                Assert.That(lateralOffset(DivaFlightPath.Sample(curve, far, i / 10f, 0f)), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Quadratic_at_zero_amplitude_is_a_constant_speed_line()
        {
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                Vector2 p = DivaFlightPath.Sample(DivaNoteFlightCurve.Quadratic, far, t, 0f);

                Assert.That(p.X, Is.EqualTo(far.X * t).Within(1e-3f));
                Assert.That(p.Y, Is.EqualTo(far.Y * t).Within(1e-3f));
            }
        }

        [TestCase(DivaNoteFlightCurve.DivaNative)]
        [TestCase(DivaNoteFlightCurve.Quadratic)]
        [TestCase(DivaNoteFlightCurve.CatmullRom)]
        public void Amplitude_scales_the_lateral_offset_linearly_and_clamps_at_200_percent(DivaNoteFlightCurve curve)
        {
            float full = lateralOffset(DivaFlightPath.Sample(curve, far, lateral_peak_t, 1f));

            Assert.That(full, Is.Not.EqualTo(0f).Within(1e-3f));
            Assert.That(lateralOffset(DivaFlightPath.Sample(curve, far, lateral_peak_t, 0.5f)), Is.EqualTo(full * 0.5f).Within(1e-3f));
            Assert.That(lateralOffset(DivaFlightPath.Sample(curve, far, lateral_peak_t, 2f)), Is.EqualTo(full * 2f).Within(1e-3f));
            Assert.That(lateralOffset(DivaFlightPath.Sample(curve, far, lateral_peak_t, 5f)), Is.EqualTo(full * 2f).Within(1e-3f));
        }

        [TestCase(DivaNoteFlightCurve.EasedSmoothStep)]
        [TestCase(DivaNoteFlightCurve.EasedExpoOut)]
        public void Eased_curves_are_straight_and_ignore_amplitude(DivaNoteFlightCurve curve)
        {
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                Vector2 sample = DivaFlightPath.Sample(curve, far, t, 1f);
                Vector2 noAmplitude = DivaFlightPath.Sample(curve, far, t, 0f);

                Assert.That(lateralOffset(sample), Is.EqualTo(0f).Within(1e-3f));
                Assert.That(noAmplitude.X, Is.EqualTo(sample.X).Within(1e-6f));
                Assert.That(noAmplitude.Y, Is.EqualTo(sample.Y).Within(1e-6f));
            }
        }

        [Test]
        public void Eased_smooth_step_matches_its_easing()
        {
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                float ease = t * t * (3f - 2f * t);

                Vector2 p = DivaFlightPath.Sample(DivaNoteFlightCurve.EasedSmoothStep, far, t, 1f);
                Assert.That(alongChord(p) / far.Length, Is.EqualTo(ease).Within(1e-3f));
            }
        }

        [Test]
        public void Eased_expo_out_covers_most_of_the_distance_on_spawn()
        {
            float covered = alongChord(DivaFlightPath.Sample(DivaNoteFlightCurve.EasedExpoOut, far, 0.2f, 1f)) / far.Length;

            Assert.That(covered, Is.GreaterThan(0.6f), "Ease-out must be fast on spawn and decelerate into the target.");
        }

        [Test]
        public void Spatial_curves_extrapolate_past_the_far_end()
        {
            // The hold strip samples beyond percent 1 so its body slides in from off-field, exactly like
            // ProjectDIVA's unclamped Bezier.
            Vector2 past = DivaFlightPath.Sample(DivaNoteFlightCurve.DivaNative, far, 1.25f, 1f);
            Assert.That(alongChord(past), Is.GreaterThan(far.Length));

            // The eased curves clamp instead: their zero end derivative makes clamping the natural continuation.
            Vector2 eased = DivaFlightPath.Sample(DivaNoteFlightCurve.EasedSmoothStep, far, 1.25f, 1f);
            Assert.That(eased.X, Is.EqualTo(far.X).Within(1e-4f));
            Assert.That(eased.Y, Is.EqualTo(far.Y).Within(1e-4f));
        }

        [Test]
        public void Zero_length_chord_produces_no_nan()
        {
            foreach (DivaNoteFlightCurve curve in Enum.GetValues<DivaNoteFlightCurve>())
            {
                Vector2 p = DivaFlightPath.Sample(curve, Vector2.Zero, 0.5f, 1f);

                Assert.That(float.IsNaN(p.X) || float.IsNaN(p.Y), Is.False);
                Assert.That(p, Is.EqualTo(Vector2.Zero));
            }
        }
    }
}
