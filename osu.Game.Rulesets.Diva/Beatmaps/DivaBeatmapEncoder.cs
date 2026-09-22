// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using osu.Game.Beatmaps;
#if DIVA_EZ2LAZER
using osu.Game.Beatmaps.Formats;
#endif
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Skinning;
using osu.Game.Storyboards;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    /// <summary>
    /// Encodes a DIVA editor beatmap as Mode:0 .osu with button/approach data in sample filenames.
    /// Wired to File → Save / undo via <see cref="Ruleset.CreateBeatmapEncoder"/> on Ez2Lazer.
    /// </summary>
#if DIVA_EZ2LAZER
    public class DivaBeatmapEncoder : IBeatmapEncoder
#else
    public class DivaBeatmapEncoder
#endif
    {
        private readonly IBeatmap beatmap;
        private readonly Storyboard? storyboard;

        public DivaBeatmapEncoder(IBeatmap beatmap, ISkin? skin, Storyboard? storyboard)
        {
            this.beatmap = beatmap;
            this.storyboard = storyboard;
            _ = skin;
        }

        public void Encode(TextWriter writer)
        {
            writer.Write(ExportToString(beatmap, storyboard));
        }

        public static string ExportToString(IBeatmap beatmap, Storyboard? storyboard = null)
        {
            var sb = new System.Text.StringBuilder();
            var metadata = beatmap.Metadata;
            var difficulty = beatmap.Difficulty;
            string audio = Path.GetFileName(metadata.AudioFile);
            if (string.IsNullOrWhiteSpace(audio))
                audio = "audio.mp3";

            sb.AppendLine("osu file format v14");
            sb.AppendLine();
            sb.AppendLine("[General]");
            sb.AppendLine($"AudioFilename: {audio}");
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"AudioLeadIn: {beatmap.AudioLeadIn}"));
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"PreviewTime: {metadata.PreviewTime}"));
            sb.AppendLine("Countdown: 0");
            sb.AppendLine("SampleSet: Soft");
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"StackLeniency: {beatmap.StackLeniency}"));
            sb.AppendLine("Mode: 0");
            sb.AppendLine($"LetterboxInBreaks: {(beatmap.LetterboxInBreaks ? 1 : 0)}");
            sb.AppendLine($"WidescreenStoryboard: {(beatmap.WidescreenStoryboard ? 1 : 0)}");
            sb.AppendLine();
            sb.AppendLine("[Editor]");
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"DistanceSpacing: {beatmap.DistanceSpacing}"));
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"BeatDivisor: {beatmap.BeatmapInfo.BeatDivisor}"));
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"GridSize: {beatmap.GridSize}"));
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"TimelineZoom: {beatmap.TimelineZoom}"));
            sb.AppendLine();
            sb.AppendLine("[Metadata]");
            sb.AppendLine($"Title:{metadata.Title}");
            sb.AppendLine($"TitleUnicode:{metadata.TitleUnicode}");
            sb.AppendLine($"Artist:{metadata.Artist}");
            sb.AppendLine($"ArtistUnicode:{metadata.ArtistUnicode}");
            sb.AppendLine($"Creator:{metadata.Author.Username}");
            sb.AppendLine($"Version:{beatmap.BeatmapInfo.DifficultyName}");
            sb.AppendLine($"Source:{metadata.Source}");
            sb.AppendLine($"Tags:{ensureNativeTag(metadata.Tags)}");
            sb.AppendLine("BeatmapID:0");
            sb.AppendLine("BeatmapSetID:-1");
            sb.AppendLine();
            sb.AppendLine("[Difficulty]");
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"HPDrainRate:{difficulty.DrainRate}"));
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"CircleSize:{difficulty.CircleSize}"));
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"OverallDifficulty:{difficulty.OverallDifficulty}"));
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"ApproachRate:{difficulty.ApproachRate}"));
            sb.AppendLine("SliderMultiplier:1.4");
            sb.AppendLine("SliderTickRate:1");
            sb.AppendLine();
            sb.AppendLine("[Events]");
            sb.AppendLine("//Background and Video events");

            if (!string.IsNullOrWhiteSpace(metadata.BackgroundFile))
                sb.AppendLine($"0,0,\"{Path.GetFileName(metadata.BackgroundFile)}\",0,0");

            appendStoryboardVideo(sb, storyboard);

            sb.AppendLine();
            sb.AppendLine("[TimingPoints]");
            appendTimingPoints(sb, beatmap);

            sb.AppendLine();
            sb.AppendLine("[HitObjects]");

            foreach (var hitObject in beatmap.HitObjects.OfType<DivaHitObject>().OrderBy(h => h.StartTime))
            {
                bool isHold = hitObject is DivaHoldHitObject;
                double duration = isHold ? ((DivaHoldHitObject)hitObject).Duration : 0;
                string sample = DivaActionEncoding.EncodeSampleFileName(hitObject.ValidAction, isHold, duration, hitObject.ApproachPieceOriginPosition, hitObject.WavKey);
                int x = (int)Math.Round(hitObject.Position.X);
                int y = (int)Math.Round(hitObject.Position.Y);
                int time = (int)Math.Round(hitObject.StartTime);

                sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{x},{y},{time},1,0,0:0:0:0:{sample}"));
            }

            return sb.ToString();
        }

        private static string ensureNativeTag(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
                return DivaActionEncoding.NATIVE_TAG;

            if (tags.Contains(DivaActionEncoding.NATIVE_TAG, StringComparison.OrdinalIgnoreCase))
                return tags;

            return $"{DivaActionEncoding.NATIVE_TAG} {tags}".Trim();
        }

        private static void appendStoryboardVideo(System.Text.StringBuilder sb, Storyboard? storyboard)
        {
            if (storyboard == null)
                return;

            foreach (var layer in storyboard.Layers)
            {
                foreach (var element in layer.Elements)
                {
                    if (element is not StoryboardVideo video)
                        continue;

                    string file = Path.GetFileName(video.Path);
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (!SupportedExtensions.VIDEO_EXTENSIONS.Contains(ext))
                        continue;

                    sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Video,{video.StartTime:0.###},\"{file}\""));
                }
            }
        }

        private static void appendTimingPoints(System.Text.StringBuilder sb, IBeatmap beatmap)
        {
            var timingPoints = beatmap.ControlPointInfo.TimingPoints.OrderBy(p => p.Time).ToArray();

            if (timingPoints.Length == 0)
            {
                sb.AppendLine("0,500,4,2,0,100,1,0");
                return;
            }

            foreach (TimingControlPoint point in timingPoints)
            {
                sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"{point.Time:0.###},{point.BeatLength:0.###},4,2,0,100,1,0"));
            }

            foreach (EffectControlPoint effect in beatmap.ControlPointInfo.EffectPoints.OrderBy(p => p.Time))
            {
                sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"{effect.Time:0.###},-100,4,2,0,100,0,{(effect.KiaiMode ? 1 : 0)}"));
            }
        }
    }
}
