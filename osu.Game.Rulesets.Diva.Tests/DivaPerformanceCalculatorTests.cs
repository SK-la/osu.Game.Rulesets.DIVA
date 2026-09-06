// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Diva.Difficulty;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaPerformanceCalculatorTests
    {
        [Test]
        public void Ruleset_exposes_performance_calculator()
        {
            Assert.That(new DivaRuleset().CreatePerformanceCalculator(), Is.InstanceOf<DivaPerformanceCalculator>());
        }

        [Test]
        public void Perfect_diva_statistics_produce_positive_pp()
        {
            var beatmapInfo = new BeatmapInfo
            {
                Difficulty = new BeatmapDifficulty
                {
                    ApproachRate = 9,
                    OverallDifficulty = 8,
                    DrainRate = 5,
                }
            };

            var attributes = new OsuDifficultyAttributes
            {
                StarRating = 5,
                MaxCombo = 100,
                AimDifficulty = 2.5,
                SpeedDifficulty = 2.5,
                SpeedNoteCount = 100,
                HitCircleCount = 100,
            };

            var score = new ScoreInfo(beatmapInfo, new DivaRuleset().RulesetInfo)
            {
                Accuracy = 1,
                MaxCombo = 100,
                Passed = true,
                Statistics = new Dictionary<HitResult, int> { [HitResult.Perfect] = 100 },
                MaximumStatistics = new Dictionary<HitResult, int> { [HitResult.Perfect] = 100 },
            };

            PerformanceAttributes result = new DivaPerformanceCalculator().Calculate(score, attributes);

            Assert.That(result.Total, Is.GreaterThan(0));
        }
    }
}
