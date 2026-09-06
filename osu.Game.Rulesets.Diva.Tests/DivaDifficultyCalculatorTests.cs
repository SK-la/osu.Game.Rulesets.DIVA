// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Diva.Difficulty;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaDifficultyCalculatorTests
    {
        [Test]
        public void DifficultyVersionIsRecalibrated()
        {
            Assert.That(calculator(createStream()).Version, Is.EqualTo(20260907));
        }

        [Test]
        public void DoublePressAlternativeIsNotAChord()
        {
            double singles = starRating(createStream(doublePress: false));
            double alternatives = starRating(createStream(doublePress: true));

            Assert.That(alternatives, Is.EqualTo(singles).Within(0.001));
        }

        [Test]
        public void RealChordIsHarderThanSinglePress()
        {
            double singles = starRating(createStream(chordWidth: 1));
            double chords = starRating(createStream(chordWidth: 2));

            Assert.That(chords, Is.GreaterThan(singles));
        }

        [Test]
        public void SimultaneousNoteOrderDoesNotAffectDifficulty()
        {
            double forward = starRating(createStream(chordWidth: 3, reverseChordOrder: false));
            double reversed = starRating(createStream(chordWidth: 3, reverseChordOrder: true));

            Assert.That(reversed, Is.EqualTo(forward).Within(0.000001));
        }

        [Test]
        public void ReadingUsesEachFutureNotesOwnPreempt()
        {
            var longPreempt = attributes(createBpmTransitionReadingMap(futureBpm: 60));
            var shortPreempt = attributes(createBpmTransitionReadingMap(futureBpm: 240));

            Assert.That(longPreempt.ReadingDifficulty, Is.GreaterThan(shortPreempt.ReadingDifficulty));
        }

        [Test]
        public void RelativeApproachDirectionDoesNotDependOnHitPosition()
        {
            double fixedPositions = starRating(createStream(scatterPositions: false));
            double scatteredPositions = starRating(createStream(scatterPositions: true));

            Assert.That(scatteredPositions, Is.EqualTo(fixedPositions).Within(0.000001));
        }

        [Test]
        public void FirstHoldContributesDifficulty()
        {
            DivaDifficultyAttributes result = attributes(createSingleHold());

            Assert.That(result.HoldDifficulty, Is.GreaterThan(0));
        }

        [Test]
        public void OverlappingHoldsIncreaseHoldDifficulty()
        {
            double isolated = attributes(createHoldPattern(duration: 150, spacing: 400)).HoldDifficulty;
            double overlapping = attributes(createHoldPattern(duration: 900, spacing: 400)).HoldDifficulty;

            Assert.That(overlapping, Is.GreaterThan(isolated));
        }

        [Test]
        public void CalibratedStarRatingAnchors()
        {
            DivaDifficultyAttributes slow = attributes(createStream(count: 32, spacingMs: 200, bpm: 240));
            DivaDifficultyAttributes normal = attributes(createStream(count: 32, spacingMs: 100, bpm: 120));
            DivaDifficultyAttributes chord = attributes(createStream(count: 32, spacingMs: 100, bpm: 120, chordWidth: 2));
            DivaDifficultyAttributes extremeAttrs = attributes(createStream(count: 64, spacingMs: 60, bpm: 60, chordWidth: 2));
            double slowSingle = slow.StarRating;
            double normalSingle = normal.StarRating;
            double denseChord = chord.StarRating;
            double extreme = extremeAttrs.StarRating;

            Assert.Multiple(() =>
            {
                Assert.That(slowSingle, Is.InRange(2.0, 3.5), $"slow single = {slowSingle:F3}★");
                Assert.That(normalSingle, Is.InRange(4.5, 6.5), $"normal single = {normalSingle:F3}★");
                Assert.That(denseChord, Is.InRange(7.5, 9.5), $"dense chord = {denseChord:F3}★");
                Assert.That(extreme, Is.GreaterThan(10), $"extreme = {extreme:F3}★");
            });
        }

        [Test]
        public void RepeatingSectionsHasBoundedLengthGrowth()
        {
            double shortMap = starRating(createStream(count: 32, spacingMs: 100));
            double longMap = starRating(createStream(count: 128, spacingMs: 100));

            Assert.That(longMap / shortMap, Is.LessThan(1.5));
        }

        private static DivaDifficultyCalculator calculator(IBeatmap beatmap)
            => new DivaDifficultyCalculator(new DivaRuleset().RulesetInfo, new FlatWorkingBeatmap(beatmap));

        private static DivaDifficultyAttributes attributes(IBeatmap beatmap)
            => (DivaDifficultyAttributes)calculator(beatmap).Calculate(Array.Empty<Mod>());

        private static double starRating(IBeatmap beatmap) => attributes(beatmap).StarRating;

        private static Beatmap createStream(
            int count = 32,
            double spacingMs = 100,
            double bpm = 120,
            int chordWidth = 1,
            bool doublePress = false,
            bool reverseChordOrder = false,
            bool scatterPositions = false)
        {
            Beatmap beatmap = createBeatmap(bpm);

            for (int i = 0; i < count; i++)
            {
                var notes = new List<DivaHitObject>();
                Vector2 position = scatterPositions
                    ? new Vector2(40 + (i % 8) * 50, 40 + (i % 5) * 35)
                    : new Vector2(240, 136);
                Vector2 approach = new Vector2(
                    MathF.Cos(i * 0.9f) * 300,
                    MathF.Sin(i * 0.9f) * 300);

                if (doublePress)
                {
                    notes.Add(new DoublePressButton
                    {
                        StartTime = 1000 + i * spacingMs,
                        Position = position,
                        ValidAction = (DivaAction)(i % 4),
                        DoubleAction = (DivaAction)(4 + (i % 4)),
                        ApproachPieceOriginPosition = approach
                    });
                }
                else
                {
                    for (int chordIndex = 0; chordIndex < chordWidth; chordIndex++)
                    {
                        notes.Add(new DivaHitObject
                        {
                            StartTime = 1000 + i * spacingMs,
                            Position = position,
                            ValidAction = (DivaAction)((i + chordIndex) % 4),
                            ApproachPieceOriginPosition = approach
                        });
                    }
                }

                if (reverseChordOrder)
                    notes.Reverse();

                beatmap.HitObjects.AddRange(notes);
            }

            return beatmap;
        }

        private static Beatmap createBpmTransitionReadingMap(double futureBpm)
        {
            Beatmap beatmap = createBeatmap(240);
            beatmap.ControlPointInfo.Add(2000, new TimingControlPoint { BeatLength = 60000 / futureBpm });
            beatmap.HitObjects.Add(new DivaHitObject
            {
                StartTime = 1000,
                ValidAction = DivaAction.Circle,
                ApproachPieceOriginPosition = new Vector2(300, 0)
            });

            for (int i = 0; i < 10; i++)
            {
                beatmap.HitObjects.Add(new DivaHitObject
                {
                    StartTime = 2500 + i * 100,
                    ValidAction = (DivaAction)(i % 4),
                    ApproachPieceOriginPosition = new Vector2(
                        MathF.Cos(i * 0.9f) * 300,
                        MathF.Sin(i * 0.9f) * 300)
                });
            }

            return beatmap;
        }

        private static Beatmap createSingleHold()
        {
            Beatmap beatmap = createBeatmap(120);
            beatmap.HitObjects.Add(new DivaHoldHitObject
            {
                StartTime = 1000,
                Duration = 800,
                ValidAction = DivaAction.Circle,
                ApproachPieceOriginPosition = new Vector2(300, 0)
            });
            return beatmap;
        }

        private static Beatmap createHoldPattern(double duration, double spacing)
        {
            Beatmap beatmap = createBeatmap(120);

            for (int i = 0; i < 12; i++)
            {
                beatmap.HitObjects.Add(new DivaHoldHitObject
                {
                    StartTime = 1000 + i * spacing,
                    Duration = duration,
                    ValidAction = (DivaAction)(i % 4),
                    ApproachPieceOriginPosition = new Vector2(300, 0)
                });
            }

            return beatmap;
        }

        private static Beatmap createBeatmap(double bpm)
        {
            var beatmap = new Beatmap
            {
                BeatmapInfo =
                {
                    Ruleset = new DivaRuleset().RulesetInfo,
                    Difficulty = new BeatmapDifficulty { OverallDifficulty = 8 }
                }
            };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 60000 / bpm });
            return beatmap;
        }
    }

    [TestFixture]
    public class DivaPerformanceCalculatorTests
    {
        [Test]
        public void PerfectPpHasCalibratedScale()
        {
            double fiveStar = calculatePp(createScore(100, 0), 5);
            double tenStar = calculatePp(createScore(100, 0), 10);

            Assert.Multiple(() =>
            {
                Assert.That(fiveStar, Is.InRange(250, 500), $"5★ SS = {fiveStar:F1}pp");
                Assert.That(tenStar, Is.InRange(1000, 2000), $"10★ SS = {tenStar:F1}pp");
            });
        }

        [Test]
        public void MissesReducePp()
        {
            double perfect = calculatePp(createScore(100, 0), 5);
            double misses = calculatePp(createScore(90, 10), 5);

            Assert.That(misses, Is.LessThan(perfect));
        }

        [Test]
        public void FailedScoreAwardsNoPp()
        {
            ScoreInfo score = createScore(100, 0);
            score.Passed = false;

            Assert.That(calculatePp(score, 10), Is.Zero);
        }

        [Test]
        public void UnplayedObjectsReducePp()
        {
            ScoreInfo complete = createScore(100, 0);
            ScoreInfo incomplete = createScore(50, 0, expected: 100);

            Assert.That(calculatePp(incomplete, 5), Is.LessThan(calculatePp(complete, 5) * 0.5));
        }

        [Test]
        public void SameMissRateHasNoSevereLengthBias()
        {
            double shortScore = calculatePp(createScore(90, 10), 5);
            double longScore = calculatePp(createScore(900, 100), 5);
            double ratio = longScore / shortScore;

            Assert.That(ratio, Is.InRange(0.85, 1.15));
        }

        [Test]
        public void NonJudgementStatisticsDoNotChangePp()
        {
            ScoreInfo normal = createScore(90, 10);
            ScoreInfo nonJudgementPadded = createScore(90, 10);
            nonJudgementPadded.Statistics[HitResult.SmallTickHit] = 500;
            nonJudgementPadded.MaximumStatistics[HitResult.SmallTickHit] = 500;

            Assert.That(calculatePp(nonJudgementPadded, 5), Is.EqualTo(calculatePp(normal, 5)).Within(0.000001));
        }

        private static double calculatePp(ScoreInfo score, double starRating)
        {
            var attributes = new DivaDifficultyAttributes
            {
                StarRating = starRating,
                MaxCombo = score.MaximumStatistics[HitResult.Perfect]
            };

            return new DivaPerformanceCalculator().Calculate(score, attributes).Total;
        }

        private static ScoreInfo createScore(int perfect, int misses, int? expected = null)
        {
            int expectedCount = expected ?? perfect + misses;
            double accuracy = expectedCount == 0 ? 0 : (double)perfect / expectedCount;

            return new ScoreInfo(new BeatmapInfo(), new DivaRuleset().RulesetInfo)
            {
                Accuracy = accuracy,
                MaxCombo = perfect,
                Passed = true,
                Statistics = new Dictionary<HitResult, int>
                {
                    [HitResult.Perfect] = perfect,
                    [HitResult.Miss] = misses
                },
                MaximumStatistics = new Dictionary<HitResult, int>
                {
                    [HitResult.Perfect] = expectedCount
                }
            };
        }
    }
}
