// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input.Events;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    /// <summary>
    /// PSP手柄的双键映射，一个note可以使用两个同方向按键之中的任意一个来打击
    /// </summary>
    public partial class DrawableDivaDoubleHitObject : DrawableDivaHitObject
    {
        private const int max_count = 2;
        private List<DivaAction> inputs = new List<DivaAction>();
        private readonly DivaAction doubleAction;

        public DrawableDivaDoubleHitObject(DivaHitObject hitObject)
            : base(hitObject)
        {
            doubleAction = ((DoublePressButton)hitObject).DoubleAction;
        }

        protected override string GetTextureLocation() => "Doubles/" + base.GetTextureLocation();

        public override bool OnPressed(KeyBindingPressEvent<DivaAction> e)
        {
            Samples.Play();

            if (Judged)
                return false;

            // 方向键 -> 符号键的映射
            var directionToSymbol = e.Action switch
            {
                DivaAction.Right => DivaAction.Circle,
                DivaAction.Down => DivaAction.Cross,
                DivaAction.Up => DivaAction.Triangle,
                DivaAction.Left => DivaAction.Square,
                _ => e.Action,
            };

            // 符号键 -> 方向键的映射
            var symbolToDirection = e.Action switch
            {
                DivaAction.Circle => DivaAction.Right,
                DivaAction.Cross => DivaAction.Down,
                DivaAction.Triangle => DivaAction.Up,
                DivaAction.Square => DivaAction.Left,
                _ => e.Action,
            };

            // 检查是否匹配ValidAction或DoubleAction：直接匹配、方向转符号、符号转方向
            // DoublePressButton只需要按其中一个键就能判定
            ValidPress = e.Action == ValidAction ||
                         e.Action == doubleAction ||
                         directionToSymbol == ValidAction ||
                         directionToSymbol == doubleAction ||
                         symbolToDirection == ValidAction ||
                         symbolToDirection == doubleAction;

            Pressed = true;

            return true;
        }
    }
}
