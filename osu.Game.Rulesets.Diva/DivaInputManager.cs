// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Input.Bindings;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Diva
{
    public partial class DivaInputManager : RulesetInputManager<DivaAction>
    {
        public DivaInputManager(RulesetInfo ruleset)
            : base(ruleset, 0, SimultaneousBindingMode.All)
        {
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

        [Description("×")]
        Cross,

        [Description("←")]
        Left,

        [Description("↑")]
        Up,

        [Description("→")]
        Right,

        [Description("↓")]
        Down,
    }
}
