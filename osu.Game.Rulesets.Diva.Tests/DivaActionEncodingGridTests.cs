// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osuTK;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaActionEncodingGridTests
    {
        [TestCase(0f, 0f)]
        [TestCase(8f, 8f)]
        [TestCase(10f, 12f)]
        [TestCase(-1f, 3f)]
        public void Playfield_and_grid_round_trip_integer_cells(float gridX, float gridY)
        {
            Vector2 playfield = DivaActionEncoding.ToPlayfieldPosition(gridX, gridY);
            Vector2 grid = DivaActionEncoding.ToGridPosition(playfield);

            Assert.That(grid.X, Is.EqualTo(gridX).Within(1e-4f));
            Assert.That(grid.Y, Is.EqualTo(gridY).Within(1e-4f));
            Assert.That(DivaActionEncoding.ToPlayfieldPosition(grid.X, grid.Y), Is.EqualTo(playfield));
        }

        [Test]
        public void Fractional_grid_survives_round_trip()
        {
            Vector2 playfield = DivaActionEncoding.ToPlayfieldPosition(3.25f, 7.5f);
            Vector2 grid = DivaActionEncoding.ToGridPosition(playfield);

            Assert.That(grid.X, Is.EqualTo(3.25f).Within(1e-4f));
            Assert.That(grid.Y, Is.EqualTo(7.5f).Within(1e-4f));
        }

        [Test]
        public void SnapToGrid_rounds_to_integer_cells_by_default()
        {
            Vector2 snapped = DivaActionEncoding.SnapToGrid(DivaActionEncoding.ToPlayfieldPosition(3.4f, 7.6f));
            Vector2 grid = DivaActionEncoding.ToGridPosition(snapped);

            Assert.That(grid.X, Is.EqualTo(3f).Within(1e-4f));
            Assert.That(grid.Y, Is.EqualTo(8f).Within(1e-4f));
        }

        [Test]
        public void SnapToGrid_can_keep_fractional_cells()
        {
            Vector2 playfield = DivaActionEncoding.ToPlayfieldPosition(3.4f, 7.6f);

            Assert.That(DivaActionEncoding.SnapToGrid(playfield, integerCells: false), Is.EqualTo(playfield));
        }

        [Test]
        public void SnapToGrid_is_stable_after_repeated_application()
        {
            Vector2 first = DivaActionEncoding.SnapToGrid(new Vector2(123.7f, 88.2f));
            Vector2 second = DivaActionEncoding.SnapToGrid(first);
            Vector2 grid = DivaActionEncoding.ToGridPosition(first);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(grid.X, Is.EqualTo(MathF.Round(grid.X)));
            Assert.That(grid.Y, Is.EqualTo(MathF.Round(grid.Y)));
            Assert.That(DivaActionEncoding.ToPlayfieldPosition(grid.X, grid.Y), Is.EqualTo(first));
        }
    }
}
