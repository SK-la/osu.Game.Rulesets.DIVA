// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osuTK;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaChartFileParserTests
    {
        [TestCase(1, 0, "Easy")]
        [TestCase(2, 8, "★8 Normal")]
        [TestCase(5, 12, "★12 Extreme")]
        [TestCase(3, -1, "Hard")]
        public void FormatDifficultyName_matches_bms_style_black_stars(int level, int hard, string expected)
        {
            Assert.That(DivaChartConstants.FormatDifficultyName(level, hard), Is.EqualTo(expected));
        }

        [Test]
        public void ResolveAction_uses_type_not_wav_key()
        {
            // type=0 Circle, key=3 is only a WAV index — must remain Circle.
            var keyMisleading = new DivaChartNote { Type = 0, Key = 3 };
            Assert.That(DivaActionEncoding.ResolveAction(keyMisleading), Is.EqualTo(DivaAction.Circle));

            var triangle = new DivaChartNote { Type = 3, Key = 0 };
            Assert.That(DivaActionEncoding.ResolveAction(triangle), Is.EqualTo(DivaAction.Triangle));

            var holdRight = new DivaChartNote { Type = 12, Key = 0 }; // 8+4 = RIGHT hold
            Assert.That(DivaActionEncoding.ResolveAction(holdRight), Is.EqualTo(DivaAction.Right));
        }

        [Test]
        public void Exporter_embeds_action_from_type_and_approach_suffix()
        {
            string chartText = """
                               1.0.4.8
                               Export Song
                               Mapper
                               Artist
                               Style
                               bg.png
                               1
                               3
                               140
                               1
                               0 140
                               -1
                               -1
                               -1
                               0 0 8 8 0 0 3
                               48 3 10 10 100 50 -1
                               -1
                               0 audio.ogg
                               -1
                               0 bg.png
                               -1
                               -1 -1
                               """;

            using var reader = new StringReader(chartText);
            DivaChart chart = DivaChartFileParser.Parse(reader, "a.diva", @"C:\songs\a");
            string osu = DivaToOsuExporter.ExportToString(chart);

            Assert.That(osu, Does.Contain("osu file format v14"));
            Assert.That(osu, Does.Contain("Mode: 0"));
            Assert.That(osu, Does.Contain(DivaActionEncoding.NATIVE_TAG));
            // type=0 → Circle → enum id 2
            Assert.That(osu, Does.Contain("diva-action-2"));
            // type=3 → Triangle → enum id 1
            Assert.That(osu, Does.Contain("diva-action-1"));
            Assert.That(osu, Does.Contain("-ax"));
            Assert.That(osu, Does.Contain("-ay"));
            Assert.That(osu, Does.Contain("AudioFilename: audio.ogg"));
            Assert.That(osu, Does.Contain("Version:★3 Easy"));
        }

        [Test]
        public void Sample_roundtrip_preserves_approach_and_hold()
        {
            var approach = new Vector2(120.5f, -80f);
            string leaf = DivaActionEncoding.EncodeSampleFileName(DivaAction.Cross, true, 1234, approach);

            var fake = new osu.Game.Rulesets.Objects.HitObject
            {
                Samples = [new osu.Game.Audio.HitSampleInfo(leaf)]
            };

            Assert.That(DivaActionEncoding.TryParseFromHitObject(fake, out var action, out bool isHold, out double duration, out Vector2? parsed), Is.True);
            Assert.That(action, Is.EqualTo(DivaAction.Cross));
            Assert.That(isHold, Is.True);
            Assert.That(duration, Is.EqualTo(1234).Within(0.01));
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed!.Value.X, Is.EqualTo(120.5f).Within(0.01f));
            Assert.That(parsed.Value.Y, Is.EqualTo(-80f).Within(0.01f));
        }

        [Test]
        public void Parses_metadata_notes_and_timing()
        {
            string chart = """
                           1.0.4.8
                           Test Song
                           Mapper
                           Artist Name
                           Pop
                           cover.jpg
                           2
                           5
                           120
                           1
                           0 120
                           -1
                           -1
                           -1
                           0 0 10 12 0 0 0
                           48 1 20 12 0 0 1
                           96 8 15 10 0 0 2 48
                           -1
                           0 song.mp3
                           -1
                           0 cover.jpg
                           -1
                           -1 -1
                           """;

            using var reader = new StringReader(chart);
            DivaChart parsed = DivaChartFileParser.Parse(reader, "test.diva", @"C:\songs\test");

            Assert.That(parsed.Metadata.Title, Is.EqualTo("Test Song"));
            Assert.That(parsed.Metadata.Artist, Is.EqualTo("Artist Name"));
            Assert.That(parsed.Metadata.Level, Is.EqualTo(2));
            Assert.That(parsed.Metadata.Hard, Is.EqualTo(5));
            Assert.That(parsed.Metadata.Bpm, Is.EqualTo(120));
            Assert.That(parsed.Notes.Count, Is.EqualTo(3));
            Assert.That(parsed.Notes[0].IsHold, Is.False);
            Assert.That(parsed.Notes[2].IsHold, Is.True);

            // 48 frames * MsPerFrame(120)
            double expectedDuration = 48 * DivaChartConstants.MsPerFrame(120);
            Assert.That(parsed.Notes[2].DurationMs, Is.EqualTo(expectedDuration).Within(0.01));
            Assert.That(parsed.WavFiles[0], Is.EqualTo("song.mp3"));
            Assert.That(DivaActionEncoding.ResolveAction(parsed.Notes[0]), Is.EqualTo(DivaAction.Circle));
            Assert.That(DivaActionEncoding.ResolveAction(parsed.Notes[1]), Is.EqualTo(DivaAction.Square));
            Assert.That(parsed.HasChanceTime, Is.False);

            Vector2 approach = DivaActionEncoding.ComputeApproachOrigin(parsed.Notes[0], 120);
            Assert.That(approach.Length, Is.EqualTo(DivaChartConstants.DISTANCE).Within(0.5f));
        }

        [Test]
        public void Parses_chance_time_frame_range_to_ms()
        {
            // 1 period = 192 frames; Chance Time frames 48..95 inclusive → exclusive end frame 96.
            string chart = """
                           1.0.4.8
                           Chance Song
                           Mapper
                           Artist
                           Style
                           bg.png
                           3
                           7
                           120
                           1
                           0 120
                           -1
                           -1
                           -1
                           0 0 8 8 0 0 0
                           -1
                           0 audio.ogg
                           -1
                           0 bg.png
                           -1
                           48 95
                           """;

            using var reader = new StringReader(chart);
            DivaChart parsed = DivaChartFileParser.Parse(reader, "chance.diva", @"C:\songs\chance");

            Assert.That(parsed.ChanceTimeStart, Is.EqualTo(48));
            Assert.That(parsed.ChanceTimeEnd, Is.EqualTo(95));
            Assert.That(parsed.HasChanceTime, Is.True);

            double msPerFrame = DivaChartConstants.MsPerFrame(120);
            Assert.That(parsed.ChanceTimeStartMs, Is.EqualTo(48 * msPerFrame).Within(0.01));
            Assert.That(parsed.ChanceTimeEndMs, Is.EqualTo(96 * msPerFrame).Within(0.01));

            string osu = DivaToOsuExporter.ExportToString(parsed);
            Assert.That(osu, Does.Contain("Version:★7 Hard"));
            Assert.That(osu, Does.Contain(",0,1")); // kiai on
            Assert.That(osu, Does.Contain(",0,0")); // kiai off
        }

        [Test]
        public void Scanner_finds_song_folders_with_diva_files()
        {
            string root = Path.Combine(Path.GetTempPath(), "diva-scan-" + Path.GetRandomFileName());
            string song = Path.Combine(root, "song1");
            Directory.CreateDirectory(song);
            File.WriteAllText(Path.Combine(song, "easy.diva"), "1.0.0.0\n");

            try
            {
                IReadOnlyList<DivaSongFolder> songs = DivaLibraryScanner.Scan([root]);
                Assert.That(songs.Count, Is.EqualTo(1));
                Assert.That(songs[0].ChartPaths.Single(), Does.EndWith("easy.diva"));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
