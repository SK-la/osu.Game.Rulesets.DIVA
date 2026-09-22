// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Diva.Edit.Checks
{
    /// <summary>
    ///     Flags notes of one button that fall inside a hold of the same button. The reference editor treats
    ///     that as a placement conflict, so the chart is at best ambiguous to play; whether the game can still
    ///     be hit through it has not been verified, hence a warning rather than a blocking problem.
    /// </summary>
    public class CheckDivaActionOverlap : ICheck
    {
        public CheckMetadata Metadata { get; } = new CheckMetadata(CheckCategory.Compose, "Notes inside a hold of the same button");

        public IEnumerable<IssueTemplate> PossibleTemplates => new IssueTemplate[]
        {
            new IssueTemplateActionOverlap(this)
        };

        public IEnumerable<Issue> Run(BeatmapVerifierContext context)
        {
            IBeatmap playable = context.CurrentDifficulty.Playable;

            foreach ((DivaHitObject covering, DivaHitObject covered) in DivaChartBuilder.FindActionOverlaps(playable))
            {
                int start = DivaChartBuilder.TimeToFrame(playable, covering.StartTime);
                int end = start + (covering is DivaHoldHitObject hold
                    ? DivaChartBuilder.FrameLengthAt(playable, covering.StartTime, hold.Duration)
                    : 0);

                yield return new IssueTemplateActionOverlap(this).Create(covered, covering, start, end, DivaChartBuilder.TimeToFrame(playable, covered.StartTime));
            }
        }

        public class IssueTemplateActionOverlap : IssueTemplate
        {
            public IssueTemplateActionOverlap(ICheck check)
                : base(check, IssueType.Warning, "Frame {0} starts inside the same button's hold over frames {1}-{2}.")
            {
            }

            public Issue Create(HitObject covered, HitObject covering, int holdStart, int holdEnd, int frame)
                => new Issue(new[] { covered, covering }, this, frame, holdStart, holdEnd);
        }
    }
}
