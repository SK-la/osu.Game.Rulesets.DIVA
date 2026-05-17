// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.Judgements;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    internal partial class SkinnableLighting : CompositeDrawable
    {
        private readonly DefaultHitExplosion explosion = new DefaultHitExplosion();

        private DrawableDivaJudgement targetObject;
        private JudgementResult targetResult;

        public SkinnableLighting()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            AddInternal(explosion);
        }

        /// <summary>
        /// Updates the lighting colour from a given hitObject and result.
        /// </summary>
        /// <param name="targetObject">The <see cref="DrawableDivaJudgement"/> that's been judged.</param>
        /// <param name="targetResult">The <see cref="JudgementResult"/> that <paramref name="targetObject"/> was judged with.</param>
        public void SetColourFrom(DrawableDivaJudgement targetObject, JudgementResult targetResult)
        {
            this.targetObject = targetObject;
            this.targetResult = targetResult;

            updateColour();
        }

        public void Animate(JudgementResult result) => explosion.Animate(result);

        public void ResetAnimation() => explosion.ResetAnimation();

        private void updateColour()
        {
            if (targetObject == null || targetResult == null)
                explosion.Colour = Color4.White;
            else
                explosion.Colour = targetResult.IsHit ? targetObject.AccentColour : Color4.Transparent;
        }
    }
}
