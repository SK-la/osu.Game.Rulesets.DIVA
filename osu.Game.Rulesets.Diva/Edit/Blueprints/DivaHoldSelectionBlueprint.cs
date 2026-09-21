// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Edit.Blueprints.Components;
using osu.Game.Rulesets.Diva.Objects;
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
        private void load(OsuColour colours)
        {
            durationHandle.Colour = colours.YellowDark;
        }

        protected override void Update()
        {
            base.Update();

            piece.UpdateFrom(HitObject, textures);
            approachHandle.Alpha = IsSelected ? 1 : 0;
            durationHandle.Alpha = IsSelected ? 1 : 0;

            double beatLength = editorBeatmap.GetBeatLengthAtTime(HitObject.StartTime);
            float cells = beatLength > 0 ? (float)(HitObject.Duration / beatLength) : 0;
            durationHandle.Position = HitObject.Position + new Vector2(cells * DivaChartConstants.DELTA_X, 0);
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (!IsSelected || e.Button != MouseButton.Left || !durationHandle.ReceivePositionalInputAt(e.ScreenSpaceMousePosition))
                return false;

            editorBeatmap.BeginChange();
            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            if (composer == null)
                return;

            double beatLength = editorBeatmap.GetBeatLengthAtTime(HitObject.StartTime);
            if (beatLength <= 0)
                return;

            Vector2 local = composer.Playfield.ToLocalSpace(e.ScreenSpaceMousePosition);
            float cells = Math.Max(1, (local.X - HitObject.Position.X) / DivaChartConstants.DELTA_X);
            double proposedEnd = HitObject.StartTime + cells * beatLength;
            double snappedEnd = Math.Max(HitObject.StartTime + beatLength, editorBeatmap.SnapTime(proposedEnd, HitObject.StartTime));

            HitObject.Duration = snappedEnd - HitObject.StartTime;
            editorBeatmap.Update(HitObject);
        }

        protected override void OnDragEnd(DragEndEvent e) => editorBeatmap.EndChange();

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
            => piece.ReceivePositionalInputAt(screenSpacePos)
               || (IsSelected && (approachHandle.ReceivePositionalInputAt(screenSpacePos) || durationHandle.ReceivePositionalInputAt(screenSpacePos)));

        public override Quad SelectionQuad => piece.ScreenSpaceDrawQuad;
    }
}
