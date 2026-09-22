// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Diva.Replays
{
    public partial class DivaFramedReplayInputHandler : FramedReplayInputHandler<DivaReplayFrame>
    {
        public DivaFramedReplayInputHandler(Replay replay)
            : base(replay)
        {
        }

        protected override bool IsImportant(DivaReplayFrame frame) => frame.Actions.Count > 0;

        protected override void CollectReplayInputs(List<IInput> inputs)
        {
            inputs.Add(new DivaReplayInput(CurrentFrame?.Actions ?? new List<DivaAction>()));
        }

        /// <summary>
        ///     The DIVA counterpart of <see cref="ReplayInputHandler.ReplayState{T}"/>.
        /// </summary>
        /// <remarks>
        ///     Upstream diffs the previous and current frame with <c>Except</c>, which collapses every repeat of
        ///     a button into one press. ProjectDIVA charts stack many notes of one button on a frame, and each
        ///     note needs its own press, so the diff counts occurrences instead.
        /// </remarks>
        private class DivaReplayInput : IInput
        {
            private readonly IReadOnlyList<DivaAction> actions;

            public DivaReplayInput(IReadOnlyList<DivaAction> actions)
            {
                this.actions = actions;
            }

            public void Apply(InputState state, IInputStateChangeHandler handler)
            {
                if (!(state is RulesetInputManagerInputState<DivaAction> inputState))
                    throw new InvalidOperationException($"{nameof(DivaReplayInput)} should only be applied to a {nameof(RulesetInputManagerInputState<DivaAction>)}");

                IReadOnlyList<DivaAction> last = inputState.LastReplayState?.PressedActions ?? (IReadOnlyList<DivaAction>)Array.Empty<DivaAction>();

                var released = new List<DivaAction>();
                var pressed = new List<DivaAction>();
                DivaReplayActionDiff.Compute(last, actions, released, pressed);

                // Kept as the baseline for the next frame, and read by ReplayStateReset to release everything
                // the replay still holds when the handler is swapped out.
                inputState.LastReplayState = new ReplayState<DivaAction> { PressedActions = actions.ToList() };

                handler.HandleInputStateChange(new ReplayStateChangeEvent<DivaAction>(state, this, released.ToArray(), pressed.ToArray()));
            }
        }
    }
}
