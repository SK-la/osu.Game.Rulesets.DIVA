// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using osu.Framework.Logging;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    /// <summary>
    /// Extracts audio from ProjectDIVA <c>RES/</c> videos into <c>WAV/</c> via ffmpeg
    /// (BASS cannot use video containers as tracks; ProjectDIVA plays A/V together in DirectShow).
    /// </summary>
    public static class DivaVideoAudioExtractor
    {
        public const string WAV_FOLDER = "WAV";
        public const string RES_FOLDER = "RES";

        private static readonly string[] video_extensions =
        [
            ".mp4", ".avi", ".wmv", ".mpg", ".mpeg", ".mov", ".m4v", ".flv", ".mkv", ".webm"
        ];

        private static readonly string[] audio_extensions =
        [
            ".mp3", ".ogg", ".wav", ".flac", ".m4a", ".aac"
        ];

        public static bool IsVideoExtension(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string ext = Path.GetExtension(path);
            foreach (string candidate in video_extensions)
            {
                if (ext.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IsAudioExtension(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string ext = Path.GetExtension(path);
            foreach (string candidate in audio_extensions)
            {
                if (ext.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static string? FindFfmpeg()
        {
            string? fromPath = findOnPath("ffmpeg") ?? findOnPath("ffmpeg.exe");
            if (fromPath != null)
                return fromPath;

            foreach (string probe in new[]
                     {
                         Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"),
                         Path.Combine(AppContext.BaseDirectory, "ffmpeg"),
                     })
            {
                if (File.Exists(probe))
                    return probe;
            }

            return null;
        }

        /// <summary>
        /// Extracts audio from <paramref name="videoFullPath"/> into <c>songFolder/WAV/&lt;name&gt;.ogg</c>.
        /// Returns chart-relative path with backslashes (e.g. <c>WAV\song.ogg</c>), or null on failure.
        /// </summary>
        public static string? ExtractToWavFolder(string songFolder, string videoFullPath)
        {
            if (!Directory.Exists(songFolder) || !File.Exists(videoFullPath))
                return null;

            string? ffmpeg = FindFfmpeg();
            if (ffmpeg == null)
            {
                Logger.Log("[DIVA] ffmpeg not found; cannot extract audio from video. Install ffmpeg on PATH.", level: LogLevel.Important);
                return null;
            }

            string wavDir = Path.Combine(songFolder, WAV_FOLDER);
            Directory.CreateDirectory(wavDir);

            string baseName = Path.GetFileNameWithoutExtension(videoFullPath);
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "bgm";

            string fileName = baseName + ".ogg";
            string outputFull = Path.Combine(wavDir, fileName);
            string relative = $"{WAV_FOLDER}\\{fileName}";

            try
            {
                if (File.Exists(outputFull)
                    && File.GetLastWriteTimeUtc(outputFull) >= File.GetLastWriteTimeUtc(videoFullPath)
                    && new FileInfo(outputFull).Length > 0)
                {
                    return relative;
                }

                string tempOut = outputFull + ".tmp";
                if (File.Exists(tempOut))
                    File.Delete(tempOut);

                var psi = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    ArgumentList =
                    {
                        "-y",
                        "-i", videoFullPath,
                        "-vn",
                        "-c:a", "libvorbis",
                        "-q:a", "5",
                        tempOut
                    },
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };

                using var process = Process.Start(psi);
                if (process == null)
                    return null;

                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit(180_000);

                if (process.ExitCode != 0 || !File.Exists(tempOut) || new FileInfo(tempOut).Length == 0)
                {
                    Logger.Log($"[DIVA] ffmpeg failed extracting '{videoFullPath}' (exit {process.ExitCode}): {trimLog(stderr)}", level: LogLevel.Important);

                    try
                    {
                        if (File.Exists(tempOut))
                            File.Delete(tempOut);
                    }
                    catch
                    {
                        // ignored
                    }

                    return null;
                }

                if (File.Exists(outputFull))
                    File.Delete(outputFull);

                File.Move(tempOut, outputFull);
                Logger.Log($"[DIVA] Extracted video BGM → {relative}");
                return relative;
            }
            catch (Exception ex)
            {
                Logger.Log($"[DIVA] Failed extracting audio from '{videoFullPath}': {ex.Message}", level: LogLevel.Important);
                return null;
            }
        }

        private static string? findOnPath(string fileName)
        {
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv))
                return null;

            foreach (string dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string candidate = Path.Combine(dir.Trim('"'), fileName);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }

        private static string trimLog(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = text.Trim();
            return text.Length <= 400 ? text : text[^400..];
        }
    }
}
