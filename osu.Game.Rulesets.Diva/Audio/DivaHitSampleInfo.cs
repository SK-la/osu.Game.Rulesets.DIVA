// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Audio;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Rulesets.Diva.Audio
{
    /// <summary>
    /// Maps hit samples to embedded ruleset resources under <c>Resources/Samples/Gameplay/</c>
    /// (ProjectDIVA <c>gamedata/hit.wav</c>).
    /// </summary>
    public class DivaHitSampleInfo : HitSampleInfo, IEquatable<DivaHitSampleInfo>
    {
        public const string NORMAL_LOOKUP = "Gameplay/hit-normal";

        public const string RESULT_MISTAKE_LOOKUP = "Gameplay/result-mistake";
        public const string RESULT_CHEAP_LOOKUP = "Gameplay/result-cheap";
        public const string RESULT_STANDARD_LOOKUP = "Gameplay/result-standard";
        public const string RESULT_GREAT_LOOKUP = "Gameplay/result-great";
        public const string RESULT_PERFECT_LOOKUP = "Gameplay/result-perfect";

        /// <summary>
        /// Ruleset <c>SampleStore</c> only auto-appends wav/mp3; embedded DIVA SE are ogg and need an explicit suffix.
        /// </summary>
        public static string ToSampleStoreName(string lookupWithoutExtension) => lookupWithoutExtension + ".ogg";

        /// <summary>ProjectDIVA <c>GREATPES</c> — score / maxScore threshold for Great.</summary>
        public const double RESULT_GREAT_RATIO = 0.7;

        /// <summary>ProjectDIVA <c>STANDARDPES</c> — score / maxScore threshold for Standard.</summary>
        public const double RESULT_STANDARD_RATIO = 0.5;

        private readonly string[] lookupNames;

        public DivaHitSampleInfo(string lookupName, int volume = 100)
            : base(string.Empty, SampleControlPoint.DEFAULT_BANK, volume: volume, useBeatmapSamples: false)
        {
            lookupNames = [lookupName];
        }

        public override IEnumerable<string> LookupNames => lookupNames;

        public static DivaHitSampleInfo Normal { get; } = new DivaHitSampleInfo(NORMAL_LOOKUP);

        public static SampleInfo ResultMistake { get; } = new SampleInfo(RESULT_MISTAKE_LOOKUP);
        public static SampleInfo ResultCheap { get; } = new SampleInfo(RESULT_CHEAP_LOOKUP);
        public static SampleInfo ResultStandard { get; } = new SampleInfo(RESULT_STANDARD_LOOKUP);
        public static SampleInfo ResultGreat { get; } = new SampleInfo(RESULT_GREAT_LOOKUP);
        public static SampleInfo ResultPerfect { get; } = new SampleInfo(RESULT_PERFECT_LOOKUP);

        /// <summary>
        /// ProjectDIVA <c>PlayHit(factor)</c> volume: <c>min(1, 0.5 + factor * 0.25)</c> mapped to 0–100.
        /// </summary>
        public static DivaHitSampleInfo CreateHit(float factor)
        {
            int volume = (int)Math.Clamp(Math.Round(100 * Math.Min(1f, 0.5f + factor * 0.25f)), 1, 100);
            return new DivaHitSampleInfo(NORMAL_LOOKUP, volume);
        }

        public bool Equals(DivaHitSampleInfo? other) => other != null && lookupNames[0] == other.lookupNames[0] && Volume == other.Volume;

        public override bool Equals(object? obj) => obj is DivaHitSampleInfo other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(lookupNames[0], Volume);
    }
}
