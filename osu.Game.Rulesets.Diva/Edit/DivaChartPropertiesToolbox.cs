// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit
{
    /// <summary>
    ///     Editor inputs for the <c>.diva</c> header fields lazer cannot derive: ProjectDIVA's difficulty
    ///     slot, the black stars and the minimum chart length. Kept in <see cref="DivaChartHeader"/> on the
    ///     playable beatmap; level and stars are additionally mirrored into the beatmap info.
    /// </summary>
    public partial class DivaChartPropertiesToolbox : EditorToolboxGroup
    {
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        private readonly BindableInt level = new BindableInt(1)
        {
            MinValue = 1,
            MaxValue = DivaChartConstants.LEVEL_NAMES.Length
        };

        private readonly BindableInt hard = new BindableInt(1)
        {
            MinValue = 1,
            MaxValue = 15
        };

        private readonly BindableInt minPeriodCount = new BindableInt(1)
        {
            MinValue = 1,
            MaxValue = DivaChartConstants.MAX_PERIOD_COUNT
        };

        private bool applyingFromModel;

        public DivaChartPropertiesToolbox()
            : base(DivaStrings.EDITOR_CHART_PROPERTIES_GROUP.ToString())
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
                Children =
                [
                    new FormSliderBar<int>
                    {
                        Caption = DivaStrings.EDITOR_CHART_LEVEL,
                        Current = level,
                        TransferValueOnCommit = true,
                        KeyboardStep = 1,
                        LabelFormat = value => DivaChartConstants.LevelName(value)
                    },
                    new FormSliderBar<int>
                    {
                        Caption = DivaStrings.EDITOR_CHART_HARD,
                        Current = hard,
                        TransferValueOnCommit = true,
                        KeyboardStep = 1,
                        LabelFormat = value => $"★{value}"
                    },
                    new FormSliderBar<int>
                    {
                        Caption = DivaStrings.EDITOR_CHART_MIN_PERIODS,
                        Current = minPeriodCount,
                        TransferValueOnCommit = true,
                        KeyboardStep = 1
                    },
                    new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 12))
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Text = DivaStrings.EDITOR_CHART_MIN_PERIODS_TOOLTIP
                    }
                ]
            };

            level.BindValueChanged(_ => applyLevelAndHard());
            hard.BindValueChanged(_ => applyLevelAndHard());
            minPeriodCount.BindValueChanged(_ => applyMinPeriodCount());
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            applyingFromModel = true;

            DivaChartHeader header = DivaBeatmap.GetOrCreateHeader(editorBeatmap);
            level.Value = Math.Clamp(header.Level, (int)level.MinValue, (int)level.MaxValue);
            hard.Value = Math.Clamp(header.Hard, (int)hard.MinValue, (int)hard.MaxValue);
            minPeriodCount.Value = header.MinPeriodCount;

            applyingFromModel = false;
        }

        private void applyLevelAndHard()
        {
            if (applyingFromModel)
                return;

            DivaChartHeader header = DivaBeatmap.GetOrCreateHeader(editorBeatmap);
            header.Level = level.Value;
            header.Hard = hard.Value;

            DivaChartHeader.MirrorToBeatmapInfo(editorBeatmap.BeatmapInfo, header);
        }

        private void applyMinPeriodCount()
        {
            if (applyingFromModel)
                return;

            DivaBeatmap.GetOrCreateHeader(editorBeatmap).MinPeriodCount = minPeriodCount.Value;
        }
    }
}
