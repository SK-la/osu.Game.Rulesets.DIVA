// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
    ///     Whole-chart batch edits: the button remaps and the frame nudges of the reference editor's "tools"
    ///     menu. Every entry here says "all" and rewrites the entire chart, so a nudge that would leave the
    ///     chart's frame range is refused rather than silently dropping notes.
    /// </summary>
    public partial class DivaBatchToolsToolbox : EditorToolboxGroup
    {
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        private readonly BindableInt nudgeFrames = new BindableInt
        {
            MinValue = -DivaChartConstants.NOTE_PER_PERIOD,
            MaxValue = DivaChartConstants.NOTE_PER_PERIOD
        };

        public DivaBatchToolsToolbox()
            : base(DivaStrings.EDITOR_BATCH_TOOLS_GROUP.ToString())
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
                Spacing = new Vector2(5),
                Children =
                [
                    createSimplifyButton(DivaStrings.EDITOR_SIMPLIFY_CROSS_CIRCLE_DOWN_RIGHT, DivaStrings.EDITOR_SIMPLIFY_CROSS_CIRCLE_DOWN_RIGHT_TOOLTIP,
                        DivaActionSimplification.ToCrossCircleDownRight),
                    createSimplifyButton(DivaStrings.EDITOR_SIMPLIFY_CIRCLE_RIGHT, DivaStrings.EDITOR_SIMPLIFY_CIRCLE_RIGHT_TOOLTIP,
                        DivaActionSimplification.ToCircleAndRight),
                    createSimplifyButton(DivaStrings.EDITOR_SIMPLIFY_ARROWS_TO_SYMBOLS, DivaStrings.EDITOR_SIMPLIFY_ARROWS_TO_SYMBOLS_TOOLTIP,
                        DivaActionSimplification.ArrowToSymbol),
                    createSimplifyButton(DivaStrings.EDITOR_SIMPLIFY_SYMBOLS_TO_ARROWS, DivaStrings.EDITOR_SIMPLIFY_SYMBOLS_TO_ARROWS_TOOLTIP,
                        DivaActionSimplification.SymbolToArrow),
                    new FormSliderBar<int>
                    {
                        Caption = DivaStrings.EDITOR_NUDGE_FRAMES,
                        Current = nudgeFrames,
                        TransferValueOnCommit = true,
                        KeyboardStep = 1
                    },
                    new RoundedButton
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 28,
                        Text = DivaStrings.EDITOR_NUDGE_NOTES,
                        TooltipText = DivaStrings.EDITOR_NUDGE_NOTES_TOOLTIP,
                        Action = nudgeNotes
                    }
                ]
            };
        }

        private RoundedButton createSimplifyButton(LocalisableString text, LocalisableString tooltip, Func<DivaAction, DivaAction> remap) => new RoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Height = 28,
            Text = text,
            TooltipText = tooltip,
            Action = () => applySimplify(remap)
        };

        private void applySimplify(Func<DivaAction, DivaAction> remap)
        {
            DivaActionSimplifyPlan plan = DivaActionSimplification.PlanActionSimplify(editorBeatmap, remap);

            if (plan.Changes.Count == 0 && plan.Removals.Count == 0)
                return;

            editorBeatmap.BeginChange();

            foreach (DivaHitObject note in plan.Removals)
                editorBeatmap.Remove(note);

            foreach ((DivaHitObject note, DivaAction action) in plan.Changes)
            {
                note.ValidAction = action;
                editorBeatmap.Update(note);
            }

            editorBeatmap.EndChange();
        }

        private void nudgeNotes()
        {
            if (DivaChartBuilder.PlanTimeNudge(editorBeatmap, nudgeFrames.Value) is not { } nudge)
                return;

            editorBeatmap.BeginChange();

            foreach ((DivaHitObject note, double time) in nudge.Notes)
            {
                note.StartTime = time;
                editorBeatmap.Update(note);
            }

            editorBeatmap.EndChange();
        }
    }
}
