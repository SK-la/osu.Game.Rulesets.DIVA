// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Game.Rulesets.Diva.Replays
{
    /// <summary>
    /// Count-based replay action diff. <c>Except</c> would collapse fifty ↑ presses into one.
    /// </summary>
    public static class DivaReplayActionDiff
    {
        public static void Compute(
            IReadOnlyList<DivaAction> last,
            IReadOnlyList<DivaAction> next,
            List<DivaAction> released,
            List<DivaAction> pressed)
        {
            released.Clear();
            pressed.Clear();

            Dictionary<DivaAction, int> lastCounts = countsOf(last);
            Dictionary<DivaAction, int> nextCounts = countsOf(next);

            foreach (KeyValuePair<DivaAction, int> pair in lastCounts)
            {
                nextCounts.TryGetValue(pair.Key, out int nextCount);

                for (int i = nextCount; i < pair.Value; i++)
                    released.Add(pair.Key);
            }

            foreach (KeyValuePair<DivaAction, int> pair in nextCounts)
            {
                lastCounts.TryGetValue(pair.Key, out int lastCount);

                for (int i = lastCount; i < pair.Value; i++)
                    pressed.Add(pair.Key);
            }
        }

        private static Dictionary<DivaAction, int> countsOf(IReadOnlyList<DivaAction> actions)
        {
            var counts = new Dictionary<DivaAction, int>();

            foreach (DivaAction action in actions)
            {
                counts.TryGetValue(action, out int count);
                counts[action] = count + 1;
            }

            return counts;
        }
    }
}
