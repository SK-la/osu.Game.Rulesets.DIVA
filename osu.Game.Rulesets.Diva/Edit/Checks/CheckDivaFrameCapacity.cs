// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Diva.Edit.Checks
{
    /// <summary>
    ///     Flags frames that use more note slots than a <c>.diva</c> frame record holds. The export refuses
    ///     such charts outright, so this surfaces the same limit while the notes can still be moved.
    /// </summary>
    public class CheckDivaFrameCapacity : ICheck
    {
        public CheckMetadata Metadata { get; } = new CheckMetadata(CheckCategory.Compose, "Frames holding more than eight notes");

        public IEnumerable<IssueTemplate> PossibleTemplates => new IssueTemplate[]
        {
            new IssueTemplateFrameOverfull(this)
        };

        public IEnumerable<Issue> Run(BeatmapVerifierContext context)
        {
            IBeatmap playable = context.CurrentDifficulty.Playable;

            foreach (var frame in playable.HitObjects.GroupBy(h => DivaChartBuilder.TimeToFrame(playable, h.StartTime)))
            {
                var notes = frame.ToArray();

                if (notes.Length > DivaChartConstants.MAX_NOTES_PER_FRAME)
                    yield return new IssueTemplateFrameOverfull(this).Create(notes, frame.Key, notes.Length);
            }
        }

        public class IssueTemplateFrameOverfull : IssueTemplate
        {
            public IssueTemplateFrameOverfull(ICheck check)
                : base(check, IssueType.Problem, "Frame {0} holds {1} notes, but a chart frame record holds at most {2}.")
            {
            }

            public Issue Create(IEnumerable<HitObject> notes, int frame, int count)
                => new Issue(notes, this, frame, count, DivaChartConstants.MAX_NOTES_PER_FRAME);
        }
    }
}
