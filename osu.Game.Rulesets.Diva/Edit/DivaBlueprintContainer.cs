// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Diva.Edit.Blueprints;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit
{
    public partial class DivaBlueprintContainer : ComposeBlueprintContainer
    {
        public new DivaHitObjectComposer Composer => (DivaHitObjectComposer)base.Composer;

        public DivaBlueprintContainer(DivaHitObjectComposer composer)
            : base(composer)
        {
        }

        protected override SelectionHandler<HitObject> CreateSelectionHandler() => new DivaSelectionHandler();

        public override HitObjectSelectionBlueprint? CreateHitObjectBlueprintFor(HitObject hitObject)
        {
            switch (hitObject)
            {
                case DivaHoldHitObject hold:
                    return new DivaHoldSelectionBlueprint(hold);

                case DivaHitObject tap:
                    return new DivaHitSelectionBlueprint(tap);
            }

            return base.CreateHitObjectBlueprintFor(hitObject);
        }

        protected override bool TryMoveBlueprints(DragEvent e, IList<(SelectionBlueprint<HitObject> blueprint, Vector2[] originalSnapPositions)> blueprints)
        {
            Vector2 distanceTravelled = e.ScreenSpaceMousePosition - e.ScreenSpaceMouseDownPosition;
            Vector2 movePosition = blueprints.First().originalSnapPositions.First() + distanceTravelled;
            var referenceBlueprint = blueprints.First().blueprint;

            var result = Composer.FindSnappedPositionAndTime(movePosition);

            bool moved = SelectionHandler.HandleMovement(new MoveSelectionEvent<HitObject>(referenceBlueprint, result.ScreenSpacePosition - referenceBlueprint.ScreenSpaceSelectionPoint));
            if (moved)
                ApplySnapResultTime(result, referenceBlueprint.Item.StartTime);

            return moved;
        }

        /// <remarks>
        ///     The editor's own scroll handling gives up when Alt is held, and this container already receives
        ///     positional input across the whole play area, so the zoom shortcut can live here.
        /// </remarks>
        protected override bool OnScroll(ScrollEvent e)
        {
            if (!e.AltPressed || e.ScrollDelta.Y == 0)
                return false;

            Composer.AdjustPlayfieldZoom(Math.Sign(e.ScrollDelta.Y));
            return true;
        }
    }
}
