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
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Configuration;
using osu.Game.Rulesets.Diva.Graphics;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Diva.Objects.Drawables.Pieces;
using osu.Game.Rulesets.Diva.Scoring;
using osu.Game.Rulesets.Diva.UI;
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

        /// <summary>
        ///     hand.png is authored so the tip sits at the texture centre, so it needs 180° to point
        ///     outward; the spin is applied on top of that.
        /// </summary>
        private const float hand_base_rotation = 180f;

        public override bool HandlePositionalInput => false;

        protected readonly Sprite ApproachHand;
        protected readonly ApproachPiece ApproachPiece;
        protected ApproachTrailLayer? ApproachTrail;
        protected Sprite? StatSprite;

        protected readonly DivaAction ValidAction;

        private bool? pendingValidPress;
        protected DivaJudgementResult.DivaMehSource PendingMehSource = DivaJudgementResult.DivaMehSource.None;

        protected BindableBool UseXb = new BindableBool(false);
        internal BindableBool EnableVisualBursts { get; } = new BindableBool(true);
        protected BindableDouble NoteSize = new BindableDouble(BASE_SIZE);

        /// <summary>Slides in from outside the field, then pops in at full size (ProjectDIVA) vs the old fade.</summary>
        protected Bindable<DivaNoteAppearance> NoteAppearanceMode { get; } = new Bindable<DivaNoteAppearance>(DivaNoteAppearance.DivaNative);

        protected Bindable<DivaNoteFlightCurve> FlightCurve { get; } = new Bindable<DivaNoteFlightCurve>(DivaNoteFlightCurve.DivaNative);

        /// <summary>Percent of the curve's own baseline lateral offset (100 = ProjectDIVA).</summary>
        protected BindableDouble FlightAmplitude { get; } = new BindableDouble(100);

        /// <summary>Percent of the default hold-body star spacing (100 = default, 0 = no stars).</summary>
        protected BindableDouble HoldStarDensity { get; } = new BindableDouble(100);

        /// <summary>Multiplier on PD <c>note_standing × MsPerFrame(BPM)</c> (default 1.0).</summary>
        protected BindableDouble ApproachPreemptScale = new BindableDouble(1.0);

        /// <summary>PD Strict when true: wrong key within window consumes the note.</summary>
        protected BindableBool JudgementLock = new BindableBool(true);

        /// <summary>Ruleset-specific input offset added to judgement <c>timeOffset</c> (ms).</summary>
        protected BindableDouble InputOffset = new BindableDouble(0);

        [Resolved(canBeNull: true)]
        private IBeatmap? beatmap { get; set; }

        /// <summary>Used to clip flying pieces to ProjectDIVA's draw range.</summary>
        [Resolved(canBeNull: true)]
        private DivaPlayfield? divaPlayfield { get; set; }

        /// <summary>Logical field size; falls back to the native field while the playfield is unresolved.</summary>
        protected Vector2 LogicalPlayfieldSize => divaPlayfield?.LogicalSize ?? DivaPlayfieldSize.DefaultNativeSize;

        /// <summary>Approach window in ms, matching ProjectDIVA standing time × user scale.</summary>
        protected double TimePreempt
        {
            get
            {
                double bpm = DivaChartConstants.BASE_BPM;

                if (beatmap != null)
                {
                    double timingBpm = beatmap.ControlPointInfo.TimingPointAt(HitObject.StartTime).BPM;
                    if (timingBpm > 0)
                        bpm = timingBpm;
                }

                return DivaChartConstants.StandingPreemptMs(bpm) * ApproachPreemptScale.Value;
            }
        }

        private double timeFadein => TimePreempt * fade_in_ratio;

        protected override JudgementResult CreateResult(Judgement judgement)
        {
            var result = new DivaJudgementResult(HitObject, judgement)
            {
                SpecialMehSource = PendingMehSource
            };

            PendingMehSource = DivaJudgementResult.DivaMehSource.None;

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
                    // hand.png canvas is authored so the tip sits at the texture centre.
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
        private void load(TextureStore textures, DivaRulesetConfigManager? config)
        {
            config?.BindWith(DivaRulesetSettings.UseXBoxButtons, UseXb);
            config?.BindWith(DivaRulesetSettings.EnableVisualBursts, EnableVisualBursts);
            config?.BindWith(DivaRulesetSettings.NoteSize, NoteSize);
            config?.BindWith(DivaRulesetSettings.ApproachPreemptScale, ApproachPreemptScale);
            config?.BindWith(DivaRulesetSettings.JudgementLock, JudgementLock);
            config?.BindWith(DivaRulesetSettings.InputOffset, InputOffset);
            config?.BindWith(DivaRulesetSettings.NoteAppearance, NoteAppearanceMode);
            config?.BindWith(DivaRulesetSettings.FlightCurve, FlightCurve);
            config?.BindWith(DivaRulesetSettings.FlightAmplitude, FlightAmplitude);
            config?.BindWith(DivaRulesetSettings.HoldStarDensity, HoldStarDensity);

            FlightCurve.BindValueChanged(_ => applyFlightSettings());
            FlightAmplitude.BindValueChanged(_ => applyFlightSettings());
            applyFlightSettings();

            NoteSize.BindValueChanged(v => Size = new Vector2((float)v.NewValue), true);

            string textureLocation = GetTextureLocation();
            DivaAction textureAction = GetTextureAction();

            StatSprite = new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Texture = textures.Get($"{textureLocation}{textureAction}Stat"),
                Depth = 2,
            };
            AddInternal(StatSprite);

            ApproachPiece.Texture = textures.Get($"{textureLocation}{textureAction}Move");
            ApproachHand.Texture = textures.Get("hand");

            Color4 trailColour = DivaProjectDivaAtlas.GetUnitColor(ValidAction);
            ApproachTrail = new ApproachTrailLayer(trailColour)
            {
                Depth = 3,
            };
            AddInternal(ApproachTrail);
        }

        /// <summary>
        ///     Root textures are face buttons (○□△✕).
        ///     <c>Doubles/</c> textures are direction keys (→←↑↓); filenames were never renamed
        ///     and still say Circle/Square/Triangle/Cross (= Right/Left/Up/Down).
        /// </summary>
        protected virtual string GetTextureLocation()
        {
            string xb = UseXb.Value ? "XB/" : "";

            if (IsDirectionAction(ValidAction))
                return "Doubles/" + xb;

            return xb;
        }

        /// <summary>
        ///     Filename stem under <c>Doubles/</c>: Right→Circle, Left→Square, Down→Cross, Up→Triangle.
        ///     Face-button notes use their own action name under the root texture folder.
        /// </summary>
        protected virtual DivaAction GetTextureAction() =>
            IsDirectionAction(ValidAction) ? DirectionToDoublesFileStem(ValidAction) : ValidAction;

        protected static bool IsDirectionAction(DivaAction action) =>
            action is DivaAction.Left or DivaAction.Right or DivaAction.Up or DivaAction.Down;

        /// <summary>Doubles/ file stem for a direction action (filenames still use face-button names).</summary>
        protected static DivaAction DirectionToDoublesFileStem(DivaAction direction) => MapDirectionToSymbol(direction);

        /// <summary>
        /// Hit SE is played by <see cref="Audio.DivaHitSamplePlayer"/> on keydown (ProjectDIVA / Taiko pattern).
        /// </summary>
        public override IEnumerable<HitSampleInfo> GetSamples() => [];

        public override void PlaySamples()
        {
            // Handled by DivaHitSamplePlayer — avoid double playback on ArmedState.Hit.
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            timeOffset += InputOffset.Value;

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

        protected override double InitialLifetimeOffset => TimePreempt;

        protected override void UpdateInitialTransforms()
        {
            if (NoteAppearanceMode.Value == DivaNoteAppearance.DivaNative)
            {
                // ProjectDIVA draws the target immediately at full alpha; the pop is driven from Update.
                this.Alpha = 1;
                return;
            }

            this.FadeInFromZero(timeFadein);
            ApproachHand.ScaleTo(2, timeFadein, Easing.In);

            ApproachHand.RotateTo(360, TimePreempt, Easing.In);
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
            var b = (float)((Time.Current - LifetimeStart) / TimePreempt);

            if (b < 1f)
            {
                ApproachPiece.UpdatePos(b);

                if (NoteAppearanceMode.Value == DivaNoteAppearance.DivaNative)
                    updateDivaNativeAppearance(b);
                else if (!hasReachedTarget)
                    ApproachTrail?.EmitAt(ApproachPiece.Position, Time.Elapsed);
            }

            OnApproachUpdate(b);
        }

        /// <summary>Radius within which the flying piece counts as merged into the fixed target's own footprint.</summary>
        private float targetMergeRadius => (float)NoteSize.Value * 0.5f;

        /// <summary>
        ///     Trail particles are spawned at the flying piece, so emitting all the way in would leave a clump of stars
        ///     resting on the stationary target note. Stop once the two visually overlap.
        /// </summary>
        private bool hasReachedTarget => ApproachPiece.Position.LengthSquared <= targetMergeRadius * targetMergeRadius;

        /// <summary>
        ///     ProjectDIVA presentation: the flying piece is drawn only inside the field (no fade), while the
        ///     fixed target shrinks back from <c>NOTE_BLOWUP</c> and the pointer spins at a constant rate.
        /// </summary>
        private void updateDivaNativeAppearance(float blend)
        {
            if (Judged)
                return;

            Vector2 fieldPosition = HitObject.Position + ApproachPiece.Position;
            bool inside = DivaPlayfieldSize.IsInsideDrawRange(fieldPosition, LogicalPlayfieldSize, (float)NoteSize.Value);

            ApproachPiece.Alpha = inside ? 1 : 0;

            if (inside && !hasReachedTarget)
                ApproachTrail?.EmitAt(ApproachPiece.Position, Time.Elapsed);

            float percent = 1f - blend;
            float scale = DivaChartConstants.NoteBlowupScale(percent);

            if (StatSprite != null)
                StatSprite.Scale = new Vector2(scale);

            ApproachHand.Scale = new Vector2(scale);
            ApproachHand.Rotation = hand_base_rotation - 360f * percent;
        }

        protected void applyFlightSettings()
        {
            float amplitude = (float)(FlightAmplitude.Value / 100.0);

            ApproachPiece.Curve = FlightCurve.Value;
            ApproachPiece.Amplitude = amplitude;

            OnFlightSettingsChanged(FlightCurve.Value, amplitude);
        }

        /// <summary>Lets subclasses keep their own curve consumers (e.g. the hold strip) in sync.</summary>
        protected virtual void OnFlightSettingsChanged(DivaNoteFlightCurve curve, float amplitude)
        {
        }

        protected virtual void OnApproachUpdate(float blend)
        {
        }

        public virtual bool OnPressed(KeyBindingPressEvent<DivaAction> e)
        {
            if (Judged)
                return false;

            if (!AcceptsInput(e.Action))
                return false;

            bool validPress = ComputeValidPress(e.Action);

            // Standard mode (lock off): wrong keys do not consume the note.
            if (!validPress && !JudgementLock.Value)
                return false;

            pendingValidPress = validPress;
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

                PendingMehSource = DivaHitJudgementEvaluator.GetMehSourceFor(result);
                r.Type = HitResult.Meh;
            });
        }
    }
}
