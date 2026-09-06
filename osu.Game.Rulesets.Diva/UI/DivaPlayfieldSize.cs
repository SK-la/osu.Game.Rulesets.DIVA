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
    }
}
