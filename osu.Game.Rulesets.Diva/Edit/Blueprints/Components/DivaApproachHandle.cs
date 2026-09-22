// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Configuration;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Diva.Objects.Drawables.Pieces;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Diva.Edit.Blueprints.Components
{
    /// <summary>
    ///     Independent approach-origin handle, drawn in the note's own local space (the blueprint keeps this
    ///     component at the note position, so local coordinates equal ProjectDIVA's note-relative
    ///     <c>nowDistance</c>). Path follows the same <see cref="DivaFlightPath"/> curve as gameplay.
    /// </summary>
    /// <remarks>
    ///     Must not be auto-sized: an <see cref="AutoSizeAxes"/> box only grows towards positive coordinates and
    ///     every child is then offset by half of it, which slides the whole trajectory (and the far handle) away
    ///     from the note — most visibly for notes whose flight starts off to the bottom right, where the measured
    ///     box is as large as the flight distance itself.
    /// </remarks>
    public partial class DivaApproachHandle : CompositeDrawable
    {
        public const float HANDLE_SIZE = 16;
        private const int curve_samples = 24;

        private readonly DivaHitObject hitObject;
        private readonly Circle handle;
        private readonly Path path;
        private readonly Bindable<DivaNoteFlightCurve> flightCurve = new Bindable<DivaNoteFlightCurve>(DivaNoteFlightCurve.DivaNative);
        private readonly BindableDouble flightAmplitude = new BindableDouble(100);

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        public DivaApproachHandle(DivaHitObject hitObject)
        {
            this.hitObject = hitObject;

            Origin = Anchor.TopLeft;

            InternalChildren =
            [
                path = new SmoothPath
                {
                    PathRadius = 1.5f,
                    BypassAutoSizeAxes = Axes.Both,
                },
                handle = new Circle
                {
                    Origin = Anchor.Centre,
                    Size = new Vector2(HANDLE_SIZE),
                }
            ];
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, DivaRulesetConfigManager? config)
        {
            handle.Colour = colours.Yellow;
            path.Colour = colours.Yellow.Opacity(0.8f);
            config?.BindWith(DivaRulesetSettings.FlightCurve, flightCurve);
            config?.BindWith(DivaRulesetSettings.FlightAmplitude, flightAmplitude);
        }

        protected override void Update()
        {
            base.Update();

            // Draw the vector the note will actually fly along rather than the stored one: the game keeps only
            // the stored direction and re-derives the length from the BPM at the note.
            double bpm = editorBeatmap.ControlPointInfo.TimingPointAt(hitObject.StartTime).BPM;
            Vector2 far = DivaActionEncoding.NormaliseApproachOrigin(hitObject.ApproachPieceOriginPosition, bpm);
            float amplitude = (float)(flightAmplitude.Value / 100.0);

            handle.Position = far;
            path.Position = Vector2.Zero;
            path.ClearVertices();

            for (int i = 0; i <= curve_samples; i++)
            {
                float percent = i / (float)curve_samples;
                path.AddVertex(DivaFlightPath.Sample(flightCurve.Value, far, percent, amplitude));
            }
        }

        protected override bool OnMouseDown(MouseDownEvent e)
            => e.Button == MouseButton.Left && handle.ReceivePositionalInputAt(e.ScreenSpaceMousePosition);

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button != MouseButton.Left || !handle.ReceivePositionalInputAt(e.ScreenSpaceMouseDownPosition))
                return false;

            editorBeatmap.BeginChange();
            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            Vector2 local = ToLocalSpace(e.ScreenSpaceMousePosition);

            // Free positioning, no grid snap: ProjectDIVA stores raw pixels here and the game only reads the
            // direction, so snapping would only quantise the angle without making the result more faithful.
            if (local.LengthSquared < 0.25f)
                return;

            double bpm = editorBeatmap.ControlPointInfo.TimingPointAt(hitObject.StartTime).BPM;
            hitObject.ApproachPieceOriginPosition = DivaActionEncoding.NormaliseApproachOrigin(local, bpm);

            editorBeatmap.Update(hitObject);
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            editorBeatmap.EndChange();
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
            => handle.ReceivePositionalInputAt(screenSpacePos);
    }
}
