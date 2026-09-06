// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if NET10_0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ExternalLibraries;
using osu.Game.Database;
using osu.Game.Models;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Difficulty;
using osu.Game.Rulesets.Diva.Localization;
using Realms;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    /// <summary>
    /// Registers ProjectDIVA song folders as externally hosted beatmap sets (Ez2Lazer only).
    /// </summary>
    public static class DivaExternalLibrarySynchronizer
    {
        public static void Synchronize(
            RealmAccess realm,
            Storage storage,
            IWorkingBeatmapCache workingBeatmapCache,
            RulesetInfo divaRulesetInfo,
            IReadOnlyList<DivaSongFolder> songs,
            Action<DivaLibraryImportPipeline.ImportProgress>? reportProgress = null,
            CancellationToken cancellationToken = default)
        {
            var realmFileStore = new RealmFileStore(realm, storage);
            HashSet<string> desiredHashes = songs.Select(s => ExternalBeatmapPathEncoding.Encode(s.FolderPath)).ToHashSet(StringComparer.Ordinal);

            realm.Write(r =>
            {
                RulesetInfo managedRuleset = r.All<RulesetInfo>().FirstOrDefault(info => info.ShortName == divaRulesetInfo.ShortName)
                                             ?? throw new InvalidOperationException("DIVA ruleset is not available in realm.");

                // HostingKind is a CLR wrapper around HostingKindInt — do not use it in Realm LINQ
                // (throws NotImplementedException). Filter on HostingKindInt or in-memory after materialise.
                foreach (var existing in r.All<BeatmapSetInfo>().Where(s => s.HostingKindInt == (int)BeatmapSetHostingKind.External).ToList())
                {
                    bool isDiva = existing.Beatmaps.Any(b => b.Ruleset.ShortName == DivaRuleset.SHORT_NAME);
                    if (!isDiva)
                        continue;

                    if (!desiredHashes.Contains(existing.Hash))
                    {
                        workingBeatmapCache.Invalidate(existing);
                        removeSet(r, existing);
                    }
                }

                for (int i = 0; i < songs.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DivaSongFolder song = songs[i];
                    reportProgress?.Invoke(new DivaLibraryImportPipeline.ImportProgress((double)i / Math.Max(songs.Count, 1), DivaStrings.Import_Linking(Path.GetFileName(song.FolderPath))));

                    string contentRoot = Path.GetFullPath(song.FolderPath);
                    string setHash = ExternalBeatmapPathEncoding.Encode(contentRoot);
                    Guid setId = stableGuid($"diva-set:{contentRoot}");

                    BeatmapSetInfo? existingSet = r.Find<BeatmapSetInfo>(setId)
                                                  ?? r.All<BeatmapSetInfo>().FirstOrDefault(s => s.Hash == setHash);

                    BeatmapSetInfo destination = existingSet ?? new BeatmapSetInfo { ID = setId };
                    destination.DateAdded = Directory.GetLastWriteTimeUtc(contentRoot);
                    destination.Hash = setHash;
                    destination.ExternalContentRoot = contentRoot;
                    destination.HostingKind = BeatmapSetHostingKind.External;
                    destination.Status = BeatmapOnlineStatus.LocallyModified;

                    var keepBeatmapIds = new HashSet<Guid>();
                    bool setDirty = existingSet == null;

                    foreach (string chartPath in song.ChartPaths)
                    {
                        DivaChartMetadata meta = DivaChartFileParser.ReadMetadata(chartPath);
                        Guid beatmapId = stableGuid($"diva-chart:{Path.GetFullPath(chartPath)}");
                        keepBeatmapIds.Add(beatmapId);

                        string relative = computeRelative(chartPath, contentRoot);
                        string fileHash = computeFileHash(chartPath);
                        (RealmFile file, bool chartMappingChanged) = replaceNamedFileMapping(destination, relative, fileHash, realmFileStore, r);
                        setDirty |= chartMappingChanged;

                        string? audio = null;
                        string background = meta.OverviewPicture;
                        string? video = null;
                        double? chartLengthMs = null;

                        try
                        {
                            // Prefer full parse for audio/background/video when cheap enough.
                            DivaChart chart = DivaChartFileParser.Parse(chartPath);
                            DivaPlaybackTimeline timeline = DivaPlaybackTimeline.Create(chart);
                            audio = timeline.AudioRelativePath;
                            background = chart.ResolveBackgroundRelativePath() ?? background;
                            video = DivaPlaybackTimeline.ResolvePrimaryVideo(chart)?.RelativePath;
                            chartLengthMs = computeChartLengthMs(chart, timeline);
                        }
                        catch
                        {
                            // Metadata-only fallback already populated.
                        }

                        if (!string.IsNullOrWhiteSpace(audio))
                        {
                            string? resolvedAudio = DivaChartTextEncoding.ResolveExistingRelativePath(contentRoot, audio) ?? audio;
                            string audioRel = resolvedAudio.Replace('\\', '/');
                            string audioFull = Path.Combine(contentRoot, audioRel.Replace('/', Path.DirectorySeparatorChar));
                            if (File.Exists(audioFull))
                                setDirty |= replaceNamedFileMapping(destination, audioRel, computeFileHash(audioFull), realmFileStore, r).Changed;
                            audio = audioRel;
                        }

                        if (!string.IsNullOrWhiteSpace(background))
                        {
                            string? resolvedBg = DivaChartTextEncoding.ResolveExistingRelativePath(contentRoot, background) ?? background;
                            string bgRel = resolvedBg.Replace('\\', '/');
                            string bgFull = Path.Combine(contentRoot, bgRel.Replace('/', Path.DirectorySeparatorChar));
                            if (File.Exists(bgFull))
                                setDirty |= replaceNamedFileMapping(destination, bgRel, computeFileHash(bgFull), realmFileStore, r).Changed;
                            background = bgRel;
                        }

                        bool hasVideoFile = false;

                        if (!string.IsNullOrWhiteSpace(video))
                        {
                            string? resolvedVideo = DivaChartTextEncoding.ResolveExistingRelativePath(contentRoot, video) ?? video;
                            string videoRel = resolvedVideo.Replace('\\', '/');
                            string videoFull = Path.Combine(contentRoot, videoRel.Replace('/', Path.DirectorySeparatorChar));
                            if (File.Exists(videoFull))
                            {
                                setDirty |= replaceNamedFileMapping(destination, videoRel, computeFileHash(videoFull), realmFileStore, r).Changed;
                                hasVideoFile = true;
                            }
                        }

                        BeatmapInfo? beatmap = destination.Beatmaps.FirstOrDefault(b => b.ID == beatmapId);

                        if (beatmap == null)
                        {
                            beatmap = new BeatmapInfo(managedRuleset, new BeatmapDifficulty(), new BeatmapMetadata())
                            {
                                ID = beatmapId,
                                BeatmapSet = destination,
                            };
                            destination.Beatmaps.Add(beatmap);
                            setDirty = true;
                        }
                        else if (beatmap.Hash != file.Hash || beatmap.MD5Hash != fileHash)
                        {
                            setDirty = true;
                        }

                        beatmap.DifficultyName = DivaChartConstants.FormatDifficultyName(meta.Level, meta.Hard);
                        beatmap.Ruleset = managedRuleset;
                        beatmap.Hash = file.Hash;
                        beatmap.MD5Hash = fileHash;
                        beatmap.Status = BeatmapOnlineStatus.LocallyModified;
                        beatmap.BeatmapSet = destination;
                        beatmap.BPM = meta.Bpm;
                        if (chartLengthMs is > 0)
                            beatmap.Length = chartLengthMs.Value;
                        beatmap.Difficulty.OverallDifficulty = Math.Clamp(meta.Hard, 1, 10);
                        // Persist chart Hard for sort; panel SR comes from difficulty-cache Calculate().
                        beatmap.StarRating = DivaNativeStarRating.FromHard(meta.Hard);
                        beatmap.Difficulty.CircleSize = 4;
                        beatmap.Difficulty.DrainRate = 5;
                        beatmap.Difficulty.ApproachRate = 8;
                        beatmap.Metadata.Title = meta.Title;
                        beatmap.Metadata.TitleUnicode = meta.Title;
                        string artist = meta.ResolveDisplayArtist();
                        beatmap.Metadata.Artist = artist;
                        beatmap.Metadata.ArtistUnicode = artist;
                        beatmap.Metadata.Author.Username = meta.Creator;
                        beatmap.Metadata.Source = "ProjectDIVA";
                        beatmap.Metadata.Tags = $"{DivaActionEncoding.NATIVE_TAG} diva-external {meta.Style}".Trim();
                        beatmap.Metadata.AudioFile = string.IsNullOrWhiteSpace(audio) ? string.Empty : audio;
                        beatmap.Metadata.BackgroundFile = string.IsNullOrWhiteSpace(background) ? string.Empty : background;
                        beatmap.HasVideo = hasVideoFile;
                    }

                    foreach (var obsolete in destination.Beatmaps.Where(b => !keepBeatmapIds.Contains(b.ID)).ToList())
                    {
                        r.Remove(obsolete.Metadata);
                        r.Remove(obsolete);
                        setDirty = true;
                    }

                    if (existingSet == null)
                        r.Add(destination, update: true);

                    if (setDirty)
                        workingBeatmapCache.Invalidate(destination);
                }
            });

            reportProgress?.Invoke(new DivaLibraryImportPipeline.ImportProgress(1, DivaStrings.Import_Linked(songs.Count)));
            Logger.Log($"[DIVA] External library sync finished: {songs.Count} song folders.");
        }

        private static void removeSet(Realm realm, BeatmapSetInfo set)
        {
            foreach (BeatmapInfo beatmap in set.Beatmaps.ToList())
            {
                realm.Remove(beatmap.Metadata);
                realm.Remove(beatmap);
            }

            realm.Remove(set);
        }

        private static double? computeChartLengthMs(DivaChart chart, DivaPlaybackTimeline timeline)
        {
            if (chart.Notes.Count == 0)
                return null;

            double end = 0;
            foreach (DivaChartNote note in chart.Notes)
                end = Math.Max(end, timeline.ToPlaybackTime(note.StartTimeMs) + note.DurationMs);

            return end > 0 ? end : null;
        }

        private static string computeRelative(string chartPath, string contentRoot)
        {
            string relative = Path.GetRelativePath(contentRoot, chartPath);
            if (relative.StartsWith("..", StringComparison.Ordinal))
                relative = Path.GetFileName(chartPath);
            return relative.Replace('\\', '/');
        }

        private static string computeFileHash(string path)
        {
            using FileStream stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        private static (RealmFile File, bool Changed) replaceNamedFileMapping(
            BeatmapSetInfo destination,
            string relative,
            string fileHash,
            RealmFileStore realmFileStore,
            Realm realm)
        {
            RealmFile file = realmFileStore.RegisterExternalHash(fileHash, realm);
            RealmNamedFileUsage? existing = destination.GetFile(relative);

            if (existing?.File.Hash == fileHash)
                return (file, false);

            if (existing != null)
                destination.Files.Remove(existing);

            destination.Files.Add(new RealmNamedFileUsage(file, relative));
            return (file, true);
        }

        private static Guid stableGuid(string seed)
        {
            byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(seed));
            return new Guid(hash);
        }
    }
}
#endif
