// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    /// <summary>
    /// 内置资源驱动的打击爆炸动画，分层播放 normal / great / perfect 三段效果。
    /// </summary>
    public partial class DefaultHitExplosion : PoolableDrawable, IHitExplosion
    {
        private const string normal_base_path = "HitExplosion/hit-normal";
        private const string great_base_path = "HitExplosion/hit-great";
        private const string perfect_base_path = "HitExplosion/hit-perfect";
        private const int normal_frame_count = 9;
        private const int great_frame_count = 9;
        private const int perfect_frame_count = 10;
        private const double min_frame_duration = 1000.0 / 30.0;

        private TextureAnimation normalAnimation = null!;
        private TextureAnimation greatAnimation = null!;
        private TextureAnimation perfectAnimation = null!;

        private JudgementResult judgementResult;

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            normalAnimation = createAnimation(textures, normal_base_path, normal_frame_count, 0);
            greatAnimation = createAnimation(textures, great_base_path, great_frame_count, 1, defaultAlpha: 0);
            perfectAnimation = createAnimation(textures, perfect_base_path, perfect_frame_count, 2, defaultAlpha: 0);

            AddRangeInternal(new Drawable[]
            {
                normalAnimation,
                greatAnimation,
                perfectAnimation
            });
        }

        private TextureAnimation createAnimation(TextureStore textures, string basePath, int frameCount, int depth, double defaultAlpha = 1)
        {
            var animation = new TextureAnimation();
            var frames = new List<Texture>();

            for (int i = 0; i < frameCount; i++)
            {
                var texture = textures.Get($"{basePath}-{i}");

                if (texture != null)
                    frames.Add(texture);
            }

            animation.Anchor = Anchor.Centre;
            animation.Origin = Anchor.Centre;
            animation.Depth = depth;
            animation.Alpha = (float)defaultAlpha;
            animation.Loop = false;
            animation.DefaultFrameLength = min_frame_duration;
            animation.AddFrames(frames);

            return animation;
        }

        public void ResetAnimation()
        {
            resetLayer(normalAnimation, 1);
            resetLayer(greatAnimation, 0);
            resetLayer(perfectAnimation, 0);
        }

        private static void resetLayer(TextureAnimation animation, float defaultAlpha)
        {
            animation.ClearTransforms();
            animation.Stop();
            animation.GotoFrame(0);
            animation.Alpha = defaultAlpha;
        }

        public void Animate(JudgementResult result)
        {
            judgementResult = result;

            if (judgementResult == null)
                return;

            if (judgementResult.Type.GetIndexForOrderedDisplay() > HitResult.Good.GetIndexForOrderedDisplay())
                return;

            playAnimationForJudgement();
        }

        private void playAnimationForJudgement()
        {
            playAnimation(normalAnimation);

            switch (judgementResult.Type)
            {
                case HitResult.Perfect:
                    playAnimation(greatAnimation);
                    playAnimation(perfectAnimation);
                    break;

                case HitResult.Great:
                    playAnimation(greatAnimation);
                    break;
            }
        }

        private void playAnimation(TextureAnimation animation)
        {
            if (animation.FrameCount > 0)
            {
                animation.Alpha = 1;
                animation.GotoFrame(0);
                animation.Play();
            }
        }
    }
}
