// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Audio;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Diva.Configuration;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Diva.Objects.Drawables.Pieces;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    public partial class DrawableDivaHitObject : DrawableHitObject<DivaHitObject>, IKeyBindingHandler<DivaAction>
    {
        public const float BASE_SIZE = 43;

        // 音符预读时间（毫秒），数值越大音符飞得越慢
        private const double time_preempt = 1250;
        private const double time_fadein = 300;
        private const double time_action = 150;

        public override bool HandlePositionalInput => false;

        protected readonly Sprite ApproachHand;
        protected readonly ApproachPiece ApproachPiece;

        protected readonly DivaAction ValidAction;

        protected bool ValidPress;
        protected bool Pressed;
        private DivaJudgementResult.DivaMehSource pendingMehSource = DivaJudgementResult.DivaMehSource.None;

        protected BindableBool UseXb = new BindableBool(false);
        protected BindableBool EnableVisualBursts = new BindableBool(true);

        protected override JudgementResult CreateResult(Judgement judgement)
        {
            var result = new DivaJudgementResult(HitObject, judgement)
            {
                SpecialMehSource = pendingMehSource
            };

            pendingMehSource = DivaJudgementResult.DivaMehSource.None;

            return result;
        }

        public DrawableDivaHitObject(DivaHitObject hitObject)
            : base(hitObject)
        {
            Size = new Vector2(BASE_SIZE);

            Origin = Anchor.Centre;
            Position = hitObject.Position;

            AddRangeInternal([
                ApproachHand = new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Rotation = 180f,
                    Depth = 1,
                },
                ApproachPiece = new ApproachPiece
                {
                    Depth = 0,
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Position = hitObject.ApproachPieceOriginPosition,
                    StartPos = hitObject.ApproachPieceOriginPosition,
                }
            ]);

            ValidAction = hitObject.ValidAction;
        }

        [BackgroundDependencyLoader(true)]
        private void load(TextureStore textures, DivaRulesetConfigManager config)
        {
            config?.BindWith(DivaRulesetSettings.UseXBoxButtons, UseXb);
            config?.BindWith(DivaRulesetSettings.EnableVisualBursts, EnableVisualBursts);
            string textureLocation = GetTextureLocation();

            AddInternal(new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Texture = textures.Get($"{textureLocation}{ValidAction.ToString()}Stat"),
                Depth = 2,
            });

            ApproachPiece.Texture = textures.Get($"{textureLocation}{ValidAction.ToString()}Move");
            ApproachHand.Texture = textures.Get("hand");
        }

        protected virtual string GetTextureLocation() => UseXb.Value ? "XB/" : "";

        public override IEnumerable<HitSampleInfo> GetSamples()
        {
            return [new HitSampleInfo(HitSampleInfo.HIT_NORMAL, SampleControlPoint.DEFAULT_BANK)];
        }

        public override void PlaySamples()
        {
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            // 情况1：未触发且超出判定窗口 → 直接Miss
            if (!userTriggered)
            {
                if (!HitObject.HitWindows.CanBeHit(timeOffset))
                {
                    ApplyMinResult();
                }

                return;
            }

            // 情况2：已判定完成 → 清理状态并退出
            if (Judged)
            {
                Pressed = false;
                ValidPress = false;
                return;
            }

            // 情况3：玩家已按下按键，进行判定
            if (Pressed)
            {
                var result = HitObject.HitWindows.ResultFor(timeOffset);

                // 如果有有效的判定结果且在允许的时间范围内
                if (result != HitResult.None && timeOffset > -time_action)
                {
                    // 根据按键是否正确给予相应判定
                    ApplyResult((r, _) =>
                    {
                        if (ValidPress)
                        {
                            r.Type = result;
                            return;
                        }

                        pendingMehSource = getMehSourceFor(result);

                        r.Type = HitResult.Meh;
                    });

                    // 清理状态
                    Pressed = false;
                    ValidPress = false;
                    return;
                }
            }

            // 情况4：超出判定窗口且Note已开始 → Miss（防止漏键）
            if (!HitObject.HitWindows.CanBeHit(timeOffset) && Time.Current > HitObject.StartTime)
            {
                // 只有在未判定的情况下才应用Miss
                if (!Judged)
                {
                    ApplyResult((r, _) => r.Type = HitResult.Miss);
                    Pressed = false;
                    ValidPress = false;
                }
            }
        }

        protected override double InitialLifetimeOffset => time_preempt;

        protected override void UpdateInitialTransforms()
        {
            this.FadeInFromZero(time_fadein);
            ApproachHand.ScaleTo(2, time_fadein, Easing.In);

            ApproachHand.RotateTo(360, time_preempt, Easing.In);
            //this.approachPiece.MoveTo(Vector2.Zero, time_preempt, Easing.None);
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            switch (state)
            {
                case ArmedState.Hit:

                    if (EnableVisualBursts.Value)
                        this.ScaleTo(2f, 1500, Easing.OutQuint).FadeOut(1500, Easing.OutQuint).Expire();
                    break;

                case ArmedState.Miss:
                    const double duration = 1000;

                    this.ScaleTo(1.1f, duration, Easing.OutQuint);
                    this.MoveToOffset(new Vector2(0, 10), duration, Easing.In);
                    this.FadeColour(Color4.Red.Opacity(0.5f), duration / 2, Easing.OutQuint).Then().FadeOut(duration / 2, Easing.InQuint).Expire();
                    break;
            }
        }

        protected override void Update()
        {
            var b = (float)((Time.Current - LifetimeStart) / time_preempt);
            if (b < 1f)
                ApproachPiece.UpdatePos(b);
        }

        public virtual bool OnPressed(KeyBindingPressEvent<DivaAction> e)
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

            // 检查是否匹配：直接匹配、方向转符号、符号转方向
            ValidPress = e.Action == ValidAction ||
                         directionToSymbol == ValidAction ||
                         symbolToDirection == ValidAction;
            Pressed = true;

            return true;
        }

        public virtual void OnReleased(KeyBindingReleaseEvent<DivaAction> e)
        {
        }

        private static DivaJudgementResult.DivaMehSource getMehSourceFor(HitResult result) => result switch
        {
            HitResult.Perfect => DivaJudgementResult.DivaMehSource.PerfectWindowWrongPress,
            HitResult.Great => DivaJudgementResult.DivaMehSource.GreatWindowWrongPress,
            HitResult.Good => DivaJudgementResult.DivaMehSource.GoodWindowWrongPress,
            HitResult.Ok => DivaJudgementResult.DivaMehSource.OkWindowWrongPress,
            _ => DivaJudgementResult.DivaMehSource.None
        };
    }
}
