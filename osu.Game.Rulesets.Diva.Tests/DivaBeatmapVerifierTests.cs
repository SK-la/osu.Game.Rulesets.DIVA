// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Edit;
using osu.Game.Rulesets.Diva.Edit.Checks;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks.Components;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaBeatmapVerifierTests
    {
        [Test]
        public void Frame_capacity_check_flags_a_frame_that_overruns_the_slots()
        {
            DivaBeatmap beatmap = beatmapWithNotesAtZero(DivaChartConstants.MAX_NOTES_PER_FRAME + 1);

            var issues = new CheckDivaFrameCapacity().Run(createContext(beatmap)).ToArray();

            Assert.That(issues, Has.Length.EqualTo(1));
            Assert.That(issues[0].Template.Type, Is.EqualTo(IssueType.Problem));
            Assert.That(issues[0].HitObjects.Count, Is.EqualTo(DivaChartConstants.MAX_NOTES_PER_FRAME + 1));
        }

        [Test]
        public void Frame_capacity_check_accepts_a_frame_that_exactly_fills_the_slots()
        {
            DivaBeatmap beatmap = beatmapWithNotesAtZero(DivaChartConstants.MAX_NOTES_PER_FRAME);

            Assert.That(new CheckDivaFrameCapacity().Run(createContext(beatmap)), Is.Empty);
        }

        [Test]
        public void Ruleset_runs_the_diva_checks_in_the_editor_verifier()
        {
            Assert.That(new DivaRuleset().CreateBeatmapVerifier(), Is.TypeOf<DivaBeatmapVerifier>());
        }

        private static DivaBeatmap beatmapWithNotesAtZero(int noteCount)
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            for (int i = 0; i < noteCount; i++)
            {
                beatmap.HitObjects.Add(new DivaHitObject
                {
                    StartTime = 0,
                    Position = DivaActionEncoding.ToPlayfieldPosition(i, 12),
                    ValidAction = DivaAction.Circle
                });
            }

            return beatmap;
        }

        private static BeatmapVerifierContext createContext(IBeatmap beatmap) => new BeatmapVerifierContext(beatmap, null!);
    }
}
