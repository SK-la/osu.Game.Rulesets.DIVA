// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Screens.Play;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.UI
{
    public partial class DivaKiaiHUD : CompositeDrawable
    {
        private const string technical_zone = "Technical Zone";
        private const string change_time_text = "Chance Time";
        private static readonly Color4 technical_zone_color = Color4.LightSkyBlue; // 蓝色
        private static readonly Color4 chance_time_color = Color4.LightGoldenrodYellow; // 金色

        private static List<(double StartTime, double EndTime)> kiaiIntervals = new List<(double, double)>();

        private readonly SpriteText showText;
        private readonly Box backgroundBanner;

        private int currentKiaiIndex = -1;

        [Resolved]
        private IGameplayClock gameplayClock { get; set; }

        public DivaKiaiHUD()
        {
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;
            RelativeSizeAxes = Axes.Both;

            InternalChild = new Container
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    // 黑色横幅背景（贴合屏幕顶部）
                    backgroundBanner = new Box
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        Height = 50, // 初始高度，会根据文本调整
                        Colour = Color4.Black,
                        Alpha = 0,
                    },
                    // 文本容器
                    showText = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Font = OsuFont.Default.With(size: 36, weight: FontWeight.Bold),
                        Colour = technical_zone_color,
                        Alpha = 0,
                        Padding = new MarginPadding { Horizontal = 20, Vertical = 10 },
                    }
                }
            };
        }

        protected override void Update()
        {
            base.Update();

            if (kiaiIntervals.Count == 0)
                return;

            double currentTime = gameplayClock.CurrentTime;
            int newKiaiIndex = -1;

            // 查找当前所在的Kiai区间
            for (int i = 0; i < kiaiIntervals.Count; i++)
            {
                if (currentTime >= kiaiIntervals[i].StartTime && currentTime <= kiaiIntervals[i].EndTime)
                {
                    newKiaiIndex = i;
                    break;
                }
            }

            // 如果Kiai状态发生变化
            if (newKiaiIndex != currentKiaiIndex)
            {
                currentKiaiIndex = newKiaiIndex;

                // Logger.Log($"Kiai state changed: {(currentKiaiIndex != -1
                //     ? $"Entered Kiai {currentKiaiIndex}"
                //     : "Exited Kiai")} at {currentTime}ms");

                if (currentKiaiIndex >= 0)
                {
                    // 进入Kiai区间
                    bool isSingleKiai = kiaiIntervals.Count == 1;
                    bool isLastKiai = currentKiaiIndex == kiaiIntervals.Count - 1;

                    // 根据状态设置文本内容和颜色
                    if (isSingleKiai)
                    {
                        // 只有一个Kiai段，同时显示两个文本用/连接
                        showText.Text = $"{technical_zone} & {change_time_text}";
                        showText.Colour = technical_zone_color;
                    }
                    else if (isLastKiai)
                    {
                        // 最后一个Kiai段，只显示Chance Time
                        showText.Text = change_time_text;
                        showText.Colour = chance_time_color;
                    }
                    else
                    {
                        // 普通Kiai段，只显示Technical Zone
                        showText.Text = technical_zone;
                        showText.Colour = technical_zone_color;
                    }

                    // 更新背景高度以匹配文本高度
                    backgroundBanner.Height = showText.DrawHeight;

                    showText.FadeIn(200);
                    backgroundBanner.FadeIn(200);
                }
                else
                {
                    // 离开Kiai区间
                    showText.FadeOut(200);
                    backgroundBanner.FadeOut(200);
                }
            }
        }

        public void SetKiaiIntervals(List<(double StartTime, double EndTime)> intervals)
        {
            kiaiIntervals.Clear();
            kiaiIntervals = intervals;
            currentKiaiIndex = -1;

            Logger.Log($"Received {intervals.Count} Kiai intervals:");

            foreach (var interval in intervals)
            {
                Logger.Log($"  Start: {interval.StartTime}, End: {interval.EndTime}");
            }
        }
    }
}
