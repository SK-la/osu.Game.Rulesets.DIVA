// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Input.Events;
using osu.Game.Extensions;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Diva.Edit
{
    public partial class DivaSelectionHandler : EditorSelectionHandler
    {
        [Resolved]
        private DivaHitObjectComposer composer { get; set; } = null!;

        private bool nudgeMovementActive;

        public override bool HandleMovement(MoveSelectionEvent<HitObject> moveEvent)
        {
            var reference = SelectedItems.OfType<DivaHitObject>().FirstOrDefault();
            if (reference == null)
                return false;

            Vector2 localDelta = this.ScreenSpaceDeltaToParentSpace(moveEvent.ScreenSpaceDelta);
            Vector2 target = composer.SnapPlayfieldPosition(reference.Position + localDelta);
            Vector2 snappedDelta = target - reference.Position;

            if (snappedDelta == Vector2.Zero)
                return true;

            EditorBeatmap.PerformOnSelection(h =>
            {
                if (h is DivaHitObject diva)
                    diva.Position += snappedDelta;
            });

            return true;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.ShiftPressed)
                return false;

            if (e.ControlPressed)
            {
                switch (e.Key)
                {
                    case Key.Left:
                        return nudgeSelection(new Vector2(-DivaChartConstants.DELTA_X, 0));

                    case Key.Right:
                        return nudgeSelection(new Vector2(DivaChartConstants.DELTA_X, 0));

                    case Key.Up:
                        return nudgeSelection(new Vector2(0, -DivaChartConstants.DELTA_Y));

                    case Key.Down:
                        return nudgeSelection(new Vector2(0, DivaChartConstants.DELTA_Y));
                }
            }

            return false;
        }

        protected override void OnKeyUp(KeyUpEvent e)
        {
            base.OnKeyUp(e);

            if (nudgeMovementActive && !e.ControlPressed)
            {
                EditorBeatmap.EndChange();
                nudgeMovementActive = false;
            }
        }

        private bool nudgeSelection(Vector2 delta)
        {
            if (!SelectedItems.OfType<DivaHitObject>().Any())
                return false;

            if (!nudgeMovementActive)
            {
                nudgeMovementActive = true;
                EditorBeatmap.BeginChange();
            }

            EditorBeatmap.PerformOnSelection(h =>
            {
                if (h is DivaHitObject diva)
                    diva.Position = composer.SnapPlayfieldPosition(diva.Position + delta);
            });

            return true;
        }
    }
}
