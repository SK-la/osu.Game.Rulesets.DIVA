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
using osu.Game.Rulesets.Diva.Scoring;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    public partial class DrawableDivaHitObject : DrawableHitObject<DivaHitObject>, IKeyBindingHandler<DivaAction>
    {
        public const float BASE_SIZE = 40;

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
            if (Judged)
            {
                Pressed = false;
                ValidPress = false;
                return;
            }

            bool withinOkWindow = DivaHitJudgementEvaluator.IsWithinOkWindow(timeOffset);

            // 玩家按键后，立即尝试判定；如果太早，则保留状态，等待进入判定窗口。
            if (Pressed)
            {
                var result = DivaHitJudgementEvaluator.GetResultFor(timeOffset);

                if (result != HitResult.None && timeOffset > -time_action)
                {
                    applyPressResult(result);

                    Pressed = false;
                    ValidPress = false;
                    return;
                }
            }

            // 未按键时，或者按键太早/未命中窗口，超出 Ok 后直接 Miss。
            if (Time.Current > HitObject.StartTime && !withinOkWindow)
            {
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

            double timeOffset = Time.Current - HitObject.StartTime;

            if (DivaHitJudgementEvaluator.IsWithinOkWindow(timeOffset) && timeOffset > -time_action)
            {
                applyPressResult(DivaHitJudgementEvaluator.GetResultFor(timeOffset));
                Pressed = false;
                ValidPress = false;
            }

            return true;
        }

        public virtual void OnReleased(KeyBindingReleaseEvent<DivaAction> e)
        {
        }

        private void applyPressResult(HitResult result)
        {
            ApplyResult((r, _) =>
            {
                if (ValidPress)
                {
                    r.Type = result;
                    return;
                }

                pendingMehSource = DivaHitJudgementEvaluator.GetMehSourceFor(result);
                r.Type = HitResult.Meh;
            });
        }
    }
}
