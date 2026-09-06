// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Difficulty;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaOsuDifficultyWorkingBeatmapTests
    {
        [Test]
        public void ConvertToOsuBeatmap_flattens_holds_to_hit_circles()
        {
            var source = new Beatmap
            {
                HitObjects =
                {
                    new DivaHitObject { StartTime = 100, Position = new Vector2(100, 100) },
                    new DivaHoldHitObject { StartTime = 200, Duration = 500, Position = new Vector2(200, 200) },
                }
            };

            var osu = DivaOsuDifficultyWorkingBeatmap.ConvertToOsuBeatmap(source);

            Assert.That(osu.HitObjects.Count, Is.EqualTo(2));
            Assert.That(osu.HitObjects, Has.All.TypeOf<HitCircle>());
            Assert.That(osu.HitObjects[0].StartTime, Is.EqualTo(100));
            Assert.That(osu.HitObjects[1].StartTime, Is.EqualTo(200));
            Assert.That(((HitCircle)osu.HitObjects[0]).Position, Is.EqualTo(new Vector2(100, 100)));
        }
    }
}
