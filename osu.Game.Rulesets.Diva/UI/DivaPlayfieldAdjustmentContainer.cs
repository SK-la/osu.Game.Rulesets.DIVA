// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Configuration;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Diva.UI
{
    /// <summary>
    ///     Scales the ProjectDIVA 480×272 logical playfield to fit the drawable ruleset area.
    /// </summary>
    public partial class DivaPlayfieldAdjustmentContainer : PlayfieldAdjustmentContainer
    {
        public static readonly Vector2 BASE_SIZE = new Vector2(DivaChartConstants.WIDTH, DivaChartConstants.HEIGHT);

        private const double default_playfield_scale = 0.92;

        protected override Container<Drawable> Content => content;
        private readonly ScalingContainer content;

        private readonly BindableDouble playfieldScale = new BindableDouble(default_playfield_scale);

        public DivaPlayfieldAdjustmentContainer()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Size = new Vector2((float)default_playfield_scale);

            InternalChild = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit,
                FillAspectRatio = BASE_SIZE.X / BASE_SIZE.Y,
                Child = content = new ScalingContainer { RelativeSizeAxes = Axes.Both }
            };
        }

        [BackgroundDependencyLoader(true)]
        private void load(DivaRulesetConfigManager? config)
        {
            config?.BindWith(DivaRulesetSettings.PlayfieldScale, playfieldScale);
            playfieldScale.BindValueChanged(v => Size = new Vector2((float)v.NewValue), true);
        }

        private partial class ScalingContainer : Container
        {
            protected override void Update()
            {
                base.Update();

                Scale = new Vector2(Parent!.ChildSize.X / BASE_SIZE.X);
                Size = Vector2.Divide(Vector2.One, Scale);
            }
        }
    }
}
