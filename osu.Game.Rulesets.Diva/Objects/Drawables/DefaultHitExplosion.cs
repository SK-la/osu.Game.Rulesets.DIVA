// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Diva.Objects.Drawables
{
    /// <summary>
    /// 内置资源驱动的打击爆炸动画，分层播放 normal / great / perfect 三段效果。
    /// </summary>
    public partial class DefaultHitExplosion : PoolableDrawable
    {
        private const double frame_duration = 50;
        private const string texture_prefix = "HitExplosion/";

        private FrameLayer normalLayer;
        private FrameLayer greatLayer;
        private FrameLayer perfectLayer;

        private JudgementResult judgementResult;

        public double AnimationDuration { get; private set; }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            normalLayer = createLayer(textures, "hit-normal", 9);
            greatLayer = createLayer(textures, "hit-great", 9);
            perfectLayer = createLayer(textures, "hit-perfect", 10);

            AddInternal(normalLayer.Sprite);
            AddInternal(greatLayer.Sprite);
            AddInternal(perfectLayer.Sprite);
        }

        protected override void PrepareForUse()
        {
            base.PrepareForUse();

            resetAnimation();
            judgementResult = null;
            AnimationDuration = 0;
        }

        public void Apply(JudgementResult result, DrawableHitObject judgedObject)
        {
            judgementResult = result;
            AnimationDuration = result?.Type switch
            {
                HitResult.Perfect => 10 * frame_duration,
                HitResult.Great => 9 * frame_duration,
                HitResult.Good => 9 * frame_duration,
                HitResult.Ok => 9 * frame_duration,
                HitResult.Meh => 9 * frame_duration,
                _ => 0
            };
        }

        public void PlayAnimation()
        {
            if (judgementResult == null || judgementResult.Type == HitResult.Miss || judgementResult.Type == HitResult.None)
                return;

            resetAnimation();

            normalLayer.Start(Time.Current);

            switch (judgementResult.Type)
            {
                case HitResult.Perfect:
                    greatLayer.Start(Time.Current);
                    perfectLayer.Start(Time.Current);
                    break;

                case HitResult.Great:
                    greatLayer.Start(Time.Current);
                    break;
            }
        }

        protected override void Update()
        {
            base.Update();

            normalLayer.Update(Time.Current);
            greatLayer.Update(Time.Current);
            perfectLayer.Update(Time.Current);
        }

        private FrameLayer createLayer(TextureStore textures, string prefix, int frameCount)
        {
            var frames = new List<Texture>(frameCount);

            for (int i = 0; i < frameCount; i++)
            {
                var texture = textures.Get($"{texture_prefix}{prefix}-{i}");

                if (texture != null)
                    frames.Add(texture);
            }

            return new FrameLayer(frames, frame_duration);
        }

        private void resetAnimation()
        {
            normalLayer?.Reset();
            greatLayer?.Reset();
            perfectLayer?.Reset();
        }

        private sealed class FrameLayer
        {
            private readonly Texture[] frames;
            private readonly double frameDuration;

            private int currentFrame;
            private bool playing;
            private double startTime;

            public Sprite Sprite { get; }

            public double TotalDuration => frames.Length * frameDuration;

            public FrameLayer(IReadOnlyList<Texture> frames, double frameDuration)
            {
                this.frames = new Texture[frames.Count];
                for (int i = 0; i < frames.Count; i++)
                    this.frames[i] = frames[i];

                this.frameDuration = frameDuration;

                Sprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Alpha = 0,
                };

                if (this.frames.Length > 0)
                    Sprite.Texture = this.frames[0];
            }

            public void Start(double currentTime)
            {
                if (frames.Length == 0)
                    return;

                playing = true;
                startTime = currentTime;
                currentFrame = 0;
                Sprite.Texture = frames[0];
                Sprite.Alpha = 1;
            }

            public void Update(double currentTime)
            {
                if (!playing || frames.Length == 0)
                    return;

                currentFrame = (int)((currentTime - startTime) / frameDuration);

                if (currentFrame >= frames.Length)
                {
                    playing = false;
                    currentFrame = frames.Length - 1;
                    Sprite.Texture = frames[currentFrame];
                    return;
                }

                Sprite.Texture = frames[currentFrame];
            }

            public void Reset()
            {
                playing = false;
                currentFrame = 0;
                Sprite.Alpha = 0;

                if (frames.Length > 0)
                    Sprite.Texture = frames[0];
            }
        }
    }
}
