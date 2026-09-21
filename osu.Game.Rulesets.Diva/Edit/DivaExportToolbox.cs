// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit
{
    public partial class DivaExportToolbox : EditorToolboxGroup
    {
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private INotificationOverlay? notifications { get; set; }

        public DivaExportToolbox()
            : base("export")
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(5),
                Children =
                [
                    new RoundedButton
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 30,
                        Text = DivaStrings.EDITOR_EXPORT_DIVA,
                        Action = exportDiva
                    }
                ]
            };
        }

        private void exportDiva()
        {
            try
            {
                DivaChart chart = DivaChartBuilder.FromBeatmap(editorBeatmap);
                string destination = resolveDestination(chart);

                DivaChartFileWriter.WriteToFile(chart, destination);
                notifications?.Post(new SimpleNotification
                {
                    Text = DivaStrings.Editor_ExportDivaComplete(destination),
                    Icon = FontAwesome.Solid.Check
                });
            }
            catch (Exception ex)
            {
                notifications?.Post(new SimpleErrorNotification
                {
                    Text = DivaStrings.Editor_ExportDivaFailed(ex.Message)
                });
            }
        }

        private string resolveDestination(DivaChart chart)
        {
            if (!string.IsNullOrWhiteSpace(chart.Metadata.SourcePath) && File.Exists(chart.Metadata.SourcePath))
                return chart.Metadata.SourcePath;

#if DIVA_EZ2LAZER
            string? contentRoot = editorBeatmap.BeatmapInfo.BeatmapSet?.GetEffectiveExternalContentRoot();
#else
            string? contentRoot = null;
#endif
            string? relative = editorBeatmap.BeatmapInfo.Path;

            if (!string.IsNullOrWhiteSpace(contentRoot) && !string.IsNullOrWhiteSpace(relative))
            {
                string full = Path.Combine(contentRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                if (full.EndsWith(".diva", StringComparison.OrdinalIgnoreCase))
                    return full;
            }

            string fileName = Path.GetFileNameWithoutExtension(relative) ?? editorBeatmap.BeatmapInfo.DifficultyName;
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "chart";

            string folder = !string.IsNullOrWhiteSpace(contentRoot)
                ? contentRoot
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            return Path.Combine(folder, fileName + ".diva");
        }
    }
}
