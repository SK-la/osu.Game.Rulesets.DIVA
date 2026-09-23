// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osuTK.Input;

namespace osu.Game.Rulesets.Diva.Edit.Blueprints
{
    /// <summary>
    ///     Shared placement behaviour of the editor's notes: a press that lands on an existing note is left to the
    ///     selection instead of being taken as a placement.
    /// </summary>
    /// <remarks>
    ///     The editor keeps a note button pressed to write notes, and the placement overlay covers the whole play
    ///     area, so it receives every left press before anything underneath — including the press that would select
    ///     an existing note and show its flight / length floats. Letting that press through is what makes "click the
    ///     note, drag its float" work without leaving the writing mode, the way the reference editor behaves.
    ///     ProjectDIVA charts cannot stack a note on the same button at the same frame, so nothing valid is lost:
    ///     a placement that overlaps an existing note is a mistake, not an overlap the user can ask for.
    /// </remarks>
    public abstract partial class DivaPlacementBlueprint : HitObjectPlacementBlueprint
    {
        [Resolved]
        protected DivaHitObjectComposer? Composer { get; private set; }

        protected DivaPlacementBlueprint(DivaHitObject hitObject)
            : base(hitObject)
        {
        }

        /// <summary>Whether the gesture names an existing note rather than an empty position.</summary>
        /// <remarks>
        ///     Not while a placement is mid-flight: the press that ends a hold may well land on a note, and it means
        ///     "finish this hold there", not "select what is under the cursor".
        /// </remarks>
        protected bool IsPressOnNote(MouseButtonEvent e)
            => e.Button == MouseButton.Left
               && PlacementActive != PlacementState.Active
               && Composer?.IsNoteAt(e.ScreenSpaceMouseDownPosition) == true;

        // Mouse-down rather than Handle(): both notes override OnMouseDown outright, which bypasses the base
        // implementation that Handle() lives in.
        protected override bool OnMouseDown(MouseDownEvent e)
            => !IsPressOnNote(e) && HandlePlacementMouseDown(e);

        /// <summary>Starts or finishes this blueprint's placement; only reached for a press on empty space.</summary>
        private protected abstract bool HandlePlacementMouseDown(MouseDownEvent e);
    }
}
