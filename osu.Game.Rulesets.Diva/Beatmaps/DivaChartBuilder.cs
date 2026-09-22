// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Objects;
using osuTK;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    /// <summary>
    /// Rebuilds a ProjectDIVA chart from a playable DIVA beatmap.
    /// BGS/video events come from <see cref="DivaBeatmap.ChartEvents"/> when present,
    /// otherwise from the source <c>.diva</c> still on disk.
    /// </summary>
    public static class DivaChartBuilder
    {
        public static DivaChart FromBeatmap(IBeatmap beatmap, DivaChart? source = null)
        {
            source ??= tryLoadSource(beatmap);
            DivaChartEvents? events = DivaBeatmap.EventsOf(beatmap);

            var timingPoints = beatmap.ControlPointInfo.TimingPoints.OrderBy(p => p.Time).ToArray();
            double headerBpm = beatmap.BeatmapInfo.BPM > 0
                ? beatmap.BeatmapInfo.BPM
                : timingPoints.FirstOrDefault()?.BPM ?? DivaChartConstants.BASE_BPM;

            var chartTiming = new List<DivaChartControlPoint>();
            foreach (TimingControlPoint point in timingPoints)
            {
                chartTiming.Add(new DivaChartControlPoint
                {
                    FrameIndex = timeToFrame(point.Time, timingPoints, headerBpm),
                    TimeMs = point.Time,
                    Bpm = point.BPM
                });
            }

            if (chartTiming.Count == 0)
            {
                chartTiming.Add(new DivaChartControlPoint
                {
                    FrameIndex = 0,
                    TimeMs = 0,
                    Bpm = headerBpm
                });
            }

            // The #WAV slot chosen for each note in the PC editor. It is display/audio metadata only —
            // action resolution uses Type — but must survive a round trip, so notes are matched on the
            // (frame, type) pair that identifies them in the file.
            Dictionary<(int Frame, int Type), int>? sourceKeys = null;

            if (source != null)
            {
                sourceKeys = new Dictionary<(int, int), int>();

                foreach (DivaChartNote sourceNote in source.Notes)
                    sourceKeys[(sourceNote.FrameIndex, sourceNote.Type)] = sourceNote.Key;
            }

            var notes = new List<DivaChartNote>();
            foreach (var hitObject in beatmap.HitObjects.OfType<DivaHitObject>().OrderBy(h => h.StartTime))
            {
                Vector2 grid = DivaActionEncoding.ToGridPosition(hitObject.Position);
                (int tailX, int tailY) = approachToTail(hitObject.Position, hitObject.ApproachPieceOriginPosition);
                bool isHold = hitObject is DivaHoldHitObject;
                int type = DivaActionEncoding.ToUnitIndex(hitObject.ValidAction);
                if (isHold)
                    type += DivaChartConstants.NOTE_TYPE_COUNT;

                int frame = timeToFrame(hitObject.StartTime, timingPoints, headerBpm);

                notes.Add(new DivaChartNote
                {
                    FrameIndex = frame,
                    StartTimeMs = hitObject.StartTime,
                    Type = type,
                    X = grid.X,
                    Y = grid.Y,
                    TailX = tailX,
                    TailY = tailY,
                    Key = sourceKeys != null && sourceKeys.TryGetValue((frame, type), out int sourceKey) ? sourceKey : 0,
                    DurationMs = isHold ? ((DivaHoldHitObject)hitObject).Duration : 0
                });
            }

            int lastFrame = 0;
            if (notes.Count > 0)
                lastFrame = Math.Max(lastFrame, notes.Max(n => n.FrameIndex));
            if (chartTiming.Count > 0)
                lastFrame = Math.Max(lastFrame, chartTiming.Max(t => t.FrameIndex));

            int periodCount = Math.Max(1, (int)Math.Ceiling((lastFrame + 1) / (double)DivaChartConstants.NOTE_PER_PERIOD));
            int frameCount = periodCount * DivaChartConstants.NOTE_PER_PERIOD;

            (int chanceStart, int chanceEnd, double chanceStartMs, double chanceEndMs) = resolveChanceTime(beatmap, timingPoints, headerBpm, source);

            var metadata = beatmap.Metadata;
            string style = extractStyle(metadata.Tags, source?.Metadata.Style ?? string.Empty);
            (IReadOnlyDictionary<int, string> wavFiles, IReadOnlyDictionary<int, string> resourceFiles,
                IReadOnlyList<DivaBgmEvent> bgmEvents, IReadOnlyList<DivaResourceEvent> resourceEvents) =
                resolveEvents(events, source, timingPoints, headerBpm);

            return new DivaChart
            {
                Metadata = new DivaChartMetadata
                {
                    Title = metadata.TitleUnicode.Length > 0 ? metadata.TitleUnicode : metadata.Title,
                    Creator = metadata.Author.Username,
                    Artist = source?.Metadata.Artist ?? metadata.Artist,
                    Style = style,
                    OverviewPicture = source?.Metadata.OverviewPicture
                                      ?? Path.GetFileName(metadata.BackgroundFile),
                    Level = source?.Metadata.Level ?? 1,
                    Hard = source?.Metadata.Hard ?? (int)Math.Clamp(Math.Round(beatmap.Difficulty.OverallDifficulty), 1, 10),
                    Bpm = headerBpm,
                    SourcePath = source?.Metadata.SourcePath ?? string.Empty,
                    SongFolder = source?.Metadata.SongFolder ?? string.Empty
                },
                PeriodCount = periodCount,
                FrameCount = frameCount,
                TimingPoints = chartTiming,
                Notes = notes,
                WavFiles = wavFiles,
                ResourceFiles = resourceFiles,
                BgmEvents = bgmEvents,
                ResourceEvents = resourceEvents,
                ChanceTimeStart = chanceStart,
                ChanceTimeEnd = chanceEnd,
                ChanceTimeStartMs = chanceStartMs,
                ChanceTimeEndMs = chanceEndMs
            };
        }

        private static DivaChart? tryLoadSource(IBeatmap beatmap)
        {
            string? path = beatmap.BeatmapInfo.Path;
#if DIVA_EZ2LAZER
            string? contentRoot = beatmap.BeatmapInfo.BeatmapSet?.GetEffectiveExternalContentRoot();
#else
            string? contentRoot = null;
#endif

            if (!string.IsNullOrWhiteSpace(contentRoot) && !string.IsNullOrWhiteSpace(path))
            {
                string full = Path.Combine(contentRoot, path.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(full) && full.EndsWith(".diva", StringComparison.OrdinalIgnoreCase))
                    return DivaChartFileParser.Parse(full);
            }

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path) && path.EndsWith(".diva", StringComparison.OrdinalIgnoreCase))
                return DivaChartFileParser.Parse(path);

            return null;
        }

        private static int timeToFrame(double timeMs, TimingControlPoint[] timingPoints, double headerBpm)
        {
            if (timingPoints.Length == 0)
                return (int)Math.Round(timeMs / DivaChartConstants.MsPerFrame(headerBpm));

            TimingControlPoint active = timingPoints[0];
            int frame = 0;
            double cursor = active.Time;

            for (int i = 1; i < timingPoints.Length; i++)
            {
                var next = timingPoints[i];
                if (next.Time > timeMs)
                    break;

                double span = next.Time - cursor;
                frame += (int)Math.Round(span / DivaChartConstants.MsPerFrame(active.BPM));
                cursor = next.Time;
                active = next;
            }

            frame += (int)Math.Round((timeMs - cursor) / DivaChartConstants.MsPerFrame(active.BPM));
            return Math.Max(0, frame);
        }

        private static (int TailX, int TailY) approachToTail(Vector2 notePos, Vector2 approach)
        {
            if (approach.LengthSquared < 0.0001f)
                return ((int)Math.Round(notePos.X + DivaChartConstants.DISTANCE), (int)Math.Round(notePos.Y));

            Vector2 far = notePos + approach;
            return ((int)Math.Round(far.X), (int)Math.Round(far.Y));
        }

        private static (int Start, int End, double StartMs, double EndMs) resolveChanceTime(
            IBeatmap beatmap,
            TimingControlPoint[] timingPoints,
            double headerBpm,
            DivaChart? source)
        {
            var kiai = beatmap.ControlPointInfo.EffectPoints.Where(p => p.KiaiMode).OrderBy(p => p.Time).ToArray();
            if (kiai.Length == 0)
            {
                if (source is { HasChanceTime: true })
                    return (source.ChanceTimeStart, source.ChanceTimeEnd, source.ChanceTimeStartMs, source.ChanceTimeEndMs);

                return (-1, -1, -1, -1);
            }

            double startMs = kiai[0].Time;
            var kiaiOff = beatmap.ControlPointInfo.EffectPoints.Where(p => !p.KiaiMode && p.Time > startMs).OrderBy(p => p.Time).FirstOrDefault();
            double endMs = kiaiOff?.Time ?? beatmap.HitObjects.LastOrDefault()?.GetEndTime() ?? startMs;
            int start = timeToFrame(startMs, timingPoints, headerBpm);
            int exclusiveEnd = timeToFrame(endMs, timingPoints, headerBpm);
            int end = Math.Max(start, exclusiveEnd - 1);
            return (start, end, startMs, endMs);
        }

        private static (IReadOnlyDictionary<int, string> Wav, IReadOnlyDictionary<int, string> Resources,
            IReadOnlyList<DivaBgmEvent> Bgm, IReadOnlyList<DivaResourceEvent> ResourceEvents)
            resolveEvents(DivaChartEvents? events, DivaChart? source, TimingControlPoint[] timingPoints, double headerBpm)
        {
            if (events == null)
            {
                return (
                    source?.WavFiles ?? new Dictionary<int, string>(),
                    source?.ResourceFiles ?? new Dictionary<int, string>(),
                    source?.BgmEvents ?? [],
                    source?.ResourceEvents ?? []
                );
            }

            var bgm = new List<DivaBgmEvent>(events.BgmEvents.Count);
            for (int i = 0; i < events.BgmEvents.Count; i++)
            {
                DivaBgmEvent e = events.BgmEvents[i];
                bgm.Add(new DivaBgmEvent
                {
                    Sequence = i,
                    FrameIndex = timeToFrame(e.TimeMs, timingPoints, headerBpm),
                    TimeMs = e.TimeMs,
                    Slot = e.Slot,
                    WavId = e.WavId,
                    DeclaredSourceOffsetMs = e.DeclaredSourceOffsetMs
                });
            }

            var resources = new List<DivaResourceEvent>(events.ResourceEvents.Count);
            for (int i = 0; i < events.ResourceEvents.Count; i++)
            {
                DivaResourceEvent e = events.ResourceEvents[i];
                resources.Add(new DivaResourceEvent
                {
                    Sequence = i,
                    FrameIndex = timeToFrame(e.TimeMs, timingPoints, headerBpm),
                    TimeMs = e.TimeMs,
                    ResourceId = e.ResourceId,
                    DeclaredSourceOffsetMs = e.DeclaredSourceOffsetMs
                });
            }

            return (
                new Dictionary<int, string>(events.WavFiles),
                new Dictionary<int, string>(events.ResourceFiles),
                bgm,
                resources
            );
        }

        private static string extractStyle(string tags, string fallback)
        {
            if (string.IsNullOrWhiteSpace(tags))
                return fallback;

            var leftover = tags.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                               .Where(t => !t.Equals(DivaActionEncoding.NATIVE_TAG, StringComparison.OrdinalIgnoreCase)
                                           && !t.Equals("diva-import", StringComparison.OrdinalIgnoreCase)
                                           && !t.Equals("diva-external", StringComparison.OrdinalIgnoreCase));
            string joined = string.Join(' ', leftover);
            return joined.Length > 0 ? joined : fallback;
        }
    }
}
