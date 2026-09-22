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
            source ??= LoadSource(beatmap);
            DivaChartEvents? events = DivaBeatmap.EventsOf(beatmap);

            var timingPoints = beatmap.ControlPointInfo.TimingPoints.OrderBy(p => p.Time).ToArray();
            double headerBpm = resolveHeaderBpm(beatmap, timingPoints);

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
            Dictionary<(int Frame, int Type), int>? sourceKeys = source != null ? buildSourceKeys(source) : null;

            var notes = new List<DivaChartNote>();
            foreach (var hitObject in beatmap.HitObjects.OfType<DivaHitObject>().OrderBy(h => h.StartTime))
            {
                Vector2 grid = DivaActionEncoding.ToGridPosition(hitObject.Position);
                (int tailX, int tailY) = approachToTail(hitObject.Position, hitObject.ApproachPieceOriginPosition, bpmAt(hitObject.StartTime, timingPoints, headerBpm));
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
                    Key = hitObject.WavKey ?? (sourceKeys != null && sourceKeys.TryGetValue((frame, type), out int sourceKey) ? sourceKey : 0),
                    DurationMs = isHold ? ((DivaHoldHitObject)hitObject).Duration : 0
                });
            }

            int lastFrame = 0;
            if (notes.Count > 0)
                lastFrame = Math.Max(lastFrame, notes.Max(n => n.FrameIndex));
            if (chartTiming.Count > 0)
                lastFrame = Math.Max(lastFrame, chartTiming.Max(t => t.FrameIndex));

            (int chanceStart, int chanceEnd, double chanceStartMs, double chanceEndMs) = resolveChanceTime(beatmap, timingPoints, headerBpm, source);

            // Everything that is addressed by frame has to fit: the reading side clamps frames into the period
            // array, so a count that only covers the notes would silently fold trailing BGS / Resource /
            // ChanceTime data onto the last measure.
            if (chanceEnd >= 0)
                lastFrame = Math.Max(lastFrame, chanceEnd);

            var metadata = beatmap.Metadata;
            DivaChartHeader? header = DivaBeatmap.HeaderOf(beatmap);
            string style = !string.IsNullOrEmpty(header?.Style)
                ? header.Style
                : DivaChartHeader.ExtractStyle(metadata.Tags, source?.Metadata.Style ?? string.Empty);
            (IReadOnlyDictionary<int, string> wavFiles, IReadOnlyDictionary<int, string> resourceFiles,
                IReadOnlyList<DivaBgmEvent> bgmEvents, IReadOnlyList<DivaResourceEvent> resourceEvents) =
                resolveEvents(events, source, timingPoints, headerBpm);

            foreach (DivaBgmEvent bgm in bgmEvents)
                lastFrame = Math.Max(lastFrame, bgm.FrameIndex);
            foreach (DivaResourceEvent resource in resourceEvents)
                lastFrame = Math.Max(lastFrame, resource.FrameIndex);

            int requiredPeriodCount = Math.Max(1, (int)Math.Ceiling((lastFrame + 1) / (double)DivaChartConstants.NOTE_PER_PERIOD));
            int periodCount = Math.Max(requiredPeriodCount, header?.MinPeriodCount ?? 1);
            int frameCount = periodCount * DivaChartConstants.NOTE_PER_PERIOD;

            return new DivaChart
            {
                Metadata = new DivaChartMetadata
                {
                    Title = metadata.TitleUnicode.Length > 0 ? metadata.TitleUnicode : metadata.Title,
                    Creator = metadata.Author.Username,
                    Artist = source?.Metadata.Artist ?? metadata.Artist,
                    Style = style,
                    OverviewPicture = !string.IsNullOrEmpty(header?.OverviewPicture)
                        ? header.OverviewPicture
                        : source?.Metadata.OverviewPicture ?? Path.GetFileName(metadata.BackgroundFile),
                    Level = header?.Level ?? source?.Metadata.Level ?? 1,
                    Hard = header?.Hard ?? source?.Metadata.Hard ?? (int)Math.Clamp(Math.Round(beatmap.Difficulty.OverallDifficulty), 1, 10),
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

        /// <summary>
        ///     The <c>.diva</c> the beatmap points at, if it is still on disk. Used as the fallback source for
        ///     everything the editor does not own; returns <see langword="null"/> for a chart with no file yet.
        /// </summary>
        public static DivaChart? LoadSource(IBeatmap beatmap)
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

        /// <summary>
        ///     Chart frame index of a playfield time under the beatmap's own header BPM and timing points —
        ///     the same mapping the export uses, so the editor can tell which notes share a frame record.
        /// </summary>
        public static int TimeToFrame(IBeatmap beatmap, double timeMs)
        {
            var timingPoints = beatmap.ControlPointInfo.TimingPoints.OrderBy(p => p.Time).ToArray();
            return timeToFrame(timeMs, timingPoints, resolveHeaderBpm(beatmap, timingPoints));
        }

        /// <summary>
        ///     Milliseconds one chart frame lasts at a playfield time — the unit the export quantises hold
        ///     lengths to, so the editor can show (and write) the length a save actually produces.
        /// </summary>
        public static double MsPerFrameAt(IBeatmap beatmap, double timeMs)
        {
            var timingPoints = beatmap.ControlPointInfo.TimingPoints.OrderBy(p => p.Time).ToArray();
            return DivaChartConstants.MsPerFrame(bpmAt(timeMs, timingPoints, resolveHeaderBpm(beatmap, timingPoints)));
        }

        /// <summary>
        ///     Same-button notes whose spans over the chart's frames overlap, earlier note first. ProjectDIVA
        ///     keeps a hold as one record plus a frame length, so its button is busy for the whole span and a
        ///     note starting inside it cannot be pressed on its own — the reference editor refuses such a
        ///     placement unless the conflicting keys are cleared.
        /// </summary>
        /// <remarks>
        ///     Only holds can cover a later note: a tap occupies its own frame alone, which is also why the
        ///     reference editor lets two taps of one button share a frame.
        /// </remarks>
        public static IEnumerable<(DivaHitObject Covering, DivaHitObject Covered)> FindActionOverlaps(IBeatmap beatmap)
        {
            foreach (var button in beatmap.HitObjects.OfType<DivaHitObject>().GroupBy(h => h.ValidAction))
            {
                DivaHitObject? covering = null;
                int coveringStart = 0;
                int coveringEnd = 0;

                foreach (DivaHitObject note in button.OrderBy(h => h.StartTime))
                {
                    int start = TimeToFrame(beatmap, note.StartTime);

                    if (covering != null && start < coveringEnd)
                        yield return (covering, note);

                    int end = start + (note is DivaHoldHitObject hold
                        ? FrameLengthAt(beatmap, note.StartTime, hold.Duration)
                        : 0);

                    // Keep the note that stays busy the longest, so a note inside several holds is reported
                    // against the one that actually reaches furthest into it.
                    if (covering == null || end > coveringEnd)
                    {
                        covering = note;
                        coveringStart = start;
                        coveringEnd = end;
                    }
                }
            }
        }

        /// <summary>
        ///     Frame length an export would write for a hold of <paramref name="durationMs"/>.
        /// </summary>
        public static int FrameLengthAt(IBeatmap beatmap, double timeMs, double durationMs)
            => DivaChartConstants.MsToFrameLength(durationMs, MsPerFrameAt(beatmap, timeMs));

        /// <summary>
        ///     The <c>#WAV</c> slot a note exports with: its own value, else the source chart's value for the
        ///     same frame and button (which is how the PC editor's charts identify a note), else 0.
        /// </summary>
        public static int ResolveWavKey(IBeatmap beatmap, DivaHitObject hitObject, DivaChart? source = null)
        {
            if (hitObject.WavKey is { } key)
                return key;

            source ??= LoadSource(beatmap);

            if (source == null)
                return 0;

            int type = DivaActionEncoding.ToUnitIndex(hitObject.ValidAction);
            if (hitObject is DivaHoldHitObject)
                type += DivaChartConstants.NOTE_TYPE_COUNT;

            return buildSourceKeys(source).TryGetValue((TimeToFrame(beatmap, hitObject.StartTime), type), out int sourceKey) ? sourceKey : 0;
        }

        private static Dictionary<(int Frame, int Type), int> buildSourceKeys(DivaChart source)
        {
            var keys = new Dictionary<(int, int), int>();

            foreach (DivaChartNote note in source.Notes)
                keys[(note.FrameIndex, note.Type)] = note.Key;

            return keys;
        }

        private static double resolveHeaderBpm(IBeatmap beatmap, TimingControlPoint[] timingPoints)
            => beatmap.BeatmapInfo.BPM > 0
                ? beatmap.BeatmapInfo.BPM
                : timingPoints.FirstOrDefault()?.BPM ?? DivaChartConstants.BASE_BPM;

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

        private static (int TailX, int TailY) approachToTail(Vector2 notePos, Vector2 approach, double bpm)
        {
            // ProjectDIVA only keeps the direction of (tail - note) and re-derives the length from the BPM, so
            // the exported tail is normalised too: that keeps an export→reload→export cycle byte-identical.
            Vector2 far = notePos + DivaActionEncoding.NormaliseApproachOrigin(approach, bpm);
            return ((int)Math.Round(far.X), (int)Math.Round(far.Y));
        }

        /// <summary>BPM in play at a time, i.e. the last timing point at or before it.</summary>
        private static double bpmAt(double timeMs, TimingControlPoint[] timingPoints, double headerBpm)
        {
            double bpm = headerBpm > 0 ? headerBpm : DivaChartConstants.BASE_BPM;

            foreach (TimingControlPoint point in timingPoints)
            {
                if (point.Time > timeMs)
                    break;

                if (point.BPM > 0)
                    bpm = point.BPM;
            }

            return bpm;
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
    }
}
