// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Judgements;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Scoring
{
    public partial class DivaScoreProcessor : ScoreProcessor
    {
        private const double combo_base = 4;

        // Kiai奖励相关字段
        private readonly List<(double StartTime, double EndTime)> kiaiIntervals = new List<(double, double)>();
        private readonly Dictionary<double, HitResult> judgementResults = new Dictionary<double, HitResult>();
        private double kiaiBonusAccuracy;

        public DivaScoreProcessor()
            : base(new DivaRuleset())
        {
            // 由于 ApplyResult 和 RevertResult 是 sealed 方法，无法重写
            // 我们使用另一种策略：在 Accuracy 变化时检查是否需要应用奖励
            // 但这会导致循环调用，所以我们需要一个标志来防止无限递归
        }

        private bool isApplyingBonus = false;

        private void onAccuracyChanged()
        {
            // 防止无限递归
            if (isApplyingBonus)
                return;

            // 当Accuracy变化时，重新计算并应用Kiai奖励
            if (kiaiIntervals.Count > 0 && judgementResults.Count > 0)
            {
                calculateKiaiBonus();
                applyKiaiBonusInternal();
            }
        }

        public override void ApplyBeatmap(IBeatmap beatmap)
        {
            base.ApplyBeatmap(beatmap);

            // 提取Kiai区间
            kiaiIntervals.Clear();
            judgementResults.Clear();
            kiaiBonusAccuracy = 0;

            var effectPoints = beatmap.ControlPointInfo.EffectPoints;

            for (int i = 0; i < effectPoints.Count; i++)
            {
                var effectPoint = effectPoints[i];

                if (effectPoint.KiaiMode)
                {
                    double startTime = effectPoint.Time;
                    // Kiai区间结束时间为下一个非Kiai效果点或谱面结束
                    double endTime = beatmap.HitObjects.LastOrDefault()?.GetEndTime() ?? startTime;

                    // 查找下一个非Kiai效果点作为结束时间
                    for (int j = i + 1; j < effectPoints.Count; j++)
                    {
                        if (!effectPoints[j].KiaiMode)
                        {
                            endTime = effectPoints[j].Time;
                            break;
                        }
                    }

                    kiaiIntervals.Add((startTime, endTime));
                }
            }
        }

        protected override JudgementResult CreateResult(HitObject hitObject, Judgement judgement)
        {
            var result = new DivaJudgementResult((DivaHitObject)hitObject, judgement);

            // 订阅事件以跟踪判定结果
            result.Applied += onJudgementApplied;
            result.Reverted += onJudgementReverted;

            return result;
        }

        private void onJudgementApplied(DivaJudgementResult result)
        {
            if (result.Type.AffectsAccuracy())
            {
                judgementResults[result.HitObject.StartTime] = result.Type;
                calculateKiaiBonus();
                applyKiaiBonus();
            }
        }

        private void onJudgementReverted(DivaJudgementResult result)
        {
            if (result.Type.AffectsAccuracy())
            {
                judgementResults.Remove(result.HitObject.StartTime);
                calculateKiaiBonus();
                applyKiaiBonus();
            }
        }

        private void calculateKiaiBonus()
        {
            kiaiBonusAccuracy = 0;

            if (kiaiIntervals.Count == 0)
                return;

            // 检查所有Kiai区间是否都通过（没有Miss）
            bool allKiaiPassed = true;

            foreach (var (startTime, endTime) in kiaiIntervals)
            {
                var kiaiJudgements = judgementResults.Where(j => j.Key >= startTime && j.Key <= endTime).ToList();

                if (kiaiJudgements.Any(j => j.Value == HitResult.Miss))
                {
                    allKiaiPassed = false;
                    break;
                }
            }

            // 如果所有Kiai区间都通过，获得3% ACC奖励
            if (allKiaiPassed)
            {
                kiaiBonusAccuracy += 0.03;
            }

            // 检查最后一个Kiai区间是否全部为Perfect/Great
            if (kiaiIntervals.Count > 0)
            {
                var lastKiai = kiaiIntervals[kiaiIntervals.Count - 1];
                var lastKiaiJudgements = judgementResults.Where(j => j.Key >= lastKiai.StartTime && j.Key <= lastKiai.EndTime).ToList();

                if (lastKiaiJudgements.Count > 0 &&
                    lastKiaiJudgements.All(j => j.Value == HitResult.Perfect || j.Value == HitResult.Great))
                {
                    kiaiBonusAccuracy += 0.05;
                }
            }
        }

        private void applyKiaiBonus()
        {
            applyKiaiBonusInternal();
        }

        private void applyKiaiBonusInternal()
        {
            if (kiaiBonusAccuracy > 0 && !isApplyingBonus)
            {
                isApplyingBonus = true;

                try
                {
                    // 直接添加Kiai奖励到Accuracy，最高不超过100%
                    double newAccuracy = Math.Min(Accuracy.Value + kiaiBonusAccuracy, 1.0);

                    // 避免无限循环：只有当值确实不同时才设置
                    if (Math.Abs(Accuracy.Value - newAccuracy) > 0.00001)
                    {
                        Accuracy.Value = newAccuracy;
                    }
                }
                finally
                {
                    isApplyingBonus = false;
                }
            }
        }

        protected override double ComputeTotalScore(double comboProgress, double accuracyProgress, double bonusPortion)
        {
            // return 1000000 * Accuracy.Value;

            return 150000 * comboProgress
                   + 850000 * Math.Pow(Accuracy.Value, 2 + 2 * Accuracy.Value) * accuracyProgress
                   + bonusPortion;
        }

        public override int GetBaseScoreForResult(HitResult result)
        {
            switch (result)
            {
                case HitResult.Perfect:
                    return 320;

                case HitResult.Great:
                    return 300;

                case HitResult.Good:
                    return 0;

                case HitResult.Ok:
                    return 0;

                case HitResult.Meh:
                    return 0;

                case HitResult.Miss:
                    return 0;
            }

            return base.GetBaseScoreForResult(result);
        }

        protected override double GetComboScoreChange(JudgementResult result)
        {
            return getBaseComboScoreForResult(result.Type) * Math.Min(Math.Max(0.5, Math.Log(result.ComboAfterJudgement, combo_base)), Math.Log(400, combo_base));
        }

        private int getBaseComboScoreForResult(HitResult result)
        {
            switch (result)
            {
                case HitResult.Perfect:
                    return 300;

                case HitResult.Great:
                    return 200;

                case HitResult.Good:
                case HitResult.Ok:
                case HitResult.Meh:
                    return 0;
            }

            return GetBaseScoreForResult(result);
        }

        // protected override IEnumerable<HitObject> EnumerateHitObjects(IBeatmap beatmap)
        // {
        //     return base.EnumerateHitObjects(beatmap).Order(JudgementOrderComparer.DEFAULT);
        // }
    }
}
