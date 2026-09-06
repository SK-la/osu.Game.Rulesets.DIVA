// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.Diva.Configuration;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Diva.UI
{
    /// <summary>
    ///     Fits the beatmap's logical note field into the drawable ruleset area (contain),
    ///     then applies the user scale as a fraction of that fitted size. Aspect follows content, not a fixed screen.
    /// </summary>
    public partial class DivaPlayfieldAdjustmentContainer : PlayfieldAdjustmentContainer
    {
        private const double default_playfield_scale = 0.92;

        protected override Container<Drawable> Content => content;

        private readonly Vector2 logicalSize;
        private readonly ScalingContainer content;
        private readonly Container scaledFit;
        private readonly BindableDouble playfieldScale = new BindableDouble(default_playfield_scale);

        public DivaPlayfieldAdjustmentContainer(Vector2 logicalSize)
        {
            this.logicalSize = logicalSize.X > 0 && logicalSize.Y > 0
                ? logicalSize
                : DivaPlayfieldSize.DefaultNativeSize;

            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            InternalChild = scaledFit = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2((float)default_playfield_scale),
                FillMode = FillMode.Fit,
                FillAspectRatio = this.logicalSize.X / this.logicalSize.Y,
                Child = content = new ScalingContainer(this.logicalSize) { RelativeSizeAxes = Axes.Both }
            };
        }

        [BackgroundDependencyLoader(true)]
        private void load(DivaRulesetConfigManager? config)
        {
            config?.BindWith(DivaRulesetSettings.PlayfieldScale, playfieldScale);
            playfieldScale.BindValueChanged(v => scaledFit.Size = new Vector2((float)v.NewValue), true);
        }

        private partial class ScalingContainer : Container
        {
            private readonly Vector2 baseSize;

            public ScalingContainer(Vector2 baseSize)
            {
                this.baseSize = baseSize;
            }

            protected override void Update()
            {
                base.Update();

                // FillMode.Fit already picked the limiting axis; width-based uniform scale matches that box.
                Scale = new Vector2(Parent!.ChildSize.X / baseSize.X);
                Size = Vector2.Divide(Vector2.One, Scale);
            }
        }
    }
}
