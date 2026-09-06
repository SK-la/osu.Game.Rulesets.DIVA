// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Pooling;
using osu.Game.Rulesets.Diva.Audio;
using osu.Game.Rulesets.Diva.Objects.Drawables;
using osu.Game.Rulesets.Diva.Scoring;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Diva.UI
{
    [Cached]
    public partial class DivaPlayfield : Playfield
    {
        private readonly JudgementContainer<DrawableDivaJudgement> judgementLayer;

        private readonly IDictionary<HitResult, DrawablePool<DrawableDivaJudgement>> poolDictionary = new Dictionary<HitResult, DrawablePool<DrawableDivaJudgement>>();

        private readonly Container judgementAboveHitObjectLayer;

        [Cached]
        private readonly DivaHitSamplePlayer hitSamplePlayer;

        public DivaPlayfield()
        {
            hitSamplePlayer = new DivaHitSamplePlayer();

            InternalChildren =
            [
                judgementLayer = new JudgementContainer<DrawableDivaJudgement> { RelativeSizeAxes = Axes.Both },
                judgementAboveHitObjectLayer = new Container { RelativeSizeAxes = Axes.Both }
            ];

            var hitWindows = new DivaHitWindows();
            foreach (var result in Enum.GetValues(typeof(HitResult)).OfType<HitResult>().Where(r => r > HitResult.None && hitWindows.IsHitResultAllowed(r)))
                poolDictionary.Add(result, new DrawableJudgementPool(result, onJudgementLoaded));

            if (!poolDictionary.ContainsKey(HitResult.Miss))
                poolDictionary.Add(HitResult.Miss, new DrawableJudgementPool(HitResult.Miss, onJudgementLoaded));

            AddRangeInternal(poolDictionary.Values);

            NewResult += onNewResult;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(HitObjectContainer);

            // After HitObjectContainer: reversed KeyBindingInputQueue hits us first (taiko/mania pattern).
            AddInternal(hitSamplePlayer);
            AddInternal(new DivaResultVoicePlayer());
        }

        private void onJudgementLoaded(DrawableDivaJudgement j)
        {
            judgementAboveHitObjectLayer.Add(j.ProxiedAboveHitObjectsContent);
        }

        private void onNewResult(DrawableHitObject judgedObject, JudgementResult result)
        {
            if (!judgedObject.DisplayResult)
                return;

            showJudgementVisual(judgedObject, result);
        }

        /// <summary>
        /// ProjectDIVA strip head press: spawn hit explosion / judgement text without scoring.
        /// Final hold score still comes from a later <see cref="DrawableHitObject.ApplyResult(HitResult)"/>.
        /// </summary>
        public void ShowHitFeedback(DrawableHitObject judgedObject, HitResult type)
        {
            if (!judgedObject.DisplayResult || type == HitResult.None)
                return;

            var feedback = new JudgementResult(judgedObject.HitObject, judgedObject.HitObject.CreateJudgement())
            {
                Type = type,
            };

            showJudgementVisual(judgedObject, feedback);
        }

        private void showJudgementVisual(DrawableHitObject judgedObject, JudgementResult result)
        {
            if (!poolDictionary.TryGetValue(result.Type, out var pool))
                return;

            DrawableDivaJudgement explosion = pool.Get(doj => doj.Apply(result, judgedObject));
            judgementLayer.Add(explosion);
        }

        private partial class DrawableJudgementPool : DrawablePool<DrawableDivaJudgement>
        {
            private readonly HitResult result;
            private readonly Action<DrawableDivaJudgement> onLoaded;

            public DrawableJudgementPool(HitResult result, Action<DrawableDivaJudgement> onLoaded)
                : base(10)
            {
                this.result = result;
                this.onLoaded = onLoaded;
            }

            protected override DrawableDivaJudgement CreateNewDrawable()
            {
                var judgement = base.CreateNewDrawable();

                // just a placeholder to initialise the correct drawable hierarchy for this pool.
                judgement.Apply(new JudgementResult(new HitObject(), new Judgement()) { Type = result }, null);

                onLoaded.Invoke(judgement);

                return judgement;
            }
        }
    }
}
