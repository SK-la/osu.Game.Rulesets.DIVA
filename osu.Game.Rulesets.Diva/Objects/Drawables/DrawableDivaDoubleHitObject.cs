// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    /// <summary>
    /// PSP手柄的双键映射，一个note可以使用两个同方向按键之中的任意一个来打击
    /// </summary>
    public partial class DrawableDivaDoubleHitObject : DrawableDivaHitObject
    {
        private readonly DivaAction doubleAction;

        public DrawableDivaDoubleHitObject(DivaHitObject hitObject)
            : base(hitObject)
        {
            doubleAction = ((DoublePressButton)hitObject).DoubleAction;
        }

        protected override string GetTextureLocation() => "Doubles/" + base.GetTextureLocation();

        protected override bool ComputeValidPress(DivaAction action)
        {
            var directionToSymbol = MapDirectionToSymbol(action);
            var symbolToDirection = MapSymbolToDirection(action);

            return action == ValidAction ||
                   action == doubleAction ||
                   directionToSymbol == ValidAction ||
                   directionToSymbol == doubleAction ||
                   symbolToDirection == ValidAction ||
                   symbolToDirection == doubleAction;
        }
    }
}
