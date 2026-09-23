// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Screens.Edit.Setup;

namespace osu.Game.Rulesets.Diva.Edit
{
    /// <summary>
    ///     The per-chart values lazer's own setup sections have no home for: ProjectDIVA's difficulty slot, the
    ///     black stars, the minimum chart length, the music style, the preview picture and the chart's key sound.
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

        private readonly BindableInt defaultWavKey = new BindableInt
        {
            MinValue = 0,
            MaxValue = 99
        };

        private FormTextBox overviewPictureBox = null!;

        private bool applyingFromModel;

        [Resolved]
        private SetupScreen? setupScreen { get; set; }

        [Resolved]
        private IBindable<WorkingBeatmap> currentWorkingBeatmap { get; set; } = null!;

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
                new FormSliderBar<int>
                {
                    Caption = DivaStrings.EDITOR_CHART_WAV_KEY,
                    HintText = DivaStrings.EDITOR_CHART_WAV_KEY_TOOLTIP,
                    Current = defaultWavKey,
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
                overviewPictureBox = new FormTextBox
                {
                    Caption = DivaStrings.EDITOR_CHART_OVERVIEW_PICTURE,
                    HintText = DivaStrings.EDITOR_CHART_OVERVIEW_PICTURE_TOOLTIP,
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
            defaultWavKey.BindValueChanged(_ => applyWavKey());

            // The picture follows the background chosen in the stock resources section, so it has to refresh when
            // that choice is made rather than only when this screen is next opened.
            if (setupScreen != null)
                setupScreen.BackgroundChanged += SyncOverviewPictureFromBackground;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            applyingFromModel = true;

            DivaChartHeader header = DivaBeatmap.GetOrCreateHeader(Beatmap);
            level.Value = Math.Clamp(header.Level, (int)level.MinValue, (int)level.MaxValue);
            hard.Value = Math.Clamp(header.Hard, (int)hard.MinValue, (int)hard.MaxValue);
            minPeriodCount.Value = header.MinPeriodCount;
            defaultWavKey.Value = header.DefaultWavKey ?? 0;

            // The header is seeded from the beatmap info, so these show the value an export would use today
            // (the source chart's style, the background file) rather than a blank.
            style.Value = header.Style;
            SyncOverviewPictureFromBackground();

            applyingFromModel = false;
        }

        /// <summary>
        ///     Shows the picture an export would use right now: an explicit filename, else the background the
        ///     settings screen has chosen.
        /// </summary>
        /// <remarks>
        ///     Showing the background rather than a blank is only that — a display. The header is left empty while
        ///     there is no explicit filename, which is what keeps the export resolving the background at export
        ///     time. Writing what is shown back into the header would freeze today's background into the chart.
        /// </remarks>
        public void SyncOverviewPictureFromBackground()
        {
            string declared = DivaBeatmap.HeaderOf(Beatmap)?.OverviewPicture ?? string.Empty;
            string background = currentWorkingBeatmap.Value.Metadata.BackgroundFile;

            string shown = !string.IsNullOrWhiteSpace(declared) ? declared
                : !string.IsNullOrWhiteSpace(background) ? Path.GetFileName(background)
                : string.Empty;

            applyingFromModel = true;
            overviewPicture.Value = shown;
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

        /// <summary>
        ///     Remembers the key sound for notes placed from now on and stamps it onto the current selection, so the
        ///     one control covers both "what the rest of the chart will use" and "fix what is already written".
        /// </summary>
        private void applyWavKey()
        {
            if (applyingFromModel)
                return;

            DivaBeatmap.GetOrCreateHeader(Beatmap).DefaultWavKey = defaultWavKey.Value;

            var selected = Beatmap.SelectedHitObjects.OfType<DivaHitObject>().Where(h => h.WavKey != defaultWavKey.Value).ToArray();

            if (selected.Length == 0)
                return;

            Beatmap.BeginChange();

            foreach (DivaHitObject hitObject in selected)
            {
                hitObject.WavKey = defaultWavKey.Value;
                Beatmap.Update(hitObject);
            }

            Beatmap.EndChange();
        }
    }
}
