// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Bindings;
using osu.Game.Audio;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Diva.Objects.Drawables.Pieces;
using osuTK;
using osuTK.Graphics;
using osu.Game.Rulesets.Diva.Configuration;
using osu.Framework.Bindables;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Judgements;
using osu.Framework.Input.Events;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    public partial class DrawableDivaHitObject : DrawableHitObject<DivaHitObject>, IKeyBindingHandler<DivaAction>
    {
        public const float BASE_SIZE = 56;

        private const double time_preempt = 850;
        private const double time_fadein = 300;
        private const double time_action = 150;

        public override bool HandlePositionalInput => false;

        protected readonly Sprite ApproachHand;
        protected readonly ApproachPiece ApproachPiece;

        protected readonly DivaAction ValidAction;

        protected bool ValidPress;
        protected bool Pressed;

        protected BindableBool UseXb = new BindableBool(false);
        protected BindableBool EnableVisualBursts = new BindableBool(true);

        protected override JudgementResult CreateResult(Judgement judgement) => new DivaJudgementResult(HitObject, judgement);

        public DrawableDivaHitObject(DivaHitObject hitObject)
            : base(hitObject)
        {
            Size = new Vector2(BASE_SIZE);

            Origin = Anchor.Centre;
            Position = hitObject.Position;

            AddRangeInternal([
                ApproachHand = new Sprite()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Rotation = 180f,
                    Depth = 1,
                },
                ApproachPiece = new ApproachPiece()
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

        protected virtual string GetTextureLocation() => (UseXb.Value) ? "XB/" : "";

        public override IEnumerable<HitSampleInfo> GetSamples() => [
            new HitSampleInfo(HitSampleInfo.HIT_NORMAL, SampleControlPoint.DEFAULT_BANK)
        ];

        public override void PlaySamples()
        {
        }

        protected static void ApplyMiss(JudgementResult r, DrawableHitObject s) => r.Type = HitResult.Miss;

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (Judged)
            {
                Pressed = false;
                return;
            }

            if (!HitObject.HitWindows.CanBeHit(timeOffset) && Time.Current > HitObject.StartTime)
            {
                ApplyResult(ApplyMiss);
                return;
            }

            var result = HitObject.HitWindows.ResultFor(timeOffset);

            if (result == HitResult.None) return;

            if (Pressed && timeOffset > (-time_action))
            {
                if (ValidPress)
                    ApplyResult((r, s) => r.Type = result);
                else
                    ApplyResult(ApplyMiss);
                Pressed = false;
            }
        }

        protected override double InitialLifetimeOffset => time_preempt;

        protected override void UpdateInitialTransforms()
        {
            this.FadeInFromZero(time_fadein);
            this.ApproachHand.ScaleTo(2, time_fadein, Easing.In);

            this.ApproachHand.RotateTo(360, time_preempt, Easing.In);
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
                this.ApproachPiece.UpdatePos(b);
        }

        public virtual bool OnPressed(KeyBindingPressEvent<DivaAction> e)
        {
            this.Samples.Play();

            if (Judged)
                return false;

            var action = e.Action switch
            {
                DivaAction.Right => DivaAction.Circle,
                DivaAction.Down => DivaAction.Cross,
                DivaAction.Up => DivaAction.Triangle,
                DivaAction.Left => DivaAction.Square,
                _ => e.Action,
            };
            ValidPress = action == ValidAction;
            Pressed = true;

            return true;
        }

        public virtual void OnReleased(KeyBindingReleaseEvent<DivaAction> e)
        {
        }
    }
}
