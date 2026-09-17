// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Diva.Replays;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaAutoGeneratorTests
    {
        [Test]
        public void Simultaneous_notes_all_reach_one_frame()
        {
            var beatmap = new Beatmap<DivaHitObject>();

            DivaAction[] chord = { DivaAction.Square, DivaAction.Triangle, DivaAction.Circle, DivaAction.Cross, DivaAction.Left, DivaAction.Up, DivaAction.Right, DivaAction.Down };

            foreach (DivaAction action in chord)
                beatmap.HitObjects.Add(new DivaHitObject { StartTime = 1000, ValidAction = action });

            List<DivaReplayFrame> frames = generateFrames(beatmap);

            // Replay input is diffed frame-to-frame and a frame carries one action set, so a second frame at the same
            // instant would silently drop every note but the last one.
            Assert.That(frames.Count(f => f.Time == 1000), Is.EqualTo(1));
            Assert.That(actionsAt(frames, 1000), Is.EquivalentTo(chord));
        }

        [Test]
        public void Repeated_button_is_released_between_notes()
        {
            var beatmap = new Beatmap<DivaHitObject>
            {
                HitObjects =
                {
                    new DivaHitObject { StartTime = 1000, ValidAction = DivaAction.Circle },
                    new DivaHitObject { StartTime = 1100, ValidAction = DivaAction.Circle },
                }
            };

            List<DivaReplayFrame> frames = generateFrames(beatmap);

            Assert.That(actionsAt(frames, 1000), Is.EquivalentTo(new[] { DivaAction.Circle }));

            // Without an intervening release the second note produces no press diff and is missed.
            Assert.That(actionsAt(frames, 1020), Is.Empty);
            Assert.That(actionsAt(frames, 1100), Is.EquivalentTo(new[] { DivaAction.Circle }));
        }

        [Test]
        public void Double_press_button_holds_both_actions()
        {
            var beatmap = new Beatmap<DivaHitObject>
            {
                HitObjects =
                {
                    new DoublePressButton { StartTime = 1000, ValidAction = DivaAction.Circle, DoubleAction = DivaAction.Right },
                }
            };

            Assert.That(actionsAt(generateFrames(beatmap), 1000), Is.EquivalentTo(new[] { DivaAction.Circle, DivaAction.Right }));
        }

        [Test]
        public void Hold_is_released_exactly_on_its_tail()
        {
            var beatmap = new Beatmap<DivaHitObject>
            {
                HitObjects =
                {
                    new DivaHoldHitObject { StartTime = 1000, Duration = 500, ValidAction = DivaAction.Circle },
                }
            };

            List<DivaReplayFrame> frames = generateFrames(beatmap);

            Assert.That(actionsAt(frames, 1000), Is.EquivalentTo(new[] { DivaAction.Circle }));
            Assert.That(actionsAt(frames, 1500), Is.Empty, "the strip tail is judged, so it must be released on time");
        }

        [Test]
        public void Hold_release_does_not_swallow_a_note_starting_on_the_tail()
        {
            var beatmap = new Beatmap<DivaHitObject>
            {
                HitObjects =
                {
                    new DivaHoldHitObject { StartTime = 1000, Duration = 500, ValidAction = DivaAction.Circle },
                    new DivaHitObject { StartTime = 1500, ValidAction = DivaAction.Circle },
                }
            };

            List<DivaReplayFrame> frames = generateFrames(beatmap);

            // A release and a press at the same instant cancel out in the frame diff, losing the tap.
            Assert.That(frames.Single(f => f.Time == 1500).Actions, Is.EquivalentTo(new[] { DivaAction.Circle }));
            Assert.That(actionsAt(frames, 1499), Is.Empty);
        }

        [Test]
        public void Frames_are_emitted_in_time_order()
        {
            var beatmap = new Beatmap<DivaHitObject>
            {
                HitObjects =
                {
                    new DivaHitObject { StartTime = 100, ValidAction = DivaAction.Circle },
                    new DivaHitObject { StartTime = 100, ValidAction = DivaAction.Cross },
                    new DivaHoldHitObject { StartTime = 900, Duration = 300, ValidAction = DivaAction.Triangle },
                    new DivaHitObject { StartTime = 5000, ValidAction = DivaAction.Cross },
                }
            };

            List<DivaReplayFrame> frames = generateFrames(beatmap);

            Assert.That(frames.Select(f => f.Time), Is.Ordered);
        }

        private static List<DivaReplayFrame> generateFrames(Beatmap<DivaHitObject> beatmap)
            => new DivaAutoGenerator(beatmap).Generate().Frames.Cast<DivaReplayFrame>().ToList();

        private static List<DivaAction> actionsAt(List<DivaReplayFrame> frames, double time)
            => frames.Single(f => f.Time == time).Actions;
    }
}
