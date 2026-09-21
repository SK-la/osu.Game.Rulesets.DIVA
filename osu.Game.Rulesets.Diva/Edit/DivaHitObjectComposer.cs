// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
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
        public static readonly DivaAction[] PLAY_ACTIONS =
        [
            DivaAction.Square,
            DivaAction.Triangle,
            DivaAction.Circle,
            DivaAction.Cross,
            DivaAction.Left,
            DivaAction.Up,
            DivaAction.Right,
            DivaAction.Down
        ];

        private readonly Bindable<TernaryState> gridSnapToggle = new Bindable<TernaryState>(TernaryState.True);
        private readonly Bindable<TernaryState> replaceOnSameTimeToggle = new Bindable<TernaryState>(TernaryState.False);
        private readonly Dictionary<DivaAction, Bindable<TernaryState>> actionStates = new Dictionary<DivaAction, Bindable<TernaryState>>();

        private RectangularPositionSnapGrid? positionSnapGrid;

        public DivaAction CurrentAction { get; private set; } = DivaAction.Circle;

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

        public override Bindable<TernaryState>? SelectionNewComboState => null;

        protected override IReadOnlyList<CompositionTool<DivaAction>> CompositionTools =>
        [
            new DivaTapCompositionTool(),
            new DivaHoldCompositionTool()
        ];

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

            foreach (var action in PLAY_ACTIONS)
            {
                var captured = action;
                yield return new DrawableTernaryButton
                {
                    Current = actionStates[captured],
                    Description = captured.GetDescription(),
                    TooltipText = DivaStrings.EDITOR_BUTTONS_GROUP,
                    CreateIcon = () => new SpriteIcon
                    {
                        Icon = FontAwesome.Regular.Circle,
                        Colour = Graphics.DivaProjectDivaAtlas.GetUnitColor(captured)
                    }
                };
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            PlayfieldContentContainer.Padding = new MarginPadding(10);

            positionSnapGrid = new RectangularPositionSnapGrid
            {
                RelativeSizeAxes = Axes.Both,
                StartPosition = { Value = DivaActionEncoding.ToPlayfieldPosition(0, 0) },
                Spacing = { Value = new Vector2(DivaChartConstants.DELTA_X, DivaChartConstants.DELTA_Y) }
            };

            LayerBelowRuleset.Add(positionSnapGrid);
            RightToolbox.Add(new DivaExportToolbox());

            gridSnapToggle.BindValueChanged(state =>
            {
                positionSnapGrid?.Alpha = state.NewValue == TernaryState.True ? 1 : 0;
            }, true);

            foreach (var (action, bindable) in actionStates)
            {
                var captured = action;
                bindable.ValueChanged += state =>
                {
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

        protected override void UpdateTernaryStates()
        {
            base.UpdateTernaryStates();

            var selected = EditorBeatmap.SelectedHitObjects.OfType<DivaHitObject>().ToArray();
            if (selected.Length == 0)
                return;

            foreach (var action in PLAY_ACTIONS)
                actionStates[action].Value = selected.GetTernaryState(h => h.ValidAction == action);
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
            if (bpm <= 0)
                bpm = DivaChartConstants.BASE_BPM;

            float distance = (float)(DivaChartConstants.DISTANCE * DivaChartConstants.BASE_BPM / bpm);
            return dir.Normalized() * distance;
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
