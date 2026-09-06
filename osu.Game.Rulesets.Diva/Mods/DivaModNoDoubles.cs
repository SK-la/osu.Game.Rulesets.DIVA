// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Diva.Mods
{
    public class DivaModNoDoubles : Mod, IApplicableToBeatmapConverter
    {
        public override string Name => "No Doubles";
        public override string Acronym => "ND";
        public override LocalisableString Description => DivaStrings.MOD_NO_DOUBLES_DESCRIPTION;
        public override ModType Type => ModType.Conversion;
        public override bool UserPlayable => true;

        public void ApplyToBeatmapConverter(IBeatmapConverter beatmapConverter)
        {
            var bc = (DivaBeatmapConverter)beatmapConverter;

            bc.AllowDoubles = false;
        }
    }
}
