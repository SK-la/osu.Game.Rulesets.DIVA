// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit
{
    /// <summary>
    /// The note buttons of the composer's toggles group: one row per direction, with the arrow half on the
    /// left and the face-button half on the right.
    /// </summary>
    /// <remarks>
    /// The upstream toggles group is a vertical flow that only accepts full-width children, so both columns
    /// have to live inside a single child — the sample bank rows are built the same way. WASD picks a row and
    /// <see cref="DivaAction.EditorToggleButtonFamily"/> picks which half of that row gets placed.
    /// </remarks>
    public partial class DivaNoteToggleGrid : CompositeDrawable, IKeyBindingHandler<DivaAction>
    {
        /// <summary>One row of the grid, ordered top to bottom.</summary>
        /// <param name="Hotkey">Editor direction key that selects this row.</param>
        /// <param name="Arrow">Arrow note placed while the arrow family is active.</param>
        /// <param name="Symbol">Face-button note placed while the symbol family is active.</param>
        public readonly record struct NotePair(DivaAction Hotkey, DivaAction Arrow, DivaAction Symbol);

        public static readonly NotePair[] NOTE_PAIRS =
        [
            new NotePair(DivaAction.EditorButtonRight, DivaAction.Right, DivaAction.Circle),
            new NotePair(DivaAction.EditorButtonDown, DivaAction.Down, DivaAction.Cross),
            new NotePair(DivaAction.EditorButtonLeft, DivaAction.Left, DivaAction.Square),
            new NotePair(DivaAction.EditorButtonUp, DivaAction.Up, DivaAction.Triangle),
        ];

        /// <summary>Every action the grid offers a button for.</summary>
        public static IEnumerable<DivaAction> NoteActions
            => NOTE_PAIRS.SelectMany(pair => new[] { pair.Arrow, pair.Symbol });

        private readonly DivaHitObjectComposer composer;

        /// <summary>Which half of a row the direction keys land on.</summary>
        private bool arrowFamily;

        public DivaNoteToggleGrid(DivaHitObjectComposer composer)
        {
            this.composer = composer;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Masking = true;
            CornerRadius = 5;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
            };

            flow.Add(createHeader());

            foreach (var pair in NOTE_PAIRS)
                flow.Add(createRow(pair));

            AddInternal(flow);
        }

        public bool OnPressed(KeyBindingPressEvent<DivaAction> e)
        {
            if (e.Repeat)
                return false;

            return HandleAction(e.Action);
        }

        public void OnReleased(KeyBindingReleaseEvent<DivaAction> e)
        {
        }

        /// <summary>
        /// Applies a direction, family or note key, returning whether the key belonged to this grid.
        /// Split out of <see cref="OnPressed"/> so the mapping stays testable without key bindings.
        /// </summary>
        public bool HandleAction(DivaAction action)
        {
            if (action == DivaAction.EditorToggleButtonFamily)
            {
                setFamily(!arrowFamily);
                return true;
            }

            foreach (var pair in NOTE_PAIRS)
            {
                if (pair.Hotkey != action)
                    continue;

                composer.SelectAction(arrowFamily ? pair.Arrow : pair.Symbol);
                return true;
            }

            // The reference editor's F1–F4 / Alt+F1–F4 name a note outright rather than a row, so they also
            // set the family: otherwise the next WASD press would keep placing the half the user just left.
            if (!isNoteAction(action))
                return false;

            arrowFamily = NOTE_PAIRS.Any(pair => pair.Arrow == action);
            composer.SelectAction(action);
            return true;
        }

        private static bool isNoteAction(DivaAction action) => NoteActions.Contains(action);

        private void setFamily(bool forArrows)
        {
            arrowFamily = forArrows;

            // Keep the row the user is on; only swap which half of it gets placed.
            foreach (var pair in NOTE_PAIRS)
            {
                if (pair.Arrow != composer.CurrentAction && pair.Symbol != composer.CurrentAction)
                    continue;

                composer.SelectAction(arrowFamily ? pair.Arrow : pair.Symbol);
                return;
            }
        }

        private Drawable createHeader()
        {
            var header = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
            };

            header.Add(createCell(createFamilyHeading(DivaStrings.EDITOR_BUTTONS_ARROW, true), true));
            header.Add(createCell(createFamilyHeading(DivaStrings.EDITOR_BUTTONS_SYMBOL, false), false));

            return header;
        }

        private Drawable createFamilyHeading(LocalisableString text, bool forArrows) => new OsuClickableContainer
        {
            RelativeSizeAxes = Axes.X,
            Height = 24,
            TooltipText = DivaStrings.EDITOR_BUTTONS_FAMILY_TOOLTIP,
            Action = () => setFamily(forArrows),
            Child = new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = text,
                Font = OsuFont.GetFont(weight: FontWeight.Regular, size: 17),
            },
        };

        private Drawable createRow(NotePair pair)
        {
            var row = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
            };

            // Both halves of a row answer to the same key, so only the arrow half carries the hint.
            row.Add(createCell(createNoteButton(pair.Arrow, pair.Hotkey), true));
            row.Add(createCell(createNoteButton(pair.Symbol, null), false));

            return row;
        }

        private DrawableTernaryButton createNoteButton(DivaAction action, DivaAction? hotkey)
        {
            Hotkey? hotkeyDisplay = hotkey is { } key ? composer.HotkeyForAction(key) : null;

            return new DrawableTernaryButton
            {
                Current = composer.StateFor(action),
                // The glyph is the icon here, so an empty caption keeps it from being drawn twice.
                Description = string.Empty,
                TooltipText = DivaStrings.EDITOR_BUTTONS_GROUP,
                Hotkey = hotkeyDisplay,
                CreateIcon = () => new Container
                {
                    // The button pins the icon to a 20px box; a plain container takes that box so the
                    // auto-sizing text can stay centred at its natural size.
                    Child = new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = action.GetLocalisableDescription(),
                        Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                    },
                },
            };
        }

        /// <summary>One half of a row, anchored apart so each side can carry its own column gap.</summary>
        private static Drawable createCell(Drawable child, bool left) => new Container
        {
            Anchor = left ? Anchor.TopLeft : Anchor.TopRight,
            Origin = left ? Anchor.TopLeft : Anchor.TopRight,
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Width = 0.5f,
            Padding = left ? new MarginPadding { Right = 1 } : new MarginPadding { Left = 1 },
            Child = child,
        };
    }
}
