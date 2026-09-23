// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Configuration;
using osu.Game.Rulesets.Diva.Edit.Blueprints.Components;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Diva.Objects.Drawables.Pieces;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Diva.Edit.Blueprints
{
    public partial class DivaHoldSelectionBlueprint : HitObjectSelectionBlueprint<DivaHoldHitObject>
    {
        private readonly DivaNotePiece piece;
        private readonly DivaApproachHandle approachHandle;
        private readonly Circle durationHandle;

        private readonly Bindable<DivaNoteFlightCurve> flightCurve = new Bindable<DivaNoteFlightCurve>(DivaNoteFlightCurve.DivaNative);
        private readonly BindableDouble flightAmplitude = new BindableDouble(100);

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private DivaHitObjectComposer? composer { get; set; }

        protected override bool AlwaysShowWhenSelected => true;

        public DivaHoldSelectionBlueprint(DivaHoldHitObject hitObject)
            : base(hitObject)
        {
            InternalChildren = new Drawable[]
            {
                piece = new DivaNotePiece(),
                approachHandle = new DivaApproachHandle(hitObject) { Alpha = 0 },
                durationHandle = new Circle
                {
                    Origin = Anchor.Centre,
                    Size = new Vector2(14),
                    Alpha = 0
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, DivaRulesetConfigManager? config)
        {
            durationHandle.Colour = colours.YellowDark;
            config?.BindWith(DivaRulesetSettings.FlightCurve, flightCurve);
            config?.BindWith(DivaRulesetSettings.FlightAmplitude, flightAmplitude);
        }

        protected override void Update()
        {
            base.Update();

            piece.UpdateFrom(HitObject, textures);
            approachHandle.Position = HitObject.Position;
            approachHandle.Alpha = IsSelected ? 1 : 0;
            durationHandle.Alpha = IsSelected ? 1 : 0;
            durationHandle.Position = HitObject.Position + tailOffset();
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (!IsSelected || e.Button != MouseButton.Left)
                return false;

            // Hit-tested at the mouse-down point, not the current one: the drag only starts once the cursor has
            // travelled past the framework's drag threshold, which is already further than these handles are wide,
            // so testing where the cursor ended up would refuse the drag and hand it to the selection instead.
            if (!isHandleAt(e.ScreenSpaceMouseDownPosition))
                return false;

            editorBeatmap.BeginChange();
            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            if (composer == null)
                return;

            Vector2 flight = flightVector();

            if (flight.LengthSquared < 1f)
                return;

            double msPerFrame = DivaChartBuilder.MsPerFrameAt(editorBeatmap, HitObject.StartTime);

            if (msPerFrame <= 0)
                return;

            // Slide along the flight instead of across the screen: the tail travels the same chord the body is
            // drawn on, so how far the cursor projects onto it is the fraction of the approach window being held.
            Vector2 local = composer.Playfield.ToLocalSpace(e.ScreenSpaceMousePosition) - HitObject.Position;
            double frames = Math.Round(Math.Max(1, Vector2.Dot(local, flight) / flight.LengthSquared * approachWindowMs) / msPerFrame);
            double length = frames * msPerFrame;

            if (Math.Abs(HitObject.Duration - length) < 0.01)
                return;

            HitObject.Duration = length;
            editorBeatmap.Update(HitObject);
        }

        protected override void OnDragEnd(DragEndEvent e) => editorBeatmap.EndChange();

        /// <summary>Whether <paramref name="screenSpacePos"/> lands on this note's flight or length handle.</summary>
        private bool isHandleAt(Vector2 screenSpacePos)
            => IsSelected
               && (approachHandle.ReceivePositionalInputAt(screenSpacePos) || durationHandle.ReceivePositionalInputAt(screenSpacePos));

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
            => piece.ReceivePositionalInputAt(screenSpacePos) || isHandleAt(screenSpacePos);

        public override Quad SelectionQuad => piece.ScreenSpaceDrawQuad;

        /// <summary>The vector the note actually flies along: only the stored direction survives, at the BPM distance.</summary>
        private Vector2 flightVector() => DivaActionEncoding.NormaliseApproachOrigin(HitObject.ApproachPieceOriginPosition, bpm);

        /// <summary>How long the note is in the air, i.e. the window the strip's length is expressed against.</summary>
        private double approachWindowMs => DivaChartConstants.StandingPreemptMs(bpm);

        private double bpm => editorBeatmap.ControlPointInfo.TimingPointAt(HitObject.StartTime).BPM;

        /// <summary>
        ///     Note-local position of the strip's far end. The game draws the body over the fraction of the flight
        ///     the hold lasts for (<c>duration / approach window</c>), so the tail float rides the same curve the
        ///     body does rather than a fixed horizontal offset that means nothing for a diagonal flight.
        /// </summary>
        private Vector2 tailOffset()
            => DivaFlightPath.Sample(flightCurve.Value, flightVector(), (float)(HitObject.Duration / approachWindowMs), (float)(flightAmplitude.Value / 100.0));
    }
}
