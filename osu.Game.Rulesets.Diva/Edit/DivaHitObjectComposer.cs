// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Edit.Tools;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Utils;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit
{
    [Cached]
    public partial class DivaHitObjectComposer : HitObjectComposer<DivaHitObject, DivaAction>
    {
        public static readonly DivaAction[] PLAY_ACTIONS = DivaNoteToggleGrid.NoteActions.ToArray();

        private const double min_playfield_zoom = -1; // 0.1x
        private const double max_playfield_zoom = 1; // 10x
        private const double playfield_zoom_step = 0.05;

        /// <summary>
        ///     Editor-only playfield zoom, as log10 of the multiplier applied on top of the fitted field (so 0 = 1x,
        ///     1 = 10x, -1 = 0.1x). Flight start points sit well outside the logical field, so the editor has to be
        ///     able to zoom out to reach them. Gameplay keeps using <see cref="DivaRulesetSettings.PlayfieldScale"/>.
        /// </summary>
        public readonly BindableDouble PlayfieldZoom = new BindableDouble(0)
        {
            MinValue = min_playfield_zoom,
            MaxValue = max_playfield_zoom,
            Precision = 0.01
        };

        private readonly Bindable<TernaryState> gridSnapToggle = new Bindable<TernaryState>(TernaryState.True);
        private readonly Bindable<TernaryState> replaceOnSameTimeToggle = new Bindable<TernaryState>(TernaryState.False);

        /// <summary>
        ///     Mirrors the placement tool as a ternary state so a single button (and the <c>X</c> hotkey) can flip
        ///     between taps and holds, the way the PC editor's Ctrl+X does, without touching the note buttons.
        /// </summary>
        private readonly Bindable<TernaryState> holdPlacementToggle = new Bindable<TernaryState>(TernaryState.False);

        private readonly Dictionary<DivaAction, Bindable<TernaryState>> actionStates = new Dictionary<DivaAction, Bindable<TernaryState>>();

        private RectangularPositionSnapGrid? positionSnapGrid;

        public DivaAction CurrentAction { get; private set; } = DivaAction.Circle;

        /// <summary>
        ///     The <c>.diva</c> this session was opened from, read once on load. Source of the export's
        ///     defaults and of the <c>#WAV</c> keys for notes the editor has not touched.
        /// </summary>
        public DivaChart? SourceChart { get; private set; }

        /// <summary>Effective <c>#WAV</c> slot of a note, i.e. what an export would write for it.</summary>
        public int ResolveWavKey(DivaHitObject hitObject) => DivaChartBuilder.ResolveWavKey(EditorBeatmap, hitObject, SourceChart);

        public bool ReplaceOnSameTime => replaceOnSameTimeToggle.Value == TernaryState.True;

        public bool GridSnapEnabled => gridSnapToggle.Value == TernaryState.True;

        public IBindable<TernaryState> GridSnapToggle => gridSnapToggle;

        public DivaHitObjectComposer(Ruleset ruleset)
            : base(ruleset)
        {
            foreach (var action in PLAY_ACTIONS)
                actionStates[action] = new Bindable<TernaryState>();

            actionStates[DivaAction.Circle].Value = TernaryState.True;
        }

        /// <summary>True while <see cref="UpdateTernaryStates"/> writes the note buttons in bulk.</summary>
        private bool syncingActionStates;

        /// <summary>True while the placement tool drives <see cref="holdPlacementToggle"/> instead of the reverse.</summary>
        private bool syncingHoldPlacementToggle;

        public override Bindable<TernaryState>? SelectionNewComboState => null;

        protected override IReadOnlyList<CompositionTool<DivaAction>> CompositionTools =>
        [
            tapTool,
            holdTool
        ];

        private readonly DivaTapCompositionTool tapTool = new DivaTapCompositionTool();
        private readonly DivaHoldCompositionTool holdTool = new DivaHoldCompositionTool();

        protected override DrawableRuleset<DivaHitObject> CreateDrawableRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new DrawableDivaEditorRuleset((DivaRuleset)ruleset, beatmap, mods);

        protected override ComposeBlueprintContainer CreateBlueprintContainer()
            => new DivaBlueprintContainer(this);

        protected override Drawable CreateHitObjectInspector() => new DivaHitObjectInspector();

        protected override IEnumerable<Drawable> CreateTernaryButtons()
        {
            yield return new DrawableTernaryButton<DivaAction>
            {
                Current = gridSnapToggle,
                Description = DivaStrings.EDITOR_GRID_SNAP,
                CreateIcon = () => new SpriteIcon { Icon = OsuIcon.EditorGridSnap },
                Action = DivaAction.EditorToggleGridSnap,
                Hotkey = HotkeyForAction(DivaAction.EditorToggleGridSnap)
            };

            yield return new DrawableTernaryButton<DivaAction>
            {
                Current = replaceOnSameTimeToggle,
                Description = DivaStrings.EDITOR_REPLACE_ON_SAME_TIME,
                TooltipText = DivaStrings.EDITOR_REPLACE_ON_SAME_TIME_TOOLTIP,
                CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.Clone },
                Action = DivaAction.EditorToggleReplaceOnSameTime,
                Hotkey = HotkeyForAction(DivaAction.EditorToggleReplaceOnSameTime)
            };

            yield return new DrawableTernaryButton<DivaAction>
            {
                Current = holdPlacementToggle,
                Description = DivaStrings.EDITOR_HOLD_PLACEMENT,
                TooltipText = DivaStrings.EDITOR_HOLD_PLACEMENT_TOOLTIP,
                CreateIcon = () => new SpriteIcon { Icon = OsuIcon.EditorHoldNote },
                Action = DivaAction.EditorToggleHoldTool,
                Hotkey = HotkeyForAction(DivaAction.EditorToggleHoldTool)
            };

            yield return new DivaNoteToggleGrid(this);
        }

        /// <summary>The selection state backing one note button of the toggles grid.</summary>
        public Bindable<TernaryState> StateFor(DivaAction action) => actionStates[action];

        /// <summary>Selects a note action as if its button had been clicked.</summary>
        public void SelectAction(DivaAction action) => actionStates[action].Value = TernaryState.True;

        [BackgroundDependencyLoader]
        private void load()
        {
            PlayfieldContentContainer.Padding = new MarginPadding(10);
            PlayfieldZoom.BindValueChanged(_ => applyPlayfieldZoom(), true);

            SourceChart = DivaChartBuilder.LoadSource(EditorBeatmap);

            positionSnapGrid = new RectangularPositionSnapGrid
            {
                RelativeSizeAxes = Axes.Both,
                StartPosition = { Value = DivaActionEncoding.ToPlayfieldPosition(0, 0) },
                Spacing = { Value = new Vector2(DivaChartConstants.DELTA_X, DivaChartConstants.DELTA_Y) }
            };

            LayerBelowRuleset.Add(positionSnapGrid);
            RightToolbox.Add(new DivaEditorViewToolbox(PlayfieldZoom));
            RightToolbox.Add(new DivaChartPropertiesToolbox());
            RightToolbox.Add(new DivaChartEventsToolbox());
            RightToolbox.Add(new DivaNoteToolsToolbox());
            RightToolbox.Add(new DivaBatchToolsToolbox());
            RightToolbox.Add(new DivaExportToolbox());

            gridSnapToggle.BindValueChanged(state =>
            {
                positionSnapGrid?.Alpha = state.NewValue == TernaryState.True ? 1 : 0;
            }, true);

            holdPlacementToggle.BindValueChanged(state =>
            {
                if (syncingHoldPlacementToggle)
                    return;

                if (state.NewValue == TernaryState.True)
                    selectPlacementTool(holdTool);
                else if (state.NewValue == TernaryState.False)
                    selectPlacementTool(tapTool);
            });

            foreach (var (action, bindable) in actionStates)
            {
                var captured = action;
                bindable.ValueChanged += state =>
                {
                    // Only a click (or hotkey) on a note button may change the placement action or push it
                    // onto the selection; the state sync below writes these bindables in bulk and would
                    // otherwise be read as a click.
                    if (syncingActionStates)
                        return;

                    if (state.NewValue != TernaryState.True)
                    {
                        if (actionStates.Values.All(b => b.Value != TernaryState.True))
                            bindable.Value = TernaryState.True;
                        return;
                    }

                    foreach (var other in actionStates)
                    {
                        if (other.Key != captured)
                            other.Value.Value = TernaryState.False;
                    }

                    CurrentAction = captured;
                    applyActionToSelection(captured);
                };
            }
        }

        protected override void Update()
        {
            base.Update();

            // The toolbar's own tap/hold buttons (and the select tool) can change the placement tool behind the
            // ternary button's back, so mirror the tool back onto the button instead of caching a stale state.
            TernaryState placement = ReferenceEquals(BlueprintContainer.CurrentTool, holdTool) ? TernaryState.True
                : ReferenceEquals(BlueprintContainer.CurrentTool, tapTool) ? TernaryState.False
                : TernaryState.Indeterminate;

            if (placement == holdPlacementToggle.Value)
                return;

            syncingHoldPlacementToggle = true;
            holdPlacementToggle.Value = placement;
            syncingHoldPlacementToggle = false;
        }

        /// <summary>Switches the placement tool through its toolbar button, keeping the radio selection in sync.</summary>
        private void selectPlacementTool(CompositionTool tool)
        {
            if (ReferenceEquals(BlueprintContainer.CurrentTool, tool))
                return;

            // The base composer keeps its tool buttons in a private collection, so reach them through the toolbox
            // tree: pressing the real button is what keeps the highlighted tool and the placement in agreement,
            // and what clears the selection the way a click does.
            LeftToolbox.ChildrenOfType<HitObjectCompositionToolButton>().FirstOrDefault(b => ReferenceEquals(b.Tool, tool))?.Select();
        }

        protected override void UpdateTernaryStates()
        {
            base.UpdateTernaryStates();

            var selected = EditorBeatmap.SelectedHitObjects.OfType<DivaHitObject>().ToArray();
            if (selected.Length == 0)
                return;

            // Writing the buttons one at a time leaves the group momentarily without a pressed button, which
            // the handlers above would "repair" by pressing whichever button they reach first — rewriting the
            // selected notes' action to it. Sync under a flag so only a real click can do that.
            syncingActionStates = true;

            try
            {
                foreach (var action in PLAY_ACTIONS)
                    actionStates[action].Value = selected.GetTernaryState(h => h.ValidAction == action);
            }
            finally
            {
                syncingActionStates = false;
            }
        }

        public SnapResult FindSnappedPositionAndTime(Vector2 screenSpacePosition)
        {
            var playfield = PlayfieldAtScreenSpacePosition(screenSpacePosition);
            Vector2 local = playfield.ToLocalSpace(screenSpacePosition);
            Vector2 snapped = SnapPlayfieldPosition(local);
            double time = BeatSnapProvider.SnapTime(EditorClock.CurrentTime);

            return new SnapResult(playfield.ToScreenSpace(snapped), time, playfield);
        }

        public SnapResult FindSnappedPosition(Vector2 screenSpacePosition)
        {
            var playfield = PlayfieldAtScreenSpacePosition(screenSpacePosition);
            Vector2 local = playfield.ToLocalSpace(screenSpacePosition);
            Vector2 snapped = SnapPlayfieldPosition(local);
            return new SnapResult(playfield.ToScreenSpace(snapped), null, playfield);
        }

        public Vector2 SnapPlayfieldPosition(Vector2 playfieldLocal)
        {
            if (gridSnapToggle.Value != TernaryState.True)
                return playfieldLocal;

            return DivaActionEncoding.SnapToGrid(playfieldLocal);
        }

        /// <summary>Steps <see cref="PlayfieldZoom"/>; driven by the Alt+wheel shortcut over the play area.</summary>
        public void AdjustPlayfieldZoom(int direction)
        {
            if (direction == 0)
                return;

            PlayfieldZoom.Value += Math.Sign(direction) * playfield_zoom_step;
        }

        /// <remarks>
        ///     Scales the shared parent of the playfield layers rather than a
        ///     <see cref="PlayfieldAdjustmentContainer"/>: hit objects, the snap grid and the blueprints each live in
        ///     their own adjustment container, and those are created by <c>DrawableRuleset</c>'s constructor before
        ///     the composer can hand anything to them.
        /// </remarks>
        private void applyPlayfieldZoom()
        {
            if (PlayfieldContentContainer == null)
                return;

            PlayfieldContentContainer.Scale = new Vector2((float)Math.Pow(10, PlayfieldZoom.Value));
        }

        public Vector2 ComputeDefaultApproach(Vector2 position, double time)
        {
            var previous = EditorBeatmap.HitObjects.OfType<DivaHitObject>()
                                        .Where(h => h.StartTime < time)
                                        .OrderBy(h => h.StartTime)
                                        .LastOrDefault();

            Vector2 dir = previous != null ? previous.Position - position : new Vector2(1, 0);
            if (dir.LengthSquared < 0.0001f)
                dir = new Vector2(1, 0);

            double bpm = EditorBeatmap.ControlPointInfo.TimingPointAt(time).BPM;

            return dir.Normalized() * DivaActionEncoding.ApproachDistance(bpm);
        }

        public void ApplyPlacementDefaults(DivaHitObject hitObject)
        {
            hitObject.ValidAction = CurrentAction;
            hitObject.ApproachPieceOriginPosition = ComputeDefaultApproach(hitObject.Position, hitObject.StartTime);
        }

        private void applyActionToSelection(DivaAction action)
        {
            if (EditorBeatmap.SelectedHitObjects.OfType<DivaHitObject>().All(h => h.ValidAction == action))
                return;

            EditorBeatmap.PerformOnSelection(h =>
            {
                if (h is DivaHitObject diva)
                    diva.ValidAction = action;
            });
        }
    }
}
