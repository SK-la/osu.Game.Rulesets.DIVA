// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Audio;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Diva.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Diva.Objects
{
    public partial class DivaHitObject : HitObject, IHasPosition
    {
        public override Judgement CreateJudgement() => new Judgement();
        protected override HitWindows CreateHitWindows() => new DivaHitWindows();

        public Vector2 Position { get; set; }

        public float X
        {
            get => Position.X;
            set => Position = new Vector2(value, Position.Y);
        }

        public float Y
        {
            get => Position.Y;
            set => Position = new Vector2(Position.X, value);
        }

        public DivaAction ValidAction;
        public Vector2 ApproachPieceOriginPosition;

        /// <summary>
        ///     ProjectDIVA <c>_key</c>: the note's <c>#WAV</c> table slot, i.e. which sound the game plays.
        ///     <see langword="null"/> keeps whatever the source chart had for this frame and button.
        /// </summary>
        /// <remarks>
        ///     Display / audio metadata only — which button to press comes from <see cref="ValidAction"/>.
        /// </remarks>
        public int? WavKey;

        /// <summary>
        ///     TODO(editor): note-local multi-node flight path (osu!std slider control points).
        ///     Empty today, in which case the path comes from <see cref="ApproachPieceOriginPosition"/>
        ///     sampled with the configured flight curve. Not yet used by rendering, SR or export.
        /// </summary>
        /// <remarks>
        ///     Unit quirk to settle before an editor writes any node: ProjectDIVA's
        ///     <c>(_tailx, _taily)</c> is in raw grid units while the note position is playfield pixels,
        ///     and <c>DivaActionEncoding.ComputeApproachOrigin</c> reproduces that mix. Nodes should be
        ///     defined in one unit (grid) and converted once.
        /// </remarks>
        public List<Vector2>? ApproachPathNodes;

        /// <summary>
        ///     The same note as a hold of <paramref name="duration"/> ms. The reference editor treats tap and
        ///     hold as one record with an optional length, so nothing but the length changes.
        /// </summary>
        public DivaHoldHitObject AsHold(double duration)
        {
            var hold = new DivaHoldHitObject { Duration = duration };
            CopyStateTo(hold);
            return hold;
        }

        /// <summary>
        ///     Copies everything that identifies the note — time, cell, button, flight vector and key sound —
        ///     leaving the concrete type (and thus tap/hold) to the caller.
        /// </summary>
        private protected void CopyStateTo(DivaHitObject target)
        {
            target.StartTime = StartTime;
            target.Position = Position;
            target.ValidAction = ValidAction;
            target.ApproachPieceOriginPosition = ApproachPieceOriginPosition;
            target.ApproachPathNodes = ApproachPathNodes;
            target.WavKey = WavKey;
            target.Samples = new List<HitSampleInfo>(Samples);
        }
    }
}
