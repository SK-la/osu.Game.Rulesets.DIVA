// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Edit;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Screens.Edit;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Diva.Tests.Editor
{
    [TestFixture]
    public partial class TestSceneDivaSetupSection : EditorClockTestScene
    {
        private EditorBeatmapContainer editorBeatmapContainer = null!;
        private DivaSetupSection section = null!;

        private EditorBeatmap editorBeatmap => editorBeatmapContainer.EditorBeatmap;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create section", () =>
            {
                var beatmap = new DivaBeatmap
                {
                    BeatmapInfo = { Ruleset = new DivaRuleset().RulesetInfo }
                };
                beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

                Beatmap.Value = CreateWorkingBeatmap(beatmap);

                Child = editorBeatmapContainer = new EditorBeatmapContainer(Beatmap.Value)
                {
                    Child = section = new DivaSetupSection()
                };
            });

            AddUntilStep("section loaded", () => section.IsLoaded);
        }

        [Test]
        public void Level_and_stars_reach_the_beatmap_info()
        {
            AddStep("pick the EXTREME slot", () => slider(DivaStrings.EDITOR_CHART_LEVEL).Current.Value = 4);
            AddStep("pick seven stars", () => slider(DivaStrings.EDITOR_CHART_HARD).Current.Value = 7);

            AddAssert("the header carries them", () =>
            {
                DivaChartHeader? header = DivaBeatmap.HeaderOf(editorBeatmap);
                return header is { Level: 4, Hard: 7 };
            });

            // The editor works on its own beatmap info clone, which is what a save writes out.
            AddAssert("the beatmap info follows", () => editorBeatmap.BeatmapInfo.DifficultyName, () => Is.EqualTo("★7 Extra"));
        }

        [Test]
        public void Style_and_overview_picture_round_trip()
        {
            AddStep("type a style and a preview picture name", () =>
            {
                textBox(DivaStrings.EDITOR_CHART_STYLE).Current.Value = "Rin";
                textBox(DivaStrings.EDITOR_CHART_OVERVIEW_PICTURE).Current.Value = "pv_alt.png";
            });

            AddAssert("the header carries both", () =>
            {
                DivaChartHeader? header = DivaBeatmap.HeaderOf(editorBeatmap);
                return header is { Style: "Rin", OverviewPicture: "pv_alt.png" };
            });

            AddStep("clear them again", () =>
            {
                textBox(DivaStrings.EDITOR_CHART_STYLE).Current.Value = string.Empty;
                textBox(DivaStrings.EDITOR_CHART_OVERVIEW_PICTURE).Current.Value = string.Empty;
            });

            AddAssert("empty means the export keeps resolving them", () =>
            {
                DivaChartHeader? header = DivaBeatmap.HeaderOf(editorBeatmap);
                return header is { Style: "", OverviewPicture: "" };
            });
        }

        [Test]
        public void Minimum_measure_count_is_kept()
        {
            AddStep("ask for 40 measures", () => slider(DivaStrings.EDITOR_CHART_MIN_PERIODS).Current.Value = 40);
            AddAssert("the header carries it", () => DivaBeatmap.HeaderOf(editorBeatmap)?.MinPeriodCount, () => Is.EqualTo(40));
        }

        private FormSliderBar<int> slider(LocalisableString caption)
            => section.ChildrenOfType<FormSliderBar<int>>().Single(bar => bar.Caption.ToString() == caption.ToString());

        private FormTextBox textBox(LocalisableString caption)
            => section.ChildrenOfType<FormTextBox>().Single(box => box.Caption.ToString() == caption.ToString());
    }
}
