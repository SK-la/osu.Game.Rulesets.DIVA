// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Edit;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.RadioButtons;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Diva.Tests.Editor
{
    [TestFixture]
    public partial class TestSceneDivaHitObjectComposer : EditorClockTestScene
    {
        private EditorBeatmapContainer editorBeatmapContainer = null!;
        private DivaHitObjectComposer composer = null!;

        private EditorBeatmap editorBeatmap => editorBeatmapContainer.EditorBeatmap;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create composer", () =>
            {
                var beatmap = new DivaBeatmap
                {
                    BeatmapInfo = { Ruleset = new DivaRuleset().RulesetInfo }
                };
                beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

                Beatmap.Value = CreateWorkingBeatmap(beatmap);

                Child = editorBeatmapContainer = new EditorBeatmapContainer(Beatmap.Value)
                {
                    Child = composer = new DivaHitObjectComposer(new DivaRuleset())
                };
            });

            AddUntilStep("composer loaded", () => composer.IsLoaded);
        }

        [Test]
        public void Composer_opens_instead_of_under_construction()
        {
            AddAssert("composer created", () => new DivaRuleset().CreateHitObjectComposer(), () => Is.Not.Null);
            AddAssert("playfield present", () => composer.Playfield, () => Is.Not.Null);
            AddAssert("no hit objects yet", () => editorBeatmap.HitObjects, () => Is.Empty);
        }

        [Test]
        public void Place_tap_snaps_time_grid_and_action()
        {
            AddStep("seek to 1000", () => EditorClock.Seek(1000));
            AddStep("select tap tool", () => selectTool(DivaStrings.EDITOR_TAP_TOOL.ToString()));
            AddStep("move to playfield centre", () => InputManager.MoveMouseTo(composer.Playfield.ScreenSpaceDrawQuad.Centre));
            AddStep("place", () => InputManager.Click(MouseButton.Left));

            AddAssert("one tap placed", () => editorBeatmap.HitObjects.Count, () => Is.EqualTo(1));
            AddAssert("is tap", () => editorBeatmap.HitObjects.Single(), () => Is.TypeOf<DivaHitObject>());
            AddAssert("not hold", () => editorBeatmap.HitObjects.Single(), () => Is.Not.TypeOf<DivaHoldHitObject>());
            AddAssert("time snapped to beat", () => editorBeatmap.HitObjects.Single().StartTime, () => Is.EqualTo(1000));
            AddAssert("action is current", () => ((DivaHitObject)editorBeatmap.HitObjects.Single()).ValidAction, () => Is.EqualTo(DivaAction.Circle));
            AddAssert("grid is integer", () =>
            {
                Vector2 grid = DivaActionEncoding.ToGridPosition(((DivaHitObject)editorBeatmap.HitObjects.Single()).Position);
                return isIntegerCell(grid.X) && isIntegerCell(grid.Y);
            });
            AddAssert("has approach", () => ((DivaHitObject)editorBeatmap.HitObjects.Single()).ApproachPieceOriginPosition.Length, () => Is.GreaterThan(0));
        }

        [Test]
        public void Place_hold_has_positive_duration()
        {
            AddStep("seek to 0", () => EditorClock.Seek(0));
            AddStep("select hold tool", () => selectTool(DivaStrings.EDITOR_HOLD_TOOL.ToString()));
            AddStep("move to playfield centre", () => InputManager.MoveMouseTo(composer.Playfield.ScreenSpaceDrawQuad.Centre));
            AddStep("commit head", () => InputManager.Click(MouseButton.Left));
            AddStep("seek one beat", () => EditorClock.Seek(500));
            AddStep("commit tail", () => InputManager.Click(MouseButton.Left));

            AddAssert("one hold placed", () => editorBeatmap.HitObjects.OfType<DivaHoldHitObject>().Count(), () => Is.EqualTo(1));
            AddAssert("duration at least one beat", () => editorBeatmap.HitObjects.OfType<DivaHoldHitObject>().Single().Duration, () => Is.GreaterThanOrEqualTo(500));
        }

        [Test]
        public void Multi_place_keeps_same_time_notes()
        {
            AddStep("seek to 1000", () => EditorClock.Seek(1000));
            AddStep("select tap tool", () => selectTool(DivaStrings.EDITOR_TAP_TOOL.ToString()));
            AddStep("place first", () =>
            {
                InputManager.MoveMouseTo(composer.Playfield.ScreenSpaceDrawQuad.Centre);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("place second offset", () =>
            {
                InputManager.MoveMouseTo(composer.Playfield.ToScreenSpace(composer.Playfield.ToLocalSpace(composer.Playfield.ScreenSpaceDrawQuad.Centre) + new Vector2(48, 0)));
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("two notes kept", () => editorBeatmap.HitObjects.Count, () => Is.EqualTo(2));
            AddAssert("same start time", () => editorBeatmap.HitObjects.All(h => h.StartTime == 1000));
        }

        [Test]
        public void Snap_keeps_grid_round_trip_stable()
        {
            AddStep("add off-grid note", () =>
            {
                editorBeatmap.Add(new DivaHitObject
                {
                    StartTime = 0,
                    Position = DivaActionEncoding.ToPlayfieldPosition(8, 8) + new Vector2(7, 5),
                    ValidAction = DivaAction.Circle,
                    ApproachPieceOriginPosition = new Vector2(500, 0),
                });
            });

            AddStep("snap through composer", () =>
            {
                var note = (DivaHitObject)editorBeatmap.HitObjects.Single();
                note.Position = composer.SnapPlayfieldPosition(note.Position);
            });

            AddAssert("round trip is integer grid", () =>
            {
                var note = (DivaHitObject)editorBeatmap.HitObjects.Single();
                Vector2 grid = DivaActionEncoding.ToGridPosition(note.Position);
                Vector2 roundTripped = DivaActionEncoding.ToPlayfieldPosition(grid.X, grid.Y);
                return isIntegerCell(grid.X)
                       && isIntegerCell(grid.Y)
                       && Vector2.Distance(note.Position, roundTripped) < 0.01f;
            });
        }

        private void selectTool(string name)
            => composer.ChildrenOfType<EditorRadioButton>().First(b => b.Text.ToString() == name).TriggerClick();

        private static bool isIntegerCell(float value) => MathF.Abs(value - MathF.Round(value)) < 1e-3f;

        private partial class EditorBeatmapContainer : PopoverContainer
        {
            private readonly IWorkingBeatmap working;

            public EditorBeatmap EditorBeatmap { get; private set; } = null!;

            public EditorBeatmapContainer(IWorkingBeatmap working)
            {
                this.working = working;
                RelativeSizeAxes = Axes.Both;
            }

            protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            {
                var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

                EditorBeatmap = new EditorBeatmap(working.GetPlayableBeatmap(new DivaRuleset().RulesetInfo));
                dependencies.CacheAs(EditorBeatmap);
                dependencies.CacheAs<IBeatSnapProvider>(EditorBeatmap);

                return dependencies;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Add(EditorBeatmap);
            }
        }
    }
}
