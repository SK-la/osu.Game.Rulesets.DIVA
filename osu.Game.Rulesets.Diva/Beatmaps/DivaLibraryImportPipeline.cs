// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Database;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    public static class DivaLibraryImportPipeline
    {
        public readonly struct ImportProgress
        {
            public ImportProgress(double progress, string statusMessage)
            {
                Progress = progress;
                StatusMessage = statusMessage;
            }

            public double Progress { get; }
            public string StatusMessage { get; }
        }

        public readonly struct ImportResult
        {
            public ImportResult(int songCount, int importedSets, int failedSets)
            {
                SongCount = songCount;
                ImportedSets = importedSets;
                FailedSets = failedSets;
            }

            public int SongCount { get; }
            public int ImportedSets { get; }
            public int FailedSets { get; }
        }

        public static async Task<ImportResult> ImportToRealmAsync(
            BeatmapManager beatmapManager,
            Storage storage,
            IReadOnlyList<string> paths,
            Action<ImportProgress>? reportProgress = null,
            CancellationToken cancellationToken = default)
        {
            reportProgress?.Invoke(new ImportProgress(0, "Scanning DIVA song folders…"));
            IReadOnlyList<DivaSongFolder> songs = DivaLibraryScanner.Scan(paths);

            if (songs.Count == 0)
            {
                reportProgress?.Invoke(new ImportProgress(1, "No .diva song folders found."));
                return new ImportResult(0, 0, 0);
            }

            string stagingRoot = storage.GetFullPath(Path.Combine("diva-import-staging", Guid.NewGuid().ToString("N")), true);
            int imported = 0;
            int failed = 0;

            try
            {
                for (int i = 0; i < songs.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DivaSongFolder song = songs[i];
                    double progress = (double)i / songs.Count;
                    reportProgress?.Invoke(new ImportProgress(progress, $"Packaging {Path.GetFileName(song.FolderPath)}…"));

                    try
                    {
                        string packaged = DivaSongSetPackager.PackageSong(song, stagingRoot);
                        reportProgress?.Invoke(new ImportProgress(progress + 0.5 / songs.Count, $"Importing {Path.GetFileName(song.FolderPath)}…"));
                        await beatmapManager.Import(new ImportTask(packaged), cancellationToken: cancellationToken).ConfigureAwait(false);
                        imported++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Logger.Error(ex, $"[DIVA] Failed to import song folder '{song.FolderPath}'.");
                    }
                }
            }
            finally
            {
                try
                {
                    if (Directory.Exists(stagingRoot))
                        Directory.Delete(stagingRoot, true);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[DIVA] Failed to clean staging folder: {ex.Message}", level: LogLevel.Important);
                }
            }

            reportProgress?.Invoke(new ImportProgress(1, $"Imported {imported}/{songs.Count} song sets."));
            return new ImportResult(songs.Count, imported, failed);
        }

#if NET10_0
        public static async Task SynchronizeExternalAsync(
            RealmAccess realm,
            Storage storage,
            RulesetInfo divaRulesetInfo,
            IReadOnlyList<string> paths,
            Action<ImportProgress>? reportProgress = null,
            CancellationToken cancellationToken = default)
        {
            reportProgress?.Invoke(new ImportProgress(0, "Scanning DIVA song folders…"));
            IReadOnlyList<DivaSongFolder> songs = DivaLibraryScanner.Scan(paths);
            await Task.Run(
                () => DivaExternalLibrarySynchronizer.Synchronize(realm, storage, divaRulesetInfo, songs, reportProgress, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
#endif
    }
}
