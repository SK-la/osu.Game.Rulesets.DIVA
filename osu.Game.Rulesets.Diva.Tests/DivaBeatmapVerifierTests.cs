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
using osu.Game.Rulesets.Objects;

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

        [Test]
        public void Action_overlap_check_flags_a_tap_inside_a_hold_of_the_same_button()
        {
            var beatmap = beatmapAt120();
            addHold(beatmap, startTime: 0, duration: 500);          // covers 48 frames, i.e. up to 500 ms
            addTap(beatmap, startTime: 250);                        // frame 24, inside the hold

            var issues = new CheckDivaActionOverlap().Run(createContext(beatmap)).ToArray();

            Assert.That(issues, Has.Length.EqualTo(1));
            Assert.That(issues[0].Template.Type, Is.EqualTo(IssueType.Warning));
            Assert.That(issues[0].HitObjects.OfType<DivaHoldHitObject>().Count(), Is.EqualTo(1), "the covering hold is reported alongside");
        }

        [Test]
        public void Action_overlap_check_ignores_notes_of_other_buttons_and_notes_after_the_hold()
        {
            var beatmap = beatmapAt120();
            addHold(beatmap, startTime: 0, duration: 500);

            addTap(beatmap, startTime: 250, action: DivaAction.Cross);   // another button
            addTap(beatmap, startTime: 500);                             // the frame the hold ends on

            Assert.That(new CheckDivaActionOverlap().Run(createContext(beatmap)), Is.Empty);
        }

        [Test]
        public void Action_overlap_check_ignores_two_taps_of_one_button_in_a_frame()
        {
            var beatmap = beatmapAt120();
            addTap(beatmap, startTime: 0);
            addTap(beatmap, startTime: 0);

            Assert.That(new CheckDivaActionOverlap().Run(createContext(beatmap)), Is.Empty,
                "the reference editor only treats holds as covering a span, so stacked taps are not its conflict");
        }

        [Test]
        public void Overlap_search_reports_the_hold_reaching_furthest_into_the_note()
        {
            var beatmap = beatmapAt120();
            addHold(beatmap, startTime: 0, duration: 200);    // ends at frame 19
            addHold(beatmap, startTime: 0, duration: 500);    // ends at frame 48
            var tap = addTap(beatmap, startTime: 300);        // frame 28

            var overlaps = DivaChartBuilder.FindActionOverlaps(beatmap).ToArray();

            Assert.That(overlaps.Where(pair => pair.Covered == tap).Select(pair => pair.Covering),
                Is.EquivalentTo(new[] { (HitObject)beatmap.HitObjects[1] }));
        }

        private static DivaBeatmap beatmapAt120()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            return beatmap;
        }

        private static DivaHoldHitObject addHold(DivaBeatmap beatmap, double startTime, double duration, DivaAction action = DivaAction.Circle)
        {
            var hold = new DivaHoldHitObject
            {
                StartTime = startTime,
                Duration = duration,
                Position = DivaActionEncoding.ToPlayfieldPosition(2, 12),
                ValidAction = action
            };

            beatmap.HitObjects.Add(hold);
            return hold;
        }

        private static DivaHitObject addTap(DivaBeatmap beatmap, double startTime, DivaAction action = DivaAction.Circle)
        {
            var tap = new DivaHitObject
            {
                StartTime = startTime,
                Position = DivaActionEncoding.ToPlayfieldPosition(3, 12),
                ValidAction = action
            };

            beatmap.HitObjects.Add(tap);
            return tap;
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
