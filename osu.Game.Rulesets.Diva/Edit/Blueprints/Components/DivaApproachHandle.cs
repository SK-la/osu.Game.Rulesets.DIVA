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
using osu.Game.Rulesets.Diva.Configuration;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Diva.Objects.Drawables.Pieces;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Diva.Edit.Blueprints.Components
{
    /// <summary>
    ///     Independent approach-origin handle. Kept AutoSize so it does not swallow playfield drags.
    ///     Path follows the same <see cref="DivaFlightPath"/> curve as gameplay.
    /// </summary>
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

        [Resolved]
        private DivaHitObjectComposer? composer { get; set; }

        public DivaApproachHandle(DivaHitObject hitObject)
        {
            this.hitObject = hitObject;

            AutoSizeAxes = Axes.Both;
            Origin = Anchor.Centre;

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

            Vector2 far = hitObject.ApproachPieceOriginPosition;
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

            if (composer != null)
            {
                Vector2 farPlayfield = composer.SnapPlayfieldPosition(hitObject.Position + local);
                hitObject.ApproachPieceOriginPosition = farPlayfield - hitObject.Position;
            }
            else
                hitObject.ApproachPieceOriginPosition = local;

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
