// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.Diva.Audio
{
    /// <summary>
    /// ProjectDIVA-style hit SE: play embedded <c>hit.wav</c> on every mapped keydown
    /// (including empty presses), with same-frame volume stacking via <c>PlayHit(factor)</c>.
    /// </summary>
    /// <remarks>
    /// Uses ruleset <see cref="ISampleStore"/> directly (not skin lookup) so gameplay SE always
    /// resolves from <c>Resources/Samples</c>.
    /// Must sit late in the playfield child list so the reversed key-binding queue reaches us
    /// before hit objects; always returns <c>false</c> so judgement still runs
    /// (taiko <c>DrumSamplePlayer</c> / mania <c>Column</c> pattern).
    /// </remarks>
    public partial class DivaHitSamplePlayer : CompositeDrawable, IKeyBindingHandler<DivaAction>
    {
        private Sample? hitSample;
        private int frameHitCount;

        private readonly IBindable<bool> samplePlaybackDisabled = new BindableBool();

        public DivaHitSamplePlayer()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader(true)]
        private void load(ISampleStore samples, ISamplePlaybackDisabler? samplePlaybackDisabler)
        {
            // DrawableRulesetDependencies namespaces ruleset Resources/Samples; must request .ogg explicitly.
            string name = DivaHitSampleInfo.ToSampleStoreName(DivaHitSampleInfo.NORMAL_LOOKUP);
            hitSample = samples.Get(name);

            if (hitSample == null)
                Logger.Log($"[DIVA] Missing hit sample '{name}'", level: LogLevel.Debug);

            if (samplePlaybackDisabler != null)
                samplePlaybackDisabled.BindTo(samplePlaybackDisabler.SamplePlaybackDisabled);
        }

        public bool OnPressed(KeyBindingPressEvent<DivaAction> e)
        {
            if ((Clock as IGameplayClock)?.IsRewinding == true)
                return false;

            frameHitCount++;
            PlayHit(frameHitCount);
            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<DivaAction> e)
        {
        }

        /// <summary>Hold-tail release hit (ProjectDIVA strip release <c>PlayHit(1)</c>).</summary>
        public void PlayReleaseHit() => PlayHit(1);

        public void PlayHit(float factor)
        {
            if ((Clock as IGameplayClock)?.IsRewinding == true)
                return;

            if (samplePlaybackDisabled.Value)
                return;

            if (hitSample == null)
                return;

            // PD: vol = min(1, 0.5 + factor * 0.25) (engine defaultVolume left to lazer master gain).
            double volume = Math.Clamp(0.5 + factor * 0.25, 0.05, 1.0);

            var channel = hitSample.GetChannel();
            channel.Volume.Value = volume;
            channel.Play();
        }

        protected override void Update()
        {
            base.Update();
            frameHitCount = 0;
        }
    }
}
