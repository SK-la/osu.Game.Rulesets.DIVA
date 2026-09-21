// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Diva.Edit.Blueprints.Components
{
    public partial class DivaApproachHandle : CompositeDrawable
    {
        private readonly DivaHitObject hitObject;
        private readonly Circle handle;
        private readonly Path path;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        public DivaApproachHandle(DivaHitObject hitObject)
        {
            this.hitObject = hitObject;

            RelativeSizeAxes = Axes.Both;

            InternalChildren =
            [
                path = new SmoothPath
                {
                    PathRadius = 1.5f,
                },
                handle = new Circle
                {
                    Origin = Anchor.Centre,
                    Size = new Vector2(16),
                }
            ];
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            handle.Colour = colours.Yellow;
            path.Colour = colours.Yellow.Opacity(0.8f);
        }

        protected override void Update()
        {
            base.Update();

            Vector2 origin = hitObject.Position;
            Vector2 far = origin + hitObject.ApproachPieceOriginPosition;

            handle.Position = far;
            path.Position = Vector2.Zero;
            path.ClearVertices();
            path.AddVertex(origin);
            path.AddVertex(far);
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button != MouseButton.Left)
                return false;

            editorBeatmap.BeginChange();
            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            hitObject.ApproachPieceOriginPosition = ToLocalSpace(e.ScreenSpaceMousePosition) - hitObject.Position;
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
