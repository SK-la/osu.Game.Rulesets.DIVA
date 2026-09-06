// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Text;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Diva.Audio;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects;
using osuTK;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    /// <summary>
    ///     Decodes ProjectDIVA <c>.diva</c> text charts into a beatmap usable by the DIVA ruleset.
    /// </summary>
    public class DivaBeatmapDecoder : Decoder<Beatmap>
    {
        public static void Register()
        {
            // EditorVer lines are typically "1.0.x.x".
            AddDecoder<Beatmap>("1.", _ => new DivaBeatmapDecoder());
            DivaStoryboardDecoder.Register();
        }

        protected override void ParseStreamInto(LineBufferedReader stream, bool isPrimaryStream, Beatmap beatmap)
        {
            var sb = new StringBuilder();

            while (stream.ReadLine() is { } line)
                sb.AppendLine(line);

            using var reader = new StringReader(sb.ToString());
            DivaChart chart = DivaChartFileParser.Parse(reader, string.Empty, string.Empty);
            DivaPlaybackTimeline timeline = DivaPlaybackTimeline.Create(chart);

            beatmap.BeatmapInfo.DifficultyName = DivaChartConstants.FormatDifficultyName(chart.Metadata.Level, chart.Metadata.Hard);
            beatmap.BeatmapInfo.Difficulty.OverallDifficulty = Math.Clamp(chart.Metadata.Hard, 1, 10);
            // Native Hard is the displayed star rating (no algorithmic SR).
            beatmap.BeatmapInfo.StarRating = Math.Max(0, chart.Metadata.Hard);
            beatmap.BeatmapInfo.Difficulty.CircleSize = 4;
            beatmap.BeatmapInfo.Difficulty.DrainRate = 5;
            beatmap.BeatmapInfo.Difficulty.ApproachRate = 8;
            beatmap.BeatmapInfo.BPM = chart.Metadata.Bpm;
            beatmap.Metadata.Title = chart.Metadata.Title;
            beatmap.Metadata.TitleUnicode = chart.Metadata.Title;
            beatmap.Metadata.Artist = chart.Metadata.Artist;
            beatmap.Metadata.ArtistUnicode = chart.Metadata.Artist;
            beatmap.Metadata.Author.Username = chart.Metadata.Creator;
            beatmap.Metadata.Source = "ProjectDIVA";
            beatmap.Metadata.Tags = $"{DivaActionEncoding.NATIVE_TAG} diva-external";
            beatmap.Metadata.AudioFile = timeline.AudioRelativePath ?? string.Empty;
            beatmap.Metadata.BackgroundFile = chart.ResolveBackgroundRelativePath() ?? string.Empty;

            foreach (DivaChartControlPoint point in chart.TimingPoints.OrderBy(p => p.TimeMs))
            {
                beatmap.ControlPointInfo.Add(timeline.ToPlaybackTime(point.TimeMs), new TimingControlPoint
                {
                    BeatLength = 60000.0 / (point.Bpm > 0 ? point.Bpm : 120)
                });
            }

            if (chart.TimingPoints.Count == 0)
            {
                beatmap.ControlPointInfo.Add(timeline.ToPlaybackTime(0), new TimingControlPoint
                {
                    BeatLength = 60000.0 / (chart.Metadata.Bpm > 0 ? chart.Metadata.Bpm : 120)
                });
            }

            if (chart.HasChanceTime)
            {
                beatmap.ControlPointInfo.Add(timeline.ToPlaybackTime(chart.ChanceTimeStartMs), new EffectControlPoint { KiaiMode = true });
                beatmap.ControlPointInfo.Add(timeline.ToPlaybackTime(chart.ChanceTimeEndMs), new EffectControlPoint { KiaiMode = false });
            }

            double headerBpm = chart.Metadata.Bpm > 0 ? chart.Metadata.Bpm : DivaChartConstants.BASE_BPM;

            foreach (DivaChartNote note in chart.Notes.OrderBy(n => n.StartTimeMs))
            {
                DivaAction action = DivaActionEncoding.ResolveAction(note);
                Vector2 position = DivaActionEncoding.ToPlayfieldPosition(note.GridX, note.GridY);
                double bpm = resolveBpmAt(chart, note.StartTimeMs, headerBpm);
                Vector2 approach = DivaActionEncoding.ComputeApproachOrigin(note, bpm);

                if (note.IsHold)
                {
                    beatmap.HitObjects.Add(new DivaHoldHitObject
                    {
                        StartTime = timeline.ToPlaybackTime(note.StartTimeMs),
                        Duration = note.DurationMs,
                        Position = position,
                        ValidAction = action,
                        Samples = [DivaHitSampleInfo.Sweep],
                        ApproachPieceOriginPosition = approach
                    });
                }
                else
                {
                    beatmap.HitObjects.Add(new DivaHitObject
                    {
                        StartTime = timeline.ToPlaybackTime(note.StartTimeMs),
                        Position = position,
                        ValidAction = action,
                        Samples = [DivaHitSampleInfo.Normal],
                        ApproachPieceOriginPosition = approach
                    });
                }
            }
        }

        private static double resolveBpmAt(DivaChart chart, double timeMs, double fallback)
        {
            double bpm = fallback;

            foreach (DivaChartControlPoint point in chart.TimingPoints.OrderBy(p => p.TimeMs))
            {
                if (point.TimeMs > timeMs)
                    break;

                if (point.Bpm > 0)
                    bpm = point.Bpm;
            }

            return bpm;
        }
    }
}
