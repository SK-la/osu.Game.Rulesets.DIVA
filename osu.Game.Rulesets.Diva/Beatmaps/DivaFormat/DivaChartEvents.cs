// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    /// <summary>
    ///     Editable BGS / RES / file-table layer of a <c>.diva</c> chart.
    ///     Times are in lazer playback space (same clock as hit objects).
    ///     <see langword="null"/> on a beatmap means "not loaded" — export may fall back to the source file.
    /// </summary>
    public sealed class DivaChartEvents
    {
        public List<DivaBgmEvent> BgmEvents { get; } = [];

        public List<DivaResourceEvent> ResourceEvents { get; } = [];

        public Dictionary<int, string> WavFiles { get; } = new Dictionary<int, string>();

        public Dictionary<int, string> ResourceFiles { get; } = new Dictionary<int, string>();

        public static DivaChartEvents FromChart(DivaChart chart, DivaPlaybackTimeline timeline)
        {
            var events = new DivaChartEvents();

            foreach (DivaBgmEvent bgm in chart.BgmEvents)
            {
                events.BgmEvents.Add(new DivaBgmEvent
                {
                    Sequence = bgm.Sequence,
                    FrameIndex = bgm.FrameIndex,
                    TimeMs = timeline.ToPlaybackTime(bgm.TimeMs),
                    Slot = bgm.Slot,
                    WavId = bgm.WavId,
                    DeclaredSourceOffsetMs = bgm.DeclaredSourceOffsetMs
                });
            }

            foreach (DivaResourceEvent resource in chart.ResourceEvents)
            {
                events.ResourceEvents.Add(new DivaResourceEvent
                {
                    Sequence = resource.Sequence,
                    FrameIndex = resource.FrameIndex,
                    TimeMs = timeline.ToPlaybackTime(resource.TimeMs),
                    ResourceId = resource.ResourceId,
                    DeclaredSourceOffsetMs = resource.DeclaredSourceOffsetMs
                });
            }

            foreach ((int id, string file) in chart.WavFiles)
                events.WavFiles[id] = file;

            foreach ((int id, string file) in chart.ResourceFiles)
                events.ResourceFiles[id] = file;

            return events;
        }

        public DivaChartEvents Clone()
        {
            var clone = new DivaChartEvents();

            foreach (DivaBgmEvent bgm in BgmEvents)
            {
                clone.BgmEvents.Add(new DivaBgmEvent
                {
                    Sequence = bgm.Sequence,
                    FrameIndex = bgm.FrameIndex,
                    TimeMs = bgm.TimeMs,
                    Slot = bgm.Slot,
                    WavId = bgm.WavId,
                    DeclaredSourceOffsetMs = bgm.DeclaredSourceOffsetMs
                });
            }

            foreach (DivaResourceEvent resource in ResourceEvents)
            {
                clone.ResourceEvents.Add(new DivaResourceEvent
                {
                    Sequence = resource.Sequence,
                    FrameIndex = resource.FrameIndex,
                    TimeMs = resource.TimeMs,
                    ResourceId = resource.ResourceId,
                    DeclaredSourceOffsetMs = resource.DeclaredSourceOffsetMs
                });
            }

            foreach ((int id, string file) in WavFiles)
                clone.WavFiles[id] = file;

            foreach ((int id, string file) in ResourceFiles)
                clone.ResourceFiles[id] = file;

            return clone;
        }

        public void CopyFrom(DivaChartEvents other)
        {
            BgmEvents.Clear();
            BgmEvents.AddRange(other.BgmEvents.Select(bgm => new DivaBgmEvent
            {
                Sequence = bgm.Sequence,
                FrameIndex = bgm.FrameIndex,
                TimeMs = bgm.TimeMs,
                Slot = bgm.Slot,
                WavId = bgm.WavId,
                DeclaredSourceOffsetMs = bgm.DeclaredSourceOffsetMs
            }));

            ResourceEvents.Clear();
            ResourceEvents.AddRange(other.ResourceEvents.Select(resource => new DivaResourceEvent
            {
                Sequence = resource.Sequence,
                FrameIndex = resource.FrameIndex,
                TimeMs = resource.TimeMs,
                ResourceId = resource.ResourceId,
                DeclaredSourceOffsetMs = resource.DeclaredSourceOffsetMs
            }));

            WavFiles.Clear();
            foreach ((int id, string file) in other.WavFiles)
                WavFiles[id] = file;

            ResourceFiles.Clear();
            foreach ((int id, string file) in other.ResourceFiles)
                ResourceFiles[id] = file;
        }
    }
}
