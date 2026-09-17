// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Rulesets.Diva.Localization;

namespace osu.Game.Rulesets.Diva.Objects.Drawables.Pieces
{
    /// <summary>
    ///     How a note and its flying piece appear before the hit time.
    /// </summary>
    public enum DivaNoteAppearance
    {
        /// <summary>
        ///     ProjectDIVA native: the flying piece is culled outside the field and pops in at full size,
        ///     while the fixed target pops out from <c>NOTE_BLOWUP</c> and spins linearly.
        /// </summary>
        [LocalisableDescription(typeof(DivaStrings), nameof(DivaStrings.APPEARANCE_DIVA_NATIVE))]
        DivaNative,

        /// <summary>Whole note fades in while the pointer scales and spins (the ruleset's previous look).</summary>
        [LocalisableDescription(typeof(DivaStrings), nameof(DivaStrings.APPEARANCE_FADE_IN))]
        FadeIn,
    }
}
