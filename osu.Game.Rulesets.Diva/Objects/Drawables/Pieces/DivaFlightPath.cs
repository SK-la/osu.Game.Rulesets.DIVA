// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Localisation;
using osu.Game.Rulesets.Diva.Localization;
using osuTK;

namespace osu.Game.Rulesets.Diva.Objects.Drawables.Pieces
{
    /// <summary>
    ///     Spatial path a flying note follows from its far spawn point to the fixed target.
    /// </summary>
    public enum DivaNoteFlightCurve
    {
        /// <summary>ProjectDIVA native S-curve: crosses the straight chord at its midpoint.</summary>
        [LocalisableDescription(typeof(DivaStrings), nameof(DivaStrings.CURVE_DIVA_NATIVE))]
        DivaNative,

        /// <summary>Single-sided quadratic arc; speed along the chord stays near constant.</summary>
        [LocalisableDescription(typeof(DivaStrings), nameof(DivaStrings.CURVE_QUADRATIC))]
        Quadratic,

        /// <summary>
        ///     Hermite / Catmull-Rom. Shares <see cref="DivaNative" />'s lateral profile but uses a
        ///     stronger ease along the chord. Once multi-node paths exist the tangents come from the
        ///     real nodes instead of the lateral profile.
        /// </summary>
        [LocalisableDescription(typeof(DivaStrings), nameof(DivaStrings.CURVE_CATMULL_ROM))]
        CatmullRom,

        /// <summary>Straight line, ease-in-out timing (both ends at zero speed).</summary>
        [LocalisableDescription(typeof(DivaStrings), nameof(DivaStrings.CURVE_EASED_SMOOTH_STEP))]
        EasedSmoothStep,

        /// <summary>Straight line, ease-out timing (fast on spawn, decelerating into the target).</summary>
        [LocalisableDescription(typeof(DivaStrings), nameof(DivaStrings.CURVE_EASED_EXPO_OUT))]
        EasedExpoOut,
    }

    /// <summary>
    ///     Samples the flight path shared by the flying piece and the hold strip.
    /// </summary>
    public static class DivaFlightPath
    {
        /// <summary>
        ///     Lateral peak of the ProjectDIVA curve as a fraction of the far distance. The spatial
        ///     curves all share this so that at 100% amplitude only their shape differs.
        /// </summary>
        public const float PD_PEAK_LATERAL = 0.0433f;

        /// <summary>Peak of <c>t(1-t)(1-2t)</c>, the DivaNative / CatmullRom lateral profile.</summary>
        private const float lateral_profile_peak = 0.09623f;

        /// <summary>Peak of <c>p(1-p)</c>, the quadratic lateral profile.</summary>
        private const float quadratic_profile_peak = 0.25f;

        /// <summary>ProjectDIVA's own 0.15 control-point offset (derived here to keep the shared peak).</summary>
        private const float diva_native_offset = PD_PEAK_LATERAL / (3f * lateral_profile_peak);

        private const float quadratic_offset = PD_PEAK_LATERAL / (2f * quadratic_profile_peak);

        private const float catmull_rom_tangent = PD_PEAK_LATERAL / lateral_profile_peak;

        /// <param name="farLocal">Note-local far end (ProjectDIVA's <c>nowDistance</c> point).</param>
        /// <param name="percent">1 on spawn (far end) → 0 at hit (note centre), matching ProjectDIVA.</param>
        /// <param name="amplitude">
        ///     Lateral multiplier; 0 collapses every spatial curve onto the straight chord. Ignored by
        ///     the eased curves, which have no lateral component.
        /// </param>
        /// <remarks>
        ///     The spatial curves are deliberately <b>not</b> clamped: the hold strip feeds
        ///     <paramref name="percent" /> above 1 so its body slides in from beyond the far end, which
        ///     is what ProjectDIVA's own unclamped <c>Bezier</c> does. The eased curves clamp instead,
        ///     since their zero end-derivatives make clamping the natural continuation.
        /// </remarks>
        public static Vector2 Sample(DivaNoteFlightCurve curve, Vector2 farLocal, float percent, float amplitude)
        {
            switch (curve)
            {
                case DivaNoteFlightCurve.EasedSmoothStep:
                    return farLocal * smoothStep(Math.Clamp(percent, 0f, 1f));

                case DivaNoteFlightCurve.EasedExpoOut:
                {
                    float t = Math.Clamp(percent, 0f, 1f);
                    // Textbook easeOutExpo overshoots t=1 slightly; pin the end instead.
                    return farLocal * (t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t));
                }

                case DivaNoteFlightCurve.Quadratic:
                {
                    Vector2 control = farLocal * 0.5f + normalOf(farLocal) * (quadratic_offset * lateralAmplitude(amplitude));
                    return control * (2f * percent * (1f - percent)) + farLocal * (percent * percent);
                }

                case DivaNoteFlightCurve.CatmullRom:
                {
                    float lateral = catmull_rom_tangent * lateralAmplitude(amplitude) * (2f * percent * percent * percent - 3f * percent * percent + percent);
                    return farLocal * smoothStep(percent) + normalOf(farLocal) * lateral;
                }

                case DivaNoteFlightCurve.DivaNative:
                default:
                {
                    Vector2 normal = normalOf(farLocal) * (diva_native_offset * lateralAmplitude(amplitude));
                    Vector2 p1 = farLocal * 0.25f + normal;
                    Vector2 p2 = farLocal * 0.75f - normal;

                    float tt = 1f - percent;
                    return p1 * (3f * percent * tt * tt) + p2 * (3f * percent * percent * tt) + farLocal * (percent * percent * percent);
                }
            }
        }

        private static float lateralAmplitude(float amplitude) => Math.Clamp(amplitude, 0f, 2f);

        /// <summary>ProjectDIVA <c>Point::normal()</c>: <c>(x,y) → (y,-x)</c>. Unnormalised, so offsets scale with the far distance.</summary>
        private static Vector2 normalOf(Vector2 v) => new Vector2(v.Y, -v.X);

        private static float smoothStep(float t) => t * t * (3f - 2f * t);
    }
}
