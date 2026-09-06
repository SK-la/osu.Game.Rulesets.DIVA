// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Game.Configuration;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    public partial class DrawableDivaJudgement : DrawableJudgement
    {
        internal SkinnableLighting Lighting { get; private set; } = null!;
        internal Color4 AccentColour { get; private set; }
        private DrawableHitObject? judgedDrawableObject;

        private static readonly Dictionary<HitResult, (Color4 Start, Color4 End)> judgement_gradients = new Dictionary<HitResult, (Color4, Color4)>
        {
            { HitResult.Perfect, (new Color4(1.0f, 0.95f, 0.4f, 1.0f), new Color4(0.95f, 0.75f, 0.2f, 1.0f)) },
            { HitResult.Great, (new Color4(0.85f, 0.95f, 1.0f, 1.0f), new Color4(0.3f, 0.6f, 0.85f, 1.0f)) },
            { HitResult.Good, (new Color4(0.6f, 1.0f, 0.5f, 1.0f), new Color4(0.2f, 0.85f, 0.2f, 1.0f)) },
            { HitResult.Ok, (new Color4(0.7f, 0.85f, 1.0f, 1.0f), new Color4(0.15f, 0.35f, 0.75f, 1.0f)) },
            { HitResult.Miss, (new Color4(0.85f, 0.5f, 1.0f, 1.0f), new Color4(0.55f, 0.15f, 0.85f, 1.0f)) }
        };

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

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

        public override void Apply(JudgementResult result, DrawableHitObject? judgedObject)
        {
            base.Apply(result, judgedObject);
            judgedDrawableObject = judgedObject;

            if (judgement_gradients.TryGetValue(result.Type, out var gradient))
            {
                AccentColour = gradient.Start;
            }
            else if (judgedObject is DrawableDivaHitObject drawableDivaHitObject)
            {
                AccentColour = drawableDivaHitObject.AccentColour.Value;
            }
        }

        protected override void PrepareForUse()
        {
            Lighting.ResetAnimation();

            if (Result != null)
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
            bool hitLightingEnabled = tryGetOsuBoolSetting(config, nameof(OsuSetting.HitLighting), fallback: true);
            bool visualBurstsEnabled = judgedDrawableObject is DrawableDivaHitObject { EnableVisualBursts.Value: true };

            Lighting.Alpha = 1;

            if (hitLightingEnabled && visualBurstsEnabled && Result != null && Result.Type != HitResult.Miss)
            {
                Lighting.Animate(Result);
                Lighting.FadeIn(1).Then().Delay(250).FadeOut(100);
            }

            base.ApplyHitAnimations();

            if (Lighting.LatestTransformEndTime > LifetimeEnd)
                LifetimeEnd = Lighting.LatestTransformEndTime;
        }

        private static bool tryGetOsuBoolSetting(OsuConfigManager configManager, string settingName, bool fallback)
        {
            if (!Enum.TryParse(settingName, out OsuSetting setting))
                return fallback;

            try
            {
                return configManager.Get<bool>(setting);
            }
            catch (InvalidCastException)
            {
                return fallback;
            }
        }

        protected override Drawable CreateDefaultJudgement(HitResult result) => new DivaJudgementPiece(this, result);

        internal string GetJudgementDisplayText(JudgementResult? result)
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
            HitResult.Perfect => DivaStrings.JUDGEMENT_COOL,
            HitResult.Great => DivaStrings.JUDGEMENT_FINE,
            HitResult.Good => DivaStrings.JUDGEMENT_SAFE,
            HitResult.Ok => DivaStrings.JUDGEMENT_SAD,
            HitResult.Meh => DivaStrings.JUDGEMENT_WRONG,
            HitResult.Miss => DivaStrings.JUDGEMENT_WORST,
            _ => result.ToString().ToUpperInvariant()
        };

        private static string getMehSuffix(DivaJudgementResult.DivaMehSource source) => source switch
        {
            DivaJudgementResult.DivaMehSource.PerfectWindowWrongPress => DivaStrings.JUDGEMENT_FINE,
            DivaJudgementResult.DivaMehSource.GreatWindowWrongPress => DivaStrings.JUDGEMENT_SAFE,
            DivaJudgementResult.DivaMehSource.GoodWindowWrongPress => DivaStrings.JUDGEMENT_SAD,
            DivaJudgementResult.DivaMehSource.OkWindowWrongPress => DivaStrings.JUDGEMENT_WRONG,
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

                if (judgement_gradients.TryGetValue(Result, out var gradient))
                {
                    JudgementText.Colour = new ColourInfo
                    {
                        TopLeft = gradient.Start,
                        TopRight = gradient.Start,
                        BottomLeft = gradient.End,
                        BottomRight = gradient.End,
                        HasSingleColour = false
                    };
                }

                JudgementText.Text = parent.GetJudgementDisplayText(parent.Result!);
            }

            public override void PlayAnimation()
            {
                JudgementText.Text = parent.GetJudgementDisplayText(parent.Result!);
                base.PlayAnimation();
            }
        }
    }
}
