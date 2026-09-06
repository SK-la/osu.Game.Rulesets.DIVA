// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Diva.Configuration;
using osu.Game.Rulesets.Diva.Graphics;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    /// <summary>
    ///     Hit explosion driven by bundled HitExplosion sheets; optional PD flame overlay.
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
        private Sprite? flameSprite;

        private JudgementResult? judgementResult;

        private float hitExplosionAlpha = 1.0f;

        [Resolved(CanBeNull = true)]
        private DivaRulesetConfigManager? config { get; set; }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            hitExplosionAlpha = (float)(config?.Get<double>(DivaRulesetSettings.HitExplosionAlpha) ?? 1.0);

            normalAnimation = createAnimation(textures, normal_base_path, normal_frame_count, 0);
            greatAnimation = createAnimation(textures, great_base_path, great_frame_count, 1, defaultAlpha: 0);
            perfectAnimation = createAnimation(textures, perfect_base_path, perfect_frame_count, 2, defaultAlpha: 0);

            flameSprite = new Sprite
            {
                Texture = DivaProjectDivaAtlas.GetFlame(textures),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(80),
                Blending = BlendingParameters.Additive,
                Alpha = 0,
                Depth = -1,
            };

            AddRangeInternal(new Drawable[]
            {
                normalAnimation,
                greatAnimation,
                perfectAnimation,
                flameSprite
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
            resetLayer(normalAnimation, hitExplosionAlpha);
            resetLayer(greatAnimation, 0);
            resetLayer(perfectAnimation, 0);

            if (flameSprite != null)
            {
                flameSprite.ClearTransforms();
                flameSprite.Alpha = 0;
                flameSprite.Scale = Vector2.One;
            }
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

            if (flameSprite?.Texture != null)
            {
                flameSprite.Alpha = hitExplosionAlpha * 0.85f;
                flameSprite.Scale = new Vector2(0.4f);
                flameSprite.ScaleTo(1.6f, 280, Easing.OutQuad)
                          .FadeOut(280);
            }

            switch (judgementResult!.Type)
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
                animation.Alpha = hitExplosionAlpha;
                animation.GotoFrame(0);
                animation.Play();
            }
        }
    }
}
