// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Game.Configuration;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    public partial class DrawableDivaJudgement : DrawableJudgement
    {
        internal SkinnableLighting Lighting { get; private set; }
        internal Color4 AccentColour { get; private set; }
        private DrawableHitObject judgedDrawableObject;

        // 定义每种判定结果的渐变色（起始色和结束色）- 根据 DIVA 原版风格
        private static readonly Dictionary<HitResult, (Color4 Start, Color4 End)> judgement_gradients = new Dictionary<HitResult, (Color4, Color4)>
        {
            // COOL - 金黄色到橙黄色渐变（上亮下暗）
            { HitResult.Perfect, (new Color4(1.0f, 0.95f, 0.4f, 1.0f), new Color4(0.95f, 0.75f, 0.2f, 1.0f)) },
            // FINE - 浅蓝色到中蓝色渐变
            { HitResult.Great, (new Color4(0.85f, 0.95f, 1.0f, 1.0f), new Color4(0.3f, 0.6f, 0.85f, 1.0f)) },
            // SAFE - 浅绿色到中绿色渐变
            { HitResult.Good, (new Color4(0.6f, 1.0f, 0.5f, 1.0f), new Color4(0.2f, 0.85f, 0.2f, 1.0f)) },
            // SAD - 浅蓝色到深蓝色渐变
            { HitResult.Ok, (new Color4(0.7f, 0.85f, 1.0f, 1.0f), new Color4(0.15f, 0.35f, 0.75f, 1.0f)) },
            // WORST - 浅紫色到深紫色渐变
            { HitResult.Miss, (new Color4(0.85f, 0.5f, 1.0f, 1.0f), new Color4(0.55f, 0.15f, 0.85f, 1.0f)) }
        };

        [Resolved]
        private OsuConfigManager config { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(Lighting = new SkinnableLighting
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Blending = BlendingParameters.Additive,
                Depth = float.MaxValue,
                Alpha = 1
            });
        }

        public override void Apply(JudgementResult result, DrawableHitObject judgedObject)
        {
            base.Apply(result, judgedObject);
            judgedDrawableObject = judgedObject;

            // 使用自定义的判定渐变色
            if (judgement_gradients.TryGetValue(result.Type, out var gradient))
            {
                AccentColour = gradient.Start; // 保留用于光照效果
            }
            else if (judgedObject is DrawableDivaHitObject drawableDivaHitObject)
            {
                AccentColour = drawableDivaHitObject.AccentColour.Value;
            }
        }

        protected override void PrepareForUse()
        {
            Lighting.ResetAnimation();
            Lighting.SetColourFrom(this, Result);

            if (judgedDrawableObject?.HitObject is DivaHitObject divaObject)
            {
                Position = divaObject.Position;
                Scale = new Vector2(1);
            }

            base.PrepareForUse();
        }

        protected override void ApplyHitAnimations()
        {
            bool hitLightingEnabled = config.Get<bool>(OsuSetting.HitLighting);
            bool visualBurstsEnabled = judgedDrawableObject is DrawableDivaHitObject drawable && drawable.EnableVisualBursts.Value;

            Lighting.Alpha = 1;

            if (hitLightingEnabled && visualBurstsEnabled && Result != null && Result.Type != HitResult.Miss)
            {
                Lighting.Animate(Result);

                // 与 osu! 一致：在 lighting 容器上用 transform 控制淡出，而非按帧数推算生命周期。
                Lighting.FadeIn(1).Then().Delay(250).FadeOut(100);
            }

            base.ApplyHitAnimations();

            if (Lighting.LatestTransformEndTime > LifetimeEnd)
                LifetimeEnd = Lighting.LatestTransformEndTime;
        }

        protected override Drawable CreateDefaultJudgement(HitResult result) => new DivaJudgementPiece(this, result);

        internal string GetJudgementDisplayText(JudgementResult result)
        {
            if (result == null)
                return string.Empty;

            string text = getResultLabel(result.Type);

            if (result is DivaJudgementResult { IsSpecialMeh: true } divaResult)
            {
                string suffix = getMehSuffix(divaResult.SpecialMehSource);

                if (!string.IsNullOrEmpty(suffix))
                    text = $"{text} {suffix}";
            }

            if (result.Type is HitResult.Miss or HitResult.Meh)
                return text;

            return $"{text} {result.ComboAfterJudgement}";
        }

        private static string getResultLabel(HitResult result) => result switch
        {
            HitResult.Perfect => "cool",
            HitResult.Great => "fine",
            HitResult.Good => "safe",
            HitResult.Ok => "sad",
            HitResult.Meh => "wrong",
            HitResult.Miss => "worst",
            _ => result.ToString().ToLowerInvariant()
        };

        private static string getMehSuffix(DivaJudgementResult.DivaMehSource source) => source switch
        {
            DivaJudgementResult.DivaMehSource.PerfectWindowWrongPress => "fine",
            DivaJudgementResult.DivaMehSource.GreatWindowWrongPress => "safe",
            DivaJudgementResult.DivaMehSource.GoodWindowWrongPress => "sad",
            DivaJudgementResult.DivaMehSource.OkWindowWrongPress => "wrong",
            _ => string.Empty
        };

        private partial class DivaJudgementPiece : DefaultJudgementPiece
        {
            private readonly DrawableDivaJudgement parent;

            public DivaJudgementPiece(DrawableDivaJudgement parent, HitResult result)
                : base(result)
            {
                this.parent = parent;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                // 应用对角线渐变色
                if (judgement_gradients.TryGetValue(Result, out var gradient))
                {
                    // 创建真正的对角线渐变（左上→右下）
                    JudgementText.Colour = new ColourInfo
                    {
                        TopLeft = gradient.Start,
                        TopRight = gradient.Start,
                        BottomLeft = gradient.End,
                        BottomRight = gradient.End,
                        HasSingleColour = false
                    };
                }

                JudgementText.Text = parent.GetJudgementDisplayText(parent.Result);
            }

            public override void PlayAnimation()
            {
                JudgementText.Text = parent.GetJudgementDisplayText(parent.Result);
                base.PlayAnimation();
            }
        }
    }
}
