// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Configuration;
using osu.Game.Screens;
using osu.Game.Screens.Menu;
using OsuSongSelect = osu.Game.Screens.Select.SongSelect;

namespace osu.Game.Rulesets.Diva.UI
{
    public partial class DivaSettingsSubsection : RulesetSettingsSubsection
    {
#if NET8_0
        protected override osu.Framework.Localisation.LocalisableString Header => new osu.Framework.Localisation.LocalisableString("osu!DIVA");
#endif

        private readonly Ruleset ruleset;
        private DivaRulesetConfigManager divaConfig = null!;
        private SettingsNote cacheStatusNote = null!;

        [Resolved(canBeNull: true)]
        private OsuGame? game { get; set; }

        [Resolved(canBeNull: true)]
        private IPerformFromScreenRunner? performFromScreen { get; set; }

        [Resolved]
        private INotificationOverlay? notificationOverlay { get; set; }

        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmapManager { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        private CancellationTokenSource? importCts;

        public DivaSettingsSubsection(Ruleset ruleset)
            : base(ruleset)
        {
            this.ruleset = ruleset;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            divaConfig = (DivaRulesetConfigManager)Config;

            if (divaConfig == null)
                return;

            Bindable<bool>? importBindable = divaConfig.GetBindable<bool>(DivaRulesetSettings.ImportToRealm);
#if NET8_0
            importBindable.Value = true;
            importBindable.Disabled = true;
#endif

            Children = new Drawable[]
            {
                new SettingsButtonV2
                {
                    Text = "Open beatmap library path wizard",
                    Action = selectPath
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = SettingsPanel.CONTENT_PADDING,
                    Child = cacheStatusNote = new SettingsNote
                    {
                        RelativeSizeAxes = Axes.X
                    }
                },
                new SettingsCheckbox
                {
                    LabelText = "Import charts into Realm",
                    TooltipText = "When enabled, .diva charts are converted to .osu and imported into the osu! library. On Ez2Lazer, disable to mount folders externally instead.",
                    Current = importBindable
                },
                new SettingsCheckbox
                {
                    LabelText = "Use XBox Button Icons",
                    Current = divaConfig.GetBindable<bool>(DivaRulesetSettings.UseXBoxButtons)
                },
                new SettingsCheckbox
                {
                    LabelText = "Enable visual bursts",
                    Current = divaConfig.GetBindable<bool>(DivaRulesetSettings.EnableVisualBursts)
                },
                new SettingsCheckbox
                {
                    LabelText = "Enable built-in hit sounds",
                    TooltipText = "Play the ruleset-embedded ProjectDIVA hit SE on key presses. Does not affect end-of-song grade VO.",
                    Current = divaConfig.GetBindable<bool>(DivaRulesetSettings.EnableBuiltinHitSounds)
                },
                new SettingsCheckbox
                {
                    LabelText = "Judgement lock",
                    TooltipText = "ProjectDIVA Strict/Standard: when enabled, a wrong key within the timing window consumes the note (WRONG). When disabled, wrong keys are ignored.",
                    Current = divaConfig.GetBindable<bool>(DivaRulesetSettings.JudgementLock)
                },
                new SettingsSlider<double>
                {
                    LabelText = "Input offset (ms)",
                    TooltipText = "Adjusts hit timing used for judgement (like Offset Plus). Does not change audio/clock sync. Range ±200ms.",
                    Current = divaConfig.GetBindable<double>(DivaRulesetSettings.InputOffset)
                },
                new SettingsSlider<double>
                {
                    LabelText = "Note size",
                    Current = divaConfig.GetBindable<double>(DivaRulesetSettings.NoteSize)
                },
                new SettingsSlider<double>
                {
                    LabelText = "Approach preempt scale",
                    TooltipText = "Multiplier on ProjectDIVA note_standing×BPM (1.0 = PD default). Lower = notes appear later.",
                    Current = divaConfig.GetBindable<double>(DivaRulesetSettings.ApproachPreemptScale)
                },
                new SettingsSlider<double>
                {
                    LabelText = "Hit Explosion alpha",
                    Current = divaConfig.GetBindable<double>(DivaRulesetSettings.HitExplosionAlpha)
                }
            };

            updatePathStatus();
        }

        private void selectPath()
        {
            IPerformFromScreenRunner? runner = performFromScreen ?? game;

            if (runner == null)
            {
                notificationOverlay?.Post(new SimpleErrorNotification { Text = "Cannot open path wizard from this screen." });
                return;
            }

            runner.PerformFromScreen(screen => { screen.Push(new DivaDirectorySelectScreen(divaConfig, applyPathsAndProcess)); }, new[] { typeof(MainMenu), typeof(OsuSongSelect) });
        }

        private void applyPathsAndProcess(IReadOnlyList<string> paths, bool importToRealm)
        {
            divaConfig.PersistLibraryPaths(paths);
            divaConfig.PersistImportToRealm(importToRealm);
            updatePathStatus();
            startProcess(paths, importToRealm);
        }

        private void startProcess(IReadOnlyList<string> paths, bool importToRealm)
        {
            if (paths.Count > 0 && !paths.Any(Directory.Exists))
            {
                notificationOverlay?.Post(new SimpleErrorNotification { Text = "Add at least one valid folder path first." });
                return;
            }

            importCts?.Cancel();
            importCts = new CancellationTokenSource();
            CancellationToken token = importCts.Token;

            bool clearing = paths.Count == 0;
            string title = clearing
                ? "Clearing DIVA library paths…"
                : importToRealm
                    ? "Importing DIVA charts…"
                    : "Linking DIVA external library…";

            if (IsLoaded) cacheStatusNote.Current.Value = new SettingsNote.Data(title, SettingsNote.Type.Informational);

            var notification = new ProgressNotification
            {
                Text = title,
                CompletionText = clearing ? "DIVA library paths cleared." : "DIVA library update complete.",
                State = ProgressNotificationState.Active,
                Progress = 0
            };

            notificationOverlay?.Post(notification);

            Task.Run(async () =>
            {
                try
                {
                    void report(DivaLibraryImportPipeline.ImportProgress p)
                    {
                        if (notification.State is ProgressNotificationState.Cancelled or ProgressNotificationState.Completed)
                            return;

                        notification.Progress = (float)Math.Clamp(p.Progress, 0, 1);
                        notification.Text = p.StatusMessage;
                    }

                    DivaCollectionSynchronizer.SyncResult? collectionSync = null;

                    if (clearing)
                        report(new DivaLibraryImportPipeline.ImportProgress(1, "Paths cleared."));
                    else if (importToRealm)
                    {
                        DivaLibraryImportPipeline.ImportResult importResult =
                            await DivaLibraryImportPipeline.ImportToRealmAsync(beatmapManager, storage, paths, report, token).ConfigureAwait(false);
                        collectionSync = DivaCollectionSynchronizer.Apply(realm, importResult.CollectionHashesByPath);
                    }
                    else
                    {
#if NET10_0
                        await DivaLibraryImportPipeline.SynchronizeExternalAsync(realm, storage, beatmapManager, ruleset.RulesetInfo, paths, report, token).ConfigureAwait(false);
                        collectionSync = DivaCollectionSynchronizer.SyncFromLibraryPaths(realm, paths);
#else
                        throw new NotSupportedException("External library linking requires Ez2Lazer (net10).");
#endif
                    }

                    string completion = clearing ? "DIVA library paths cleared." : "DIVA library update complete.";

                    if (collectionSync is { CollectionCount: > 0 } sync)
                    {
                        completion += $" Synced {sync.CollectionCount} path collection(s), {sync.ChartCount} chart(s).";
                        notificationOverlay?.Post(new SimpleNotification
                        {
                            Text = $"Synced {sync.CollectionCount} DIVA path collection(s), {sync.ChartCount} chart(s)."
                        });
                    }

                    notification.CompletionText = completion;
                    notification.State = ProgressNotificationState.Completed;

                    if (IsLoaded)
                        Schedule(updatePathStatus);
                }
                catch (OperationCanceledException)
                {
                    notification.State = ProgressNotificationState.Cancelled;
                }
                catch (Exception ex)
                {
                    notification.State = ProgressNotificationState.Cancelled;
                    Logger.Error(ex, "[DIVA] Library update failed");
                    notificationOverlay?.Post(new SimpleErrorNotification
                    {
                        Text = $"DIVA library update failed: {ex.GetType().Name}: {ex.Message}"
                    });
                }
            }, token);
        }

        private void updatePathStatus()
        {
            IReadOnlyList<string> paths = divaConfig.GetLibraryPaths();

            if (paths.Count == 0)
            {
                cacheStatusNote.Current.Value = new SettingsNote.Data("未配置曲库路径。", SettingsNote.Type.Informational);
                return;
            }

            IReadOnlyList<DivaSongFolder> songs = DivaLibraryScanner.Scan(paths);
            int difficultyCount = songs.Sum(s => s.ChartPaths.Count);

            cacheStatusNote.Current.Value = new SettingsNote.Data(
                $"路径数 {paths.Count}，总歌曲数 {songs.Count}，总难度数 {difficultyCount}",
                SettingsNote.Type.Informational);
        }
    }
}
