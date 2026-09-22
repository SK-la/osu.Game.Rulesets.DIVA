// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Edit;
using osu.Game.Rulesets.Diva.Edit.Blueprints.Components;
using osu.Game.Rulesets.Diva.Edit.Tools;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Graphics.UserInterface;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.RadioButtons;
using osu.Game.Screens.Edit.Components.TernaryButtons;
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
            AddAssert("is tap", () => editorBeatmap.HitObjects.Single(), Is.TypeOf<DivaHitObject>);
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

        [TestCase(400f, 0f)]
        [TestCase(-400f, 120f)]
        [TestCase(0f, -300f)]
        public void Approach_handle_matches_the_flight_geometry(float approachX, float approachY)
        {
            addApproachNote(approachX, approachY);

            AddAssert("far handle sits on the flight start", farHandleDistance, () => Is.LessThan(1.5f));
            AddAssert("path origin sits on the note", pathOriginDistance, () => Is.LessThan(1.5f));
        }

        [TestCase(-1d)]
        [TestCase(1d)]
        public void Playfield_zoom_keeps_blueprints_aligned(double zoom)
        {
            addApproachNote(400, 0);

            float widthBeforeZoom = 0;

            AddStep("remember field size", () => widthBeforeZoom = composer.Playfield.ScreenSpaceDrawQuad.Width);
            AddStep($"zoom to 10^{zoom}", () => composer.PlayfieldZoom.Value = zoom);
            AddUntilStep("field resized", () => Math.Abs(composer.Playfield.ScreenSpaceDrawQuad.Width - widthBeforeZoom) > 1);

            // Blueprints are drawn in their own adjustment container, so a zoom that only reaches the playfield
            // would leave the handle (and every dragged note) at the unscaled position.
            AddAssert("far handle follows the zoomed field", farHandleDistance, () => Is.LessThan(1.5f));
            AddAssert("path origin follows the zoomed field", pathOriginDistance, () => Is.LessThan(1.5f));
        }

        [Test]
        public void Alt_scroll_over_the_playfield_zooms_the_view()
        {
            addApproachNote(400, 0);

            AddStep("move mouse onto the play area", () => InputManager.MoveMouseTo(composer.Playfield));
            AddStep("hold alt", () => InputManager.PressKey(Key.LAlt));
            AddStep("scroll up", () => InputManager.ScrollVerticalBy(1));
            AddUntilStep("zoomed in", () => composer.PlayfieldZoom.Value, () => Is.GreaterThan(0));

            AddStep("scroll down", () => InputManager.ScrollVerticalBy(-1));
            AddUntilStep("back to 1x", () => composer.PlayfieldZoom.Value, () => Is.EqualTo(0).Within(0.001));

            AddStep("release alt", () => InputManager.ReleaseKey(Key.LAlt));
            AddStep("scroll down without alt", () => InputManager.ScrollVerticalBy(-1));
            AddAssert("scroll without alt leaves zoom alone", () => composer.PlayfieldZoom.Value, () => Is.EqualTo(0).Within(0.001));
        }

        private DivaHitObject approachNote = null!;
        private DivaApproachHandle approachHandle = null!;

        private void addApproachNote(float approachX, float approachY)
        {
            DivaApproachHandle? handle = null;

            AddStep("add selected note with an approach origin", () =>
            {
                approachNote = new DivaHitObject
                {
                    StartTime = 1000,
                    Position = DivaActionEncoding.ToPlayfieldPosition(5, 5),
                    ValidAction = DivaAction.Circle,
                    ApproachPieceOriginPosition = new Vector2(approachX, approachY),
                };

                editorBeatmap.Add(approachNote);
                editorBeatmap.SelectedHitObjects.Add(approachNote);
            });

            AddUntilStep("approach handle drawn", () => (handle = this.ChildrenOfType<DivaApproachHandle>().SingleOrDefault()) != null);
            AddStep("keep handle", () => approachHandle = handle!);
        }

        [Test]
        public void Dragging_the_handle_sets_the_direction_without_snapping_or_lengthening()
        {
            addApproachNote(400, 0);

            // The drag target is deliberately off the note grid and further away than the note flies.
            var dragDelta = new Vector2(310, 400);
            Vector2 expected = DivaActionEncoding.NormaliseApproachOrigin(dragDelta, 120);

            AddStep("drag the handle there", () =>
            {
                Vector2 grab = approachNote.Position + DivaActionEncoding.NormaliseApproachOrigin(new Vector2(400, 0), 120);

                InputManager.MoveMouseTo(composer.Playfield.ToScreenSpace(grab));
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(composer.Playfield.ToScreenSpace(approachNote.Position + dragDelta));
                InputManager.ReleaseButton(MouseButton.Left);
            });

            AddAssert("vector is the dragged direction at the BPM flight distance", () =>
                Vector2.Distance(approachNote.ApproachPieceOriginPosition, expected) < 0.05f);
        }

        [Test]
        public void Note_tools_clear_taps_covered_by_a_hold()
        {
            DivaHoldHitObject? hold = null;
            DivaHitObject? covered = null;

            AddStep("add a hold and a tap inside it", () =>
            {
                hold = new DivaHoldHitObject
                {
                    StartTime = 0,
                    Duration = 500,
                    Position = DivaActionEncoding.ToPlayfieldPosition(2, 12),
                    ValidAction = DivaAction.Circle
                };

                covered = new DivaHitObject
                {
                    StartTime = 250,
                    Position = DivaActionEncoding.ToPlayfieldPosition(3, 12),
                    ValidAction = DivaAction.Circle
                };

                editorBeatmap.Add(hold);
                editorBeatmap.Add(covered);
            });

            AddStep("press the cleanup button", () => noteToolButton(DivaStrings.EDITOR_CLEAR_OVERLAPPING_TAPS.ToString()).TriggerClick());

            AddAssert("only the hold is left", () => editorBeatmap.HitObjects, () => Is.EqualTo(new[] { hold }));
        }

        [Test]
        public void Selecting_a_note_does_not_rewrite_its_action()
        {
            AddStep("add a square note", () => editorBeatmap.Add(new DivaHitObject
            {
                StartTime = 500,
                Position = DivaActionEncoding.ToPlayfieldPosition(4, 12),
                ValidAction = DivaAction.Square
            }));

            AddStep("select it while the tool still places circles", () => editorBeatmap.SelectedHitObjects.Add(editorBeatmap.HitObjects.Single()));

            AddAssert("note keeps its button", () => ((DivaHitObject)editorBeatmap.HitObjects.Single()).ValidAction, () => Is.EqualTo(DivaAction.Square));
        }

        [Test]
        public void Note_tools_convert_selected_notes_between_tap_and_hold()
        {
            AddStep("add a selected tap", () =>
            {
                var tap = new DivaHitObject
                {
                    StartTime = 500,
                    Position = DivaActionEncoding.ToPlayfieldPosition(4, 12),
                    ValidAction = DivaAction.Square
                };

                editorBeatmap.Add(tap);
                editorBeatmap.SelectedHitObjects.Add(tap);
            });

            AddStep("convert to hold", () => noteToolButton(DivaStrings.EDITOR_CONVERT_TO_HOLD.ToString()).TriggerClick());

            AddAssert("hold keeps the note and takes one beat", () =>
            {
                var hold = editorBeatmap.HitObjects.OfType<DivaHoldHitObject>().Single();

                // One beat at 120 BPM is 500 ms, i.e. exactly 48 frames: the length survives the export.
                return hold.StartTime == 500
                       && hold.ValidAction == DivaAction.Square
                       && Math.Abs(hold.Duration - 500) < 0.01
                       && DivaChartBuilder.FrameLengthAt(editorBeatmap, hold.StartTime, hold.Duration) == 48;
            });

            AddAssert("the replacement is selected", () => editorBeatmap.SelectedHitObjects.Single(), () => Is.TypeOf<DivaHoldHitObject>());

            AddStep("convert back to tap", () => noteToolButton(DivaStrings.EDITOR_CONVERT_TO_TAP.ToString()).TriggerClick());

            AddAssert("tap is back where it was", () =>
            {
                var converted = editorBeatmap.HitObjects.Single();

                return converted is not DivaHoldHitObject
                       && converted.StartTime == 500
                       && ((DivaHitObject)converted).ValidAction == DivaAction.Square;
            });
        }

        [Test]
        public void Batch_tools_simplify_the_chart_and_nudge_note_times()
        {
            AddStep("add a square and a circle on one frame", () =>
            {
                var square = new DivaHitObject
                {
                    StartTime = 1000,
                    Position = DivaActionEncoding.ToPlayfieldPosition(4, 12),
                    ValidAction = DivaAction.Square
                };

                var circle = new DivaHitObject
                {
                    StartTime = 1000,
                    Position = DivaActionEncoding.ToPlayfieldPosition(6, 12),
                    ValidAction = DivaAction.Circle
                };

                editorBeatmap.Add(square);
                editorBeatmap.Add(circle);
            });

            AddStep("simplify to ✕◯↓→", () => batchToolButton(DivaStrings.EDITOR_SIMPLIFY_CROSS_CIRCLE_DOWN_RIGHT.ToString()).TriggerClick());

            AddAssert("the □ collapsed onto the ◯ that was already there", () =>
            {
                var remaining = editorBeatmap.HitObjects.OfType<DivaHitObject>().ToArray();

                // □ and ◯ share a frame and a button after the rewrite, and a frame holds one slot per button.
                return remaining.Length == 1
                       && remaining[0].ValidAction == DivaAction.Circle
                       && Math.Abs(remaining[0].Position.X - DivaActionEncoding.ToPlayfieldPosition(6, 12).X) < 0.001f;
            });

            AddStep("nudge one frame later", () =>
            {
                batchToolBox().ChildrenOfType<FormSliderBar<int>>().Single().Current.Value = 1;
                batchToolButton(DivaStrings.EDITOR_NUDGE_NOTES.ToString()).TriggerClick();
            });

            AddAssert("the note moved one chart frame", () =>
            {
                double expected = DivaChartBuilder.FrameToTime(editorBeatmap, DivaChartBuilder.TimeToFrame(editorBeatmap, 1000) + 1);
                return Math.Abs(editorBeatmap.HitObjects.Single().StartTime - expected) < 0.01;
            });

            AddStep("nudge back onto the frame it started on", () =>
            {
                batchToolBox().ChildrenOfType<FormSliderBar<int>>().Single().Current.Value = -1;
                batchToolButton(DivaStrings.EDITOR_NUDGE_NOTES.ToString()).TriggerClick();
            });

            AddAssert("the note is back", () => editorBeatmap.HitObjects.Single().StartTime, () => Is.EqualTo(1000).Within(0.01));

            AddStep("move the note onto the first frame", () =>
            {
                var note = (DivaHitObject)editorBeatmap.HitObjects.Single();
                note.StartTime = 0;
                editorBeatmap.Update(note);
            });

            AddStep("nudge before the chart start", () =>
            {
                batchToolBox().ChildrenOfType<FormSliderBar<int>>().Single().Current.Value = -1;
                batchToolButton(DivaStrings.EDITOR_NUDGE_NOTES.ToString()).TriggerClick();
            });

            AddAssert("the nudge was refused", () => editorBeatmap.HitObjects.Single().StartTime, () => Is.EqualTo(0).Within(0.01));
        }

        private DivaBatchToolsToolbox batchToolBox() => composer.ChildrenOfType<DivaBatchToolsToolbox>().Single();

        private RoundedButton batchToolButton(string text)
            => batchToolBox().ChildrenOfType<RoundedButton>()
                            .Single(button => button.Text.ToString() == text);

        private RoundedButton noteToolButton(string text)
            => composer.ChildrenOfType<DivaNoteToolsToolbox>().Single()
                       .ChildrenOfType<RoundedButton>()
                       .Single(button => button.Text.ToString() == text);

        /// <summary>Screen-space distance from the far handle to the point gameplay spawns the flying piece at.</summary>
        private float farHandleDistance()
        {
            // The handle draws the vector the note actually flies along: the game (and the export) keep only
            // the stored direction and re-derive the length from the BPM.
            double bpm = editorBeatmap.ControlPointInfo.TimingPointAt(approachNote.StartTime).BPM;
            Vector2 flight = DivaActionEncoding.NormaliseApproachOrigin(approachNote.ApproachPieceOriginPosition, bpm);
            Vector2 flightStart = composer.Playfield.ToScreenSpace(approachNote.Position + flight);
            return Vector2.Distance(approachHandle.ChildrenOfType<Circle>().Single().ScreenSpaceDrawQuad.Centre, flightStart);
        }

        /// <summary>Screen-space distance from the drawn path's note end to the note itself.</summary>
        private float pathOriginDistance()
        {
            var path = approachHandle.ChildrenOfType<SmoothPath>().Single();
            return Vector2.Distance(approachHandle.ToScreenSpace(path.Vertices[0]), composer.Playfield.ToScreenSpace(approachNote.Position));
        }

        [Test]
        public void Editor_bindings_cover_wasd_and_family_toggle()
        {
            AddAssert("W/A/S/D pick a direction", () =>
                isBound(DivaAction.EditorButtonUp, InputKey.W)
                && isBound(DivaAction.EditorButtonLeft, InputKey.A)
                && isBound(DivaAction.EditorButtonDown, InputKey.S)
                && isBound(DivaAction.EditorButtonRight, InputKey.D));

            AddAssert("5 toggles the family", () => isBound(DivaAction.EditorToggleButtonFamily, InputKey.Number5));
            AddAssert("X toggles tap/hold placement", () => isBound(DivaAction.EditorToggleHoldTool, InputKey.X));
        }

        [Test]
        public void Hold_placement_toggle_follows_and_drives_the_toolbar()
        {
            AddStep("select tap tool", () => selectTool(DivaStrings.EDITOR_TAP_TOOL.ToString()));
            AddAssert("toggle reads tap", () => holdPlacementButton().Current.Value, () => Is.EqualTo(TernaryState.False));

            AddStep("press the hold toggle", () => holdPlacementButton().TriggerClick());
            AddUntilStep("hold tool active", () => composer.BlueprintContainer.CurrentTool, () => Is.TypeOf<DivaHoldCompositionTool>());
            AddAssert("toolbar follows the toggle", () => holdPlacementButton().Current.Value, () => Is.EqualTo(TernaryState.True));
            AddAssert("hold radio highlighted", () => toolButton(DivaStrings.EDITOR_HOLD_TOOL.ToString()).Selected.Value, () => Is.True);
            AddAssert("tap radio cleared", () => toolButton(DivaStrings.EDITOR_TAP_TOOL.ToString()).Selected.Value, () => Is.False);

            AddAssert("note button untouched", () => composer.CurrentAction, () => Is.EqualTo(DivaAction.Circle));

            AddStep("press the toggle again", () => holdPlacementButton().TriggerClick());
            AddUntilStep("tap tool active", () => composer.BlueprintContainer.CurrentTool, () => Is.TypeOf<DivaTapCompositionTool>());
            AddAssert("toggle reads tap again", () => holdPlacementButton().Current.Value, () => Is.EqualTo(TernaryState.False));

            AddStep("click the hold radio directly", () => selectTool(DivaStrings.EDITOR_HOLD_TOOL.ToString()));
            AddUntilStep("toggle mirrors the radio", () => holdPlacementButton().Current.Value, () => Is.EqualTo(TernaryState.True));
        }

        [Test]
        public void Note_buttons_are_four_rows_of_two_columns()
        {
            AddUntilStep("eight note buttons laid out in two columns", () =>
            {
                var buttons = noteButtons();

                if (buttons.Length != 8)
                    return false;

                // Rows and columns only need to be distinguishable, so round away sub-pixel drift.
                static int bucket(float value) => (int)MathF.Round(value / 5f);

                var rows = buttons.GroupBy(b => bucket(b.ScreenSpaceDrawQuad.Centre.Y)).ToArray();
                var columns = buttons.Select(b => bucket(b.ScreenSpaceDrawQuad.Centre.X)).Distinct().ToArray();

                return rows.Length == 4
                       && rows.All(r => r.Count() == 2)
                       && columns.Length == 2;
            });
        }

        [Test]
        public void Note_buttons_use_the_arrow_and_symbol_glyphs()
        {
            AddUntilStep("each button shows its own note glyph", () =>
            {
                var glyphs = noteButtons().Select(glyphOf).ToArray();

                // These are the same strings the key binding settings caption each action with.
                var expected = DivaNoteToggleGrid.NoteActions.Select(a => a.GetLocalisableDescription().ToString()).ToArray();

                return glyphs.Length == 8
                       && glyphs.All(g => !string.IsNullOrEmpty(g))
                       && glyphs.Distinct().Count() == 8
                       && glyphs.OrderBy(g => g).SequenceEqual(expected.OrderBy(g => g));
            });
        }

        [Test]
        public void Direction_keys_follow_the_arrow_or_symbol_family()
        {
            AddAssert("starts on a symbol", () => composer.CurrentAction, () => Is.EqualTo(DivaAction.Circle));

            AddStep("press W", () => grid().HandleAction(DivaAction.EditorButtonUp));
            AddAssert("triangle picked", () => composer.CurrentAction, () => Is.EqualTo(DivaAction.Triangle));

            AddStep("press 5", () => grid().HandleAction(DivaAction.EditorToggleButtonFamily));
            AddAssert("family swap keeps the row", () => composer.CurrentAction, () => Is.EqualTo(DivaAction.Up));

            AddStep("press S", () => grid().HandleAction(DivaAction.EditorButtonDown));
            AddAssert("down picked", () => composer.CurrentAction, () => Is.EqualTo(DivaAction.Down));

            AddStep("press 5 again", () => grid().HandleAction(DivaAction.EditorToggleButtonFamily));
            AddAssert("back to the symbol half", () => composer.CurrentAction, () => Is.EqualTo(DivaAction.Cross));

            AddAssert("unrelated action is not handled", () => grid().HandleAction(DivaAction.EditorTapTool), () => Is.False);
        }

        private DivaNoteToggleGrid grid() => composer.ChildrenOfType<DivaNoteToggleGrid>().Single();

        private DrawableTernaryButton[] noteButtons() => grid().ChildrenOfType<DrawableTernaryButton>().ToArray();

        /// <summary>The glyph a note button draws as its icon.</summary>
        private static string glyphOf(DrawableTernaryButton button)
            => button.Icon.ChildrenOfType<SpriteText>().Single().Text.ToString();

        private static bool isBound(DivaAction action, InputKey key)
            => new DivaRuleset().GetDefaultKeyBindings(Rulesets.Ruleset.EDITOR_VARIANT)
                                .Any(b => action.Equals(b.Action) && b.KeyCombination.Keys.SequenceEqual(new[] { key }));

        private void selectTool(string name)
            => composer.ChildrenOfType<EditorRadioButton>().First(b => b.Text.ToString() == name).TriggerClick();

        private HitObjectCompositionToolButton toolButton(string name)
            => composer.ChildrenOfType<HitObjectCompositionToolButton>().Single(b => b.Text.ToString() == name);

        private DrawableTernaryButton<DivaAction> holdPlacementButton()
            => composer.ChildrenOfType<DrawableTernaryButton<DivaAction>>().Single(b => b.Action == DivaAction.EditorToggleHoldTool);

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
