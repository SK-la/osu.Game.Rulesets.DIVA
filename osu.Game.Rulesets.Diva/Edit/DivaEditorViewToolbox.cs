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
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Edit;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit
{
    /// <summary>
    ///     Editor view controls. Zoom is deliberately not a ruleset setting: it must not change how gameplay
    ///     scales the field, and it does not need to survive the editing session.
    /// </summary>
    public partial class DivaEditorViewToolbox : EditorToolboxGroup
    {
        private readonly BindableDouble playfieldZoom;

        public DivaEditorViewToolbox(BindableDouble playfieldZoom)
            : base(DivaStrings.EDITOR_VIEW_GROUP.ToString())
        {
            this.playfieldZoom = playfieldZoom;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(5),
                Children =
                [
                    new FormSliderBar<double>
                    {
                        Caption = DivaStrings.EDITOR_PLAYFIELD_ZOOM,
                        Current = playfieldZoom,
                        KeyboardStep = 0.05f,
                        LabelFormat = zoom => $"×{Math.Pow(10, zoom):0.##}"
                    },
                    new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 12))
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Text = DivaStrings.EDITOR_PLAYFIELD_ZOOM_TOOLTIP
                    }
                ]
            };
        }
    }
}
