// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Diva.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaHitJudgementEvaluatorTests
    {
        [TestCase(0, HitResult.Perfect)]
        [TestCase(32.0, HitResult.Perfect)]
        [TestCase(32.01, HitResult.Great)]
        [TestCase(50.0, HitResult.Great)]
        [TestCase(50.01, HitResult.Good)]
        [TestCase(80.0, HitResult.Good)]
        [TestCase(80.01, HitResult.Ok)]
        [TestCase(120.0, HitResult.Ok)]
        [TestCase(120.01, HitResult.None)]
        public void GetResultFor_uses_expected_windows(double timeOffset, HitResult expected)
        {
            Assert.That(DivaHitJudgementEvaluator.GetResultFor(timeOffset), Is.EqualTo(expected));
            Assert.That(DivaHitJudgementEvaluator.GetResultFor(-timeOffset), Is.EqualTo(expected));
        }

        [TestCase(true, 0, HitResult.Perfect)]
        [TestCase(true, 32.0, HitResult.Perfect)]
        [TestCase(true, 32.01, HitResult.Great)]
        [TestCase(true, 50.0, HitResult.Great)]
        [TestCase(true, 50.01, HitResult.Good)]
        [TestCase(true, 80.0, HitResult.Good)]
        [TestCase(true, 80.01, HitResult.Ok)]
        [TestCase(true, 120.0, HitResult.Ok)]
        [TestCase(true, 120.01, HitResult.None)]
        [TestCase(false, 0, HitResult.Meh)]
        [TestCase(false, 32.0, HitResult.Meh)]
        [TestCase(false, 50.0, HitResult.Meh)]
        [TestCase(false, 80.0, HitResult.Meh)]
        [TestCase(false, 120.0, HitResult.Meh)]
        [TestCase(false, 120.01, HitResult.None)]
        public void GetPressResult_uses_timing_and_key_correctness(bool validPress, double timeOffset, HitResult expected)
        {
            Assert.That(DivaHitJudgementEvaluator.GetPressResult(validPress, timeOffset), Is.EqualTo(expected));
            Assert.That(DivaHitJudgementEvaluator.GetPressResult(validPress, -timeOffset), Is.EqualTo(expected));
        }

        [TestCase(HitResult.Perfect, DivaJudgementResult.DivaMehSource.PerfectWindowWrongPress)]
        [TestCase(HitResult.Great, DivaJudgementResult.DivaMehSource.GreatWindowWrongPress)]
        [TestCase(HitResult.Good, DivaJudgementResult.DivaMehSource.GoodWindowWrongPress)]
        [TestCase(HitResult.Ok, DivaJudgementResult.DivaMehSource.OkWindowWrongPress)]
        [TestCase(HitResult.Miss, DivaJudgementResult.DivaMehSource.None)]
        public void GetMehSourceFor_maps_sources(HitResult result, DivaJudgementResult.DivaMehSource expected)
        {
            Assert.That(DivaHitJudgementEvaluator.GetMehSourceFor(result), Is.EqualTo(expected));
        }

        [Test]
        public void Meh_window_is_not_obtainable_from_timing()
        {
            var hitWindows = new DivaHitWindows();

            Assert.That(hitWindows.WindowFor(HitResult.Meh), Is.EqualTo(0));
            Assert.That(hitWindows.IsHitResultAllowed(HitResult.Meh), Is.True);
            Assert.That(hitWindows.IsHitResultAllowed(HitResult.Miss), Is.True);
            Assert.That(hitWindows.WindowFor(HitResult.Miss), Is.EqualTo(0));
        }

        [TestCase(120.0, false)]
        [TestCase(120.01, true)]
        [TestCase(200.0, true)]
        public void ShouldMiss_matches_the_ok_boundary(double timeOffset, bool expected)
        {
            Assert.That(DivaHitJudgementEvaluator.ShouldMiss(timeOffset), Is.EqualTo(expected));
            Assert.That(DivaHitJudgementEvaluator.ShouldMiss(-timeOffset), Is.False);
        }
    }
}
