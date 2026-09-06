// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Text;
using NUnit.Framework;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaChartTextEncodingTests
    {
        [Test]
        public void DecodeFile_prefers_ansi_when_referenced_audio_exists()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding gbk = Encoding.GetEncoding(936);

            string root = Path.Combine(Path.GetTempPath(), "diva-enc-" + Path.GetRandomFileName());
            Directory.CreateDirectory(root);

            // Mixed ASCII + CJK filename with spaces — typical ProjectDIVA community charts.
            const string audio_name = "【Terror】 COVER 测试.mp3";
            File.WriteAllBytes(Path.Combine(root, audio_name), [0]);

            const string chart_text = $"""
                                      1.0.4.8
                                      曲名测试
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
                                      0 {audio_name}
                                      -1
                                      0 cover.jpg
                                      -1
                                      -1 -1
                                      """;

            string chartPath = Path.Combine(root, "chart.diva");
            File.WriteAllBytes(chartPath, gbk.GetBytes(chart_text.Replace("\r\n", "\n")));

            try
            {
                DivaChart chart = DivaChartFileParser.Parse(chartPath);
                Assert.That(chart.ResolvePrimaryAudioRelativePath(), Is.EqualTo(audio_name));
                Assert.That(File.Exists(Path.Combine(root, chart.ResolvePrimaryAudioRelativePath()!)), Is.True);
                Assert.That(chart.Metadata.Title, Is.EqualTo("曲名测试"));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void DecodeBytes_prefers_encoding_when_referenced_audio_exists()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding gbk = Encoding.GetEncoding(936);

            string root = Path.Combine(Path.GetTempPath(), "diva-enc-bytes-" + Path.GetRandomFileName());
            Directory.CreateDirectory(root);

            const string audio_name = "【Terror】 COVER 测试.mp3";
            File.WriteAllBytes(Path.Combine(root, audio_name), [0]);

            const string chart_text = $"""
                                      1.0.4.8
                                      曲名测试
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
                                      0 {audio_name}
                                      -1
                                      0 cover.jpg
                                      -1
                                      -1 -1
                                      """;

            byte[] bytes = gbk.GetBytes(chart_text.Replace("\r\n", "\n"));

            try
            {
                string text = DivaChartTextEncoding.DecodeBytes(bytes, root);
                Assert.That(text, Does.Contain(audio_name));
                Assert.That(DivaChartTextEncoding.DetectBestEncoding(bytes, root).CodePage, Is.EqualTo(936));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void ResolveExistingRelativePath_matches_ascii_hint_when_exact_name_missing()
        {
            string root = Path.Combine(Path.GetTempPath(), "diva-resolve-" + Path.GetRandomFileName());
            Directory.CreateDirectory(root);
            const string real = "【Terror】 COVER 测试.mp3";
            File.WriteAllBytes(Path.Combine(root, real), [0]);

            try
            {
                string? resolved = DivaChartTextEncoding.ResolveExistingRelativePath(root, "mojibake Terror COVER.mp3");
                Assert.That(resolved, Is.EqualTo(real));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
