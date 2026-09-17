// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Game.Rulesets.Diva.Objects.Drawables.Pieces
{
    public partial class ApproachPiece : Sprite
    {
        public Vector2 StartPos;

        /// <summary>Path shape, synced from the ruleset setting by the owning drawable.</summary>
        public DivaNoteFlightCurve Curve = DivaNoteFlightCurve.DivaNative;

        /// <summary>Lateral multiplier; 1.0 is the curve's ProjectDIVA-matching baseline.</summary>
        public float Amplitude = 1f;

        /// <param name="blend">0 at spawn → 1 at note time; ProjectDIVA's percent runs the other way.</param>
        public void UpdatePos(float blend)
        {
            Position = DivaFlightPath.Sample(Curve, StartPos, 1f - blend, Amplitude);
        }
    }
}
