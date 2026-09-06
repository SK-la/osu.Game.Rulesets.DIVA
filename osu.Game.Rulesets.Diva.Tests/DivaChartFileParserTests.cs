// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;

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
        public void Exporter_embeds_action_sample_names_and_black_star_version()
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
            Assert.That(osu, Does.Contain("diva-action-1")); // Triangle (UNIT/key 3)
            Assert.That(osu, Does.Contain("AudioFilename: audio.ogg"));
            Assert.That(osu, Does.Contain("Version:★3 Easy"));
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
                           96 8 15 10 0 0 2 500
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
            Assert.That(parsed.Notes[2].DurationMs, Is.EqualTo(500).Within(0.01));
            Assert.That(parsed.WavFiles[0], Is.EqualTo("song.mp3"));
            Assert.That(DivaActionEncoding.ResolveAction(parsed.Notes[0]), Is.EqualTo(DivaAction.Circle));
            Assert.That(DivaActionEncoding.ResolveAction(parsed.Notes[1]), Is.EqualTo(DivaAction.Square));
            Assert.That(parsed.HasChanceTime, Is.False);
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
