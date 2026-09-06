// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.IO;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Storyboards;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaChartStreamDecodeTests
    {
        [Test]
        public void DecodeBytes_roundtrips_gbk_title_and_audio_name()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding gbk = Encoding.GetEncoding(936);

            string root = Path.Combine(Path.GetTempPath(), "diva-bytes-" + Path.GetRandomFileName());
            Directory.CreateDirectory(root);

            string audioName = "【Terror】 COVER 测试.mp3";
            File.WriteAllBytes(Path.Combine(root, audioName), [0]);

            string chartText = buildMinimalChart("曲名测试", audioName);
            byte[] bytes = gbk.GetBytes(chartText.Replace("\r\n", "\n"));

            try
            {
                string decoded = DivaChartTextEncoding.DecodeBytes(bytes, root);
                Assert.That(decoded, Does.Contain("曲名测试"));
                Assert.That(decoded, Does.Contain(audioName));

                using var reader = new StringReader(decoded);
                DivaChart chart = DivaChartFileParser.Parse(reader, "chart.diva", root);
                Assert.That(chart.Metadata.Title, Is.EqualTo("曲名测试"));
                Assert.That(chart.ResolvePrimaryAudioRelativePath(), Is.EqualTo(audioName));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void LineBufferedReader_utf8_misread_is_bypassed_via_FileStream_path()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding gbk = Encoding.GetEncoding(936);

            string root = Path.Combine(Path.GetTempPath(), "diva-stream-" + Path.GetRandomFileName());
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(Path.Combine(root, "RES"));

            string videoName = "PDA HD テストPV_オレンジ.avi";
            string videoRelative = Path.Combine("RES", videoName);
            File.WriteAllBytes(Path.Combine(root, videoRelative), [0]);
            File.WriteAllBytes(Path.Combine(root, "cover.jpg"), [0]);

            string chartText = buildResourceVideoChart("曲名テスト", videoName);
            string chartPath = Path.Combine(root, "chart.diva");
            File.WriteAllBytes(chartPath, gbk.GetBytes(chartText.Replace("\r\n", "\n")));

            try
            {
                // Simulate lazer: open FileStream then wrap with UTF-8 LineBufferedReader (corrupts CJK if read as text).
                using (var fileStream = File.OpenRead(chartPath))
                using (var lineReader = new LineBufferedReader(fileStream))
                {
                    // Force a UTF-8 peek like Decoder.GetDecoder does.
                    Assert.That(lineReader.PeekLine(), Does.StartWith("1."));

                    DivaChartStreamDecode.Result decoded = DivaChartStreamDecode.Parse(lineReader);
                    Assert.That(decoded.SongFolder, Is.EqualTo(root));
                    Assert.That(decoded.Chart.Metadata.Title, Is.EqualTo("曲名テスト"));

                    DivaVideoPlayback? video = DivaPlaybackTimeline.ResolvePrimaryVideo(decoded.Chart);
                    Assert.That(video, Is.Not.Null);
                    Assert.That(video!.Value.RelativePath.Replace('\\', '/'), Is.EqualTo(videoRelative.Replace('\\', '/')));
                }

                using (var fileStream = File.OpenRead(chartPath))
                using (var lineReader = new LineBufferedReader(fileStream))
                {
                    var beatmap = new DivaBeatmapDecoder().Decode(lineReader);
                    Assert.That(beatmap.Metadata.Title, Is.EqualTo("曲名テスト"));
                }

                using (var fileStream = File.OpenRead(chartPath))
                using (var lineReader = new LineBufferedReader(fileStream))
                {
                    var storyboard = new DivaStoryboardDecoder().Decode(lineReader);
                    var videos = storyboard.GetLayer("Video").Elements.OfType<StoryboardVideo>().ToArray();
                    Assert.That(videos, Has.Length.EqualTo(1));
                    Assert.That(videos[0].Path.Replace('\\', '/'), Is.EqualTo(videoRelative.Replace('\\', '/')));
                    Assert.That(File.Exists(Path.Combine(root, videos[0].Path)), Is.True);
                }
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void LineBufferedReader_over_MemoryStream_uses_DecodeBytes()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // Use GBK (common on CN systems / community charts). Without a song folder, ACP wins ties;
            // encoding the payload in ACP/GBK keeps DecodeBytes deterministic across locales.
            Encoding gbk = Encoding.GetEncoding(936);

            string chartText = buildMinimalChart("曲名测试", "song.mp3");
            byte[] bytes = gbk.GetBytes(chartText.Replace("\r\n", "\n"));

            using var memory = new MemoryStream(bytes);
            using var lineReader = new LineBufferedReader(memory);

            Assert.That(lineReader.PeekLine(), Does.StartWith("1."));

            DivaChartStreamDecode.Result decoded = DivaChartStreamDecode.Parse(lineReader);
            Assert.That(decoded.Chart.Metadata.Title, Is.EqualTo("曲名测试"));
        }

        private static string buildMinimalChart(string title, string audioName)
        {
            return $"""
                    1.0.4.8
                    {title}
                    Mapper
                    Artist
                    Style
                    cover.jpg
                    1
                    3
                    120
                    1
                    0 120
                    -1
                    -1
                    -1
                    0 0 8 8 0 0 0
                    -1
                    0 {audioName}
                    -1
                    0 cover.jpg
                    -1
                    -1 -1
                    """;
        }

        private static string buildResourceVideoChart(string title, string videoFileName)
        {
            return $"""
                    1.0.4.8
                    {title}
                    Mapper
                    Artist
                    Style
                    cover.jpg
                    1
                    1
                    120
                    2
                    0 120
                    -1
                    0 5
                    48 7
                    -1
                    -1
                    96 0 8 8 0 0 0
                    -1
                    -1
                    0 cover.jpg
                    5 splash.png
                    7 {videoFileName}
                    -1
                    -1 -1
                    """;
        }
    }
}
