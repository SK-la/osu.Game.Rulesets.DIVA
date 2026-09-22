// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.IO;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects;
using osuTK;

namespace osu.Game.Rulesets.Diva.Tests
{
    [TestFixture]
    public class DivaBeatmapEncoderTests
    {
        [Test]
        public void Encoder_round_trips_tap_and_hold_through_converter()
        {
            var source = new DivaBeatmap
            {
                BeatmapInfo =
                {
                    Ruleset = new DivaRuleset().RulesetInfo,
                    DifficultyName = "★5 Normal",
                    Metadata =
                    {
                        Title = "Save Song",
                        TitleUnicode = "Save Song",
                        Artist = "Artist",
                        ArtistUnicode = "Artist",
                        Source = "ProjectDIVA",
                        Tags = DivaActionEncoding.NATIVE_TAG
                    }
                }
            };
            source.Metadata.Author.Username = "Mapper";
            source.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            source.HitObjects.Add(new DivaHitObject
            {
                StartTime = 1000,
                Position = DivaActionEncoding.ToPlayfieldPosition(8, 8),
                ValidAction = DivaAction.Circle,
                ApproachPieceOriginPosition = new Vector2(400, 0)
            });
            source.HitObjects.Add(new DivaHoldHitObject
            {
                StartTime = 1500,
                Duration = 500,
                Position = DivaActionEncoding.ToPlayfieldPosition(10, 12),
                ValidAction = DivaAction.Cross,
                ApproachPieceOriginPosition = new Vector2(0, -300)
            });

            string encoded = DivaBeatmapEncoder.ExportToString(source, null);

            Assert.That(encoded, Does.Contain("osu file format v14"));
            Assert.That(encoded, Does.Contain("Mode: 0"));
            Assert.That(encoded, Does.Contain("diva-action-2"));
            Assert.That(encoded, Does.Contain("diva-hold-3-500"));

            using var reader = new LineBufferedReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(encoded)));
            var decoded = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
            decoded.BeatmapInfo.Ruleset = new DivaRuleset().RulesetInfo;

            var converted = (DivaBeatmap)new DivaBeatmapConverter(decoded, new DivaRuleset()).Convert();
            Assert.That(converted.HitObjects.Count, Is.EqualTo(2));

            var tap = converted.HitObjects.Single(h => h is not DivaHoldHitObject);
            Assert.That(tap.ValidAction, Is.EqualTo(DivaAction.Circle));
            Assert.That(tap.StartTime, Is.EqualTo(1000).Within(0.5));
            Assert.That(tap.ApproachPieceOriginPosition.X, Is.EqualTo(400).Within(0.5f));

            var hold = converted.HitObjects.OfType<DivaHoldHitObject>().Single();
            Assert.That(hold.ValidAction, Is.EqualTo(DivaAction.Cross));
            Assert.That(hold.Duration, Is.EqualTo(500).Within(0.5));
            Assert.That(hold.ApproachPieceOriginPosition.Y, Is.EqualTo(-300).Within(0.5f));
        }

        [Test]
        public void Chart_builder_round_trips_notes_through_diva_writer()
        {
            var beatmap = new DivaBeatmap
            {
                BeatmapInfo =
                {
                    BPM = 120,
                    DifficultyName = "★5 Normal",
                    Metadata = { Title = "Chart Song", Artist = "Artist" }
                }
            };
            beatmap.Metadata.Author.Username = "Mapper";
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.HitObjects.Add(new DivaHitObject
            {
                StartTime = 0,
                Position = DivaActionEncoding.ToPlayfieldPosition(10, 12),
                ValidAction = DivaAction.Circle,
                ApproachPieceOriginPosition = DivaActionEncoding.ComputeApproachOrigin(
                    DivaActionEncoding.ToPlayfieldPosition(10, 12), 0, 0, 120)
            });
            beatmap.HitObjects.Add(new DivaHoldHitObject
            {
                StartTime = 1000,
                Duration = 500,
                Position = DivaActionEncoding.ToPlayfieldPosition(15, 10),
                ValidAction = DivaAction.Cross,
                ApproachPieceOriginPosition = DivaActionEncoding.ComputeApproachOrigin(
                    DivaActionEncoding.ToPlayfieldPosition(15, 10), 0, 0, 120)
            });

            DivaChart chart = DivaChartBuilder.FromBeatmap(beatmap);
            string written = DivaChartFileWriter.ExportToString(chart);

            using var reader = new StringReader(written);
            DivaChart parsed = DivaChartFileParser.Parse(reader, "built.diva", @"C:\songs\built");

            Assert.That(parsed.Notes.Count, Is.EqualTo(2));
            Assert.That(parsed.Notes[0].IsHold, Is.False);
            Assert.That(parsed.Notes[1].IsHold, Is.True);
            Assert.That(DivaActionEncoding.ResolveAction(parsed.Notes[0]), Is.EqualTo(DivaAction.Circle));
            Assert.That(DivaActionEncoding.ResolveAction(parsed.Notes[1]), Is.EqualTo(DivaAction.Cross));
            Assert.That(parsed.Notes[0].X, Is.EqualTo(10).Within(1e-3f));
            Assert.That(parsed.Notes[0].Y, Is.EqualTo(12).Within(1e-3f));
            Assert.That(parsed.Notes[1].DurationMs, Is.EqualTo(500).Within(1));
            Assert.That(parsed.Notes[0].FrameIndex, Is.EqualTo(0));
            Assert.That(parsed.Notes[1].FrameIndex, Is.EqualTo(96));
        }

        [Test]
        public void Chart_builder_prefers_beatmap_events_over_source_file()
        {
            var beatmap = new DivaBeatmap
            {
                BeatmapInfo = { BPM = 120 },
                ChartEvents = new DivaChartEvents()
            };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.ChartEvents.WavFiles[0] = "edited.ogg";
            beatmap.ChartEvents.ResourceFiles[3] = "clip.mp4";
            beatmap.ChartEvents.BgmEvents.Add(new DivaBgmEvent
            {
                TimeMs = 1000,
                Slot = 2,
                WavId = 0,
                DeclaredSourceOffsetMs = 250
            });
            beatmap.ChartEvents.ResourceEvents.Add(new DivaResourceEvent
            {
                TimeMs = 500,
                ResourceId = 3
            });

            var source = new DivaChart
            {
                WavFiles = new Dictionary<int, string> { [0] = "stale.ogg" },
                ResourceFiles = new Dictionary<int, string> { [1] = "old.png" },
                BgmEvents =
                [
                    new DivaBgmEvent { TimeMs = 0, Slot = 0, WavId = 0 }
                ]
            };

            DivaChart chart = DivaChartBuilder.FromBeatmap(beatmap, source);

            Assert.That(chart.WavFiles[0], Is.EqualTo("edited.ogg"));
            Assert.That(chart.ResourceFiles[3], Is.EqualTo("clip.mp4"));
            Assert.That(chart.BgmEvents.Count, Is.EqualTo(1));
            Assert.That(chart.BgmEvents[0].Slot, Is.EqualTo(2));
            Assert.That(chart.BgmEvents[0].DeclaredSourceOffsetMs, Is.EqualTo(250));
            Assert.That(chart.BgmEvents[0].FrameIndex, Is.EqualTo(96));
            Assert.That(chart.ResourceEvents.Single().ResourceId, Is.EqualTo(3));
            Assert.That(chart.ResourceEvents.Single().FrameIndex, Is.EqualTo(48));
        }

        [Test]
        public void Chart_builder_falls_back_to_source_when_events_missing()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            var source = new DivaChart
            {
                WavFiles = new Dictionary<int, string> { [7] = "from-disk.ogg" },
                BgmEvents =
                [
                    new DivaBgmEvent { FrameIndex = 12, TimeMs = 125, Slot = 1, WavId = 7 }
                ]
            };

            DivaChart chart = DivaChartBuilder.FromBeatmap(beatmap, source);

            Assert.That(chart.WavFiles[7], Is.EqualTo("from-disk.ogg"));
            Assert.That(chart.BgmEvents.Single().WavId, Is.EqualTo(7));
            Assert.That(chart.BgmEvents.Single().FrameIndex, Is.EqualTo(12));
        }

        [Test]
        public void Ruleset_exposes_encoder_for_editor_save()
        {
            var encoder = new DivaRuleset().CreateBeatmapEncoder(new DivaBeatmap(), null, null);
            Assert.That(encoder, Is.Not.Null);
            Assert.That(encoder, Is.InstanceOf<osu.Game.Beatmaps.Formats.IBeatmapEncoder>());
            Assert.That(encoder, Is.TypeOf<DivaBeatmapEncoder>());
        }
    }
}
