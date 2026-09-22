// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        public void Chart_builder_preserves_wav_key_from_source()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.HitObjects.Add(new DivaHitObject
            {
                StartTime = 0,
                Position = DivaActionEncoding.ToPlayfieldPosition(10, 12),
                ValidAction = DivaAction.Circle
            });

            int type = DivaActionEncoding.ToUnitIndex(DivaAction.Circle);

            var source = new DivaChart
            {
                Notes = [new DivaChartNote { FrameIndex = 0, Type = type, Key = 5 }]
            };

            DivaChart chart = DivaChartBuilder.FromBeatmap(beatmap, source);
            Assert.That(chart.Notes.Single().Key, Is.EqualTo(5));

            using var reader = new StringReader(DivaChartFileWriter.ExportToString(chart));
            DivaChart parsed = DivaChartFileParser.Parse(reader, "built.diva", @"C:\songs\built");

            Assert.That(parsed.Notes.Single().Key, Is.EqualTo(5));
        }

        [Test]
        public void Chart_builder_leaves_wav_key_unset_without_source()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.HitObjects.Add(new DivaHitObject
            {
                StartTime = 0,
                Position = DivaActionEncoding.ToPlayfieldPosition(10, 12),
                ValidAction = DivaAction.Cross
            });

            DivaChart chart = DivaChartBuilder.FromBeatmap(beatmap);

            Assert.That(chart.Notes.Single().Key, Is.Zero);
        }

        [Test]
        public void Writer_quantises_hold_length_to_whole_frames()
        {
            var chart = minimalChart(1, [new DivaChartNote { FrameIndex = 0, Type = DivaChartConstants.NOTE_TYPE_COUNT, X = 8, Y = 8, DurationMs = 505 }]);

            // 505 ms at 120 BPM is 48.48 frames; ProjectDIVA reads an int, so the field must be "48".
            Assert.That(DivaChartFileWriter.ExportToString(chart), Does.Contain("0 8 8 8 0 0 0 48\r\n"));
        }

        /// <summary>
        ///     The inspector shows and edits hold lengths by chart frame, so its number has to be the one a save
        ///     writes; a different rounding there would change the length on the next export.
        /// </summary>
        [Test]
        public void Frame_length_helper_matches_what_the_writer_emits()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            // 505 ms at 120 BPM is 48.48 frames; the writer rounds away from zero, and so must the editor.
            Assert.That(DivaChartBuilder.FrameLengthAt(beatmap, 0, 505), Is.EqualTo(48));
        }

        [Test]
        public void Frame_length_helper_keeps_a_hold_at_least_one_frame_wide()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            Assert.That(DivaChartBuilder.FrameLengthAt(beatmap, 0, 1), Is.EqualTo(1), "a sub-frame hold still occupies a frame");
            Assert.That(DivaChartBuilder.FrameLengthAt(beatmap, 0, 0), Is.Zero);
        }

        /// <summary>
        ///     Writing a length back as <c>frames * frame duration</c> must land on the same frame count, i.e.
        ///     editing the inspector's frame field and saving cannot drift the length.
        /// </summary>
        [Test]
        public void Frame_length_round_trips_through_the_editor()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 150 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 400 });

            double msPerFrame = DivaChartBuilder.MsPerFrameAt(beatmap, 0);

            for (int frames = 1; frames <= 8; frames++)
                Assert.That(DivaChartBuilder.FrameLengthAt(beatmap, 0, frames * msPerFrame), Is.EqualTo(frames));
        }

        [Test]
        public void Writer_rejects_more_notes_than_a_frame_holds()
        {
            var notes = new List<DivaChartNote>();

            for (int i = 0; i <= DivaChartConstants.MAX_NOTES_PER_FRAME; i++)
                notes.Add(new DivaChartNote { FrameIndex = 0, Type = 0 });

            Assert.Throws<InvalidDataException>(() => DivaChartFileWriter.ExportToString(minimalChart(1, notes)));
        }

        [Test]
        public void Writer_rejects_period_count_beyond_editor_limit()
        {
            var chart = minimalChart(DivaChartConstants.MAX_PERIOD_COUNT + 1, []);

            Assert.Throws<InvalidDataException>(() => DivaChartFileWriter.ExportToString(chart));
        }

        [Test]
        public void Writer_emits_format_version_and_chance_time_line()
        {
            string written = DivaChartFileWriter.ExportToString(minimalChart(1, []));

            Assert.That(written, Does.StartWith(DivaChartConstants.DIVA_CHART_FORMAT_VERSION + "\r\n"));
            Assert.That(written, Does.EndWith("-1 -1\r\n"));
        }

        /// <summary>
        ///     The game and the reference editor string-compare the format version, so where it sorts is part
        ///     of the on-disk contract rather than a cosmetic choice.
        /// </summary>
        [Test]
        public void Format_version_sorts_above_the_reference_editor_revision()
        {
            string version = DivaChartConstants.DIVA_CHART_FORMAT_VERSION;

            Assert.That(version, Does.StartWith("1."), "the decoders and the chart sniff test match on the \"1.\" prefix");
            Assert.That(string.CompareOrdinal(version, "1.0.1.0"), Is.GreaterThanOrEqualTo(0), "below 1.0.1.0 the game skips the ChanceTime line");
            Assert.That(string.CompareOrdinal(version, "1.0.4.7"), Is.GreaterThan(0), "at or below 1.0.4.7 the game rounds BPM values");
        }

        [Test]
        public void Chart_header_overrides_source_level_and_stars()
        {
            var beatmap = new DivaBeatmap
            {
                BeatmapInfo = { BPM = 120 },
                ChartHeader = new DivaChartHeader { Level = 4, Hard = 9 }
            };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            var source = new DivaChart { Metadata = new DivaChartMetadata { Level = 1, Hard = 2 } };

            DivaChart chart = DivaChartBuilder.FromBeatmap(beatmap, source);

            Assert.That(chart.Metadata.Level, Is.EqualTo(4));
            Assert.That(chart.Metadata.Hard, Is.EqualTo(9));
        }

        [Test]
        public void Chart_header_seeds_level_and_stars_from_beatmap_info()
        {
            var info = new BeatmapInfo
            {
                DifficultyName = "★7 Hard",
                Difficulty = { OverallDifficulty = 3 },
                Metadata = { Tags = $"{DivaActionEncoding.NATIVE_TAG} diva-external Miku" }
            };

            DivaChartHeader header = DivaChartHeader.FromBeatmapInfo(info);

            Assert.That(header.Level, Is.EqualTo(3));
            Assert.That(header.Hard, Is.EqualTo(7));
            Assert.That(header.Style, Is.EqualTo("Miku"));
        }

        [Test]
        public void Level_name_parsing_ignores_level_names_inside_longer_words()
        {
            Assert.That(DivaChartConstants.TryParseLevelName("★7 Extreme", out int level), Is.True);
            Assert.That(level, Is.EqualTo(5));

            Assert.That(DivaChartConstants.TryParseLevelName("★5Hard", out level), Is.True);
            Assert.That(level, Is.EqualTo(3));

            Assert.That(DivaChartConstants.TryParseLevelName("NotEasy", out _), Is.False);
            Assert.That(DivaChartConstants.TryParseLevelName("My Song", out _), Is.False);
        }

        [Test]
        public void Mirroring_writes_level_and_stars_into_beatmap_info()
        {
            var info = new BeatmapInfo { DifficultyName = "★1 Easy" };

            DivaChartHeader.MirrorToBeatmapInfo(info, new DivaChartHeader { Level = 4, Hard = 12 });

            // The difficulty name keeps the true star count; gameplay difficulty stays inside its 0-10 range.
            Assert.That(info.DifficultyName, Is.EqualTo("★12 Extra"));
            Assert.That(info.Difficulty.OverallDifficulty, Is.EqualTo(10));

            DivaChartHeader restored = DivaChartHeader.FromBeatmapInfo(info);
            Assert.That(restored.Level, Is.EqualTo(4));
            Assert.That(restored.Hard, Is.EqualTo(12));
        }

        [Test]
        public void Chart_builder_extends_periods_to_cover_events_past_the_last_note()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.HitObjects.Add(new DivaHitObject
            {
                StartTime = 0,
                Position = DivaActionEncoding.ToPlayfieldPosition(10, 12),
                ValidAction = DivaAction.Circle
            });

            // The reading side clamps frames into the period array, so a BGS event at frame 500 has to pull
            // the exported measure count up to 3 even though the only note sits in measure 1.
            var source = new DivaChart
            {
                BgmEvents = [new DivaBgmEvent { FrameIndex = 500, TimeMs = 5208, Slot = 0, WavId = 1 }]
            };

            Assert.That(DivaChartBuilder.FromBeatmap(beatmap, source).PeriodCount, Is.EqualTo(3));
        }

        [Test]
        public void Chart_builder_extends_periods_to_cover_chance_time()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.ControlPointInfo.Add(3000, new EffectControlPoint { KiaiMode = true });
            beatmap.ControlPointInfo.Add(4000, new EffectControlPoint { KiaiMode = false });
            beatmap.HitObjects.Add(new DivaHitObject
            {
                StartTime = 0,
                Position = DivaActionEncoding.ToPlayfieldPosition(10, 12),
                ValidAction = DivaAction.Circle
            });

            // Chance Time ends on frame 383 at 120 BPM, i.e. inside measure 2.
            Assert.That(DivaChartBuilder.FromBeatmap(beatmap).PeriodCount, Is.EqualTo(2));
        }

        [Test]
        public void Chart_header_min_periods_extends_the_exported_chart()
        {
            var beatmap = new DivaBeatmap
            {
                BeatmapInfo = { BPM = 120 },
                ChartHeader = new DivaChartHeader { MinPeriodCount = 12 }
            };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.HitObjects.Add(new DivaHitObject
            {
                StartTime = 0,
                Position = DivaActionEncoding.ToPlayfieldPosition(10, 12),
                ValidAction = DivaAction.Circle
            });

            Assert.That(DivaChartBuilder.FromBeatmap(beatmap).PeriodCount, Is.EqualTo(12));
        }

        [Test]
        public void Chart_header_min_periods_never_shrinks_the_chart()
        {
            var beatmap = new DivaBeatmap
            {
                BeatmapInfo = { BPM = 120 },
                ChartHeader = new DivaChartHeader { MinPeriodCount = 1 }
            };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.HitObjects.Add(new DivaHitObject
            {
                StartTime = 5000,
                Position = DivaActionEncoding.ToPlayfieldPosition(10, 12),
                ValidAction = DivaAction.Circle
            });

            // A note on frame 480 still needs measure 3, however small the lower bound is set.
            Assert.That(DivaChartBuilder.FromBeatmap(beatmap).PeriodCount, Is.EqualTo(3));
        }

        [Test]
        public void Chart_builder_prefers_note_wav_key_over_source()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.HitObjects.Add(new DivaHitObject
            {
                StartTime = 0,
                Position = DivaActionEncoding.ToPlayfieldPosition(10, 12),
                ValidAction = DivaAction.Circle,
                WavKey = 9
            });

            int type = DivaActionEncoding.ToUnitIndex(DivaAction.Circle);
            var source = new DivaChart { Notes = [new DivaChartNote { FrameIndex = 0, Type = type, Key = 5 }] };

            Assert.That(DivaChartBuilder.FromBeatmap(beatmap, source).Notes.Single().Key, Is.EqualTo(9));
        }

        [Test]
        public void Resolve_wav_key_falls_back_to_source_slot()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            var note = new DivaHitObject
            {
                StartTime = 0,
                Position = DivaActionEncoding.ToPlayfieldPosition(10, 12),
                ValidAction = DivaAction.Circle
            };
            beatmap.HitObjects.Add(note);

            int type = DivaActionEncoding.ToUnitIndex(DivaAction.Circle);
            var source = new DivaChart { Notes = [new DivaChartNote { FrameIndex = 0, Type = type, Key = 5 }] };

            Assert.That(DivaChartBuilder.ResolveWavKey(beatmap, note, source), Is.EqualTo(5));
            Assert.That(DivaChartBuilder.ResolveWavKey(beatmap, note, new DivaChart()), Is.Zero);

            note.WavKey = 2;
            Assert.That(DivaChartBuilder.ResolveWavKey(beatmap, note, source), Is.EqualTo(2));
        }

        [Test]
        public void Encoder_round_trips_wav_key_through_converter()
        {
            var source = new DivaBeatmap
            {
                BeatmapInfo =
                {
                    Ruleset = new DivaRuleset().RulesetInfo,
                    DifficultyName = "★5 Normal",
                    Metadata = { Title = "Key Song", Artist = "Artist", Tags = DivaActionEncoding.NATIVE_TAG }
                }
            };
            source.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            source.HitObjects.Add(new DivaHitObject
            {
                StartTime = 1000,
                Position = DivaActionEncoding.ToPlayfieldPosition(8, 8),
                ValidAction = DivaAction.Circle,
                WavKey = 7
            });

            string encoded = DivaBeatmapEncoder.ExportToString(source, null);
            Assert.That(encoded, Does.Contain("diva-action-"));
            Assert.That(encoded, Does.Contain("-k7"));

            using var reader = new LineBufferedReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(encoded)));
            var decoded = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
            decoded.BeatmapInfo.Ruleset = new DivaRuleset().RulesetInfo;

            var converted = (DivaBeatmap)new DivaBeatmapConverter(decoded, new DivaRuleset()).Convert();

            Assert.That(converted.HitObjects.Single().WavKey, Is.EqualTo(7));
        }

        [Test]
        public void Approach_origin_is_normalised_to_the_bpm_scaled_distance()
        {
            Vector2 normalised = DivaActionEncoding.NormaliseApproachOrigin(new Vector2(30, 40), 150);

            Assert.That(normalised.Length, Is.EqualTo(DivaActionEncoding.ApproachDistance(150)).Within(1e-3f));
            Assert.That(normalised.X / normalised.Y, Is.EqualTo(30f / 40f).Within(1e-3f));

            // 150 BPM is 120/150 of the base flight distance.
            Assert.That(DivaActionEncoding.ApproachDistance(150), Is.EqualTo(DivaChartConstants.DISTANCE * 0.8f).Within(1e-3f));
        }

        [Test]
        public void Approach_origin_without_a_direction_flies_rightwards()
        {
            Assert.That(DivaActionEncoding.NormaliseApproachOrigin(Vector2.Zero, 120), Is.EqualTo(new Vector2(DivaChartConstants.DISTANCE, 0)));
            Assert.That(DivaActionEncoding.ApproachDistance(0), Is.EqualTo(DivaChartConstants.DISTANCE));
        }

        [Test]
        public void Chart_builder_writes_the_normalised_flight_vector_as_the_tail()
        {
            var beatmap = new DivaBeatmap { BeatmapInfo = { BPM = 120 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            Vector2 position = DivaActionEncoding.ToPlayfieldPosition(10, 12);
            beatmap.HitObjects.Add(new DivaHitObject
            {
                StartTime = 0,
                Position = position,
                ValidAction = DivaAction.Circle,
                // A drag that ended off the grid and further away than the note actually flies.
                ApproachPieceOriginPosition = new Vector2(300, 400)
            });

            DivaChartNote note = DivaChartBuilder.FromBeatmap(beatmap).Notes.Single();

            float distance = DivaActionEncoding.ApproachDistance(120);
            Assert.That(note.TailX, Is.EqualTo((int)Math.Round(position.X + distance * 0.6f)));
            Assert.That(note.TailY, Is.EqualTo((int)Math.Round(position.Y + distance * 0.8f)));
        }

        private static DivaChart minimalChart(int periodCount, IReadOnlyList<DivaChartNote> notes) => new DivaChart
        {
            Metadata = new DivaChartMetadata
            {
                Title = "Chart Song",
                Creator = "Mapper",
                Artist = "Artist",
                Style = "Style",
                OverviewPicture = "bg.png",
                Level = 3,
                Hard = 5,
                Bpm = 120
            },
            PeriodCount = periodCount,
            FrameCount = periodCount * DivaChartConstants.NOTE_PER_PERIOD,
            Notes = notes
        };

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
