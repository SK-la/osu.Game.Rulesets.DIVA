// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    public interface IDivaChartFormat
    {
        string FormatName { get; }

        bool CanHandle(string path, Stream? peekStream = null);

        DivaChartMetadata ReadMetadata(string path);

        DivaChart Decode(string path);
    }
}
