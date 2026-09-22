// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Diva.Edit.Checks;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks.Components;

namespace osu.Game.Rulesets.Diva.Edit
{
    /// <summary>
    ///     DIVA-specific editor checks. The generic <see cref="BeatmapVerifier"/> runs alongside this one.
    /// </summary>
    public class DivaBeatmapVerifier : IBeatmapVerifier
    {
        private readonly List<ICheck> checks = new List<ICheck>
        {
            // Compose
            new CheckDivaFrameCapacity(),
        };

        public IEnumerable<Issue> Run(BeatmapVerifierContext context)
        {
            return checks.SelectMany(check => check.Run(context));
        }
    }
}
