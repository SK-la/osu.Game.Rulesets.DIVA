// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
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
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit
{
    public partial class DivaHitObjectInspector : HitObjectInspector
    {
        private const float integer_precision = 1;
        private const float unlocked_precision = 0.01f;

        private readonly BindableFloat gridX = new BindableFloat
        {
            MinValue = -20,
            MaxValue = 40,
            Precision = integer_precision
        };

        private readonly BindableFloat gridY = new BindableFloat
        {
            MinValue = -20,
            MaxValue = 30,
            Precision = integer_precision
        };

        private readonly BindableFloat approachX = new BindableFloat
        {
            MinValue = -2000,
            MaxValue = 2000,
            Precision = integer_precision
        };

        private readonly BindableFloat approachY = new BindableFloat
        {
            MinValue = -2000,
            MaxValue = 2000,
            Precision = integer_precision
        };

        private readonly BindableDouble duration = new BindableDouble
        {
            MinValue = 0,
            MaxValue = 60000,
            Precision = 1
        };

        /// <summary>
        ///     Hold length in the chart frames the export stores, i.e. the same value the file will hold.
        ///     ProjectDIVA has no sub-frame resolution, so this is the number that survives a save.
        /// </summary>
        private readonly BindableInt holdFrames = new BindableInt
        {
            MinValue = 1,
            MaxValue = 2000
        };

        private readonly BindableInt wavKey = new BindableInt
        {
            MinValue = 0,
            MaxValue = 99
        };

        private FillFlowContainer editors = null!;
        private FormSliderBar<float> gridXSlider = null!;
        private FormSliderBar<float> gridYSlider = null!;
        private FormSliderBar<float> approachXSlider = null!;
        private FormSliderBar<float> approachYSlider = null!;
        private FormSliderBar<double> durationSlider = null!;
        private FormSliderBar<int> holdFramesSlider = null!;
        private FormSliderBar<int> wavKeySlider = null!;

        [Resolved]
        private DivaHitObjectComposer composer { get; set; } = null!;

        private bool applyingFromSelection;

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(editors = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(8),
                Padding = new MarginPadding { Top = 8 },
                Children = new Drawable[]
                {
                    gridXSlider = createFloatSlider(DivaStrings.EDITOR_INSPECTOR_GRID_X, gridX),
                    gridYSlider = createFloatSlider(DivaStrings.EDITOR_INSPECTOR_GRID_Y, gridY),
                    approachXSlider = createFloatSlider(DivaStrings.EDITOR_INSPECTOR_APPROACH_X, approachX),
                    approachYSlider = createFloatSlider(DivaStrings.EDITOR_INSPECTOR_APPROACH_Y, approachY),
                    durationSlider = new FormSliderBar<double>
                    {
                        Caption = DivaStrings.EDITOR_INSPECTOR_DURATION,
                        Current = duration,
                        TransferValueOnCommit = true,
                        KeyboardStep = 50,
                    },
                    holdFramesSlider = new FormSliderBar<int>
                    {
                        Caption = DivaStrings.EDITOR_INSPECTOR_HOLD_FRAMES,
                        Current = holdFrames,
                        TransferValueOnCommit = true,
                        KeyboardStep = 1,
                    },
                    wavKeySlider = new FormSliderBar<int>
                    {
                        Caption = DivaStrings.EDITOR_INSPECTOR_WAV_KEY,
                        Current = wavKey,
                        TransferValueOnCommit = true,
                        KeyboardStep = 1,
                    }
                }
            });

            composer.GridSnapToggle.BindValueChanged(_ =>
            {
                applyPrecision();
                syncFromSelection(EditorBeatmap.SelectedHitObjects.ToArray());
            }, true);

            gridX.BindValueChanged(_ => applyGrid());
            gridY.BindValueChanged(_ => applyGrid());
            approachX.BindValueChanged(_ => applyApproach());
            approachY.BindValueChanged(_ => applyApproach());
            duration.BindValueChanged(_ => applyDuration());
            holdFrames.BindValueChanged(_ => applyHoldFrames());
            wavKey.BindValueChanged(_ => applyWavKey());
        }

        protected override void AddInspectorValues(HitObject[] objects)
        {
            base.AddInspectorValues(objects);
            syncFromSelection(objects);
        }

        private void applyPrecision()
        {
            float precision = composer.GridSnapEnabled ? integer_precision : unlocked_precision;
            float step = composer.GridSnapEnabled ? 1 : 0.01f;

            gridX.Precision = precision;
            gridY.Precision = precision;

            gridXSlider.KeyboardStep = step;
            gridYSlider.KeyboardStep = step;

            // The flight vector is a direction, not a grid position: the game derives its length from the BPM,
            // so rounding it to whole pixels on the note grid would only lose angle resolution.
            approachX.Precision = unlocked_precision;
            approachY.Precision = unlocked_precision;
            approachXSlider.KeyboardStep = 0.01f;
            approachYSlider.KeyboardStep = 0.01f;
        }

        private void syncFromSelection(HitObject[] objects)
        {
            bool single = objects.Length == 1 && objects[0] is DivaHitObject;
            editors.Alpha = single ? 1 : 0;

            if (!single || objects[0] is not DivaHitObject diva)
                return;

            applyingFromSelection = true;

            Vector2 grid = DivaActionEncoding.ToGridPosition(diva.Position);
            gridX.Value = snapInspectorValue(grid.X);
            gridY.Value = snapInspectorValue(grid.Y);

            // Show the vector the note flies along, i.e. after the game's direction-only normalisation.
            Vector2 approach = DivaActionEncoding.NormaliseApproachOrigin(
                diva.ApproachPieceOriginPosition,
                EditorBeatmap.ControlPointInfo.TimingPointAt(diva.StartTime).BPM);

            approachX.Value = roundToPrecision(approach.X);
            approachY.Value = roundToPrecision(approach.Y);

            if (diva is DivaHoldHitObject hold)
            {
                durationSlider.Alpha = 1;
                holdFramesSlider.Alpha = 1;
                duration.Value = hold.Duration;

                // What a save would write, so ms→frames rounding is visible while editing.
                holdFrames.Value = Math.Clamp(DivaChartBuilder.FrameLengthAt(EditorBeatmap, hold.StartTime, hold.Duration),
                    holdFrames.MinValue, holdFrames.MaxValue);
            }
            else
            {
                durationSlider.Alpha = 0;
                holdFramesSlider.Alpha = 0;
            }

            // Shows the value an export would write, i.e. including a key inherited from the source chart.
            wavKey.Value = composer.ResolveWavKey(diva);

            applyingFromSelection = false;
        }

        private float snapInspectorValue(float value)
            => composer.GridSnapEnabled ? MathF.Round(value) : roundToPrecision(value);

        private static float roundToPrecision(float value) => MathF.Round(value / unlocked_precision) * unlocked_precision;

        private void applyGrid()
        {
            if (applyingFromSelection)
                return;

            var selected = EditorBeatmap.SelectedHitObjects.OfType<DivaHitObject>().ToArray();
            if (selected.Length != 1)
                return;

            float x = snapInspectorValue(gridX.Value);
            float y = snapInspectorValue(gridY.Value);
            Vector2 playfield = DivaActionEncoding.ToPlayfieldPosition(x, y);

            if (composer.GridSnapEnabled)
                playfield = DivaActionEncoding.SnapToGrid(playfield);

            if (selected[0].Position == playfield)
                return;

            EditorBeatmap.BeginChange();
            selected[0].Position = playfield;
            EditorBeatmap.Update(selected[0]);
            EditorBeatmap.EndChange();
        }

        private void applyApproach()
        {
            if (applyingFromSelection)
                return;

            var selected = EditorBeatmap.SelectedHitObjects.OfType<DivaHitObject>().ToArray();
            if (selected.Length != 1)
                return;

            var origin = DivaActionEncoding.NormaliseApproachOrigin(
                new Vector2(roundToPrecision(approachX.Value), roundToPrecision(approachY.Value)),
                EditorBeatmap.ControlPointInfo.TimingPointAt(selected[0].StartTime).BPM);

            if (selected[0].ApproachPieceOriginPosition == origin)
                return;

            EditorBeatmap.BeginChange();
            selected[0].ApproachPieceOriginPosition = origin;
            EditorBeatmap.Update(selected[0]);
            EditorBeatmap.EndChange();
        }

        private void applyDuration()
        {
            if (applyingFromSelection)
                return;

            if (EditorBeatmap.SelectedHitObjects.OfType<DivaHoldHitObject>().SingleOrDefault() is not DivaHoldHitObject hold)
                return;

            if (hold.Duration == duration.Value)
                return;

            EditorBeatmap.BeginChange();
            hold.Duration = duration.Value;
            EditorBeatmap.Update(hold);
            EditorBeatmap.EndChange();
        }

        /// <summary>
        ///     Writes the length by chart frame, i.e. the value the file itself stores: the milliseconds are
        ///     derived back from the frame duration in play at the hold, so a save keeps exactly this length.
        /// </summary>
        private void applyHoldFrames()
        {
            if (applyingFromSelection)
                return;

            if (EditorBeatmap.SelectedHitObjects.OfType<DivaHoldHitObject>().SingleOrDefault() is not DivaHoldHitObject hold)
                return;

            double length = holdFrames.Value * DivaChartBuilder.MsPerFrameAt(EditorBeatmap, hold.StartTime);

            if (hold.Duration == length)
                return;

            EditorBeatmap.BeginChange();
            hold.Duration = length;
            EditorBeatmap.Update(hold);
            EditorBeatmap.EndChange();
        }

        private void applyWavKey()        {
            if (applyingFromSelection)
                return;

            var selected = EditorBeatmap.SelectedHitObjects.OfType<DivaHitObject>().ToArray();
            if (selected.Length != 1)
                return;

            if (composer.ResolveWavKey(selected[0]) == wavKey.Value)
                return;

            EditorBeatmap.BeginChange();
            selected[0].WavKey = wavKey.Value;
            EditorBeatmap.Update(selected[0]);
            EditorBeatmap.EndChange();
        }

        private static FormSliderBar<float> createFloatSlider(LocalisableString caption, BindableFloat current) => new FormSliderBar<float>
        {
            Caption = caption,
            Current = current,
            TransferValueOnCommit = true,
            KeyboardStep = 1,
        };
    }
}
