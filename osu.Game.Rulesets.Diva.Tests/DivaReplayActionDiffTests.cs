// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Rulesets.Diva.Replays;

namespace osu.Game.Rulesets.Diva.Tests
{
    /// <summary>
    /// A frame can hold many copies of one button (a chord of stacked notes), so the replay diff has to keep
    /// repeats instead of collapsing them the way <c>Except</c> would.
    /// </summary>
    [TestFixture]
    public class DivaReplayActionDiffTests
    {
        [Test]
        public void Repeated_presses_are_diffed_per_occurrence()
        {
            var released = new List<DivaAction>();
            var pressed = new List<DivaAction>();

            DivaReplayActionDiff.Compute(actions(), actions(DivaAction.Circle, DivaAction.Circle), released, pressed);

            Assert.That(released, Is.Empty);
            Assert.That(pressed, Is.EqualTo(new[] { DivaAction.Circle, DivaAction.Circle }));
        }

        [Test]
        public void Dropping_one_of_two_holds_releases_once()
        {
            var released = new List<DivaAction>();
            var pressed = new List<DivaAction>();

            DivaReplayActionDiff.Compute(actions(DivaAction.Circle, DivaAction.Circle), actions(DivaAction.Circle), released, pressed);

            Assert.That(released, Is.EqualTo(new[] { DivaAction.Circle }));
            Assert.That(pressed, Is.Empty);
        }

        [Test]
        public void Unchanged_frames_emit_nothing()
        {
            var released = new List<DivaAction>();
            var pressed = new List<DivaAction>();

            DivaReplayActionDiff.Compute(actions(DivaAction.Circle, DivaAction.Up), actions(DivaAction.Up, DivaAction.Circle), released, pressed);

            Assert.That(released, Is.Empty);
            Assert.That(pressed, Is.Empty);
        }

        [Test]
        public void Mixed_chord_diffs_each_button_separately()
        {
            var released = new List<DivaAction>();
            var pressed = new List<DivaAction>();

            DivaReplayActionDiff.Compute(actions(DivaAction.Circle), actions(DivaAction.Circle, DivaAction.Up, DivaAction.Up), released, pressed);

            Assert.That(released, Is.Empty);
            Assert.That(pressed, Is.EqualTo(new[] { DivaAction.Up, DivaAction.Up }));
        }

        [Test]
        public void Differing_actions_release_every_occurrence()
        {
            var released = new List<DivaAction>();
            var pressed = new List<DivaAction>();

            DivaReplayActionDiff.Compute(actions(DivaAction.Circle, DivaAction.Circle, DivaAction.Up), actions(), released, pressed);

            Assert.That(released, Is.EquivalentTo(new[] { DivaAction.Circle, DivaAction.Circle, DivaAction.Up }));
            Assert.That(pressed, Is.Empty);
        }

        private static List<DivaAction> actions(params DivaAction[] actions) => new List<DivaAction>(actions);
    }
}
