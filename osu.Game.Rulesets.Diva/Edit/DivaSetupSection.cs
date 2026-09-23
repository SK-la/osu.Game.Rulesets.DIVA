// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Screens.Edit.Setup;

namespace osu.Game.Rulesets.Diva.Edit
{
    /// <summary>
    ///     The per-chart values lazer's own setup sections have no home for: ProjectDIVA's difficulty slot, the
    ///     black stars, the minimum chart length, the music style and the preview picture.
    /// </summary>
    /// <remarks>
    ///     Belongs to the setup screen rather than the compose toolboxes: none of it changes while arranging
    ///     notes, and the picture / audio / video / hit-sound pickers it belongs next to are already there.
    ///     Kept in <see cref="DivaChartHeader"/> on the playable beatmap; level and stars are additionally
    ///     mirrored into the beatmap info so a save keeps them.
    /// </remarks>
    public partial class DivaSetupSection : SetupSection
    {
        public override LocalisableString Title => DivaStrings.EDITOR_CHART_PROPERTIES_GROUP;

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

        private readonly Bindable<string> style = new Bindable<string>(string.Empty);

        private readonly Bindable<string> overviewPicture = new Bindable<string>(string.Empty);

        private bool applyingFromModel;

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new FormSliderBar<int>
                {
                    Caption = DivaStrings.EDITOR_CHART_LEVEL,
                    Current = level,
                    TransferValueOnCommit = true,
                    KeyboardStep = 1,
                    LabelFormat = value => DivaChartConstants.LevelName(value),
                    TabbableContentContainer = this,
                },
                new FormSliderBar<int>
                {
                    Caption = DivaStrings.EDITOR_CHART_HARD,
                    Current = hard,
                    TransferValueOnCommit = true,
                    KeyboardStep = 1,
                    LabelFormat = value => $"★{value}",
                    TabbableContentContainer = this,
                },
                new FormSliderBar<int>
                {
                    Caption = DivaStrings.EDITOR_CHART_MIN_PERIODS,
                    HintText = DivaStrings.EDITOR_CHART_MIN_PERIODS_TOOLTIP,
                    Current = minPeriodCount,
                    TransferValueOnCommit = true,
                    KeyboardStep = 1,
                    TabbableContentContainer = this,
                },
                new FormTextBox
                {
                    Caption = DivaStrings.EDITOR_CHART_STYLE,
                    PlaceholderText = DivaStrings.EDITOR_CHART_INHERITED,
                    Current = style,
                    TabbableContentContainer = this,
                },
                new FormTextBox
                {
                    Caption = DivaStrings.EDITOR_CHART_OVERVIEW_PICTURE,
                    PlaceholderText = DivaStrings.EDITOR_CHART_INHERITED,
                    Current = overviewPicture,
                    TabbableContentContainer = this,
                }
            };

            level.BindValueChanged(_ => applyLevelAndHard());
            hard.BindValueChanged(_ => applyLevelAndHard());
            minPeriodCount.BindValueChanged(_ => applyMinPeriodCount());
            style.BindValueChanged(_ => applyStyle());
            overviewPicture.BindValueChanged(_ => applyOverviewPicture());
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            applyingFromModel = true;

            DivaChartHeader header = DivaBeatmap.GetOrCreateHeader(Beatmap);
            level.Value = Math.Clamp(header.Level, (int)level.MinValue, (int)level.MaxValue);
            hard.Value = Math.Clamp(header.Hard, (int)hard.MinValue, (int)hard.MaxValue);
            minPeriodCount.Value = header.MinPeriodCount;

            // The header is seeded from the beatmap info, so these show the value an export would use today
            // (the source chart's style, the background file) rather than a blank.
            style.Value = header.Style;
            overviewPicture.Value = header.OverviewPicture;

            applyingFromModel = false;
        }

        private void applyLevelAndHard()
        {
            if (applyingFromModel)
                return;

            DivaChartHeader header = DivaBeatmap.GetOrCreateHeader(Beatmap);
            header.Level = level.Value;
            header.Hard = hard.Value;

            DivaChartHeader.MirrorToBeatmapInfo(Beatmap.BeatmapInfo, header);
        }

        private void applyMinPeriodCount()
        {
            if (applyingFromModel)
                return;

            DivaBeatmap.GetOrCreateHeader(Beatmap).MinPeriodCount = minPeriodCount.Value;
        }

        /// <summary>Empty means "keep inferring it", which is what an export does without a header value.</summary>
        private void applyStyle()
        {
            if (applyingFromModel)
                return;

            DivaBeatmap.GetOrCreateHeader(Beatmap).Style = style.Value.Trim();
        }

        private void applyOverviewPicture()
        {
            if (applyingFromModel)
                return;

            DivaBeatmap.GetOrCreateHeader(Beatmap).OverviewPicture = overviewPicture.Value.Trim();
        }
    }
}
