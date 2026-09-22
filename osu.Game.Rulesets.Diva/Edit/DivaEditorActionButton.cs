// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit
{
    /// <summary>
    ///     A toolbar button that runs a one-shot editor command instead of holding a state, for the reference
    ///     editor's menu items that carry a shortcut (its <c>Ctrl+Delete</c>). Laid out like
    ///     <see cref="osu.Game.Screens.Edit.Components.TernaryButtons.DrawableTernaryButton"/> so it can sit in
    ///     the same column, but it never shows itself as pressed.
    /// </summary>
    public partial class DivaEditorActionButton : OsuButton, IKeyBindingHandler<DivaAction>, IHasTooltip
    {
        /// <summary>The action this button answers to; <see langword="null"/> makes it click-only.</summary>
        public DivaAction? Command { get; init; }

        /// <summary>Runs on click or on <see cref="Command"/>.</summary>
        public Action? Run { get; init; }

        public LocalisableString Description
        {
            get => Text;
            set => Text = value;
        }

        public LocalisableString TooltipText { get; set; }

        /// <summary>A function which creates a drawable icon to represent this item.</summary>
        public Func<Drawable>? CreateIcon { get; init; }

        public Hotkey? Hotkey { get; init; }

        public Drawable Icon { get; private set; } = null!;

        public DivaEditorActionButton()
            : base(HoverSampleSet.Button)
        {
            RelativeSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            BackgroundColour = colourProvider.Background3;

            Add(Icon = (CreateIcon?.Invoke() ?? new Circle()).With(b =>
            {
                b.Anchor = Anchor.CentreLeft;
                b.Origin = Anchor.CentreLeft;
                b.Size = new Vector2(20);
                b.X = 10;
            }));

            if (Hotkey == null)
                return;

            SpriteText.Origin = Anchor.BottomLeft;
            SpriteText.Y = -1;

            Add(new HotkeyDisplay
            {
                Hotkey = Hotkey.Value,
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.TopLeft,
                X = 40,
                Y = 1
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Action = () =>
            {
                if (Enabled.Value)
                    Run?.Invoke();
            };
        }

        protected override SpriteText CreateText() => new OsuSpriteText
        {
            Depth = -1,
            Origin = Anchor.CentreLeft,
            Anchor = Anchor.CentreLeft,
            X = 40f
        };

        public bool OnPressed(KeyBindingPressEvent<DivaAction> e)
        {
            if (e.Repeat || !Nullable.Equals(Command, e.Action))
                return false;

            TriggerClick();
            return true;
        }

        public void OnReleased(KeyBindingReleaseEvent<DivaAction> e)
        {
        }
    }
}
