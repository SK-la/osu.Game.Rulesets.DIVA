// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Collections;
using osu.Game.Database;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    /// <summary>
    /// Creates or updates Realm <see cref="BeatmapCollection"/>s from configured DIVA library roots,
    /// matching BMS path → collection behaviour (folder basename as collection name).
    /// </summary>
    public static class DivaCollectionSynchronizer
    {
        public const string DEFAULT_COLLECTION_NAME = "DIVA Collection";

        public readonly struct SyncResult
        {
            public SyncResult(int collectionCount, int chartCount)
            {
                CollectionCount = collectionCount;
                ChartCount = chartCount;
            }

            public int CollectionCount { get; }
            public int ChartCount { get; }
        }

        public static string CollectionNameForPath(string path)
        {
            // Library paths may be Windows-style even when tests/CI run on Linux.
            string trimmed = path.TrimEnd('\\', '/');
            int lastSep = Math.Max(trimmed.LastIndexOf('\\'), trimmed.LastIndexOf('/'));
            string collectionName = lastSep >= 0 ? trimmed[(lastSep + 1)..] : trimmed;

            return string.IsNullOrEmpty(collectionName) ? DEFAULT_COLLECTION_NAME : collectionName;
        }

        /// <summary>
        /// Writes collections from an explicit path → MD5 map (used after Realm import).
        /// </summary>
        public static SyncResult Apply(RealmAccess realm, IReadOnlyDictionary<string, IReadOnlyList<string>> hashesByLibraryPath)
        {
            if (hashesByLibraryPath.Count == 0)
                return new SyncResult(0, 0);

            var merged = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach ((string path, IReadOnlyList<string> hashes) in hashesByLibraryPath)
            {
                if (hashes.Count == 0)
                    continue;

                string name = CollectionNameForPath(path);

                if (merged.TryGetValue(name, out List<string>? existing))
                    existing.AddRange(hashes);
                else
                    merged[name] = hashes.ToList();
            }

            return writeCollections(realm, merged);
        }

        /// <summary>
        /// Collects DIVA beatmap MD5s under each library root and upserts matching collections.
        /// External mounts are matched via <c>ExternalContentRoot</c>; imported sets are matched by
        /// scanning song folders and resolving sets already linked under that root when possible.
        /// </summary>
        public static SyncResult SyncFromLibraryPaths(RealmAccess realm, IReadOnlyList<string> libraryPaths)
        {
            if (libraryPaths.Count == 0)
                return new SyncResult(0, 0);

            var hashesByPath = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (string path in libraryPaths)
            {
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                    continue;

                string normalisedRoot = Path.GetFullPath(path);
                var hashes = new List<string>();

                realm.Run(r =>
                {
                    foreach (BeatmapSetInfo set in r.All<BeatmapSetInfo>())
                    {
                        if (!setBelongsUnderRoot(set, normalisedRoot))
                            continue;

                        if (set.Beatmaps.All(b => b.Ruleset.ShortName != DivaRuleset.SHORT_NAME))
                            continue;

                        foreach (BeatmapInfo beatmap in set.Beatmaps)
                        {
                            if (beatmap.Ruleset.ShortName != DivaRuleset.SHORT_NAME)
                                continue;

                            if (!string.IsNullOrEmpty(beatmap.MD5Hash))
                                hashes.Add(beatmap.MD5Hash);
                        }
                    }
                });

                if (hashes.Count == 0)
                    continue;

                if (hashesByPath.TryGetValue(normalisedRoot, out List<string>? existing))
                    existing.AddRange(hashes);
                else
                    hashesByPath[normalisedRoot] = hashes;
            }

            return Apply(realm, hashesByPath.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value,
                StringComparer.OrdinalIgnoreCase));
        }

        private static SyncResult writeCollections(RealmAccess realm, Dictionary<string, List<string>> hashesByCollectionName)
        {
            int syncedCount = 0;
            int totalCharts = 0;

            foreach ((string collectionName, List<string> beatmapHashes) in hashesByCollectionName)
            {
                List<string> distinctHashes = beatmapHashes
                    .Where(h => !string.IsNullOrEmpty(h))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (distinctHashes.Count == 0)
                    continue;

                realm.Write(r =>
                {
                    BeatmapCollection? existing = r.All<BeatmapCollection>()
                        .FirstOrDefault(c => c.Name == collectionName);

                    if (existing != null)
                    {
                        existing.BeatmapMD5Hashes.Clear();

                        foreach (string hash in distinctHashes)
                            existing.BeatmapMD5Hashes.Add(hash);

                        existing.LastModified = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        r.Add(new BeatmapCollection(collectionName, distinctHashes));
                    }
                });

                syncedCount++;
                totalCharts += distinctHashes.Count;
            }

            return new SyncResult(syncedCount, totalCharts);
        }

        private static bool setBelongsUnderRoot(BeatmapSetInfo set, string normalisedRoot)
        {
#if NET10_0
            if (set.HostingKind == BeatmapSetHostingKind.External
                && !string.IsNullOrWhiteSpace(set.ExternalContentRoot))
            {
                string contentRoot = Path.GetFullPath(set.ExternalContentRoot);
                return isUnderOrEqual(contentRoot, normalisedRoot);
            }
#endif

            // Imported sets have no external root; SyncFromLibraryPaths only covers external mounts.
            // Import mode passes hashes via Apply() instead.
            return false;
        }

        private static bool isUnderOrEqual(string candidateFullPath, string rootFullPath)
        {
            string candidate = candidateFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string root = rootFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (candidate.Equals(root, StringComparison.OrdinalIgnoreCase))
                return true;

            string prefix = root + Path.DirectorySeparatorChar;
            return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                   || candidate.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
