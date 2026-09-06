// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Rulesets.Diva.Localization;

namespace osu.Game.Rulesets.Diva.Mods
{
    public partial class DivaModKey1 : DivaKeyMod
    {
        public override int KeyCount => 1;
        public override string Name => "One Button";
        public override string Acronym => "1B";
        public override LocalisableString Description => DivaStrings.MOD_KEY1_DESCRIPTION;
    }
}
