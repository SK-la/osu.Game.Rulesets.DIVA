// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;

namespace osu.Game.Rulesets.Diva.Difficulty
{
    /// <summary>
    /// Star rating uses osu!standard difficulty skills on DIVA objects projected to hit circles.
    /// </summary>
    public static class DivaDifficulty
    {
        public static DifficultyCalculator CreateCalculator(IWorkingBeatmap beatmap)
            => new OsuDifficultyCalculator(new OsuRuleset().RulesetInfo, new DivaOsuDifficultyWorkingBeatmap(beatmap));
    }
}
