// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Diva.UI;
using osuTK;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaPlayfieldSizeTests
    {
        [Test]
        public void Compute_Empty_UsesNativeDefault()
        {
            Assert.That(DivaPlayfieldSize.Compute([]), Is.EqualTo(DivaPlayfieldSize.DefaultNativeSize));
        }

        [Test]
        public void Compute_NativePositions_KeepsNativeField()
        {
            var notes = new[]
            {
                new DivaHitObject { Position = new Vector2(240, 136) },
                new DivaHitObject { Position = new Vector2(400, 200) },
            };

            Assert.That(DivaPlayfieldSize.Compute(notes), Is.EqualTo(DivaPlayfieldSize.DefaultNativeSize));
        }

        [Test]
        public void Compute_StdBottomEdge_ExpandsHeight()
        {
            var notes = new[]
            {
                new DivaHitObject { Position = new Vector2(256, 384) },
            };

            Vector2 size = DivaPlayfieldSize.Compute(notes);

            Assert.That(size.X, Is.EqualTo(DivaPlayfieldSize.DefaultNativeSize.X));
            Assert.That(size.Y, Is.EqualTo(384 + DivaPlayfieldSize.EDGE_PADDING));
        }

        [Test]
        public void Compute_StdCorner_ExpandsBothAxes()
        {
            var notes = new[]
            {
                new DivaHitObject { Position = new Vector2(512, 384) },
            };

            Vector2 size = DivaPlayfieldSize.Compute(notes);

            Assert.That(size.X, Is.EqualTo(512 + DivaPlayfieldSize.EDGE_PADDING));
            Assert.That(size.Y, Is.EqualTo(384 + DivaPlayfieldSize.EDGE_PADDING));
        }

        [Test]
        public void IsInsideDrawRange_matches_project_diva_20px_margin()
        {
            Vector2 size = DivaPlayfieldSize.DefaultNativeSize;
            const float note_size = 24;

            Assert.That(DivaPlayfieldSize.IsInsideDrawRange(new Vector2(-19.9f, 100), size, note_size), Is.True);
            Assert.That(DivaPlayfieldSize.IsInsideDrawRange(new Vector2(-20.1f, 100), size, note_size), Is.False);

            Assert.That(DivaPlayfieldSize.IsInsideDrawRange(new Vector2(100, size.Y + 19.9f), size, note_size), Is.True);
            Assert.That(DivaPlayfieldSize.IsInsideDrawRange(new Vector2(100, size.Y + 20.1f), size, note_size), Is.False);

            Assert.That(DivaPlayfieldSize.IsInsideDrawRange(new Vector2(size.X + 19.9f, 100), size, note_size), Is.True);
            Assert.That(DivaPlayfieldSize.IsInsideDrawRange(new Vector2(size.X + 20.1f, 100), size, note_size), Is.False);

            Assert.That(DivaPlayfieldSize.IsInsideDrawRange(new Vector2(100, -20.1f), size, note_size), Is.False);
        }

        [Test]
        public void IsInsideDrawRange_widens_the_margin_for_large_notes()
        {
            Vector2 size = DivaPlayfieldSize.DefaultNativeSize;

            // note_size 64 → margin 32, so a piece 30px outside is still drawn.
            Assert.That(DivaPlayfieldSize.IsInsideDrawRange(new Vector2(-30, 100), size, 64), Is.True);
            Assert.That(DivaPlayfieldSize.IsInsideDrawRange(new Vector2(-33, 100), size, 64), Is.False);
        }
    }
}
