// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Rulesets.Diva.Edit
{
    public partial class DivaHitObjectInspector : HitObjectInspector
    {
        protected override void AddInspectorValues(HitObject[] objects)
        {
            base.AddInspectorValues(objects);

            if (objects.Length != 1 || objects[0] is not DivaHitObject diva)
                return;

            AddHeader(DivaStrings.EDITOR_INSPECTOR_ACTION.ToString());
            AddValue(diva.ValidAction.GetDescription());

            var grid = DivaActionEncoding.ToGridPosition(diva.Position);
            AddHeader(DivaStrings.EDITOR_INSPECTOR_GRID.ToString());
            AddValue($"({grid.X:0.##}, {grid.Y:0.##})");

            AddHeader(DivaStrings.EDITOR_INSPECTOR_APPROACH.ToString());
            AddValue($"({diva.ApproachPieceOriginPosition.X:0.##}, {diva.ApproachPieceOriginPosition.Y:0.##})");
        }
    }
}
