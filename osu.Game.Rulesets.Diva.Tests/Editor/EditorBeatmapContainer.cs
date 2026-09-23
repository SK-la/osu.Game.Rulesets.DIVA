// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Diva;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.Diva.Tests.Editor
{
    /// <summary>
    ///     Caches an <see cref="EditorBeatmap"/> for whatever is placed inside, so composer and setup
    ///     sections can be tested without an <see cref="osu.Game.Screens.Edit.Editor"/>.
    /// </summary>
    internal partial class EditorBeatmapContainer : PopoverContainer
    {
        private readonly IWorkingBeatmap working;

        public EditorBeatmap EditorBeatmap { get; private set; } = null!;

        public EditorBeatmapContainer(IWorkingBeatmap working)
        {
            this.working = working;
            RelativeSizeAxes = Axes.Both;
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

            EditorBeatmap = new EditorBeatmap(working.GetPlayableBeatmap(new DivaRuleset().RulesetInfo));
            dependencies.CacheAs(EditorBeatmap);
            dependencies.CacheAs<IBeatSnapProvider>(EditorBeatmap);

            return dependencies;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Add(EditorBeatmap);
        }
    }
}
