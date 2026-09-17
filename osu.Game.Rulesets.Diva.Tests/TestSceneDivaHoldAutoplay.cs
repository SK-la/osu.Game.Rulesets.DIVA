// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Timing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Diva.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Storyboards;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Diva.Tests
{
    /// <summary>
    /// Autoplay replay coverage for chords and strips: a chord, a strip whose head, body and tail all share their
    /// instant with other notes, and replay rewinds before and inside the strip.
    /// </summary>
    public partial class TestSceneDivaHoldAutoplay : PlayerTestScene
    {
        /// <summary>Gameplay time is driven through the working beatmap's virtual track, as in upstream rewind tests.</summary>
        [Resolved]
        private AudioManager audioManager { get; set; } = null!;

        protected override bool Autoplay => true;

        protected override Ruleset CreatePlayerRuleset() => new DivaRuleset();

        protected override WorkingBeatmap CreateWorkingBeatmap(IBeatmap beatmap, Storyboard storyboard = null!)
            => new ClockBackedTestWorkingBeatmap(beatmap, storyboard, new FramedClock(new ManualClock { Rate = 1 }), audioManager);

        [Test]
        public void TestAutoplayJudgementsSurviveRewind()
        {
            AddUntilStep("wait for track to start running", () => Beatmap.Value.Track.IsRunning);

            seekTo(firstPassEnd);
            AddAssert("first pass judged cleanly", allJudgedCleanly);
            AddAssert("strip judged in first pass", () => judgedHoldCount() == 1);

            // Behind every note, so the replay hands the presses out again on the way forward.
            seekTo(2500);
            AddAssert("rewind reverted the strip", () => holdDrawables().All(h => !h.Judged));

            seekTo(firstPassEnd);
            AddAssert("replayed judgements are clean", allJudgedCleanly);
            AddAssert("strip judged after rewind", () => judgedHoldCount() == 1);
        }

        [Test]
        public void TestStripRevivedInsideItsBodyStaysHittable()
        {
            AddUntilStep("wait for track to start running", () => Beatmap.Value.Track.IsRunning);

            seekTo(firstPassEnd);
            AddAssert("strip judged in first pass", () => judgedHoldCount() == 1);

            // Land inside the body: the strip's judgement is reverted while its key is still held.
            seekTo(3300);
            AddAssert("strip judgement reverted", () => holdDrawables().All(h => !h.Judged));

            // Then behind the head, so the replayed press can be delivered again.
            seekTo(2500);
            seekTo(firstPassEnd);
            AddAssert("strip judged after the body rewind", () => judgedHoldCount() == 1);
            AddAssert("no SAD / wrong / miss anywhere", allJudgedCleanly);
        }

        private const double firstPassEnd = 4600;

        private void seekTo(double time)
        {
            AddStep($"seek to {time}", () => Beatmap.Value.Track.Seek(time));

            // Allow a few frames of lenience for the frame-stable clock to walk to the target.
            AddUntilStep($"gameplay clock reached {time}", () => Precision.AlmostEquals(time, Player.GameplayClockContainer.CurrentTime, 100));
        }

        private bool allJudgedCleanly()
            => Player.Results.All(r => !r.HasResult || r.Type is HitResult.Perfect or HitResult.Great);

        private int judgedHoldCount()
            => holdDrawables().Count(h => h.Judged && h.Result.Type is HitResult.Perfect or HitResult.Great);

        private IEnumerable<DrawableDivaHoldHitObject> holdDrawables()
            => Player.DrawableRuleset.Playfield.AllHitObjects.OfType<DrawableDivaHoldHitObject>();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new Beatmap
            {
                BeatmapInfo =
                {
                    Ruleset = ruleset,
                    Difficulty = new BeatmapDifficulty { OverallDifficulty = 6, CircleSize = 4 },
                }
            };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            // Chord: two notes at the same instant on different buttons.
            addNote(beatmap, 2000, DivaAction.Square, new Vector2(120, 120));
            addNote(beatmap, 2000, DivaAction.Circle, new Vector2(300, 120));

            // Strip: head shares its instant with another note, another note lands mid-body, release shares the tail.
            addHold(beatmap, 3000, 1000, DivaAction.Cross, new Vector2(220, 80));
            addNote(beatmap, 3000, DivaAction.Triangle, new Vector2(100, 200));
            addNote(beatmap, 3500, DivaAction.Circle, new Vector2(360, 200));
            addNote(beatmap, 4000, DivaAction.Square, new Vector2(220, 240));

            return beatmap;
        }

        private static void addNote(Beatmap beatmap, double time, DivaAction action, Vector2 position)
        {
            beatmap.HitObjects.Add(new DivaHitObject
            {
                StartTime = time,
                Position = position,
                ValidAction = action,
                ApproachPieceOriginPosition = new Vector2(500, 0),
            });
        }

        private static void addHold(Beatmap beatmap, double time, double duration, DivaAction action, Vector2 position)
        {
            beatmap.HitObjects.Add(new DivaHoldHitObject
            {
                StartTime = time,
                Duration = duration,
                Position = position,
                ValidAction = action,
                ApproachPieceOriginPosition = new Vector2(500, 0),
            });
        }
    }
}
