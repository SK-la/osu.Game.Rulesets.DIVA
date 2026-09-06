// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva.Audio;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    public partial class DivaBeatmapConverter : BeatmapConverter<DivaHitObject>
    {
        public int TargetButtons;
        public bool AllowDoubles = true;
        private readonly float osuObjectSize;

        private const float approach_piece_distance = 1200;

        private DivaAction prevAction = DivaAction.Triangle;
        private Vector2 prevObjectPos = Vector2.Zero;
        private int streamLength;

        public DivaBeatmapConverter(IBeatmap beatmap, Ruleset ruleset)
            : base(beatmap, ruleset)
        {
            TargetButtons = beatmap.BeatmapInfo.Difficulty.OverallDifficulty switch
            {
                >= 6.0f => 4,
                >= 4.5f => 3,
                >= 2f => 2,
                _ => 1
            };

            osuObjectSize = (54.4f - 4.48f * beatmap.Difficulty.CircleSize) * 2;
        }

        public override bool CanConvert() => Beatmap.HitObjects.All(h => h is IHasPosition || h is DivaHitObject);

        protected override Beatmap<DivaHitObject> CreateBeatmap() => new DivaBeatmap();

        protected override IEnumerable<DivaHitObject> ConvertHitObject(HitObject original, IBeatmap beatmap, CancellationToken cancellationToken)
        {
            if (original is DivaHitObject native)
            {
                yield return native;

                yield break;
            }

            var positionData = original as IHasPosition;
            // ReSharper disable once SuspiciousTypeConversion.Global
            var comboData = original as IHasCombo;
            bool newCombo = comboData?.NewCombo ?? true;
            Vector2 position = positionData?.Position ?? Vector2.Zero;

            if (DivaActionEncoding.TryParseFromHitObject(original, out DivaAction encodedAction, out bool isHold, out double durationMs, out Vector2? encodedApproach))
            {
                Vector2 approach = encodedApproach ?? getApproachPieceOriginPos(position);

                if (isHold)
                {
                    yield return new DivaHoldHitObject
                    {
                        Samples = [DivaHitSampleInfo.Normal],
                        StartTime = original.StartTime,
                        Duration = durationMs,
                        Position = position,
                        ValidAction = encodedAction,
                        ApproachPieceOriginPosition = approach
                    };
                }
                else
                {
                    yield return new DivaHitObject
                    {
                        Samples = [DivaHitSampleInfo.Normal],
                        StartTime = original.StartTime,
                        Position = position,
                        ValidAction = encodedAction,
                        ApproachPieceOriginPosition = approach
                    };
                }

                // Keep adjacent-note fallback chain updated even when approach was encoded.
                prevObjectPos = position;

                yield break;
            }

            switch (original)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                case IHasPathWithRepeats when AllowDoubles:
                    yield return new DoublePressButton
                    {
                        Samples = [DivaHitSampleInfo.Normal],
                        StartTime = original.StartTime,
                        Position = position,
                        ValidAction = validAction(position, newCombo),
                        DoubleAction = doubleAction(prevAction),
                        ApproachPieceOriginPosition = getApproachPieceOriginPos(position)
                    };

                    break;

                default:
                    yield return new DivaHitObject
                    {
                        Samples = [DivaHitSampleInfo.Normal],
                        StartTime = original.StartTime,
                        Position = position,
                        ValidAction = validAction(position, newCombo),
                        ApproachPieceOriginPosition = getApproachPieceOriginPos(position)
                    };

                    break;
            }
        }

        private static DivaAction doubleAction(DivaAction ac) => ac switch
        {
            DivaAction.Circle => DivaAction.Right,
            DivaAction.Cross => DivaAction.Down,
            DivaAction.Square => DivaAction.Left,
            _ => DivaAction.Up
        };

        private DivaAction validAction(Vector2 currentObjectPos, bool newCombo)
        {
            float distance = (prevObjectPos - currentObjectPos).Length;

            if (distance < osuObjectSize * 1.2 && (streamLength < 20 || !newCombo))
            {
                streamLength++;
                return prevAction;
            }

            streamLength = 0;

            var ac = DivaAction.Circle;

            switch (prevAction)
            {
                case DivaAction.Circle:
                    if (TargetButtons < 2) break;

                    ac = DivaAction.Cross;
                    break;

                case DivaAction.Cross:
                    if (TargetButtons < 3) break;

                    ac = DivaAction.Square;
                    break;

                case DivaAction.Square:
                    if (TargetButtons < 4) break;

                    ac = DivaAction.Triangle;
                    break;
            }

            prevAction = ac;
            return ac;
        }

        private Vector2 getApproachPieceOriginPos(Vector2 currentObjectPos)
        {
            Vector2 dir = prevObjectPos - currentObjectPos;
            prevObjectPos = currentObjectPos;

            if (dir == Vector2.Zero)
                return new Vector2(approach_piece_distance);

            return dir.Normalized() * approach_piece_distance;
        }
    }
}
