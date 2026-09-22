// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    /// <summary>
    ///     Times a whole-chart nudge would move each note and each chart event to, produced by
    ///     <see cref="DivaChartBuilder.PlanTimeNudge" />.
    /// </summary>
    public sealed class DivaTimeNudge
    {
        public DivaTimeNudge(
            IReadOnlyDictionary<DivaHitObject, double> notes,
            IReadOnlyDictionary<DivaBgmEvent, double> bgm,
            IReadOnlyDictionary<DivaResourceEvent, double> resources)
        {
            Notes = notes;
            Bgm = bgm;
            Resources = resources;
        }

        public IReadOnlyDictionary<DivaHitObject, double> Notes { get; }

        public IReadOnlyDictionary<DivaBgmEvent, double> Bgm { get; }

        public IReadOnlyDictionary<DivaResourceEvent, double> Resources { get; }
    }
}
