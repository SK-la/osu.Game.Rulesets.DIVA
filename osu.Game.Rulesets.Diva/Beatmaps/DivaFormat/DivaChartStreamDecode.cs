// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Reflection;
using System.Text;
using osu.Game.IO;

namespace osu.Game.Rulesets.Diva.Beatmaps.DivaFormat
{
    /// <summary>
    /// Parses a <c>.diva</c> chart from a <see cref="LineBufferedReader"/> without trusting its UTF-8 text.
    /// Lazer always constructs <see cref="LineBufferedReader"/> with UTF-8, which corrupts ANSI/CJK charts;
    /// this helper reopens via <see cref="FileStream.Name"/> or raw seekable bytes and
    /// <see cref="DivaChartTextEncoding"/>.
    /// </summary>
    public static class DivaChartStreamDecode
    {
        private static readonly FieldInfo? stream_reader_field = typeof(LineBufferedReader)
            .GetField("streamReader", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Result of decoding a chart, including the song folder when known (for path resolution).
        /// </summary>
        public readonly struct Result
        {
            public Result(DivaChart chart, string songFolder, string sourcePath)
            {
                Chart = chart;
                SongFolder = songFolder;
                SourcePath = sourcePath;
            }

            public DivaChart Chart { get; }
            public string SongFolder { get; }
            public string SourcePath { get; }
        }

        /// <summary>
        /// Parses a chart from <paramref name="buffered"/>, preferring disk path / raw bytes over UTF-8 lines.
        /// </summary>
        public static Result Parse(LineBufferedReader buffered)
        {
            if (tryParseFromUnderlyingStream(buffered, out Result result))
                return result;

            return parseFromBufferedLines(buffered);
        }

        private static bool tryParseFromUnderlyingStream(LineBufferedReader buffered, out Result result)
        {
            result = default;

            Stream? baseStream = tryGetBaseStream(buffered);
            if (baseStream == null)
                return false;

            try
            {
                if (baseStream is FileStream fileStream && !string.IsNullOrEmpty(fileStream.Name) && File.Exists(fileStream.Name))
                {
                    string path = fileStream.Name;
                    string songFolder = Path.GetDirectoryName(path) ?? string.Empty;
                    result = new Result(DivaChartFileParser.Parse(path), songFolder, path);
                    return true;
                }

                if (!baseStream.CanSeek)
                    return false;

                long originalPosition = baseStream.Position;
                baseStream.Position = 0;

                byte[] bytes;

                try
                {
                    using var memory = new MemoryStream();
                    baseStream.CopyTo(memory);
                    bytes = memory.ToArray();
                }
                finally
                {
                    try
                    {
                        baseStream.Position = originalPosition;
                    }
                    catch
                    {
                        // Stream may already be closed by a concurrent dispose; ignore.
                    }
                }

                if (bytes.Length == 0)
                    return false;

                const string memory_song_folder = "";
                const string memory_source_path = "";
                string text = DivaChartTextEncoding.DecodeBytes(bytes, memory_song_folder);

                using var reader = new StringReader(text);
                result = new Result(DivaChartFileParser.Parse(reader, memory_source_path, memory_song_folder), memory_song_folder, memory_source_path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Result parseFromBufferedLines(LineBufferedReader buffered)
        {
            var sb = new StringBuilder();

            while (buffered.ReadLine() is { } line)
                sb.AppendLine(line);

            using var reader = new StringReader(sb.ToString());
            return new Result(DivaChartFileParser.Parse(reader, string.Empty, string.Empty), string.Empty, string.Empty);
        }

        private static Stream? tryGetBaseStream(LineBufferedReader buffered)
        {
            if (stream_reader_field == null)
                return null;

            try
            {
                if (stream_reader_field.GetValue(buffered) is not StreamReader streamReader)
                    return null;

                return streamReader.BaseStream;
            }
            catch
            {
                return null;
            }
        }
    }
}
