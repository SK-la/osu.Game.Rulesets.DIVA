// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Game.Rulesets.Diva.Scoring
{
    /// <summary>
    ///     Picks the single note a press judges, mirroring ProjectDIVA's <c>NoteMana::OnKeyEvent</c>.
    /// </summary>
    /// <remarks>
    ///     ProjectDIVA chooses one note per keystroke, not every note inside the window: the closest note matching
    ///     the button wins outright, and only when the scanned range holds no match does the closest mismatching
    ///     non-strip note take the wrong-key hit. Kept free of drawables so the choice can be asserted directly.
    /// </remarks>
    public static class DivaPressTargetSelector
    {
        /// <summary>
        ///     One note a press may be aimed at. Candidates are supplied in ascending press-time order, which is
        ///     the order ProjectDIVA scans its unit list in.
        /// </summary>
        /// <param name="PressOffset">Signed distance between the press and the note's press time.</param>
        /// <param name="Matches">Whether the button pressed is one this note accepts.</param>
        /// <param name="IsStrip">Whether this note is a hold/strip. Strips never take the wrong-key hit.</param>
        /// <param name="WrongKeyEligible">Whether the wrong-key penalty is enabled for this note.</param>
        public readonly record struct Candidate(double PressOffset, bool Matches, bool IsStrip, bool WrongKeyEligible);

        /// <summary>
        ///     Returns the index of the chosen candidate in <paramref name="candidates"/>, or -1 when the press
        ///     judges nothing.
        /// </summary>
        public static int Select(IReadOnlyList<Candidate> candidates)
        {
            int best = -1;
            double bestDistance = double.MaxValue;
            bool matched = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];

                // ProjectDIVA ends its scan once non-strip notes fall out of the keystroke frame. A strip may lie
                // outside it: one whose head is still ahead is a valid future press target.
                if (candidate.PressOffset < -DivaHitJudgementEvaluator.SAD_WINDOW && !candidate.IsStrip)
                    break;

                double distance = Math.Abs(candidate.PressOffset);

                if (candidate.Matches)
                {
                    // The first match displaces whatever mismatch was tentatively the closest.
                    if (!matched || distance < bestDistance)
                    {
                        matched = true;
                        bestDistance = distance;
                        best = i;
                    }
                }
                else if (!matched && candidate.WrongKeyEligible && !candidate.IsStrip && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }
    }
}
