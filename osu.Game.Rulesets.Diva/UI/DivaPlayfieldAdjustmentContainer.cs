// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.Rulesets.Osu.UI;

namespace osu.Game.Rulesets.Diva.UI
{
    public partial class DivaPlayfieldAdjustmentContainer : OsuPlayfieldAdjustmentContainer
    {
        public DivaPlayfieldAdjustmentContainer()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            // 使用完整的游戏区域，不进行缩放
            // Size = new Vector2(0.8f);
        }
    }
}
