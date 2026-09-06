// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
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

        private const float playfield_size_adjust = 0.92f;

        protected override Container<Drawable> Content => content;
        private readonly ScalingContainer content;

        public DivaPlayfieldAdjustmentContainer()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Size = new Vector2(playfield_size_adjust);

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
