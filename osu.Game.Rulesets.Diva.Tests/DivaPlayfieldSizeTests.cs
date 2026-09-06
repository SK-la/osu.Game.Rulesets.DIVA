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
    }
}
