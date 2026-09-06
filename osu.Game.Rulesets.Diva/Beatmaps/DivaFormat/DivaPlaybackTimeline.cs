// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    /// <summary>
    /// Maps ProjectDIVA's event-driven media clock to lazer's single-track clock.
    /// </summary>
    public sealed class DivaPlaybackTimeline
    {
        public string? AudioRelativePath { get; private init; }
        public int EventFrameIndex { get; private init; }
        public double EventTimeMs { get; private init; }
        public double SourceOffsetMs { get; private init; }
        public int? BgmWavId { get; private init; }
        public int? ResourceId { get; private init; }
        public bool UsesResourceVideo => ResourceId != null;
        public bool HasAdditionalAudioSegments { get; private init; }

        /// <summary>
        /// Added to raw DIVA frame time to express the same instant on lazer's untrimmed source track.
        /// </summary>
        public double OffsetMs => SourceOffsetMs - EventTimeMs;

        public double ToPlaybackTime(double divaTimeMs) => divaTimeMs + OffsetMs;

        public static DivaPlaybackTimeline Create(DivaChart chart)
        {
            if (chart == null)
                throw new ArgumentNullException(nameof(chart));

            DivaPlaybackTimeline? fromBgm = createFromBgm(chart);
            if (fromBgm != null)
                return fromBgm;

            DivaPlaybackTimeline? fromResource = createFromResource(chart);
            if (fromResource != null)
                return fromResource;

            return new DivaPlaybackTimeline
            {
                AudioRelativePath = resolveFallbackAudio(chart)
            };
        }

        /// <summary>
        /// Resolves the first RES video event for storyboard playback, mapped onto lazer's track clock.
        /// </summary>
        public static DivaVideoPlayback? ResolvePrimaryVideo(DivaChart chart)
        {
            if (chart == null)
                throw new ArgumentNullException(nameof(chart));

            List<DivaResourceEvent> effective = getEffectiveVideoEvents(chart);

            if (effective.Count == 0)
                return null;

            DivaResourceEvent primary = effective[0];
            string? relative = chart.ResolveRelativePath(chart.ResourceFiles[primary.ResourceId]);

            if (string.IsNullOrWhiteSpace(relative))
                return null;

            double videoSourceOffset = chart.ResourceEvents
                                           .Where(e => e.DeclaredSourceOffsetMs != null)
                                           .OrderBy(e => e.Sequence)
                                           .Select(e => e.DeclaredSourceOffsetMs!.Value)
                                           .LastOrDefault();

            DivaPlaybackTimeline timeline = Create(chart);

            // Storyboard video starts at StartTime with file position 0; choose StartTime so that
            // at the resource event's playback instant the file position equals videoSourceOffset.
            double startTime = timeline.ToPlaybackTime(primary.TimeMs) - videoSourceOffset;

            return new DivaVideoPlayback(relative, startTime, videoSourceOffset, primary.TimeMs);
        }

        private static DivaPlaybackTimeline? createFromBgm(DivaChart chart)
        {
            List<DivaBgmEvent> effective = chart.BgmEvents
                                                     .GroupBy(e => (e.FrameIndex, e.Slot))
                                                     .Select(g => g.OrderBy(e => e.Sequence).Last())
                                                     .Where(e => chart.WavFiles.TryGetValue(e.WavId, out string? path)
                                                                 && !string.IsNullOrWhiteSpace(path))
                                                     .OrderBy(e => e.TimeMs)
                                                     .ThenBy(e => e.Sequence)
                                                     .ToList();

            if (effective.Count == 0)
                return null;

            // GameCore::Run iterates slots ascending while reusing channel 0, so the highest
            // slot at the first event time is the audible segment after that frame is processed.
            double firstTime = effective[0].TimeMs;
            DivaBgmEvent primary = effective.Where(e => Math.Abs(e.TimeMs - firstTime) < 0.0001)
                                                 .OrderBy(e => e.Slot)
                                                 .ThenBy(e => e.Sequence)
                                                 .Last();

            // ProjectDIVA's playOffset is a map keyed by WAV id and parsing later declarations
            // overwrites earlier values before gameplay begins.
            double sourceOffset = chart.BgmEvents
                                       .Where(e => e.WavId == primary.WavId && e.DeclaredSourceOffsetMs != null)
                                       .OrderBy(e => e.Sequence)
                                       .Select(e => e.DeclaredSourceOffsetMs!.Value)
                                       .LastOrDefault();

            string path = chart.WavFiles[primary.WavId];

            bool additionalSegments = effective.Any(e =>
                e.TimeMs > primary.TimeMs + 0.0001);

            return new DivaPlaybackTimeline
            {
                AudioRelativePath = chart.ResolveRelativePath(path),
                EventFrameIndex = primary.FrameIndex,
                EventTimeMs = primary.TimeMs,
                SourceOffsetMs = sourceOffset,
                BgmWavId = primary.WavId,
                HasAdditionalAudioSegments = additionalSegments
            };
        }

        private static DivaPlaybackTimeline? createFromResource(DivaChart chart)
        {
            List<DivaResourceEvent> effective = getEffectiveVideoEvents(chart);

            if (effective.Count == 0)
                return null;

            DivaResourceEvent primary = effective[0];

            // Unlike BGS playOffset, VideoEngine::m_pTime is one global value.
            double sourceOffset = chart.ResourceEvents
                                       .Where(e => e.DeclaredSourceOffsetMs != null)
                                       .OrderBy(e => e.Sequence)
                                       .Select(e => e.DeclaredSourceOffsetMs!.Value)
                                       .LastOrDefault();

            return new DivaPlaybackTimeline
            {
                AudioRelativePath = chart.ResolveRelativePath(chart.ResourceFiles[primary.ResourceId]),
                EventFrameIndex = primary.FrameIndex,
                EventTimeMs = primary.TimeMs,
                SourceOffsetMs = sourceOffset,
                ResourceId = primary.ResourceId,
                HasAdditionalAudioSegments = effective.Count > 1
            };
        }

        private static List<DivaResourceEvent> getEffectiveVideoEvents(DivaChart chart)
            => chart.ResourceEvents
                    .GroupBy(e => e.FrameIndex)
                    .Select(g => g.OrderBy(e => e.Sequence).Last())
                    .Where(e => chart.ResourceFiles.TryGetValue(e.ResourceId, out string? path)
                                && DivaVideoAudioExtractor.IsVideoExtension(path))
                    .OrderBy(e => e.TimeMs)
                    .ThenBy(e => e.Sequence)
                    .ToList();

        private static string? resolveFallbackAudio(DivaChart chart)
        {
            foreach (string file in chart.WavFiles.Values)
            {
                if (DivaVideoAudioExtractor.IsAudioExtension(file))
                    return chart.ResolveRelativePath(file);
            }

            foreach (string file in chart.WavFiles.Values)
            {
                if (!string.IsNullOrWhiteSpace(file))
                    return chart.ResolveRelativePath(file);
            }

            return null;
        }
    }

    public readonly record struct DivaVideoPlayback(
        string RelativePath,
        double StoryboardStartTimeMs,
        double SourceOffsetMs,
        double EventTimeMs);
}
