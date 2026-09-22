// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Diva.Objects.Drawables;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.Diva.UI
{
    /// <summary>
    /// Fans a single action press/release to every matching alive note.
    /// </summary>
    /// <remarks>
    /// Framework key bindings stop at the first handler that returns true, so per-note
    /// <see cref="IKeyBindingHandler{T}"/> would consume a chord. Replay frames can also
    /// carry multiple copies of the same action; each copy is a separate press here.
    /// </remarks>
    public partial class DivaPlayfieldInputHub : Component, IKeyBindingHandler<DivaAction>
    {
        private readonly DivaPlayfield playfield;
        private readonly Dictionary<DivaAction, int> pressCounts = new Dictionary<DivaAction, int>();

        public DivaPlayfieldInputHub(DivaPlayfield playfield)
        {
            this.playfield = playfield;
        }

        public bool OnPressed(KeyBindingPressEvent<DivaAction> e)
        {
            if ((Clock as IGameplayClock)?.IsRewinding == true)
                return false;

            addPress(e.Action);

            bool handledValid = false;

            foreach (DrawableDivaHitObject note in aliveNotes())
            {
                if (!note.MatchesPressedAction(e.Action))
                    continue;

                handledValid |= note.TryHandlePress(e.Action);
            }

            if (!handledValid)
            {
                foreach (DrawableDivaHitObject note in aliveNotes())
                {
                    if (note.MatchesPressedAction(e.Action))
                        continue;

                    if (note.TryHandlePress(e.Action))
                        break;
                }
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<DivaAction> e)
        {
            if ((Clock as IGameplayClock)?.IsRewinding == true)
                return;

            int remaining = removePress(e.Action);
            var holding = aliveNotes()
                          .OfType<DrawableDivaHoldHitObject>()
                          .Where(h => h.IsHoldingAction(e.Action))
                          .OrderBy(h => Math.Abs(Time.Current - h.HitObject.GetEndTime()))
                          .ToList();

            int excess = holding.Count - remaining;

            for (int i = 0; i < excess; i++)
                holding[i].TryHandleRelease(e.Action);
        }

        private IEnumerable<DrawableDivaHitObject> aliveNotes()
            => playfield.HitObjectContainer.AliveObjects.OfType<DrawableDivaHitObject>();

        private void addPress(DivaAction action)
        {
            pressCounts.TryGetValue(action, out int count);
            pressCounts[action] = count + 1;
        }

        private int removePress(DivaAction action)
        {
            if (!pressCounts.TryGetValue(action, out int count) || count <= 1)
            {
                pressCounts.Remove(action);
                return 0;
            }

            return pressCounts[action] = count - 1;
        }
    }
}
