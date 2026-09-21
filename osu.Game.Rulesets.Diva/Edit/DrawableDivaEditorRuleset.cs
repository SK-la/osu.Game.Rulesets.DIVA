// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Diva.Edit
{
    public partial class DrawableDivaEditorRuleset : DrawableDivaRuleset
    {
        public DrawableDivaEditorRuleset(DivaRuleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod>? mods = null)
            : base(ruleset, beatmap, mods)
        {
        }

        protected override Playfield CreatePlayfield() => new DivaEditorPlayfield(ResolveLogicalPlayfieldSize());

        private partial class DivaEditorPlayfield : DivaPlayfield
        {
            public DivaEditorPlayfield(osuTK.Vector2 logicalSize)
                : base(logicalSize)
            {
            }

            protected override GameplayCursorContainer? CreateCursor() => null;
        }
    }
}
