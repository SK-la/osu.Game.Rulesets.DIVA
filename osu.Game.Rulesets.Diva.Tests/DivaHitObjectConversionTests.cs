// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Diva.Objects;
using osuTK;

namespace osu.Game.Rulesets.Diva.Tests
{
    /// <summary>
    ///     The reference editor keeps tap and hold as one record with an optional length, so converting between
    ///     them must not disturb anything else about the note.
    /// </summary>
    [TestFixture]
    public class DivaHitObjectConversionTests
    {
        [Test]
        public void Tap_becomes_a_hold_with_every_other_field_intact()
        {
            var tap = new DivaHitObject
            {
                StartTime = 1250,
                Position = new Vector2(97, 121),
                ValidAction = DivaAction.Triangle,
                ApproachPieceOriginPosition = new Vector2(-300, 120),
                WavKey = 7
            };

            DivaHoldHitObject hold = tap.AsHold(500);

            Assert.That(hold.StartTime, Is.EqualTo(1250));
            Assert.That(hold.Position, Is.EqualTo(new Vector2(97, 121)));
            Assert.That(hold.ValidAction, Is.EqualTo(DivaAction.Triangle));
            Assert.That(hold.ApproachPieceOriginPosition, Is.EqualTo(new Vector2(-300, 120)));
            Assert.That(hold.WavKey, Is.EqualTo(7));
            Assert.That(hold.Duration, Is.EqualTo(500));
        }

        [Test]
        public void Hold_becomes_a_tap_with_every_other_field_intact()
        {
            var hold = new DivaHoldHitObject
            {
                StartTime = 800,
                Duration = 750,
                Position = new Vector2(49, 64),
                ValidAction = DivaAction.Left,
                ApproachPieceOriginPosition = new Vector2(500, 0),
                WavKey = null
            };

            DivaHitObject tap = hold.AsTap();

            Assert.That(tap, Is.Not.TypeOf<DivaHoldHitObject>());
            Assert.That(tap.StartTime, Is.EqualTo(800));
            Assert.That(tap.Position, Is.EqualTo(new Vector2(49, 64)));
            Assert.That(tap.ValidAction, Is.EqualTo(DivaAction.Left));
            Assert.That(tap.ApproachPieceOriginPosition, Is.EqualTo(new Vector2(500, 0)));
            Assert.That(tap.WavKey, Is.Null, "an inherited key stays inherited rather than turning into an override");
        }
    }
}
