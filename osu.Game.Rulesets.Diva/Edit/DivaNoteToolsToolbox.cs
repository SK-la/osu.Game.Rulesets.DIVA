// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit
{
    /// <summary>
    ///     Note-level batch actions. The reference editor exposes these in its "tools" menu; here they work on
    ///     the current selection, and on the whole chart when nothing is selected.
    /// </summary>
    public partial class DivaNoteToolsToolbox : EditorToolboxGroup
    {
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        public DivaNoteToolsToolbox()
            : base(DivaStrings.EDITOR_NOTE_TOOLS_GROUP.ToString())
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
                    new RoundedButton
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 28,
                        Text = DivaStrings.EDITOR_RESTORE_WAV_KEY,
                        TooltipText = DivaStrings.EDITOR_RESTORE_WAV_KEY_TOOLTIP,
                        Action = restoreWavKeys
                    },
                    new RoundedButton
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 28,
                        Text = DivaStrings.EDITOR_CLEAR_OVERLAPPING_TAPS,
                        TooltipText = DivaStrings.EDITOR_CLEAR_OVERLAPPING_TAPS_TOOLTIP,
                        Action = clearOverlappingTaps
                    }
                ]
            };
        }

        /// <summary>
        ///     Drops the editor's <c>#WAV</c> overrides so the notes fall back to the source chart's slots
        ///     again. Note that a note whose key was inherited is already stored as "not overridden".
        /// </summary>
        private void restoreWavKeys()
        {
            var targets = editorBeatmap.SelectedHitObjects.OfType<DivaHitObject>().ToArray();

            if (targets.Length == 0)
                targets = editorBeatmap.HitObjects.OfType<DivaHitObject>().ToArray();

            var overridden = targets.Where(h => h.WavKey != null).ToArray();
            if (overridden.Length == 0)
                return;

            editorBeatmap.BeginChange();

            foreach (DivaHitObject hitObject in overridden)
            {
                hitObject.WavKey = null;
                editorBeatmap.Update(hitObject);
            }

            editorBeatmap.EndChange();
        }

        /// <summary>
        ///     Drops the taps a hold already covers, which is the one conflict direction that can be resolved
        ///     without a decision from the user: the hold keeps its span, overlapping holds are left alone.
        /// </summary>
        private void clearOverlappingTaps()
        {
            var covered = DivaChartBuilder.FindActionOverlaps(editorBeatmap)
                                           .Select(pair => pair.Covered)
                                           .Where(h => h is not DivaHoldHitObject)
                                           .Distinct()
                                           .ToArray();

            if (covered.Length == 0)
                return;

            editorBeatmap.BeginChange();

            foreach (DivaHitObject hitObject in covered)
                editorBeatmap.Remove(hitObject);

            editorBeatmap.EndChange();
        }
    }
}
