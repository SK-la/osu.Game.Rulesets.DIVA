// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Screens.Play;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.UI
{
    public partial class DivaKiaiHUD : CompositeDrawable
    {
        private readonly SpriteText technicalZoneText;
        private readonly SpriteText chanceTimeText;
        private List<(double StartTime, double EndTime)> kiaiIntervals = new List<(double, double)>();
        private int currentKiaiIndex = -1;

        [Resolved]
        private IGameplayClock gameplayClock { get; set; }

        public DivaKiaiHUD()
        {
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer()
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        // Technical Zone 文本（蓝色）
                        technicalZoneText = new SpriteText
                        {
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                            Margin = new MarginPadding { Top = 20, Left = 20 },
                            Font = OsuFont.Default.With(size: 36, weight: FontWeight.Bold),
                            Colour = new Color4(0, 191, 255, 255),
                            Alpha = 0,
                        },
                        // Chance Time 文本（金色）
                        chanceTimeText = new SpriteText
                        {
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                            Margin = new MarginPadding { Top = 65, Left = 20 },
                            Font = OsuFont.Default.With(size: 36, weight: FontWeight.Bold),
                            Colour = new Color4(255, 215, 0, 255),
                            Alpha = 0,
                        }
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

                if (currentKiaiIndex >= 0)
                {
                    // 进入Kiai区间
                    bool isSingleKiai = kiaiIntervals.Count == 1;
                    bool isLastKiai = currentKiaiIndex == kiaiIntervals.Count - 1;

                    // 如果只有一个Kiai段，同时显示两个文本
                    if (isSingleKiai)
                    {
                        technicalZoneText.FadeIn(200);
                        chanceTimeText.FadeIn(200);
                    }
                    else if (isLastKiai)
                    {
                        // 最后一个Kiai段，只显示Chance Time
                        technicalZoneText.FadeOut(200);
                        chanceTimeText.FadeIn(200);
                    }
                    else
                    {
                        // 普通Kiai段，只显示Technical Zone
                        technicalZoneText.FadeIn(200);
                        chanceTimeText.FadeOut(200);
                    }
                }
                else
                {
                    // 离开Kiai区间
                    technicalZoneText.FadeOut(200);
                    chanceTimeText.FadeOut(200);
                }
            }
        }

        public void SetKiaiIntervals(List<(double StartTime, double EndTime)> intervals)
        {
            kiaiIntervals = intervals;
            currentKiaiIndex = -1;
        }
    }
}
