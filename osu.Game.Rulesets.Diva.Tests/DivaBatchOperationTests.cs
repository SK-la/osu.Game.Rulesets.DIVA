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
    public class DivaBatchOperationTests
    {
        private const double beat_length = 500; // 120 BPM: one chart frame is 10.4167ms, one measure 2000ms.

        [Test]
        public void Simplify_to_cross_circle_down_right()
        {
            Assert.Multiple(() =>
            {
                // The reference editor's odd units step down into their even neighbour, keeping ✕◯↓→.
                Assert.That(DivaActionSimplification.ToCrossCircleDownRight(DivaAction.Square), Is.EqualTo(DivaAction.Circle));
                Assert.That(DivaActionSimplification.ToCrossCircleDownRight(DivaAction.Triangle), Is.EqualTo(DivaAction.Cross));
                Assert.That(DivaActionSimplification.ToCrossCircleDownRight(DivaAction.Left), Is.EqualTo(DivaAction.Right));
                Assert.That(DivaActionSimplification.ToCrossCircleDownRight(DivaAction.Up), Is.EqualTo(DivaAction.Down));
                Assert.That(DivaActionSimplification.ToCrossCircleDownRight(DivaAction.Circle), Is.EqualTo(DivaAction.Circle));
                Assert.That(DivaActionSimplification.ToCrossCircleDownRight(DivaAction.Cross), Is.EqualTo(DivaAction.Cross));
                Assert.That(DivaActionSimplification.ToCrossCircleDownRight(DivaAction.Right), Is.EqualTo(DivaAction.Right));
                Assert.That(DivaActionSimplification.ToCrossCircleDownRight(DivaAction.Down), Is.EqualTo(DivaAction.Down));
            });
        }

        [Test]
        public void Simplify_to_circle_and_right()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DivaActionSimplification.ToCircleAndRight(DivaAction.Square), Is.EqualTo(DivaAction.Circle));
                Assert.That(DivaActionSimplification.ToCircleAndRight(DivaAction.Triangle), Is.EqualTo(DivaAction.Circle));
                Assert.That(DivaActionSimplification.ToCircleAndRight(DivaAction.Cross), Is.EqualTo(DivaAction.Circle));
                Assert.That(DivaActionSimplification.ToCircleAndRight(DivaAction.Circle), Is.EqualTo(DivaAction.Circle));

                Assert.That(DivaActionSimplification.ToCircleAndRight(DivaAction.Left), Is.EqualTo(DivaAction.Right));
                Assert.That(DivaActionSimplification.ToCircleAndRight(DivaAction.Down), Is.EqualTo(DivaAction.Right));
                Assert.That(DivaActionSimplification.ToCircleAndRight(DivaAction.Up), Is.EqualTo(DivaAction.Right));
                Assert.That(DivaActionSimplification.ToCircleAndRight(DivaAction.Right), Is.EqualTo(DivaAction.Right));
            });
        }

        [Test]
        public void Symbols_and_arrows_swap_within_their_unit_pair()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DivaActionSimplification.ArrowToSymbol(DivaAction.Right), Is.EqualTo(DivaAction.Circle));
                Assert.That(DivaActionSimplification.ArrowToSymbol(DivaAction.Left), Is.EqualTo(DivaAction.Square));
                Assert.That(DivaActionSimplification.ArrowToSymbol(DivaAction.Down), Is.EqualTo(DivaAction.Cross));
                Assert.That(DivaActionSimplification.ArrowToSymbol(DivaAction.Up), Is.EqualTo(DivaAction.Triangle));

                Assert.That(DivaActionSimplification.SymbolToArrow(DivaAction.Circle), Is.EqualTo(DivaAction.Right));
                Assert.That(DivaActionSimplification.SymbolToArrow(DivaAction.Square), Is.EqualTo(DivaAction.Left));
                Assert.That(DivaActionSimplification.SymbolToArrow(DivaAction.Cross), Is.EqualTo(DivaAction.Down));
                Assert.That(DivaActionSimplification.SymbolToArrow(DivaAction.Triangle), Is.EqualTo(DivaAction.Up));
            });
        }

        [Test]
        public void Simplify_plan_rewrites_buttons_and_drops_the_duplicates_its_remap_creates()
        {
            var circle = note(0, DivaAction.Circle);
            var square = note(0, DivaAction.Square);
            var far = note(4000, DivaAction.Square);
            var beatmap = createBeatmap(circle, square, far);

            DivaActionSimplifyPlan plan = DivaActionSimplification.PlanActionSimplify(beatmap, DivaActionSimplification.ToCrossCircleDownRight);

            Assert.Multiple(() =>
            {
                // □ becomes ◯ on a frame that already plays ◯, so the □ note has to go — the kept note is the
                // one that already had the button (and keeps its position, flight and key).
                Assert.That(plan.Removals, Is.EquivalentTo(new[] { square }));
                Assert.That(plan.Changes, Does.Not.ContainKey(square));
                Assert.That(plan.Changes[far], Is.EqualTo(DivaAction.Circle));
                Assert.That(plan.Changes, Does.Not.ContainKey(circle));
            });
        }

        [Test]
        public void Simplify_plan_leaves_duplicates_of_untouched_buttons_alone()
        {
            // Two ◯ notes on one frame are placeable in the editor on purpose; a remap that does not touch
            // them must not start deleting them.
            var first = note(0, DivaAction.Circle);
            var second = note(0, DivaAction.Circle);
            var beatmap = createBeatmap(first, second);

            DivaActionSimplifyPlan plan = DivaActionSimplification.PlanActionSimplify(beatmap, DivaActionSimplification.ArrowToSymbol);

            Assert.Multiple(() =>
            {
                Assert.That(plan.Changes, Is.Empty);
                Assert.That(plan.Removals, Is.Empty);
            });
        }

        [Test]
        public void Frame_to_time_inverts_frame_lookup()
        {
            var beatmap = createBeatmap(note(2000, DivaAction.Circle));

            Assert.Multiple(() =>
            {
                Assert.That(DivaChartBuilder.TimeToFrame(beatmap, 2000), Is.EqualTo(192));
                Assert.That(DivaChartBuilder.FrameToTime(beatmap, 192), Is.EqualTo(2000).Within(1));
                Assert.That(DivaChartBuilder.FrameToTime(beatmap, 0), Is.EqualTo(0).Within(1));
            });
        }

        [Test]
        public void Time_nudge_moves_notes_and_events_by_frames()
        {
            var beatmap = createBeatmap(note(2000, DivaAction.Circle));
            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(beatmap);
            events.BgmEvents.Add(new DivaBgmEvent { Sequence = 0, TimeMs = 1000, Slot = 0, WavId = 0 });
            events.ResourceEvents.Add(new DivaResourceEvent { Sequence = 0, TimeMs = 1000, ResourceId = 0 });

            DivaTimeNudge? nudge = DivaChartBuilder.PlanTimeNudge(beatmap, -96);

            Assert.That(nudge, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(nudge!.Notes.Values.Single(), Is.EqualTo(1000).Within(1));
                Assert.That(nudge.Bgm.Values.Single(), Is.EqualTo(0).Within(1));
                Assert.That(nudge.Resources.Values.Single(), Is.EqualTo(0).Within(1));
            });
        }

        [Test]
        public void Time_nudge_refuses_to_push_content_out_of_the_chart()
        {
            var beatmap = createBeatmap(note(0, DivaAction.Circle));
            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(beatmap);
            events.BgmEvents.Add(new DivaBgmEvent { Sequence = 0, TimeMs = 1000, Slot = 0, WavId = 0 });

            Assert.Multiple(() =>
            {
                // The note sits on frame 0 and would leave the chart; the reference editor drops it there.
                Assert.That(DivaChartBuilder.PlanTimeNudge(beatmap, -1), Is.Null);
                Assert.That(DivaChartBuilder.PlanTimeNudge(beatmap, 0), Is.Null);
                Assert.That(DivaChartBuilder.PlanTimeNudge(beatmap, 1), Is.Not.Null);
            });
        }

        private static DivaHitObject note(double time, DivaAction action) => new DivaHitObject
        {
            StartTime = time,
            Position = DivaActionEncoding.ToPlayfieldPosition(8, 8),
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
