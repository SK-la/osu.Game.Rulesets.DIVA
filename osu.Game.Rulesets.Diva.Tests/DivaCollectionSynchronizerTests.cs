// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Diva.Beatmaps;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaCollectionSynchronizerTests
    {
        [TestCase(@"E:\Songs\ProjectDIVA", "ProjectDIVA")]
        [TestCase(@"E:\Songs\ProjectDIVA\", "ProjectDIVA")]
        [TestCase(@"E:/Songs/My Diva Lib/", "My Diva Lib")]
        public void CollectionNameForPath_uses_folder_basename(string path, string expected)
        {
            Assert.That(DivaCollectionSynchronizer.CollectionNameForPath(path), Is.EqualTo(expected));
        }

        [Test]
        public void CollectionNameForPath_falls_back_to_default()
        {
            Assert.That(DivaCollectionSynchronizer.CollectionNameForPath(""), Is.EqualTo(DivaCollectionSynchronizer.DEFAULT_COLLECTION_NAME));
        }
    }
}
