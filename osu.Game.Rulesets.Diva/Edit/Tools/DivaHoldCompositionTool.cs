// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Rulesets.Diva.Edit.Blueprints;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;

namespace osu.Game.Rulesets.Diva.Edit.Tools
{
    public class DivaHoldCompositionTool : CompositionTool<DivaAction>
    {
        public DivaHoldCompositionTool()
            : base(DivaStrings.EDITOR_HOLD_TOOL)
        {
            Action = DivaAction.EditorHoldTool;
        }

        public override Drawable CreateIcon() => new SpriteIcon { Icon = OsuIcon.EditorHoldNote };

        public override HitObjectPlacementBlueprint CreatePlacementBlueprint() => new DivaHoldPlacementBlueprint();
    }
}
