// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Diva.Replays
{
    public partial class DivaAutoGenerator : AutoGenerator
    {
        protected Replay Replay;
        protected List<ReplayFrame> Frames => Replay.Frames;

        public new Beatmap<DivaHitObject> Beatmap => (Beatmap<DivaHitObject>)base.Beatmap;

        public DivaAutoGenerator(IBeatmap beatmap)
            : base(beatmap)
        {
            Replay = new Replay();
        }

        public override Replay Generate()
        {
            Frames.Add(new DivaReplayFrame());

            double prevTime = 0d;

            for (int i = 0; i < Beatmap.HitObjects.Count; i++)
            {
                DivaHitObject hitObject = Beatmap.HitObjects[i];
                // ProjectDIVA auto presses/releases at the ideal Start/End instants.
                double hitTime = hitObject.StartTime;

                if (i > 0)
                {
                    double releaseAnchor = Beatmap.HitObjects[i - 1] is DivaHoldHitObject prevHold
                        ? prevHold.EndTime
                        : prevTime;

                    if (releaseAnchor < hitTime)
                        Frames.Add(new DivaReplayFrame(lerp(releaseAnchor, hitTime, 0.1)));
                }

                if (hitObject is DivaHoldHitObject hold)
                {
                    Frames.Add(new DivaReplayFrame(hitTime, hold.ValidAction));
                    Frames.Add(new DivaReplayFrame(hold.EndTime));
                    prevTime = hold.EndTime;
                    continue;
                }

                if (hitObject is DoublePressButton dButt)
                    Frames.Add(new DivaReplayFrame(hitTime, hitObject.ValidAction, dButt.DoubleAction));
                else
                    Frames.Add(new DivaReplayFrame(hitTime, hitObject.ValidAction));

                prevTime = hitTime;
            }

            return Replay;
        }

        private double lerp(double a, double b, double t) => a + (b - a) * t;
    }
}
