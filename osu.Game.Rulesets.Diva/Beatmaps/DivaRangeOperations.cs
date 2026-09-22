// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    /// <summary>
    ///     The reference editor's interval commands: it keeps one record per chart frame, so "a time point" is a
    ///     frame and an interval is a frame range — a unit that does not move when the BPM does. A note belongs
    ///     to the interval its <em>start</em> frame falls in; its tail is part of no interval of its own.
    /// </summary>
    public static class DivaRangeOperations
    {
        public static List<DivaHitObject> NotesAtFrame(IBeatmap beatmap, int frame) => NotesInFrameRange(beatmap, frame, frame);

        /// <summary>Notes whose start frame lies in <c>[fromFrame, toFrame]</c>, in either order.</summary>
        public static List<DivaHitObject> NotesInFrameRange(IBeatmap beatmap, int fromFrame, int toFrame)
        {
            (int from, int to) = Normalise(fromFrame, toFrame);

            var result = new List<DivaHitObject>();

            foreach (DivaHitObject note in beatmap.HitObjects.OfType<DivaHitObject>())
            {
                int frame = DivaChartBuilder.TimeToFrame(beatmap, note.StartTime);

                if (frame >= from && frame <= to)
                    result.Add(note);
            }

            return result;
        }

        /// <summary>
        ///     Copies the interval <c>[fromFrame, toFrame]</c> to <paramref name="targetStartFrame"/>, i.e. moves
        ///     every note and BGS / RES event by <c>targetStartFrame - fromFrame</c> frames. A hold keeps its frame
        ///     length, the way the reference editor copies its length field.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         BPM and STOP are chart-wide tables rather than per-frame content, so they are not copied: the
        ///         frame ↔ millisecond mapping used to place the copies is derived from them, and copying one
        ///         along would move every landing point computed here. The reference editor copies raw records
        ///         and so avoids that by staying in frame space.
        ///     </para>
        ///     <para>
        ///         Copies pushed past the last frame are dropped and <see langword="null"/> comes back only for a
        ///         nonsensical target, matching the reference editor's "copied, overflow ignored".
        ///     </para>
        /// </remarks>
        public static DivaRangeCopyPlan? PlanRangeCopy(IBeatmap beatmap, int fromFrame, int toFrame, int targetStartFrame)
        {
            if (targetStartFrame < 0 || targetStartFrame > DivaChartConstants.MAX_FRAME_INDEX)
                return null;

            (int from, int to) = Normalise(fromFrame, toFrame);
            int delta = targetStartFrame - from;

            var notes = new List<DivaHitObject>();

            foreach (DivaHitObject note in NotesInFrameRange(beatmap, from, to))
            {
                int targetFrame = DivaChartBuilder.TimeToFrame(beatmap, note.StartTime) + delta;

                if (targetFrame < 0 || targetFrame > DivaChartConstants.MAX_FRAME_INDEX)
                    continue;

                double startTime = DivaChartBuilder.FrameToTime(beatmap, targetFrame);
                DivaHitObject copy = note.Clone();
                copy.StartTime = startTime;

                if (copy is DivaHoldHitObject copyHold && note is DivaHoldHitObject sourceHold)
                {
                    int length = DivaChartBuilder.FrameLengthAt(beatmap, sourceHold.StartTime, sourceHold.Duration);
                    int endFrame = Math.Min(DivaChartConstants.MAX_FRAME_INDEX, targetFrame + length);
                    copyHold.Duration = Math.Max(1, DivaChartBuilder.FrameToTime(beatmap, endFrame) - startTime);
                }

                notes.Add(copy);
            }

            var bgm = new List<DivaBgmEvent>();
            var resources = new List<DivaResourceEvent>();

            if (DivaBeatmap.EventsOf(beatmap) is { } events)
            {
                foreach (DivaBgmEvent e in events.BgmEvents)
                {
                    if (tryShift(beatmap, e.TimeMs, from, to, delta, out double time))
                        bgm.Add(new DivaBgmEvent { Sequence = e.Sequence, TimeMs = time, Slot = e.Slot, WavId = e.WavId, DeclaredSourceOffsetMs = e.DeclaredSourceOffsetMs });
                }

                foreach (DivaResourceEvent e in events.ResourceEvents)
                {
                    if (tryShift(beatmap, e.TimeMs, from, to, delta, out double time))
                        resources.Add(new DivaResourceEvent { Sequence = e.Sequence, TimeMs = time, ResourceId = e.ResourceId, DeclaredSourceOffsetMs = e.DeclaredSourceOffsetMs });
                }
            }

            return new DivaRangeCopyPlan(notes, bgm, resources);
        }

        private static bool tryShift(IBeatmap beatmap, double timeMs, int from, int to, int frameDelta, out double time)
        {
            int frame = DivaChartBuilder.TimeToFrame(beatmap, timeMs);

            if (frame < from || frame > to)
            {
                time = 0;
                return false;
            }

            int target = frame + frameDelta;

            if (target < 0 || target > DivaChartConstants.MAX_FRAME_INDEX)
            {
                time = 0;
                return false;
            }

            time = DivaChartBuilder.FrameToTime(beatmap, target);
            return true;
        }

        private static (int From, int To) Normalise(int fromFrame, int toFrame)
            => fromFrame <= toFrame ? (fromFrame, toFrame) : (toFrame, fromFrame);
    }

    /// <summary>
    ///     Everything a <see cref="DivaRangeOperations.PlanRangeCopy"/> would add: detached copies, safe to insert
    ///     once the caller has taken a change scope.
    /// </summary>
    /// <remarks>
    ///     Event <c>FrameIndex</c> is deliberately left unset — the export derives it from <c>TimeMs</c>.
    /// </remarks>
    public sealed class DivaRangeCopyPlan
    {
        public DivaRangeCopyPlan(IReadOnlyList<DivaHitObject> notes, IReadOnlyList<DivaBgmEvent> bgmEvents, IReadOnlyList<DivaResourceEvent> resourceEvents)
        {
            Notes = notes;
            BgmEvents = bgmEvents;
            ResourceEvents = resourceEvents;
        }

        public IReadOnlyList<DivaHitObject> Notes { get; }

        public IReadOnlyList<DivaBgmEvent> BgmEvents { get; }

        public IReadOnlyList<DivaResourceEvent> ResourceEvents { get; }
    }
}
