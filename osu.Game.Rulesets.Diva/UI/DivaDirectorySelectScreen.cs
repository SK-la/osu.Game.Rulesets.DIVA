// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Configuration;
using osu.Game.Screens;
using osuTK;

namespace osu.Game.Rulesets.Diva.UI
{
    public partial class DivaDirectorySelectScreen : OsuScreen
    {
        private readonly DivaRulesetConfigManager config;
        private readonly List<string> stagedPaths;
        private readonly Action<IReadOnlyList<string>, bool>? applyAction;
        private readonly Bindable<bool> importToRealm = new BindableBool(true);

        private OsuDirectorySelector directorySelector = null!;
        private FillFlowContainer pathList = null!;

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        [Resolved(canBeNull: true)]
        private IDialogOverlay? dialogOverlay { get; set; }

        [Resolved(canBeNull: true)]
        private INotificationOverlay? notifications { get; set; }

        private bool separateAudioRunning;

        public DivaDirectorySelectScreen(DivaRulesetConfigManager config, Action<IReadOnlyList<string>, bool>? applyAction = null)
        {
            this.config = config;
            this.applyAction = applyAction;
            stagedPaths = config.GetLibraryPaths().ToList();
            importToRealm.Value = config.GetImportToRealm();
#if NET8_0
            importToRealm.Value = true;
            importToRealm.Disabled = true;
#endif
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            base.OnSuspending(e);
            this.FadeOut(250);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            string? initialPath = stagedPaths.LastOrDefault();

            InternalChild = new Container
            {
                Masking = true,
                CornerRadius = 10,
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(0.7f, 0.85f),
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        RowDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize)
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 6),
                                    Margin = new MarginPadding(10),
                                    Children = new Drawable[]
                                    {
                                        new TooltipTextFlowContainer
                                        {
                                            Text = "DIVA beatmap library paths",
                                            TextAnchor = Anchor.TopCentre,
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            TooltipText = "Select folders that contain ProjectDIVA song packages (folders with .diva files)."
                                        },
                                        new TooltipTextFlowContainer
                                        {
                                            Text = "Add one or more song library roots, then Apply. Import mode converts charts into the osu! library.",
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            TooltipText = "Ez2Lazer can disable Import to mount folders externally without copying files."
                                        },
                                        new SettingsCheckbox
                                        {
                                            LabelText = "Import charts into Realm",
                                            TooltipText = "On: convert .diva → .osu and import. Off (Ez2Lazer only): external folder mount.",
                                            Current = importToRealm
                                        }
                                    }
                                }
                            },
                            new Drawable[]
                            {
                                new GridContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    RowDimensions = new[]
                                    {
                                        new Dimension(GridSizeMode.Absolute, 240),
                                        new Dimension()
                                    },
                                    Content = new[]
                                    {
                                        new Drawable[]
                                        {
                                            directorySelector = new OsuDirectorySelector(initialPath)
                                            {
                                                RelativeSizeAxes = Axes.Both
                                            }
                                        },
                                        new Drawable[]
                                        {
                                            new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Direction = FillDirection.Vertical,
                                                Spacing = new Vector2(0, 8),
                                                Children = new Drawable[]
                                                {
                                                    new FillFlowContainer
                                                    {
                                                        RelativeSizeAxes = Axes.X,
                                                        AutoSizeAxes = Axes.Y,
                                                        Direction = FillDirection.Horizontal,
                                                        Spacing = new Vector2(10, 0),
                                                        Children = new Drawable[]
                                                        {
                                                            new RoundedButton
                                                            {
                                                                Width = 180,
                                                                Text = "Add current path",
                                                                Action = addSelectedPath
                                                            },
                                                            new RoundedButton
                                                            {
                                                                Width = 140,
                                                                Text = "Clear list",
                                                                Action = () =>
                                                                {
                                                                    stagedPaths.Clear();
                                                                    refreshPathList();
                                                                }
                                                            }
                                                        }
                                                    },
                                                    new OsuTextFlowContainer(cp => cp.Font = OsuFont.Default.With(size: 16))
                                                    {
                                                        Text = "Added paths",
                                                        RelativeSizeAxes = Axes.X,
                                                        AutoSizeAxes = Axes.Y
                                                    },
                                                    new OsuScrollContainer
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        Child = pathList = new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Vertical,
                                                            Spacing = new Vector2(0, 2)
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(10),
                                    Padding = new MarginPadding(10),
                                    Children = new Drawable[]
                                    {
                                        new RoundedButton
                                        {
                                            Width = 160,
                                            Text = "Close",
                                            Action = this.Exit
                                        },
                                        new RoundedButton
                                        {
                                            Width = 160,
                                            Text = "Apply",
                                            Action = applyPaths
                                        },
                                        new RoundedButton
                                        {
                                            Width = 180,
                                            Text = "分离音轨",
                                            Action = requestSeparateAudio
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            refreshPathList();
        }

        private void addSelectedPath()
        {
            string? selectedPath = directorySelector.CurrentPath.Value?.FullName;

            if (string.IsNullOrWhiteSpace(selectedPath) || !Directory.Exists(selectedPath))
                return;

            if (stagedPaths.Any(path => string.Equals(path, selectedPath, StringComparison.OrdinalIgnoreCase)))
                return;

            stagedPaths.Add(selectedPath);
            refreshPathList();
        }

        private void applyPaths()
        {
            config.PersistLibraryPaths(stagedPaths);
            config.PersistImportToRealm(importToRealm.Value);
            applyAction?.Invoke(stagedPaths.ToArray(), importToRealm.Value);
        }

        private void requestSeparateAudio()
        {
            if (separateAudioRunning)
                return;

            if (stagedPaths.Count == 0)
            {
                notifications?.Post(new SimpleNotification
                {
                    Text = "Add at least one library path before separating audio tracks."
                });
                return;
            }

            dialogOverlay?.Push(new SeparateAudioDialog(runSeparateAudio));
        }

        private async void runSeparateAudio()
        {
            if (separateAudioRunning)
                return;

            separateAudioRunning = true;

            try
            {
                string[] roots = stagedPaths.ToArray();

                DivaAudioTrackSeparator.SeparationResult result = await Task.Run(() => DivaAudioTrackSeparator.Separate(roots))
                                                                           .ConfigureAwait(true);

                notifications?.Post(new SimpleNotification { Text = $"[DIVA] {result.Summary}" });

                // Refresh Realm / import so Metadata.AudioFile picks up WAV associations.
                applyPaths();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[DIVA] Separate audio failed.");
                notifications?.Post(new SimpleNotification { Text = $"[DIVA] Separate audio failed: {ex.Message}" });
            }
            finally
            {
                separateAudioRunning = false;
            }
        }

        private void refreshPathList()
        {
            pathList.Clear();

            if (stagedPaths.Count == 0)
            {
                pathList.Add(new OsuTextFlowContainer(cp => cp.Font = OsuFont.Default.With(size: 14))
                {
                    Text = "No paths added yet.",
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                });
                return;
            }

            foreach (string path in stagedPaths)
            {
                pathList.Add(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 40,
                    Masking = true,
                    CornerRadius = 6,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background3
                        },
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Left = 10, Right = 6, Top = 6, Bottom = 6 },
                            ColumnDimensions = new[]
                            {
                                new Dimension(),
                                new Dimension(GridSizeMode.Absolute, 92)
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new TruncatingSpriteText
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Text = path,
                                        Font = OsuFont.Default.With(size: 16)
                                    },
                                    new RoundedButton
                                    {
                                        Width = 86,
                                        Height = 24,
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Text = "Remove",
                                        Action = () => requestRemovePath(path)
                                    }
                                }
                            }
                        }
                    }
                });
            }
        }

        private void requestRemovePath(string path)
        {
            dialogOverlay?.Push(new RemovePathDialog(path, () =>
            {
                stagedPaths.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
                refreshPathList();
            }));
        }

        private partial class RemovePathDialog : DangerousActionDialog
        {
            public RemovePathDialog(string path, Action onConfirm)
            {
                HeaderText = "Remove library path?";
                BodyText = path;
                DangerousAction = onConfirm;
            }
        }

        private partial class SeparateAudioDialog : DangerousActionDialog
        {
            public SeparateAudioDialog(Action onConfirm)
            {
                HeaderText = "分离音轨并修改谱面？";
                BodyText =
                    "将扫描当前库路径下的歌曲：若 WAV 文件夹没有音频，会尝试用 ffmpeg 从 RES 视频抽出音轨写入 WAV，"
                    + "并修改该歌曲下所有 .diva 的 wav 关联。此操作会改磁盘上的谱面文件，请先备份。完成后会自动 Apply。";
                DangerousAction = onConfirm;
            }
        }

        private partial class TooltipTextFlowContainer : OsuTextFlowContainer, IHasTooltip
        {
            public LocalisableString TooltipText { get; set; } = string.Empty;
        }
    }
}
