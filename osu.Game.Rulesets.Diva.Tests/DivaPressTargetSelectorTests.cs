// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Diva.Scoring;

namespace osu.Game.Rulesets.Diva.Tests
{
    /// <summary>
    /// ProjectDIVA judges one note per keystroke. Candidates arrive in ascending press-time order, so their
    /// signed offsets decrease along the list.
    /// </summary>
    [TestFixture]
    public class DivaPressTargetSelectorTests
    {
        private static DivaPressTargetSelector.Candidate matched(double offset, bool strip = false)
            => new DivaPressTargetSelector.Candidate(offset, true, strip, true);

        private static DivaPressTargetSelector.Candidate mismatched(double offset, bool strip = false, bool wrongKeyEligible = true)
            => new DivaPressTargetSelector.Candidate(offset, false, strip, wrongKeyEligible);

        [Test]
        public void Closest_matching_note_wins()
        {
            Assert.That(DivaPressTargetSelector.Select([matched(20), matched(-30), matched(90)]), Is.EqualTo(0));
        }

        [Test]
        public void A_match_beats_a_nearer_mismatch()
        {
            // ProjectDIVA prefers the closest note it can actually hit over a nearer wrong-key one.
            Assert.That(DivaPressTargetSelector.Select([mismatched(-10), matched(90)]), Is.EqualTo(1));
        }

        [Test]
        public void Closest_mismatch_is_penalised_when_no_match_exists()
        {
            Assert.That(DivaPressTargetSelector.Select([mismatched(-200), mismatched(-250)]), Is.EqualTo(0));
        }

        [Test]
        public void Mismatch_is_ignored_when_the_wrong_key_penalty_is_off()
        {
            Assert.That(DivaPressTargetSelector.Select([mismatched(-10, wrongKeyEligible: false)]), Is.EqualTo(-1));
        }

        [Test]
        public void A_strip_never_takes_the_wrong_key_penalty()
        {
            // Strips only ever start on their own head press.
            Assert.That(DivaPressTargetSelector.Select([mismatched(0, strip: true)]), Is.EqualTo(-1));
        }

        [Test]
        public void Nothing_is_selected_when_every_note_is_out_of_reach()
        {
            Assert.That(DivaPressTargetSelector.Select([matched(-400)]), Is.EqualTo(-1));
        }

        [Test]
        public void Scan_stops_once_non_strip_notes_leave_the_keystroke_frame()
        {
            // The match sits 400ms ahead, past the scan bound, so the near mismatch is penalised instead.
            // This also guards against walking a chart's whole future on every press.
            Assert.That(DivaPressTargetSelector.Select([mismatched(-10), matched(-400)]), Is.EqualTo(0));
        }

        [Test]
        public void A_strip_beyond_the_frame_is_still_reachable()
        {
            // A strip whose head is still ahead straddles the frame, so the scan must not stop at it.
            Assert.That(DivaPressTargetSelector.Select([matched(-400, strip: true)]), Is.EqualTo(0));
        }

        [Test]
        public void First_match_displaces_a_nearer_mismatch_seen_before_it()
        {
            // The list is walked in order, so the mismatch is tentatively chosen before the match appears.
            Assert.That(DivaPressTargetSelector.Select([mismatched(-5), matched(250)]), Is.EqualTo(1));
        }

        [Test]
        public void Later_matches_do_not_displace_a_closer_one()
        {
            Assert.That(DivaPressTargetSelector.Select([matched(10), matched(120)]), Is.EqualTo(0));
        }
    }
}
