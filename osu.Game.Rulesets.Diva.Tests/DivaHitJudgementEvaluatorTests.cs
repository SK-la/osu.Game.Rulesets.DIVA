using NUnit.Framework;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Diva.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public partial class DivaHitJudgementEvaluatorTests
    {
        [TestCase(0, HitResult.Perfect)]
        [TestCase(22.5, HitResult.Perfect)]
        [TestCase(22.51, HitResult.Great)]
        [TestCase(45.0, HitResult.Great)]
        [TestCase(45.01, HitResult.Good)]
        [TestCase(90.0, HitResult.Good)]
        [TestCase(90.01, HitResult.Ok)]
        [TestCase(135.0, HitResult.Ok)]
        [TestCase(135.01, HitResult.None)]
        public void GetResultFor_uses_expected_windows(double timeOffset, HitResult expected)
        {
            Assert.That(DivaHitJudgementEvaluator.GetResultFor(timeOffset), Is.EqualTo(expected));
            Assert.That(DivaHitJudgementEvaluator.GetResultFor(-timeOffset), Is.EqualTo(expected));
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
        }
    }
}

