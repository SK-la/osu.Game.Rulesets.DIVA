// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Diva.Audio;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaHitSampleInfoTests
    {
        [Test]
        public void Normal_uses_hit_normal_lookup()
        {
            Assert.That(DivaHitSampleInfo.Normal.LookupNames.Single(), Is.EqualTo(DivaHitSampleInfo.NORMAL_LOOKUP));
        }

        [Test]
        public void CreateHit_scales_volume_like_project_diva_play_hit()
        {
            Assert.That(DivaHitSampleInfo.CreateHit(1).Volume, Is.EqualTo(75));
            Assert.That(DivaHitSampleInfo.CreateHit(2).Volume, Is.EqualTo(100));
            Assert.That(DivaHitSampleInfo.CreateHit(0).Volume, Is.EqualTo(50));
        }
    }

    [TestFixture]
    public class DivaSongResultEvaluatorTests
    {
        [Test]
        public void Failed_maps_to_mistake()
        {
            Assert.That(DivaSongResultEvaluator.Evaluate(true, 1000, 1000, emptyStats()), Is.EqualTo(DivaSongResult.Mistake));
        }

        [Test]
        public void Perfect_clear_maps_to_perfect()
        {
            var stats = new Dictionary<HitResult, int>
            {
                [HitResult.Perfect] = 10,
                [HitResult.Great] = 5
            };

            Assert.That(DivaSongResultEvaluator.Evaluate(false, 900, 1000, stats), Is.EqualTo(DivaSongResult.Perfect));
        }

        [Test]
        public void Score_ratio_maps_great_standard_cheap()
        {
            var stats = new Dictionary<HitResult, int> { [HitResult.Perfect] = 5, [HitResult.Good] = 1 };

            Assert.That(DivaSongResultEvaluator.Evaluate(false, 800, 1000, stats), Is.EqualTo(DivaSongResult.Great));
            Assert.That(DivaSongResultEvaluator.Evaluate(false, 600, 1000, stats), Is.EqualTo(DivaSongResult.Standard));
            Assert.That(DivaSongResultEvaluator.Evaluate(false, 400, 1000, stats), Is.EqualTo(DivaSongResult.Cheap));
        }

        private static Dictionary<HitResult, int> emptyStats() => new Dictionary<HitResult, int>();
    }
}
