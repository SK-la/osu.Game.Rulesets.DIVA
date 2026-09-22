// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Diva.Objects.Drawables;
using osu.Game.Rulesets.Diva.Scoring;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.Diva.UI
{
    /// <summary>
    /// Routes each press/release to the single note it judges.
    /// </summary>
    /// <remarks>
    /// Framework key bindings stop at the first handler that returns true, so per-note
    /// <see cref="IKeyBindingHandler{T}"/> would consume a chord. A frame may also carry several copies of the
    /// same action for different notes of a chord; each copy picks its own target here.
    /// </remarks>
    public partial class DivaPlayfieldInputHub : Component, IKeyBindingHandler<DivaAction>
    {
        private readonly DivaPlayfield playfield;

        public DivaPlayfieldInputHub(DivaPlayfield playfield)
        {
            this.playfield = playfield;
        }

        public bool OnPressed(KeyBindingPressEvent<DivaAction> e)
        {
            if ((Clock as IGameplayClock)?.IsRewinding == true)
                return false;

            findPressTarget(e.Action)?.TryHandlePress(e.Action);
            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<DivaAction> e)
        {
            if ((Clock as IGameplayClock)?.IsRewinding == true)
                return;

            // ProjectDIVA hands a release to the first held strip of that button, and only one.
            foreach (DrawableDivaHitObject note in aliveNotes())
            {
                if (note is DrawableDivaHoldHitObject hold && hold.IsHoldingAction(e.Action))
                {
                    hold.TryHandleRelease(e.Action);
                    return;
                }
            }
        }

        /// <summary>
        ///     Picks the single note a press judges, via <see cref="DivaPressTargetSelector"/>.
        /// </summary>
        private DrawableDivaHitObject? findPressTarget(DivaAction action)
        {
            var notes = new List<DrawableDivaHitObject>();
            var candidates = new List<DivaPressTargetSelector.Candidate>();

            foreach (DrawableDivaHitObject note in aliveNotes())
            {
                if (!note.AcceptsPressNow)
                    continue;

                notes.Add(note);
                candidates.Add(new DivaPressTargetSelector.Candidate(
                    note.PressTimeOffsetAt(Time.Current),
                    note.MatchesPressedAction(action),
                    note is DrawableDivaHoldHitObject,
                    note.JudgementLocked));
            }

            int index = DivaPressTargetSelector.Select(candidates);

            return index < 0 ? null : notes[index];
        }

        private IEnumerable<DrawableDivaHitObject> aliveNotes()
            => playfield.HitObjectContainer.AliveObjects.OfType<DrawableDivaHitObject>();
    }
}
