// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Diva.Difficulty.Preprocessing
{
    internal enum DivaLogicalAction
    {
        Square,
        Triangle,
        Circle,
        Cross
    }

    internal sealed class DivaDifficultyEvent
    {
        private readonly List<int> pressRequirements = new List<int>();
        private readonly List<int> releaseRequirements = new List<int>();

        public double StartTime { get; }

        public IReadOnlyList<int> PressRequirements => pressRequirements;

        public IReadOnlyList<int> ReleaseRequirements => releaseRequirements;

        public IReadOnlyList<int> InputOptions { get; private set; } = Array.Empty<int>();

        public int PressCount { get; private set; }

        public int ReleaseCount { get; private set; }

        public int ActiveHoldCount { get; set; }

        public int StartingHoldCount { get; private set; }

        public double LongestStartingHoldDuration { get; private set; }

        public int ApproachDensity { get; set; }

        public int ApproachDirectionSpread { get; set; }

        public bool HasReleaseConflict => ReleaseCount > 0 && PressCount > 0;

        public DivaDifficultyEvent(double startTime)
        {
            StartTime = startTime;
        }

        public void AddPressRequirement(int actionMask, double holdDuration)
        {
            if (!pressRequirements.Contains(actionMask))
                pressRequirements.Add(actionMask);

            if (holdDuration > 0)
            {
                StartingHoldCount++;
                LongestStartingHoldDuration = Math.Max(LongestStartingHoldDuration, holdDuration);
            }
        }

        public void AddReleaseRequirement(int actionMask)
        {
            if (!releaseRequirements.Contains(actionMask))
                releaseRequirements.Add(actionMask);
        }

        public void Finalise()
        {
            PressCount = minimumRequiredInputs(pressRequirements);
            ReleaseCount = minimumRequiredInputs(releaseRequirements);
            InputOptions = minimumInputMasks(pressRequirements.Concat(releaseRequirements)).ToArray();
        }

        private static int minimumRequiredInputs(IReadOnlyCollection<int> requirements)
            => requirements.Count == 0 ? 0 : bitCount(minimumInputMasks(requirements).First());

        private static IEnumerable<int> minimumInputMasks(IEnumerable<int> requirements)
        {
            int[] required = requirements.Distinct().ToArray();

            if (required.Length == 0)
                return [0];

            var valid = new List<int>();
            int minimumCount = int.MaxValue;

            for (int candidate = 1; candidate < 1 << 4; candidate++)
            {
                if (required.Any(requirement => (candidate & requirement) == 0))
                    continue;

                int count = bitCount(candidate);

                if (count < minimumCount)
                {
                    minimumCount = count;
                    valid.Clear();
                }

                if (count == minimumCount)
                    valid.Add(candidate);
            }

            return valid;
        }

        private static int bitCount(int value)
        {
            int count = 0;

            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }
    }

    public class DivaDifficultyHitObject : DifficultyHitObject
    {
        public IReadOnlyList<int> InputOptions { get; }

        public int PressCount { get; }

        public int ReleaseCount { get; }

        public int ActiveHoldCount { get; }

        public int StartingHoldCount { get; }

        public double LongestStartingHoldDuration { get; }

        public int ApproachDensity { get; }

        public int ApproachDirectionSpread { get; }

        public bool HasReleaseConflict { get; }

        internal DivaDifficultyHitObject(
            HitObject eventObject,
            HitObject previousEventObject,
            DivaDifficultyEvent difficultyEvent,
            double clockRate,
            List<DifficultyHitObject> objects,
            int index)
            : base(eventObject, previousEventObject, clockRate, objects, index)
        {
            InputOptions = difficultyEvent.InputOptions;
            PressCount = difficultyEvent.PressCount;
            ReleaseCount = difficultyEvent.ReleaseCount;
            ActiveHoldCount = difficultyEvent.ActiveHoldCount;
            StartingHoldCount = difficultyEvent.StartingHoldCount;
            LongestStartingHoldDuration = difficultyEvent.LongestStartingHoldDuration / clockRate;
            ApproachDensity = difficultyEvent.ApproachDensity;
            ApproachDirectionSpread = difficultyEvent.ApproachDirectionSpread;
            HasReleaseConflict = difficultyEvent.HasReleaseConflict;
        }

        internal static DivaLogicalAction NormaliseAction(DivaAction action) => action switch
        {
            DivaAction.Square or DivaAction.Left => DivaLogicalAction.Square,
            DivaAction.Triangle or DivaAction.Up => DivaLogicalAction.Triangle,
            DivaAction.Circle or DivaAction.Right => DivaLogicalAction.Circle,
            DivaAction.Cross or DivaAction.Down => DivaLogicalAction.Cross,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        internal static int ActionMask(DivaAction action) => 1 << (int)NormaliseAction(action);
    }
}
