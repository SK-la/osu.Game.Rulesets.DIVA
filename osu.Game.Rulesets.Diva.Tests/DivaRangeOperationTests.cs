// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects;
using osuTK;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaRangeOperationTests
    {
        private const double beat_length = 500; // 120 BPM: one chart frame is 10.4167ms, one measure 2000ms.

        [Test]
        public void Frame_range_picks_notes_by_their_start_frame()
        {
            var before = note(0);
            var inside = note(2000);
            var after = note(4000);
            var beatmap = createBeatmap(before, inside, after);

            Assert.Multiple(() =>
            {
                Assert.That(DivaRangeOperations.NotesInFrameRange(beatmap, 100, 300), Is.EquivalentTo(new[] { inside }));
                Assert.That(DivaRangeOperations.NotesInFrameRange(beatmap, 300, 100), Is.EquivalentTo(new[] { inside }), "the range is read in either order");
                Assert.That(DivaRangeOperations.NotesInFrameRange(beatmap, 192, 192), Is.EquivalentTo(new[] { inside }), "the range includes both of its ends");
                Assert.That(DivaRangeOperations.NotesAtFrame(beatmap, 0), Is.EquivalentTo(new[] { before }));
                Assert.That(DivaRangeOperations.NotesAtFrame(beatmap, 191), Is.Empty);
            });
        }

        [Test]
        public void A_hold_is_selected_by_its_start_frame_only()
        {
            // The reference editor keeps one record per frame, so a hold belongs to the frame it starts on —
            // its tail is part of no interval of its own.
            var hold = new DivaHoldHitObject
            {
                StartTime = 0,
                Duration = 2000,
                Position = position,
                ValidAction = DivaAction.Circle
            };

            var beatmap = createBeatmap(hold);

            Assert.Multiple(() =>
            {
                Assert.That(DivaRangeOperations.NotesAtFrame(beatmap, 0), Is.EquivalentTo(new[] { hold }));
                Assert.That(DivaRangeOperations.NotesInFrameRange(beatmap, 1, 200), Is.Empty);
            });
        }

        [Test]
        public void Range_copy_moves_notes_by_the_offset_its_target_implies()
        {
            var source = note(2000, DivaAction.Square);
            var beatmap = createBeatmap(source);

            DivaRangeCopyPlan? plan = DivaRangeOperations.PlanRangeCopy(beatmap, 192, 192, 384);

            Assert.That(plan, Is.Not.Null);
            Assert.Multiple(() =>
            {
                DivaHitObject copy = plan!.Notes.Single();
                Assert.That(copy, Is.Not.SameAs(source), "the original stays in place");
                Assert.That(DivaChartBuilder.TimeToFrame(beatmap, copy.StartTime), Is.EqualTo(384));
                Assert.That(copy.StartTime, Is.EqualTo(4000).Within(1));
                Assert.That(copy.ValidAction, Is.EqualTo(DivaAction.Square));
                Assert.That(copy.Position, Is.EqualTo(source.Position));
                Assert.That(copy.ApproachPieceOriginPosition, Is.EqualTo(source.ApproachPieceOriginPosition));
                Assert.That(beatmap.HitObjects, Has.Count.EqualTo(1), "planning does not touch the beatmap");
            });
        }

        [Test]
        public void Range_copy_keeps_a_holds_frame_length()
        {
            var hold = new DivaHoldHitObject
            {
                StartTime = 2000,
                Duration = 1000, // 96 frames at 120 BPM.
                Position = position,
                ValidAction = DivaAction.Circle
            };

            var beatmap = createBeatmap(hold);

            DivaRangeCopyPlan? plan = DivaRangeOperations.PlanRangeCopy(beatmap, 192, 192, 0);

            Assert.That(plan, Is.Not.Null);
            var copy = (DivaHoldHitObject)plan!.Notes.Single();

            Assert.Multiple(() =>
            {
                Assert.That(copy.StartTime, Is.EqualTo(0).Within(1));
                Assert.That(DivaChartBuilder.FrameLengthAt(beatmap, copy.StartTime, copy.Duration), Is.EqualTo(96));
            });
        }

        [Test]
        public void Range_copy_drops_what_would_leave_the_chart()
        {
            var start = note(0);
            var end = note(4000);
            var beatmap = createBeatmap(start, end);

            // Moving the range to frame 0 shifts the last note 192 frames earlier; pushing it past the end of
            // the chart instead drops only the notes that overflow.
            DivaRangeCopyPlan? shifted = DivaRangeOperations.PlanRangeCopy(beatmap, 0, 384, 0);
            Assert.That(shifted, Is.Not.Null);
            Assert.That(shifted!.Notes, Has.Count.EqualTo(2), "an offset of zero re-copies everything");

            DivaRangeCopyPlan? overflowing = DivaRangeOperations.PlanRangeCopy(beatmap, 0, 384, DivaChartConstants.MAX_FRAME_INDEX - 100);
            Assert.That(overflowing, Is.Not.Null);
            Assert.That(overflowing!.Notes.Select(n => DivaChartBuilder.TimeToFrame(beatmap, n.StartTime)).ToArray(),
                        Is.EqualTo(new[] { DivaChartConstants.MAX_FRAME_INDEX - 100 }),
                        "only the copy that fits is kept");

            Assert.That(DivaRangeOperations.PlanRangeCopy(beatmap, 0, 384, DivaChartConstants.MAX_FRAME_INDEX + 1), Is.Null);
            Assert.That(DivaRangeOperations.PlanRangeCopy(beatmap, 0, 384, -1), Is.Null);
        }

        [Test]
        public void Range_copy_takes_bgm_and_resource_events_along()
        {
            var beatmap = createBeatmap(note(2000));
            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(beatmap);
            events.BgmEvents.Add(new DivaBgmEvent { Sequence = 0, TimeMs = 2000, Slot = 1, WavId = 7 });
            events.ResourceEvents.Add(new DivaResourceEvent { Sequence = 0, TimeMs = 2000, ResourceId = 3 });
            events.BgmEvents.Add(new DivaBgmEvent { Sequence = 1, TimeMs = 6000, Slot = 0, WavId = 0 });

            DivaRangeCopyPlan? plan = DivaRangeOperations.PlanRangeCopy(beatmap, 192, 192, 0);

            Assert.That(plan, Is.Not.Null);
            Assert.Multiple(() =>
            {
                DivaBgmEvent bgm = plan!.BgmEvents.Single();
                Assert.That(bgm.TimeMs, Is.EqualTo(0).Within(1));
                Assert.That(bgm.Slot, Is.EqualTo(1));
                Assert.That(bgm.WavId, Is.EqualTo(7));

                Assert.That(plan.ResourceEvents.Single().TimeMs, Is.EqualTo(0).Within(1));
                Assert.That(plan.ResourceEvents.Single().ResourceId, Is.EqualTo(3));
                Assert.That(events.BgmEvents, Has.Count.EqualTo(2), "planning does not touch the events");
            });
        }

        private static readonly Vector2 position = DivaActionEncoding.ToPlayfieldPosition(8, 8);

        private static DivaHitObject note(double time, DivaAction action = DivaAction.Circle) => new DivaHitObject
        {
            StartTime = time,
            Position = position,
            ValidAction = action,
            ApproachPieceOriginPosition = new Vector2(400, 0)
        };

        private static DivaBeatmap createBeatmap(params DivaHitObject[] notes)
        {
            var beatmap = new DivaBeatmap
            {
                BeatmapInfo = { Ruleset = new DivaRuleset().RulesetInfo }
            };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = beat_length });

            foreach (DivaHitObject note in notes)
                beatmap.HitObjects.Add(note);

            return beatmap;
        }
    }
}
