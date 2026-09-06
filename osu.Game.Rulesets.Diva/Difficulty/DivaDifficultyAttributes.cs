// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Diva.Difficulty
{
    public class DivaDifficultyAttributes : DifficultyAttributes
    {
        public double SpeedDifficulty { get; set; }
        public double PatternDifficulty { get; set; }
        public double ReadingDifficulty { get; set; }
        public double HoldDifficulty { get; set; }
    }
}
