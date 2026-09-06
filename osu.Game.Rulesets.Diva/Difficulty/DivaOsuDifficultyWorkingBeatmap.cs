// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Mods;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.Diva.Difficulty
{
    /// <summary>
    /// Feeds <see cref="osu.Game.Rulesets.Osu.Difficulty.OsuDifficultyCalculator"/> a beatmap of osu hit circles
    /// derived from DIVA objects (holds flattened like <c>DivaToOsuExporter</c>).
    /// </summary>
    public class DivaOsuDifficultyWorkingBeatmap : WorkingBeatmap
    {
        private readonly IWorkingBeatmap source;
        private readonly OsuBeatmap osuBeatmap;

        public DivaOsuDifficultyWorkingBeatmap(IWorkingBeatmap source)
            : base((BeatmapInfo)source.BeatmapInfo, null)
        {
            this.source = source;
            osuBeatmap = ConvertToOsuBeatmap(source.Beatmap);
        }

        public static OsuBeatmap ConvertToOsuBeatmap(IBeatmap? sourceBeatmap)
        {
            var result = new OsuBeatmap();

            if (sourceBeatmap == null)
                return result;

            result.BeatmapInfo = sourceBeatmap.BeatmapInfo;
            result.ControlPointInfo = sourceBeatmap.ControlPointInfo;
            result.BeatmapInfo.Difficulty = sourceBeatmap.Difficulty.Clone();

            bool first = true;

            foreach (HitObject hitObject in sourceBeatmap.HitObjects.OrderBy(h => h.StartTime))
            {
                switch (hitObject)
                {
                    case OsuHitObject osu:
                        result.HitObjects.Add(osu);
                        first = false;
                        break;

                    case DivaHitObject diva:
                        result.HitObjects.Add(new HitCircle
                        {
                            StartTime = diva.StartTime,
                            Position = diva.Position,
                            NewCombo = first || (diva as IHasComboInformation)?.NewCombo == true,
                        });
                        first = false;
                        break;
                }
            }

            return result;
        }

        protected override IBeatmap GetBeatmap() => osuBeatmap;

        public override IBeatmap GetPlayableBeatmap(IRulesetInfo ruleset, IReadOnlyList<Mod> mods, CancellationToken cancellationToken = default)
        {
            // DIVA conversion mods cast to DivaBeatmapConverter — strip them before osu preprocessing.
            Mod[] filtered = mods.Where(m => m is not DivaKeyMod and not DivaModNoDoubles).ToArray();
            return base.GetPlayableBeatmap(ruleset, filtered, cancellationToken);
        }

        public override Texture? GetBackground() => source.GetBackground();

        protected override Track GetBeatmapTrack() => source.Track;

        protected override ISkin GetSkin() => source.Skin;

        public override Stream? GetStream(string storagePath) => source.GetStream(storagePath);
    }
}
