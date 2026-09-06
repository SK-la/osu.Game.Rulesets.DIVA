// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Text;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Storyboards;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    /// <summary>
    /// Builds a lazer storyboard from ProjectDIVA <c>.diva</c> charts (primarily RES video → Video layer).
    /// </summary>
    public class DivaStoryboardDecoder : Decoder<Storyboard>
    {
        public static void Register()
        {
            // Same EditorVer magic as <see cref="DivaBeatmapDecoder"/> ("1.0.x.x").
            AddDecoder<Storyboard>("1.", _ => new DivaStoryboardDecoder());
        }

        protected override void ParseStreamInto(LineBufferedReader stream, bool isPrimaryStream, Storyboard storyboard)
        {
            if (!isPrimaryStream)
                return;

            var sb = new StringBuilder();

            while (stream.ReadLine() is { } line)
                sb.AppendLine(line);

            using var reader = new StringReader(sb.ToString());

            // Song folder is unknown at decode time; ApplyVideo falls back to RES/ for bare names.
            // External sync registers the on-disk relative path so storyboard lookup can resolve via basename.
            DivaChart chart = DivaChartFileParser.Parse(reader, string.Empty, string.Empty);
            ApplyVideo(storyboard, chart);
        }

        /// <summary>
        /// Adds the primary RES video as a storyboard video when present and lazer-supported.
        /// </summary>
        public static void ApplyVideo(Storyboard storyboard, DivaChart chart)
        {
            DivaVideoPlayback? video = DivaPlaybackTimeline.ResolvePrimaryVideo(chart);

            if (video == null)
                return;

            string path = video.Value.RelativePath.Replace('\\', '/');

            if (!path.Contains('/'))
                path = $"RES/{path}";

            string extension = Path.GetExtension(path).ToLowerInvariant();

            if (!SupportedExtensions.VIDEO_EXTENSIONS.Contains(extension))
                return;

            storyboard.GetLayer(@"Video").Add(new StoryboardVideo(
                StoryboardElementSource.Beatmap,
                path,
                video.Value.StoryboardStartTimeMs));
        }
    }
}
