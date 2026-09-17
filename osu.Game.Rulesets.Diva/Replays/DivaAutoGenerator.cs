// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Diva.Replays
{
    /// <summary>
    ///     Turns the chart into replay frames.
    /// </summary>
    /// <remarks>
    ///     Every frame carries the complete set of buttons held at its time, and only one frame exists per instant.
    ///     This matters because replay input is diffed against the previous frame: a button that is already held
    ///     produces no new press, and a frame can only carry one action set. Frames emitted per hit object would
    ///     therefore drop every note of a chord but the last one.
    /// </remarks>
    public partial class DivaAutoGenerator : AutoGenerator
    {
        /// <summary>Gap left after a non-hold press when the next press of the same button is not close behind.</summary>
        private const double release_delay = 20;

        /// <summary>Fraction of the gap used when the next press of the same button is close behind.</summary>
        private const double tight_release_fraction = 0.9;

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

            List<ActionPoint> points = buildActionPoints();
            separateReleaseCollisions(points);

            // Actions accumulate across frames; only the delta between consecutive frames is turned into input events.
            var held = new List<DivaAction>();

            foreach (var group in points.GroupBy(p => p.Time).OrderBy(g => g.Key))
            {
                foreach (ActionPoint point in group)
                {
                    if (point.Press)
                        held.Add(point.Action);
                    else
                        held.Remove(point.Action);
                }

                Frames.Add(new DivaReplayFrame(group.Key, held.ToArray()));
            }

            return Replay;
        }

        private List<ActionPoint> buildActionPoints()
        {
            Dictionary<DivaAction, List<int>> pressIndices = buildPressIndex();

            var points = new List<ActionPoint>();

            for (int i = 0; i < Beatmap.HitObjects.Count; i++)
            {
                DivaHitObject hitObject = Beatmap.HitObjects[i];

                foreach (DivaAction action in pressedActions(hitObject))
                {
                    points.Add(new ActionPoint { Time = hitObject.StartTime, Action = action, Press = true });
                    points.Add(new ActionPoint { Time = releaseTimeFor(i, hitObject, action, pressIndices), Action = action, Press = false });
                }
            }

            return points;
        }

        /// <summary>Objects that press each button, in chart order.</summary>
        private Dictionary<DivaAction, List<int>> buildPressIndex()
        {
            var index = new Dictionary<DivaAction, List<int>>();

            for (int i = 0; i < Beatmap.HitObjects.Count; i++)
            {
                foreach (DivaAction action in pressedActions(Beatmap.HitObjects[i]))
                {
                    if (!index.TryGetValue(action, out List<int>? indices))
                        index[action] = indices = new List<int>();

                    indices.Add(i);
                }
            }

            return index;
        }

        private static IEnumerable<DivaAction> pressedActions(DivaHitObject hitObject)
        {
            yield return hitObject.ValidAction;

            if (hitObject is DoublePressButton doublePress && doublePress.DoubleAction != hitObject.ValidAction)
                yield return doublePress.DoubleAction;
        }

        private double releaseTimeFor(int index, DivaHitObject hitObject, DivaAction action, Dictionary<DivaAction, List<int>> pressIndices)
        {
            double endTime = hitObject.GetEndTime();

            // ProjectDIVA scores the strip tail, so a hold has to be released exactly on it.
            if (hitObject is DivaHoldHitObject hold && hold.Duration > 0)
                return endTime;

            double? nextPress = nextPressTime(index, action, pressIndices);

            if (nextPress == null || nextPress.Value > endTime + release_delay)
                return endTime + release_delay;

            return endTime + (nextPress.Value - endTime) * tight_release_fraction;
        }

        private double? nextPressTime(int index, DivaAction action, Dictionary<DivaAction, List<int>> pressIndices)
        {
            if (!pressIndices.TryGetValue(action, out List<int>? indices))
                return null;

            int position = indices.BinarySearch(index);

            if (position < 0)
                position = ~position;
            else
                position++;

            return position < indices.Count ? Beatmap.HitObjects[indices[position]].StartTime : null;
        }

        /// <summary>
        ///     A release and a press of the same button at the same instant cancel out in the frame diff, losing the
        ///     press. Nudge the release a millisecond earlier — every judgement window is far wider.
        /// </summary>
        private static void separateReleaseCollisions(List<ActionPoint> points)
        {
            var pressTimes = new HashSet<(double Time, DivaAction Action)>();

            foreach (ActionPoint point in points)
            {
                if (point.Press)
                    pressTimes.Add((point.Time, point.Action));
            }

            for (int i = 0; i < points.Count; i++)
            {
                ActionPoint point = points[i];

                if (point.Press || !pressTimes.Contains((point.Time, point.Action)))
                    continue;

                point.Time -= 1;
                points[i] = point;
            }
        }

        private struct ActionPoint
        {
            public double Time;
            public DivaAction Action;
            public bool Press;
        }
    }
}
