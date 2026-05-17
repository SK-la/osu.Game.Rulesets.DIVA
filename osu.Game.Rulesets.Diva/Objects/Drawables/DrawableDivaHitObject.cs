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
using osu.Game.Rulesets.Diva.Audio;
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
        private const double fade_in_ratio = 0.24;

        public override bool HandlePositionalInput => false;

        protected readonly Sprite ApproachHand;
        protected readonly ApproachPiece ApproachPiece;

        protected readonly DivaAction ValidAction;

        private bool? pendingValidPress;
        private DivaJudgementResult.DivaMehSource pendingMehSource = DivaJudgementResult.DivaMehSource.None;

        protected BindableBool UseXb = new BindableBool(false);
        internal BindableBool EnableVisualBursts { get; } = new BindableBool(true);
        protected BindableDouble NoteSize = new BindableDouble(BASE_SIZE);
        protected BindableDouble ApproachDuration = new BindableDouble(1250);

        private double timePreempt => ApproachDuration.Value;
        private double timeFadein => ApproachDuration.Value * fade_in_ratio;

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
            config?.BindWith(DivaRulesetSettings.NoteSize, NoteSize);
            config?.BindWith(DivaRulesetSettings.ApproachDuration, ApproachDuration);

            NoteSize.BindValueChanged(_ => Size = new Vector2((float)NoteSize.Value), true);
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
            return [CreateHitSample()];
        }

        protected virtual HitSampleInfo CreateHitSample()
        {
            if (HitObject is DivaHoldHitObject)
                return DivaHitSampleInfo.Sweep;

            return DivaHitSampleInfo.Normal;
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (!userTriggered)
            {
                if (DivaHitJudgementEvaluator.ShouldMiss(timeOffset))
                    ApplyResult((r, _) => r.Type = HitResult.Miss);

                return;
            }

            if (pendingValidPress == null)
                return;

            bool validPress = pendingValidPress.Value;
            pendingValidPress = null;

            var result = DivaHitJudgementEvaluator.GetPressResult(validPress, timeOffset);

            if (result != HitResult.None)
                applyPressResult(result, validPress);
        }

        protected override double InitialLifetimeOffset => timePreempt;

        protected override void UpdateInitialTransforms()
        {
            this.FadeInFromZero(timeFadein);
            ApproachHand.ScaleTo(2, timeFadein, Easing.In);

            ApproachHand.RotateTo(360, timePreempt, Easing.In);
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            switch (state)
            {
                case ArmedState.Hit:
                    this.FadeOut(100).Expire();
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
            var b = (float)((Time.Current - LifetimeStart) / timePreempt);
            if (b < 1f)
                ApproachPiece.UpdatePos(b);
        }

        public virtual bool OnPressed(KeyBindingPressEvent<DivaAction> e)
        {
            if (Judged)
                return false;

            if (!AcceptsInput(e.Action))
                return false;

            pendingValidPress = ComputeValidPress(e.Action);
            return UpdateResult(true);
        }

        public virtual void OnReleased(KeyBindingReleaseEvent<DivaAction> e)
        {
        }

        protected virtual bool AcceptsInput(DivaAction action) => true;

        protected virtual bool ComputeValidPress(DivaAction action)
        {
            var directionToSymbol = MapDirectionToSymbol(action);
            var symbolToDirection = MapSymbolToDirection(action);

            return action == ValidAction ||
                   directionToSymbol == ValidAction ||
                   symbolToDirection == ValidAction;
        }

        protected static DivaAction MapDirectionToSymbol(DivaAction action) => action switch
        {
            DivaAction.Right => DivaAction.Circle,
            DivaAction.Down => DivaAction.Cross,
            DivaAction.Up => DivaAction.Triangle,
            DivaAction.Left => DivaAction.Square,
            _ => action,
        };

        protected static DivaAction MapSymbolToDirection(DivaAction action) => action switch
        {
            DivaAction.Circle => DivaAction.Right,
            DivaAction.Cross => DivaAction.Down,
            DivaAction.Triangle => DivaAction.Up,
            DivaAction.Square => DivaAction.Left,
            _ => action,
        };

        private void applyPressResult(HitResult result, bool validPress)
        {
            ApplyResult((r, _) =>
            {
                if (validPress)
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
