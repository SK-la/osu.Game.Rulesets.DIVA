// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects;
using osuTK;

namespace osu.Game.Rulesets.Diva.UI
{
    /// <summary>
    ///     Resolves the logical note field size from chart content instead of a fixed screen mapping.
    /// </summary>
    public static class DivaPlayfieldSize
    {
        /// <summary>
        ///     Room for note sprite half-extents near the edge (settings allow note size up to 64).
        /// </summary>
        public const float EDGE_PADDING = 40f;

        public static Vector2 DefaultNativeSize => new Vector2(DivaChartConstants.WIDTH, DivaChartConstants.HEIGHT);

        /// <summary>
        ///     At least the ProjectDIVA 480×272 field; expands when notes sit in larger spaces (e.g. osu!std 512×384).
        /// </summary>
        public static Vector2 Compute(IEnumerable<DivaHitObject> hitObjects)
        {
            float maxX = 0;
            float maxY = 0;

            foreach (DivaHitObject hitObject in hitObjects)
            {
                maxX = Math.Max(maxX, hitObject.Position.X);
                maxY = Math.Max(maxY, hitObject.Position.Y);
            }

            float width = Math.Max(DivaChartConstants.WIDTH, maxX + EDGE_PADDING);
            float height = Math.Max(DivaChartConstants.HEIGHT, maxY + EDGE_PADDING);

            return new Vector2(width, height);
        }

        /// <summary>
        ///     ProjectDIVA <c>InsideDrawRangeEx</c>: the field plus a 20px margin. Flying pieces outside
        ///     this box are not drawn at all (no fade — this is the whole appear effect).
        /// </summary>
        /// <param name="notePosition">Playfield-space position of the point being tested.</param>
        /// <param name="logicalSize">Logical field size (see <see cref="Compute"/>).</param>
        /// <param name="noteSize">Current note sprite size, used to widen the margin for larger notes.</param>
        public static bool IsInsideDrawRange(Vector2 notePosition, Vector2 logicalSize, float noteSize)
        {
            float margin = MathF.Max(20f, noteSize * 0.5f);

            return notePosition.X > -margin && notePosition.Y > -margin
                   && notePosition.X < logicalSize.X + margin && notePosition.Y < logicalSize.Y + margin;
        }
    }
}
