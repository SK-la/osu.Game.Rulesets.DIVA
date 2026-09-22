// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.StateChanges.Events;
using osu.Game.Input.Handlers;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.Diva
{
    public partial class DivaInputManager : RulesetInputManager<DivaAction>
    {
        public DivaInputManager(RulesetInfo ruleset)
            : base(ruleset, 0, SimultaneousBindingMode.All)
        {
        }

        /// <summary>
        ///     Replay presses and releases are delivered in frame order, which is inverted while the gameplay clock runs
        ///     backwards; hit objects refuse to judge in that state, so dispatching them would only leave state behind
        ///     that the next forward pass consumes. The replay diff is tracked on the input state rather than here, so
        ///     the actions are handed out again once the clock moves forward.
        /// </summary>
        public override void HandleInputStateChange(InputStateChangeEvent inputStateChange)
        {
            if (inputStateChange is ReplayInputHandler.ReplayStateChangeEvent<DivaAction> && (Clock as IGameplayClock)?.IsRewinding == true)
                return;

            base.HandleInputStateChange(inputStateChange);
        }
    }

    public enum DivaAction
    {
        [Description("□")]
        Square,

        [Description("△")]
        Triangle,

        [Description("◯")]
        Circle,

        [Description("X")]
        Cross,

        [Description("←")]
        Left,

        [Description("↑")]
        Up,

        [Description("→")]
        Right,

        [Description("↓")]
        Down,

        [Description("Tap")]
        EditorTapTool = 10000,

        [Description("Hold")]
        EditorHoldTool,

        [Description("Tap ⇄ Hold")]
        EditorToggleHoldTool,

        [Description("Grid Snap")]
        EditorToggleGridSnap,

        [Description("Replace on same time")]
        EditorToggleReplaceOnSameTime,

        [Description("Note ↑")]
        EditorButtonUp,

        [Description("Note ←")]
        EditorButtonLeft,

        [Description("Note ↓")]
        EditorButtonDown,

        [Description("Note →")]
        EditorButtonRight,

        [Description("Arrow / Symbol")]
        EditorToggleButtonFamily,

        [Description("Delete at time point")]
        EditorDeleteAtCurrentTime,
    }
}
