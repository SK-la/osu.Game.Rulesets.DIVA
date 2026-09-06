// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Localisation;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Diva.UI;
using osuTK;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    public class DivaBeatmap : Beatmap<DivaHitObject>
    {
        /// <summary>
        ///     Logical note-field size used for playfield fitting (content-derived; at least ProjectDIVA 480×272).
        /// </summary>
        public Vector2 LogicalPlayfieldSize { get; set; } = DivaPlayfieldSize.DefaultNativeSize;

        public override IEnumerable<BeatmapStatistic> GetStatistics()
        {
            int holdNotes = HitObjects.Count(h => h is DivaHoldHitObject);
            int notes = HitObjects.Count - holdNotes;
            int sum = Math.Max(1, notes + holdNotes);

            return new[]
            {
                new BeatmapStatistic
                {
                    Name = BeatmapStatisticStrings.Notes,
                    CreateIcon = () => new BeatmapStatisticIcon(BeatmapStatisticsIconType.Circles),
                    Content = notes.ToString(),
                    BarDisplayLength = notes / (float)sum,
                },
                new BeatmapStatistic
                {
                    Name = BeatmapStatisticStrings.HoldNotes,
                    CreateIcon = () => new BeatmapStatisticIcon(BeatmapStatisticsIconType.Sliders),
                    Content = holdNotes.ToString(),
                    BarDisplayLength = holdNotes / (float)sum,
                },
            };
        }
    }
}
