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
using osu.Framework.Localisation;
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
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Screens;
using osu.Game.Screens.Menu;
using OsuSongSelect = osu.Game.Screens.Select.SongSelect;

namespace osu.Game.Rulesets.Diva.UI
{
    public partial class DivaSettingsSubsection : RulesetSettingsSubsection
    {
#if NET8_0
        protected override LocalisableString Header => DivaStrings.SETTINGS_HEADER;
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
                    Text = DivaStrings.SETTINGS_OPEN_PATH_WIZARD,
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
                    LabelText = DivaStrings.SETTINGS_IMPORT_TO_REALM,
                    TooltipText = DivaStrings.SETTINGS_IMPORT_TO_REALM_TOOLTIP,
                    Current = importBindable
                },
                new SettingsCheckbox
                {
                    LabelText = DivaStrings.SETTINGS_USE_XBOX_BUTTON_ICONS,
                    Current = divaConfig.GetBindable<bool>(DivaRulesetSettings.UseXBoxButtons)
                },
                new SettingsCheckbox
                {
                    LabelText = DivaStrings.SETTINGS_ENABLE_VISUAL_BURSTS,
                    Current = divaConfig.GetBindable<bool>(DivaRulesetSettings.EnableVisualBursts)
                },
                new SettingsCheckbox
                {
                    LabelText = DivaStrings.SETTINGS_ENABLE_BUILTIN_HIT_SOUNDS,
                    TooltipText = DivaStrings.SETTINGS_ENABLE_BUILTIN_HIT_SOUNDS_TOOLTIP,
                    Current = divaConfig.GetBindable<bool>(DivaRulesetSettings.EnableBuiltinHitSounds)
                },
                new SettingsCheckbox
                {
                    LabelText = DivaStrings.SETTINGS_JUDGEMENT_LOCK,
                    TooltipText = DivaStrings.SETTINGS_JUDGEMENT_LOCK_TOOLTIP,
                    Current = divaConfig.GetBindable<bool>(DivaRulesetSettings.JudgementLock)
                },
                new SettingsSlider<double>
                {
                    LabelText = DivaStrings.SETTINGS_INPUT_OFFSET,
                    TooltipText = DivaStrings.SETTINGS_INPUT_OFFSET_TOOLTIP,
                    Current = divaConfig.GetBindable<double>(DivaRulesetSettings.InputOffset)
                },
                new SettingsSlider<double>
                {
                    LabelText = DivaStrings.SETTINGS_NOTE_SIZE,
                    Current = divaConfig.GetBindable<double>(DivaRulesetSettings.NoteSize)
                },
                new SettingsSlider<double>
                {
                    LabelText = DivaStrings.SETTINGS_APPROACH_PREEMPT_SCALE,
                    TooltipText = DivaStrings.SETTINGS_APPROACH_PREEMPT_SCALE_TOOLTIP,
                    Current = divaConfig.GetBindable<double>(DivaRulesetSettings.ApproachPreemptScale)
                },
                new SettingsSlider<double>
                {
                    LabelText = DivaStrings.SETTINGS_HIT_EXPLOSION_ALPHA,
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
                notificationOverlay?.Post(new SimpleErrorNotification { Text = DivaStrings.SETTINGS_CANNOT_OPEN_WIZARD });
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
                notificationOverlay?.Post(new SimpleErrorNotification { Text = DivaStrings.SETTINGS_ADD_VALID_PATH_FIRST });
                return;
            }

            importCts?.Cancel();
            importCts = new CancellationTokenSource();
            CancellationToken token = importCts.Token;

            bool clearing = paths.Count == 0;
            LocalisableString title = clearing
                ? DivaStrings.SETTINGS_CLEARING_LIBRARY
                : importToRealm
                    ? DivaStrings.SETTINGS_IMPORTING_LIBRARY
                    : DivaStrings.SETTINGS_LINKING_LIBRARY;

            if (IsLoaded) cacheStatusNote.Current.Value = new SettingsNote.Data(title, SettingsNote.Type.Informational);

            var notification = new ProgressNotification
            {
                Text = title,
                CompletionText = clearing ? DivaStrings.SETTINGS_LIBRARY_CLEARED : DivaStrings.SETTINGS_LIBRARY_UPDATE_COMPLETE,
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
                        report(new DivaLibraryImportPipeline.ImportProgress(1, DivaStrings.SETTINGS_PATHS_CLEARED_PROGRESS.ToString()));
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

                    string completion = clearing
                        ? DivaStrings.SETTINGS_LIBRARY_CLEARED.ToString()
                        : DivaStrings.SETTINGS_LIBRARY_UPDATE_COMPLETE.ToString();

                    if (collectionSync is { CollectionCount: > 0 } sync)
                    {
                        completion += DivaStrings.Settings_CollectionsSynced(sync.CollectionCount, sync.ChartCount);
                        notificationOverlay?.Post(new SimpleNotification
                        {
                            Text = DivaStrings.Settings_CollectionsSyncedDiva(sync.CollectionCount, sync.ChartCount)
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
                        Text = DivaStrings.Settings_LibraryUpdateFailed(ex.GetType().Name, ex.Message)
                    });
                }
            }, token);
        }

        private void updatePathStatus()
        {
            IReadOnlyList<string> paths = divaConfig.GetLibraryPaths();

            if (paths.Count == 0)
            {
                cacheStatusNote.Current.Value = new SettingsNote.Data(DivaStrings.SETTINGS_NO_PATHS_CONFIGURED, SettingsNote.Type.Informational);
                return;
            }

            IReadOnlyList<DivaSongFolder> songs = DivaLibraryScanner.Scan(paths);
            int difficultyCount = songs.Sum(s => s.ChartPaths.Count);

            cacheStatusNote.Current.Value = new SettingsNote.Data(
                DivaStrings.Settings_PathStatus(paths.Count, songs.Count, difficultyCount),
                SettingsNote.Type.Informational);
        }
    }
}
