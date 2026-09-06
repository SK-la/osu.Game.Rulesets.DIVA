// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Difficulty;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaDifficultyCalculatorTests
    {
        [Test]
        public void ResolveNativeStarRating_uses_black_star_from_difficulty_name()
        {
            var info = new BeatmapInfo
            {
                DifficultyName = "★12 Extreme",
                Difficulty = new BeatmapDifficulty { OverallDifficulty = 10 }
            };

            Assert.That(DivaDifficultyCalculator.ResolveNativeStarRating(info), Is.EqualTo(12));
        }

        [Test]
        public void ResolveNativeStarRating_falls_back_to_overall_difficulty()
        {
            var info = new BeatmapInfo
            {
                DifficultyName = "Hard",
                Difficulty = new BeatmapDifficulty { OverallDifficulty = 8 }
            };

            Assert.That(DivaDifficultyCalculator.ResolveNativeStarRating(info), Is.EqualTo(8));
        }
    }
}
