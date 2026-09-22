// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Difficulty;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    /// <summary>
    ///     The <c>.diva</c> header fields lazer's beatmap models have no home for: ProjectDIVA's difficulty
    ///     slot and black stars, the music style, the preview picture and the chart's minimum length.
    /// </summary>
    /// <remarks>
    ///     Held on the playable beatmap so <see cref="DivaChartBuilder"/> can prefer these over the source
    ///     chart on disk, exactly like <see cref="DivaBeatmap.ChartEvents"/>. Seeded by
    ///     <see cref="FromBeatmapInfo"/>, which reads back what the decoder wrote into the beatmap info.
    /// </remarks>
    public sealed class DivaChartHeader
    {
        /// <summary>ProjectDIVA difficulty slot (header line 7), 1–5; indexes the game's <c>HardType</c> array.</summary>
        public int Level { get; set; } = 1;

        /// <summary>ProjectDIVA <c>Hard</c> (header line 8), the black stars shown next to the level.</summary>
        public int Hard { get; set; } = 1;

        /// <summary>ProjectDIVA <c>_musicStyle</c> (header line 5), loaded as the game's <c>singer</c>.</summary>
        public string Style { get; set; } = string.Empty;

        /// <summary>Song-select preview picture resource (header line 6).</summary>
        public string OverviewPicture { get; set; } = string.Empty;

        /// <summary>
        ///     Lower bound for the exported <see cref="DivaChart.PeriodCount"/>. Trailing measures past the last
        ///     note hold nothing but BGS / Resource / ChanceTime data, and the reading side clamps everything to
        ///     the frame array, so the builder raises the count to whatever the contents need and to this
        ///     value, whichever is larger.
        /// </summary>
        public int MinPeriodCount { get; set; } = 1;

        /// <summary>
        ///     Writes the two fields lazer has a home for back where the decoder reads them, so that level and
        ///     stars survive a save / reload instead of only living in the in-session header.
        /// </summary>
        /// <remarks>
        ///     Overall difficulty is clamped to the 0–10 gameplay range while the header keeps the true star
        ///     value: ProjectDIVA charts may print more than ten stars, which the difficulty name carries.
        /// </remarks>
        public static void MirrorToBeatmapInfo(BeatmapInfo info, DivaChartHeader header)
        {
            info.DifficultyName = DivaChartConstants.FormatDifficultyName(header.Level, header.Hard);
            info.Difficulty.OverallDifficulty = Math.Clamp(header.Hard, 1, 10);
        }

        /// <summary>
        ///     Seeds a header from what the decoder left on the beatmap info: the difficulty name carries
        ///     level and stars, the tags carry the style.
        /// </summary>
        /// <remarks>
        ///     The preview picture is deliberately left unset: the decoder stores the <em>resolved</em> background
        ///     path, so the exact name the chart declared cannot be recovered from it. An empty value keeps the
        ///     export resolving the picture the way it always did (source chart first, background file second),
        ///     and only an explicit value written here overrides it.
        /// </remarks>
        public static DivaChartHeader FromBeatmapInfo(IBeatmapInfo info)
        {
            int hard = (int)Math.Round(DivaNativeStarRating.Resolve(info));

            return new DivaChartHeader
            {
                Level = DivaChartConstants.TryParseLevelName(info.DifficultyName, out int level) ? level : 1,
                Hard = hard > 0 ? hard : 1,
                Style = ExtractStyle(info.Metadata.Tags, string.Empty)
            };
        }

        /// <summary>
        ///     Recovers <c>_musicStyle</c> from the beatmap tags, which also hold the ruleset's own import
        ///     markers. Anything left over is treated as the style, matching what the decoder writes.
        /// </summary>
        public static string ExtractStyle(string? tags, string fallback)
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
