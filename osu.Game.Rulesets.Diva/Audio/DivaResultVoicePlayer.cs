// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.Diva.Audio
{
    /// <summary>
    /// Plays ProjectDIVA end-of-song grade VO once when the play passes or fails
    /// (slightly earlier than PD's post-count ScoreMenu timing).
    /// </summary>
    public partial class DivaResultVoicePlayer : CompositeDrawable
    {
        private Sample? mistake;
        private Sample? cheap;
        private Sample? standard;
        private Sample? great;
        private Sample? perfect;

        private readonly IBindable<bool> samplePlaybackDisabled = new BindableBool();

        [Resolved]
        private ScoreProcessor scoreProcessor { get; set; } = null!;

        [Resolved]
        private HealthProcessor healthProcessor { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private GameplayState? gameplayState { get; set; }

        private bool played;

        public DivaResultVoicePlayer()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader(true)]
        private void load(ISampleStore samples, ISamplePlaybackDisabler? samplePlaybackDisabler)
        {
            mistake = getSample(samples, DivaHitSampleInfo.RESULT_MISTAKE_LOOKUP);
            cheap = getSample(samples, DivaHitSampleInfo.RESULT_CHEAP_LOOKUP);
            standard = getSample(samples, DivaHitSampleInfo.RESULT_STANDARD_LOOKUP);
            great = getSample(samples, DivaHitSampleInfo.RESULT_GREAT_LOOKUP);
            perfect = getSample(samples, DivaHitSampleInfo.RESULT_PERFECT_LOOKUP);

            if (samplePlaybackDisabler != null)
                samplePlaybackDisabled.BindTo(samplePlaybackDisabler.SamplePlaybackDisabled);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            scoreProcessor.HasCompleted.BindValueChanged(onCompleted, true);
        }

        protected override void Update()
        {
            base.Update();

            if (!played && gameplayState?.HasFailed == true)
                playVoice(failed: true);
        }

        private void onCompleted(ValueChangedEvent<bool> completed)
        {
            if (!completed.NewValue)
                return;

            playVoice(failed: healthProcessor.HasFailed || (gameplayState?.HasFailed ?? false));
        }

        private void playVoice(bool failed)
        {
            if (played)
                return;

            if (gameplayState?.HasQuit == true)
                return;

            if (samplePlaybackDisabled.Value)
                return;

            played = true;

            DivaSongResult result = DivaSongResultEvaluator.Evaluate(
                failed,
                scoreProcessor.TotalScore.Value,
                scoreProcessor.MaximumTotalScore,
                scoreProcessor.Statistics);

            Sample? sample = result switch
            {
                DivaSongResult.Perfect => perfect,
                DivaSongResult.Great => great,
                DivaSongResult.Standard => standard,
                DivaSongResult.Cheap => cheap,
                _ => mistake
            };

            sample?.GetChannel().Play();
        }

        private static Sample? getSample(ISampleStore samples, string lookup)
        {
            string name = DivaHitSampleInfo.ToSampleStoreName(lookup);
            Sample? sample = samples.Get(name);

            if (sample == null)
                Logger.Log($"[DIVA] Missing result sample '{name}'", level: LogLevel.Debug);

            return sample;
        }
    }
}
