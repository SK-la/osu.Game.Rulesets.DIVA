// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    /// <summary>
    ///     The button remaps behind the reference editor's "tools" menu, which rewrites every note of a chart
    ///     so its button belongs to a smaller set. Mappings are stated in ProjectDIVA's unit numbering
    ///     (<see cref="DivaActionEncoding.ToUnitIndex" />), which is what those routines switch on:
    ///     <c>0</c> ◯, <c>1</c> □, <c>2</c> ✕, <c>3</c> △, <c>4</c> →, <c>5</c> ←, <c>6</c> ↓, <c>7</c> ↑.
    /// </summary>
    public static class DivaActionSimplification
    {
        /// <summary>Keeps ✕, ◯, ↓, → — the odd units step down into their even neighbour.</summary>
        public static DivaAction ToCrossCircleDownRight(DivaAction action)
        {
            int unit = DivaActionEncoding.ToUnitIndex(action);
            return DivaActionEncoding.FromUnitType(unit % 2 == 1 ? unit - 1 : unit);
        }

        /// <summary>Keeps ◯ and → — the symbol half collapses onto ◯, the arrow half onto →.</summary>
        public static DivaAction ToCircleAndRight(DivaAction action)
            => DivaActionEncoding.ToUnitIndex(action) < 4 ? DivaAction.Circle : DivaAction.Right;

        /// <summary>Replaces every arrow with the symbol of its unit pair (→◯, ←□, ↓✕, ↑△).</summary>
        public static DivaAction ArrowToSymbol(DivaAction action)
        {
            int unit = DivaActionEncoding.ToUnitIndex(action);
            return DivaActionEncoding.FromUnitType(unit >= 4 ? unit - 4 : unit);
        }

        /// <summary>Replaces every symbol with the arrow of its unit pair (◯→, □←, ✕↓, △↑).</summary>
        public static DivaAction SymbolToArrow(DivaAction action)
        {
            int unit = DivaActionEncoding.ToUnitIndex(action);
            return DivaActionEncoding.FromUnitType(unit < 4 ? unit + 4 : unit);
        }

        /// <summary>
        ///     What a remap would do to a chart: the notes whose button changes, and the notes that have to go
        ///     because the remap puts two notes of one button on the same frame.
        /// </summary>
        /// <remarks>
        ///     A frame stores one slot per button, so a chart cannot play two notes of a button at one frame —
        ///     the reference editor drops the duplicates its own remaps create. Duplicates between notes the
        ///     remap did not touch (which the editor can place on purpose) are left alone.
        /// </remarks>
        public static DivaActionSimplifyPlan PlanActionSimplify(IBeatmap beatmap, Func<DivaAction, DivaAction> remap)
        {
            var notes = beatmap.HitObjects.OfType<DivaHitObject>().ToArray();
            var changes = new Dictionary<DivaHitObject, DivaAction>();
            var removals = new List<DivaHitObject>();

            foreach (DivaHitObject note in notes)
            {
                DivaAction mapped = remap(note.ValidAction);

                if (mapped != note.ValidAction)
                    changes[note] = mapped;
            }

            foreach (var frame in notes.GroupBy(n => (Frame: DivaChartBuilder.TimeToFrame(beatmap, n.StartTime), IsHold: n is DivaHoldHitObject)))
            {
                foreach (var button in frame.GroupBy(n => changes.TryGetValue(n, out DivaAction mapped) ? mapped : n.ValidAction))
                {
                    // Keep the note that already had the surviving button: it keeps its own position, flight
                    // and key sound, while the notes the remap rewrote cannot keep theirs anyway.
                    var group = button.OrderBy(n => changes.ContainsKey(n) ? 1 : 0).ToArray();

                    if (group.Length < 2 || group.All(n => !changes.ContainsKey(n)))
                        continue;

                    removals.AddRange(group.Skip(1));
                }
            }

            // A note that is being dropped must not also carry a button change.
            foreach (DivaHitObject removed in removals)
                changes.Remove(removed);

            return new DivaActionSimplifyPlan(changes, removals);
        }
    }

    /// <summary>Result of <see cref="DivaActionSimplification.PlanActionSimplify" />; the caller applies it.</summary>
    public readonly struct DivaActionSimplifyPlan
    {
        public DivaActionSimplifyPlan(IReadOnlyDictionary<DivaHitObject, DivaAction> changes, IReadOnlyList<DivaHitObject> removals)
        {
            Changes = changes;
            Removals = removals;
        }

        /// <summary>Notes whose button the remap changed, and the button they become.</summary>
        public IReadOnlyDictionary<DivaHitObject, DivaAction> Changes { get; }

        /// <summary>Notes to remove because they now share a frame and a button with an earlier note.</summary>
        public IReadOnlyList<DivaHitObject> Removals { get; }
    }
}
