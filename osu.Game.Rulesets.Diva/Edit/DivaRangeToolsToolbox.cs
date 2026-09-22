// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit
{
    /// <summary>
    ///     Frame-interval commands of the reference editor: delete the notes on one frame, delete the notes of a
    ///     range, and copy a range to another frame. Everything is expressed in chart frames (1/192 measure),
    ///     which is the unit those commands take and the unit the export writes.
    /// </summary>
    public partial class DivaRangeToolsToolbox : EditorToolboxGroup
    {
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        private readonly BindableInt fromFrame = new BindableInt { MinValue = 0, MaxValue = DivaChartConstants.MAX_FRAME_INDEX };
        private readonly BindableInt toFrame = new BindableInt(DivaChartConstants.NOTE_PER_PERIOD) { MinValue = 0, MaxValue = DivaChartConstants.MAX_FRAME_INDEX };
        private readonly BindableInt targetFrame = new BindableInt { MinValue = 0, MaxValue = DivaChartConstants.MAX_FRAME_INDEX };

        public DivaRangeToolsToolbox()
            : base(DivaStrings.EDITOR_RANGE_TOOLS_GROUP.ToString())
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(6),
                Children = new Drawable[]
                {
                    createFrameSlider(DivaStrings.EDITOR_RANGE_FROM, fromFrame),
                    createFrameSlider(DivaStrings.EDITOR_RANGE_TO, toFrame),
                    createButton(DivaStrings.EDITOR_DELETE_AT_CURRENT_FRAME, DivaStrings.EDITOR_DELETE_AT_CURRENT_FRAME_TOOLTIP, deleteAtCurrentFrame),
                    createButton(DivaStrings.EDITOR_DELETE_RANGE, DivaStrings.EDITOR_DELETE_RANGE_TOOLTIP, deleteRange),
                    createFrameSlider(DivaStrings.EDITOR_RANGE_TARGET, targetFrame),
                    createButton(DivaStrings.EDITOR_COPY_RANGE, DivaStrings.EDITOR_COPY_RANGE_TOOLTIP, copyRange)
                }
            };
        }

        /// <summary>Removes every note starting on the playhead's frame — the reference editor's Ctrl+Delete.</summary>
        public void DeleteNotesAtCurrentTime() => deleteAtFrame(DivaChartBuilder.TimeToFrame(editorBeatmap, editorClock.CurrentTime));

        private void deleteAtCurrentFrame() => DeleteNotesAtCurrentTime();

        private void deleteAtFrame(int frame)
        {
            List<DivaHitObject> notes = DivaRangeOperations.NotesAtFrame(editorBeatmap, frame);

            if (notes.Count > 0)
                editorBeatmap.RemoveRange(notes);
        }

        private void deleteRange()
        {
            List<DivaHitObject> notes = DivaRangeOperations.NotesInFrameRange(editorBeatmap, fromFrame.Value, toFrame.Value);

            if (notes.Count > 0)
                editorBeatmap.RemoveRange(notes);
        }

        private void copyRange()
        {
            if (DivaRangeOperations.PlanRangeCopy(editorBeatmap, fromFrame.Value, toFrame.Value, targetFrame.Value) is not { } plan)
                return;

            editorBeatmap.BeginChange();

            editorBeatmap.AddRange(plan.Notes);

            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(editorBeatmap);
            events.BgmEvents.AddRange(plan.BgmEvents);
            events.ResourceEvents.AddRange(plan.ResourceEvents);

            editorBeatmap.EndChange();
        }

        private static FormSliderBar<int> createFrameSlider(LocalisableString caption, BindableInt current) => new FormSliderBar<int>
        {
            Caption = caption,
            Current = current,
            TransferValueOnCommit = true,
            KeyboardStep = 1
        };

        private static RoundedButton createButton(LocalisableString text, LocalisableString tooltip, Action action) => new RoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Height = 28,
            Text = text,
            TooltipText = tooltip,
            Action = action
        };
    }
}
