// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            public ImportResult(int songCount, int importedSets, int failedSets, IReadOnlyDictionary<string, IReadOnlyList<string>>? collectionHashesByPath = null)
            {
                SongCount = songCount;
                ImportedSets = importedSets;
                FailedSets = failedSets;
                CollectionHashesByPath = collectionHashesByPath
                                         ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            }

            public int SongCount { get; }
            public int ImportedSets { get; }
            public int FailedSets { get; }
            public IReadOnlyDictionary<string, IReadOnlyList<string>> CollectionHashesByPath { get; }
        }

        public static async Task<ImportResult> ImportToRealmAsync(
            BeatmapManager beatmapManager,
            Storage storage,
            IReadOnlyList<string> paths,
            Action<ImportProgress>? reportProgress = null,
            CancellationToken cancellationToken = default)
        {
            reportProgress?.Invoke(new ImportProgress(0, "Scanning DIVA song folders…"));

            var hashesByPath = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            int imported = 0;
            int failed = 0;
            int songCount = 0;

            string stagingRoot = storage.GetFullPath(Path.Combine("diva-import-staging", Guid.NewGuid().ToString("N")), true);

            try
            {
                foreach (string rootPath in paths)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                        continue;

                    string normalisedRoot = Path.GetFullPath(rootPath);
                    IReadOnlyList<DivaSongFolder> songs = DivaLibraryScanner.Scan([normalisedRoot]);
                    songCount += songs.Count;

                    if (!hashesByPath.ContainsKey(normalisedRoot))
                        hashesByPath[normalisedRoot] = [];

                    for (int i = 0; i < songs.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        DivaSongFolder song = songs[i];
                        double progress = songCount == 0 ? 0 : (double)(imported + failed) / Math.Max(songCount, 1);
                        reportProgress?.Invoke(new ImportProgress(progress, $"Packaging {Path.GetFileName(song.FolderPath)}…"));

                        try
                        {
                            string packaged = DivaSongSetPackager.PackageSong(song, stagingRoot);
                            reportProgress?.Invoke(new ImportProgress(progress + 0.01, $"Importing {Path.GetFileName(song.FolderPath)}…"));

                            Live<BeatmapSetInfo>? live = await beatmapManager.Import(new ImportTask(packaged), cancellationToken: cancellationToken).ConfigureAwait(false);

                            if (live != null)
                            {
                                live.PerformRead(set =>
                                {
                                    foreach (BeatmapInfo beatmap in set.Beatmaps)
                                    {
                                        if (!string.IsNullOrEmpty(beatmap.MD5Hash))
                                            hashesByPath[normalisedRoot].Add(beatmap.MD5Hash);
                                    }
                                });
                            }

                            imported++;
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            Logger.Error(ex, $"[DIVA] Failed to import song folder '{song.FolderPath}'.");
                        }
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

            if (songCount == 0)
            {
                reportProgress?.Invoke(new ImportProgress(1, "No .diva song folders found."));
                return new ImportResult(0, 0, 0);
            }

            reportProgress?.Invoke(new ImportProgress(1, $"Imported {imported}/{songCount} song sets."));
            return new ImportResult(
                songCount,
                imported,
                failed,
                hashesByPath.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyList<string>)kvp.Value,
                    StringComparer.OrdinalIgnoreCase));
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
