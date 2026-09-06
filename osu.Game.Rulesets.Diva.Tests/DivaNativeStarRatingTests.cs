// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Difficulty;
using osu.Game.Rulesets.Diva.Objects;
using osuTK;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaNativeStarRatingTests
    {
        [Test]
        public void FromHard_uses_chart_hard()
        {
            Assert.That(DivaNativeStarRating.FromHard(12), Is.EqualTo(12));
            Assert.That(DivaNativeStarRating.FromHard(0), Is.EqualTo(0));
            Assert.That(DivaNativeStarRating.FromHard(-1), Is.EqualTo(0));
        }

        [Test]
        public void Resolve_uses_black_star_from_difficulty_name()
        {
            var info = new BeatmapInfo
            {
                DifficultyName = DivaChartConstants.FormatDifficultyName(5, 12),
                Difficulty = new BeatmapDifficulty { OverallDifficulty = 8 }
            };

            Assert.That(DivaNativeStarRating.Resolve(info), Is.EqualTo(12));
        }

        [Test]
        public void Resolve_falls_back_to_overall_difficulty()
        {
            var info = new BeatmapInfo
            {
                DifficultyName = "Hard",
                Difficulty = new BeatmapDifficulty { OverallDifficulty = 8 }
            };

            Assert.That(DivaNativeStarRating.Resolve(info), Is.EqualTo(8));
        }

        [Test]
        public void Persisted_hard_differs_from_algorithmic_calculator_sr()
        {
            // Dual-path: Realm sort key is Hard; Calculate() returns algorithmic stars for the panel.
            double persisted = DivaNativeStarRating.FromHard(8);
            var beatmap = new Beatmap
            {
                BeatmapInfo =
                {
                    Ruleset = new DivaRuleset().RulesetInfo,
                    DifficultyName = DivaChartConstants.FormatDifficultyName(3, 8),
                    Difficulty = new BeatmapDifficulty { OverallDifficulty = 8 },
                    StarRating = persisted
                }
            };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            for (int i = 0; i < 32; i++)
            {
                beatmap.HitObjects.Add(new DivaHitObject
                {
                    StartTime = 1000 + i * 100,
                    ValidAction = (DivaAction)(i % 4),
                    ApproachPieceOriginPosition = new Vector2(300, 0)
                });
            }

            double algorithmic = new DivaDifficultyCalculator(new DivaRuleset().RulesetInfo, new FlatWorkingBeatmap(beatmap))
                .Calculate()
                .StarRating;

            Assert.That(beatmap.BeatmapInfo.StarRating, Is.EqualTo(persisted));
            Assert.That(algorithmic, Is.Not.EqualTo(persisted).Within(0.01));
            Assert.That(algorithmic, Is.InRange(4.5, 6.5));
        }
    }
}
