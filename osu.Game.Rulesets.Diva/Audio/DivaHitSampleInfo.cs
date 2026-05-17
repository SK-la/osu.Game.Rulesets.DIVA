// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Audio;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Rulesets.Diva.Audio
{
    /// <summary>
    /// Maps hit object samples to embedded ruleset resources under <c>Resources/Samples/Gameplay/</c>.
    /// </summary>
    public class DivaHitSampleInfo : HitSampleInfo, IEquatable<DivaHitSampleInfo>
    {
        public const string NORMAL_LOOKUP = "Gameplay/hit-normal";
        public const string SWEEP_LOOKUP = "Gameplay/sweep";

        private readonly string[] lookupNames;

        public DivaHitSampleInfo(string lookupName)
            : base(string.Empty, SampleControlPoint.DEFAULT_BANK, useBeatmapSamples: false)
        {
            lookupNames = [lookupName];
        }

        public override IEnumerable<string> LookupNames => lookupNames;

        public static DivaHitSampleInfo Normal { get; } = new DivaHitSampleInfo(NORMAL_LOOKUP);

        public static DivaHitSampleInfo Sweep { get; } = new DivaHitSampleInfo(SWEEP_LOOKUP);

        public bool Equals(DivaHitSampleInfo other)
            => other != null && lookupNames[0] == other.lookupNames[0];

        public override bool Equals(object obj)
            => obj is DivaHitSampleInfo other && Equals(other);

        public override int GetHashCode() => lookupNames[0].GetHashCode();
    }
}
