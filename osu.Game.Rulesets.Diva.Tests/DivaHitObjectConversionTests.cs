// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Objects;
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
        [TestCase(false)]
        [TestCase(true)]
        public void Clone_through_base_type_preserves_diva_state(bool isHold)
        {
            DivaHitObject source = isHold ? new DivaHoldHitObject { Duration = 750 } : new DivaHitObject();
            source.StartTime = 800;
            source.Position = new Vector2(49, 64);
            source.ValidAction = DivaAction.Left;
            source.ApproachPieceOriginPosition = new Vector2(500, 0);
            source.WavKey = 7;

            var clone = (DivaHitObject)((HitObject)source).Clone();

            Assert.Multiple(() =>
            {
                Assert.That(clone, Is.Not.SameAs(source));
                Assert.That(clone.GetType(), Is.EqualTo(source.GetType()));
                Assert.That(clone.StartTime, Is.EqualTo(source.StartTime));
                Assert.That(clone.Position, Is.EqualTo(source.Position));
                Assert.That(clone.ValidAction, Is.EqualTo(source.ValidAction));
                Assert.That(clone.ApproachPieceOriginPosition, Is.EqualTo(source.ApproachPieceOriginPosition));
                Assert.That(clone.WavKey, Is.EqualTo(source.WavKey));
                if (isHold)
                    Assert.That(((DivaHoldHitObject)clone).Duration, Is.EqualTo(750));
            });
        }

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
