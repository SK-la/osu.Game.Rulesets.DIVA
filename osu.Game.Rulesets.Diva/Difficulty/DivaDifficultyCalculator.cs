// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Difficulty.Preprocessing;
using osu.Game.Rulesets.Diva.Difficulty.Skills;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Diva.Difficulty
{
    public class DivaDifficultyCalculator : DifficultyCalculator
    {
        private const double event_time_epsilon = 1;
        private const double star_rating_multiplier = 0.57;
        private const double skill_compression_exponent = 0.52;

        public override int Version => 20260907;

        public DivaDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods) =>
        [
            new DivaSpeed(mods),
            new DivaPattern(mods),
            new DivaReading(mods),
            new DivaHold(mods)
        ];

        protected override Mod[] DifficultyAdjustmentMods =>
        [
            new OsuModDoubleTime(),
            new OsuModHalfTime(),
            new OsuModNightcore(),
            new OsuModDaycore()
        ];

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, Mod[] mods)
        {
            DivaHitObject[] notes = beatmap.HitObjects
                                                .OfType<DivaHitObject>()
                                                .OrderBy(h => h.StartTime)
                                                .ToArray();

            if (notes.Length == 0)
                return Array.Empty<DifficultyHitObject>();

            double clockRate = ModUtils.CalculateRateWithMods(mods);
            List<DivaDifficultyEvent> events = createEvents(notes);
            assignHoldLoad(events, notes);
            assignApproachReading(events, notes, beatmap);

            var objects = new List<DifficultyHitObject>(events.Count);
            HitObject? previousEventObject = null;

            foreach (DivaDifficultyEvent difficultyEvent in events)
            {
                difficultyEvent.Finalise();

                var eventObject = new DivaHitObject { StartTime = difficultyEvent.StartTime };
                eventObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
                previousEventObject ??= eventObject;

                objects.Add(new DivaDifficultyHitObject(
                    eventObject,
                    previousEventObject,
                    difficultyEvent,
                    clockRate,
                    objects,
                    objects.Count));

                previousEventObject = eventObject;
            }

            return objects;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills)
        {
            if (beatmap.HitObjects.Count == 0)
                return new DivaDifficultyAttributes { Mods = mods };

            double speed = compress(skills.OfType<DivaSpeed>().Single().DifficultyValue());
            double pattern = compress(skills.OfType<DivaPattern>().Single().DifficultyValue());
            double reading = compress(skills.OfType<DivaReading>().Single().DifficultyValue());
            double hold = compress(skills.OfType<DivaHold>().Single().DifficultyValue());

            double combined = Math.Sqrt(
                speed * speed
                + 0.70 * pattern * pattern
                + 0.60 * reading * reading
                + 0.25 * hold * hold);

            return new DivaDifficultyAttributes
            {
                StarRating = combined * star_rating_multiplier,
                Mods = mods,
                MaxCombo = beatmap.HitObjects.Count(h => h is DivaHitObject),
                SpeedDifficulty = speed * star_rating_multiplier,
                PatternDifficulty = pattern * star_rating_multiplier,
                ReadingDifficulty = reading * star_rating_multiplier,
                HoldDifficulty = hold * star_rating_multiplier
            };
        }

        private static double compress(double rawDifficulty)
            => Math.Pow(Math.Max(rawDifficulty, 0), skill_compression_exponent);

        private static List<DivaDifficultyEvent> createEvents(IEnumerable<DivaHitObject> notes)
        {
            var inputs = new List<EventInput>();

            foreach (DivaHitObject note in notes)
            {
                int pressMask = DivaDifficultyHitObject.ActionMask(note.ValidAction);

                if (note is DoublePressButton doublePress)
                    pressMask |= DivaDifficultyHitObject.ActionMask(doublePress.DoubleAction);

                double holdDuration = note is DivaHoldHitObject hold ? Math.Max(hold.Duration, 0) : 0;
                inputs.Add(new EventInput(note.StartTime, pressMask, false, holdDuration));

                if (holdDuration > 0)
                {
                    int releaseMask = DivaDifficultyHitObject.ActionMask(note.ValidAction);
                    inputs.Add(new EventInput(note.StartTime + holdDuration, releaseMask, true, 0));
                }
            }

            var events = new List<DivaDifficultyEvent>();
            DivaDifficultyEvent? currentEvent = null;

            foreach (EventInput input in inputs.OrderBy(i => i.Time))
            {
                if (currentEvent == null || input.Time - currentEvent.StartTime > event_time_epsilon)
                {
                    currentEvent = new DivaDifficultyEvent(input.Time);
                    events.Add(currentEvent);
                }

                if (input.IsRelease)
                    currentEvent.AddReleaseRequirement(input.ActionMask);
                else
                    currentEvent.AddPressRequirement(input.ActionMask, input.HoldDuration);
            }

            return events;
        }

        private static void assignHoldLoad(List<DivaDifficultyEvent> events, IEnumerable<DivaHitObject> notes)
        {
            HoldInterval[] holds = notes
                                   .OfType<DivaHoldHitObject>()
                                   .Where(h => h.Duration > 0)
                                   .Select(h => new HoldInterval(
                                       h.StartTime,
                                       h.EndTime,
                                       DivaDifficultyHitObject.ActionMask(h.ValidAction)))
                                   .OrderBy(h => h.StartTime)
                                   .ToArray();

            int nextHold = 0;
            var activeEnds = new PriorityQueue<HoldInterval, double>();
            var activeActionCounts = new int[4];

            foreach (DivaDifficultyEvent difficultyEvent in events)
            {
                while (nextHold < holds.Length && holds[nextHold].StartTime <= difficultyEvent.StartTime + event_time_epsilon)
                {
                    HoldInterval hold = holds[nextHold];
                    activeEnds.Enqueue(hold, hold.EndTime);
                    adjustActionCounts(activeActionCounts, hold.ActionMask, 1);
                    nextHold++;
                }

                while (activeEnds.TryPeek(out HoldInterval expired, out _) && expired.EndTime < difficultyEvent.StartTime - event_time_epsilon)
                {
                    activeEnds.Dequeue();
                    adjustActionCounts(activeActionCounts, expired.ActionMask, -1);
                }

                difficultyEvent.ActiveHoldCount = activeActionCounts.Count(count => count > 0);
            }
        }

        private static void adjustActionCounts(int[] counts, int actionMask, int delta)
        {
            for (int action = 0; action < counts.Length; action++)
            {
                if ((actionMask & (1 << action)) != 0)
                    counts[action] += delta;
            }
        }

        private static void assignApproachReading(List<DivaDifficultyEvent> events, IEnumerable<DivaHitObject> notes, IBeatmap beatmap)
        {
            ApproachWindow[] windows = notes
                                       .Select(note =>
                                       {
                                           double bpm = beatmap.ControlPointInfo.TimingPointAt(note.StartTime).BPM;

                                           if (bpm <= 0)
                                               bpm = DivaChartConstants.BASE_BPM;

                                           double preempt = DivaChartConstants.StandingPreemptMs(bpm);
                                           return new ApproachWindow(
                                               note.StartTime - preempt,
                                               note.StartTime,
                                               directionBucket(note.ApproachPieceOriginPosition));
                                       })
                                       .OrderBy(w => w.VisibleFrom)
                                       .ToArray();

            int nextWindow = 0;
            int activeCount = 0;
            var directionCounts = new int[8];
            var activeEnds = new PriorityQueue<ApproachWindow, double>();

            foreach (DivaDifficultyEvent difficultyEvent in events)
            {
                while (nextWindow < windows.Length && windows[nextWindow].VisibleFrom <= difficultyEvent.StartTime + event_time_epsilon)
                {
                    ApproachWindow window = windows[nextWindow++];
                    activeEnds.Enqueue(window, window.HitTime);
                    directionCounts[window.DirectionBucket]++;
                    activeCount++;
                }

                while (activeEnds.TryPeek(out ApproachWindow expired, out _) && expired.HitTime < difficultyEvent.StartTime - event_time_epsilon)
                {
                    activeEnds.Dequeue();
                    directionCounts[expired.DirectionBucket]--;
                    activeCount--;
                }

                difficultyEvent.ApproachDensity = activeCount;
                difficultyEvent.ApproachDirectionSpread = directionCounts.Count(count => count > 0);
            }
        }

        private static int directionBucket(osuTK.Vector2 approachOrigin)
        {
            if (approachOrigin.LengthSquared < 1e-4f)
                return 0;

            double angle = Math.Atan2(approachOrigin.Y, approachOrigin.X);
            int bucket = (int)Math.Floor((angle + Math.PI) / (2 * Math.PI) * 8);
            return ((bucket % 8) + 8) % 8;
        }

        private readonly record struct EventInput(double Time, int ActionMask, bool IsRelease, double HoldDuration);

        private readonly record struct HoldInterval(double StartTime, double EndTime, int ActionMask);

        private readonly record struct ApproachWindow(double VisibleFrom, double HitTime, int DirectionBucket);
    }
}
