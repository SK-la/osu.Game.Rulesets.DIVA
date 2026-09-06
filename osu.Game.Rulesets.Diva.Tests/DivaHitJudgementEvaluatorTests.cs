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

        [Test]
        public void Hold_head_press_must_use_start_relative_offset()
        {
            // Framework feeds Time.Current - EndTime into CheckForResult for IHasDuration.
            // A perfect head press therefore arrives as -Duration and must be remapped.
            const double duration = 500;
            const double framework_offset_at_head = -duration;

            Assert.That(DivaHitJudgementEvaluator.GetHoldPressResult(true, framework_offset_at_head), Is.EqualTo(HitResult.None),
                "Raw EndTime offset would ignore a correct head press and later Miss.");
            Assert.That(DivaHitJudgementEvaluator.GetHoldPressResult(true, framework_offset_at_head + duration), Is.EqualTo(HitResult.Perfect));
            Assert.That(DivaHitJudgementEvaluator.ShouldMissHold(framework_offset_at_head + duration + 350.01), Is.True);
        }

        [TestCase(0, HitResult.Perfect)]
        [TestCase(50.0, HitResult.Perfect)]
        [TestCase(50.01, HitResult.Great)]
        [TestCase(100.0, HitResult.Great)]
        [TestCase(100.01, HitResult.Good)]
        [TestCase(200.0, HitResult.Good)]
        [TestCase(200.01, HitResult.Ok)]
        [TestCase(300.0, HitResult.Ok)]
        [TestCase(300.01, HitResult.None)]
        public void GetHoldResultFor_matches_project_diva_strip_windows(double timeOffset, HitResult expected)
        {
            Assert.That(DivaHitJudgementEvaluator.GetHoldResultFor(timeOffset), Is.EqualTo(expected));
            Assert.That(DivaHitJudgementEvaluator.GetHoldResultFor(-timeOffset), Is.EqualTo(expected));
        }

        [TestCase(HitResult.Perfect, HitResult.Great, HitResult.Great)]
        [TestCase(HitResult.Ok, HitResult.Perfect, HitResult.Ok)]
        [TestCase(HitResult.Perfect, HitResult.Miss, HitResult.Miss)]
        public void CombineHoldResults_keeps_the_worse_grade(HitResult head, HitResult release, HitResult expected)
        {
            Assert.That(DivaHitJudgementEvaluator.CombineHoldResults(head, release), Is.EqualTo(expected));
        }

        [TestCase(350.0, false)]
        [TestCase(350.01, true)]
        public void ShouldMissHold_matches_project_diva_delay_timeout(double timeOffset, bool expected)
        {
            Assert.That(DivaHitJudgementEvaluator.ShouldMissHold(timeOffset), Is.EqualTo(expected));
            Assert.That(DivaHitJudgementEvaluator.ShouldMissHold(-timeOffset), Is.False);
        }
    }
}
