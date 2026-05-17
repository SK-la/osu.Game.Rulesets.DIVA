// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Diva.Audio;

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
        public void Sweep_uses_sweep_lookup()
        {
            Assert.That(DivaHitSampleInfo.Sweep.LookupNames.Single(), Is.EqualTo(DivaHitSampleInfo.SWEEP_LOOKUP));
        }
    }
}
