// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.IO;
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
        public void StandingPreemptMs_matches_project_diva_note_standing()
        {
            Assert.That(DivaChartConstants.StandingPreemptMs(120), Is.EqualTo(2000).Within(0.01));
            Assert.That(DivaChartConstants.StandingPreemptMs(150), Is.EqualTo(1600).Within(0.01));
            Assert.That(DivaChartConstants.StandingPreemptMs(100), Is.EqualTo(2400).Within(0.01));
        }

        [Test]
        public void Frame_timeline_is_linear_at_constant_120_bpm()
        {
            string chart = """
                           1.0.4.8
                           Timeline Song
                           Mapper
                           Artist
                           Style
                           bg.png
                           1
                           1
                           120
                           1
                           0 120
                           -1
                           -1
                           -1
                           0 0 8 8 0 0 0
                           48 1 8 8 0 0 1
                           96 2 8 8 0 0 2
                           -1
                           0 audio.ogg
                           -1
                           0 bg.png
                           -1
                           -1 -1
                           """;

            using var reader = new StringReader(chart);
            DivaChart parsed = DivaChartFileParser.Parse(reader, "timeline.diva", @"C:\songs\timeline");

            double ms = DivaChartConstants.MsPerFrame(120);
            Assert.That(parsed.Notes[0].StartTimeMs, Is.EqualTo(0).Within(0.01));
            Assert.That(parsed.Notes[1].StartTimeMs, Is.EqualTo(48 * ms).Within(0.01));
            Assert.That(parsed.Notes[2].StartTimeMs, Is.EqualTo(96 * ms).Within(0.01));
        }

        [Test]
        public void Frame_timeline_respects_mid_chart_bpm_change()
        {
            // Frames 0..47 at 120 BPM, then BPM 240 from frame 48; note at frame 96.
            // After frame 48, each frame is half as long → frames 48..96 span 48 * MsPerFrame(240).
            string chart = """
                           1.0.4.8
                           Bpm Change Song
                           Mapper
                           Artist
                           Style
                           bg.png
                           1
                           1
                           120
                           1
                           0 120
                           48 240
                           -1
                           -1
                           -1
                           0 0 8 8 0 0 0
                           96 1 8 8 0 0 1
                           -1
                           0 audio.ogg
                           -1
                           0 bg.png
                           -1
                           -1 -1
                           """;

            using var reader = new StringReader(chart);
            DivaChart parsed = DivaChartFileParser.Parse(reader, "bpm.diva", @"C:\songs\bpm");

            double expected =
                48 * DivaChartConstants.MsPerFrame(120)
                + 48 * DivaChartConstants.MsPerFrame(240);

            Assert.That(parsed.Notes[0].StartTimeMs, Is.EqualTo(0).Within(0.01));
            Assert.That(parsed.Notes[1].StartTimeMs, Is.EqualTo(expected).Within(0.01));
            Assert.That(parsed.TimingPoints.Count, Is.EqualTo(2));
            Assert.That(parsed.TimingPoints[1].Bpm, Is.EqualTo(240));
            Assert.That(parsed.TimingPoints[1].TimeMs, Is.EqualTo(48 * DivaChartConstants.MsPerFrame(120)).Within(0.01));
        }

        [Test]
        public void PlaybackTimeline_maps_qianchen_bgs_frame_to_audio_clock()
        {
            string chart = """
                           1.0.4.8
                           前尘如梦
                           Mapper
                           Artist
                           Style
                           bg.png
                           4
                           8
                           115
                           4
                           0 115
                           -1
                           -1
                           230 0 1
                           -1
                           576 0 8 8 0 0 0
                           -1
                           0 decoy.mp3
                           1 WAV\前尘如梦 00_00_00-00_04_03.mp3
                           -1
                           0 bg.png
                           -1
                           500 600
                           """;

            using var reader = new StringReader(chart);
            DivaChart parsed = DivaChartFileParser.Parse(reader, "前尘如梦.diva", @"C:\songs\前尘如梦");
            DivaPlaybackTimeline timeline = DivaPlaybackTimeline.Create(parsed);

            double rawNoteTime = 576 * DivaChartConstants.MsPerFrame(115);
            double mappedNoteTime = rawNoteTime - 2500;

            Assert.That(parsed.BgmEvents.Single().FrameIndex, Is.EqualTo(230));
            Assert.That(parsed.BgmEvents.Single().TimeMs, Is.EqualTo(2500).Within(0.01));
            Assert.That(parsed.Notes.Single().StartTimeMs, Is.EqualTo(rawNoteTime).Within(0.01), "Parser must retain raw DIVA time.");
            Assert.That(timeline.BgmWavId, Is.EqualTo(1));
            Assert.That(timeline.AudioRelativePath, Is.EqualTo("WAV/前尘如梦 00_00_00-00_04_03.mp3"));
            Assert.That(timeline.OffsetMs, Is.EqualTo(-2500).Within(0.01));
            Assert.That(timeline.ToPlaybackTime(parsed.Notes.Single().StartTimeMs), Is.EqualTo(mappedNoteTime).Within(0.01));
            Assert.That(mappedNoteTime - DivaChartConstants.StandingPreemptMs(115), Is.EqualTo(1673.91).Within(0.02));

            string osu = DivaToOsuExporter.ExportToString(parsed);
            Assert.That(osu, Does.Contain("AudioFilename: 前尘如梦 00_00_00-00_04_03.mp3"));
            Assert.That(osu, Does.Contain("-2500,521.739"));
            Assert.That(osu, Does.Match(@"(?m)^\d+,\d+,3761,1,0,"));

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(chart));
            using var lineReader = new LineBufferedReader(stream);
            var decoded = new DivaBeatmapDecoder().Decode(lineReader);
            Assert.That(decoded.HitObjects.Single().StartTime, Is.EqualTo(mappedNoteTime).Within(0.01));
            Assert.That(decoded.ControlPointInfo.TimingPoints.First().Time, Is.EqualTo(-2500).Within(0.01));
            Assert.That(decoded.ControlPointInfo.EffectPoints[0].Time,
                Is.EqualTo(timeline.ToPlaybackTime(parsed.ChanceTimeStartMs)).Within(0.01));
            Assert.That(decoded.ControlPointInfo.EffectPoints[1].Time,
                Is.EqualTo(timeline.ToPlaybackTime(parsed.ChanceTimeEndMs)).Within(0.01));
        }

        [Test]
        public void PlaybackTimeline_negative_bgs_position_seeks_source()
        {
            string chart = """
                           1.0.4.8
                           Skip Song
                           Mapper
                           Artist
                           Style
                           bg.png
                           1
                           1
                           120
                           1
                           0 120
                           -1
                           -1
                           -1000 0 0
                           -1
                           0 0 8 8 0 0 0
                           -1
                           0 audio.ogg
                           -1
                           0 bg.png
                           -1
                           -1 -1
                           """;

            using var reader = new StringReader(chart);
            DivaChart parsed = DivaChartFileParser.Parse(reader, "skip.diva", @"C:\songs\skip");
            DivaPlaybackTimeline timeline = DivaPlaybackTimeline.Create(parsed);

            Assert.That(parsed.BgmEvents.Single().DeclaredSourceOffsetMs, Is.EqualTo(1000));
            Assert.That(parsed.Notes[0].StartTimeMs, Is.EqualTo(0));
            Assert.That(timeline.SourceOffsetMs, Is.EqualTo(1000));
            Assert.That(timeline.OffsetMs, Is.EqualTo(1000));
            Assert.That(timeline.ToPlaybackTime(parsed.Notes[0].StartTimeMs), Is.EqualTo(1000).Within(0.01));
        }

        [TestCase(104)]
        [TestCase(243)]
        public void PlaybackTimeline_resource_only_uses_first_video_event(int videoStartFrame)
        {
            string chart = $"""
                           1.0.4.8
                           Resource Song
                           Mapper
                           Artist
                           Style
                           bg.png
                           1
                           1
                           120
                           2
                           0 120
                           -1
                           0 5
                           {videoStartFrame} 7
                           -1
                           -1
                           {videoStartFrame + 48} 0 8 8 0 0 0
                           -1
                           -1
                           0 bg.png
                           5 splash.png
                           7 movie.avi
                           -1
                           -1 -1
                           """;

            using var reader = new StringReader(chart);
            DivaChart parsed = DivaChartFileParser.Parse(reader, "resource.diva", @"C:\songs\resource");
            DivaPlaybackTimeline timeline = DivaPlaybackTimeline.Create(parsed);

            double eventTime = videoStartFrame * DivaChartConstants.MsPerFrame(120);
            Assert.That(timeline.ResourceId, Is.EqualTo(7));
            Assert.That(timeline.AudioRelativePath, Is.EqualTo("movie.avi"));
            Assert.That(timeline.EventTimeMs, Is.EqualTo(eventTime).Within(0.01));
            Assert.That(timeline.OffsetMs, Is.EqualTo(-eventTime).Within(0.01));
            Assert.That(timeline.ToPlaybackTime(parsed.Notes.Single().StartTimeMs),
                Is.EqualTo(48 * DivaChartConstants.MsPerFrame(120)).Within(0.01));
        }

        [Test]
        public void PlaybackTimeline_zero_when_no_media_event()
        {
            string chart = """
                           1.0.4.8
                           Plain Song
                           Mapper
                           Artist
                           Style
                           bg.png
                           1
                           1
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
                           -1 -1
                           """;

            using var reader = new StringReader(chart);
            DivaChart parsed = DivaChartFileParser.Parse(reader, "plain.diva", @"C:\songs\plain");
            DivaPlaybackTimeline timeline = DivaPlaybackTimeline.Create(parsed);

            Assert.That(timeline.AudioRelativePath, Is.EqualTo("audio.ogg"));
            Assert.That(timeline.OffsetMs, Is.EqualTo(0));
            Assert.That(parsed.Notes[0].StartTimeMs, Is.EqualTo(0));
        }

        [Test]
        public void PlaybackTimeline_reports_additional_bgm_segments()
        {
            string chart = """
                           1.0.4.8
                           Multi BGM Song
                           Mapper
                           Artist
                           Style
                           bg.png
                           2
                           1
                           120
                           1
                           0 120
                           -1
                           -1
                           48 0 1
                           96 0 2
                           -1
                           144 0 8 8 0 0 0
                           -1
                           0 decoy.ogg
                           1 main.ogg
                           2 second.ogg
                           -1
                           0 bg.png
                           -1
                           -1 -1
                           """;

            using var reader = new StringReader(chart);
            DivaChart parsed = DivaChartFileParser.Parse(reader, "multi.diva", @"C:\songs\multi");
            DivaPlaybackTimeline timeline = DivaPlaybackTimeline.Create(parsed);

            Assert.That(timeline.BgmWavId, Is.EqualTo(1), "The event's WAV id, not dictionary order, selects the track.");
            Assert.That(timeline.AudioRelativePath, Is.EqualTo("main.ogg"));
            Assert.That(timeline.HasAdditionalAudioSegments, Is.True);
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
