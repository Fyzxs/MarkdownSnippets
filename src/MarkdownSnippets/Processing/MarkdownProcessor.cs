using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownSnippets
{
    /// <summary>
    /// Merges <see cref="Snippet"/>s with an input file/text.
    /// </summary>
    public class MarkdownProcessor
    {
        IReadOnlyDictionary<string, IReadOnlyList<Snippet>> snippets;
        AppendSnippetGroupToMarkdown appendSnippetGroup;
        bool writeHeader;
        string? header;
        int tocLevel;
        List<string> tocExcludes;
        List<string> snippetSourceFiles;
        IncludeProcessor includeProcessor;

        public MarkdownProcessor(
            IReadOnlyDictionary<string, IReadOnlyList<Snippet>> snippets,
            IReadOnlyCollection<Include> includes,
            AppendSnippetGroupToMarkdown appendSnippetGroup,
            IReadOnlyList<string> snippetSourceFiles,
            int tocLevel,
            bool writeHeader,
            string rootDirectory,
            string? header = null,
            IEnumerable<string>? tocExcludes = null)
        {
            Guard.AgainstNull(snippets, nameof(snippets));
            Guard.AgainstNull(appendSnippetGroup, nameof(appendSnippetGroup));
            Guard.AgainstNull(snippetSourceFiles, nameof(snippetSourceFiles));
            Guard.AgainstNull(includes, nameof(includes));
            Guard.AgainstEmpty(header, nameof(header));
            Guard.AgainstNegativeAndZero(tocLevel, nameof(tocLevel));
            Guard.AgainstNullAndEmpty(rootDirectory, nameof(rootDirectory));
            rootDirectory = Path.GetFullPath(rootDirectory);
            this.snippets = snippets;
            this.appendSnippetGroup = appendSnippetGroup;
            this.writeHeader = writeHeader;
            this.header = header;
            this.tocLevel = tocLevel;
            if (tocExcludes == null)
            {
                this.tocExcludes = new List<string>();
            }
            else
            {
                this.tocExcludes = tocExcludes.ToList();
            }

            this.snippetSourceFiles = snippetSourceFiles
                .Select(x => x.Replace('\\', '/'))
                .ToList();
            includeProcessor = new IncludeProcessor(includes, rootDirectory);
        }

        public async Task<string> Apply(string input, string? file = null)
        {
            Guard.AgainstNull(input, nameof(input));
            Guard.AgainstEmpty(file, nameof(file));
            var builder = StringBuilderCache.Acquire();
            try
            {
                using var reader = new StringReader(input);
                using var writer = new StringWriter(builder);
                var processResult = await Apply(reader, writer, file);
                var missing = processResult.MissingSnippets;
                if (missing.Any())
                {
                    throw new MissingSnippetsException(missing);
                }

                return builder.ToString();
            }
            finally
            {
                StringBuilderCache.Release(builder);
            }
        }

        /// <summary>
        /// Apply to <paramref name="writer"/>.
        /// </summary>
        public async Task<ProcessResult> Apply(TextReader textReader, TextWriter writer, string? file = null)
        {
            Guard.AgainstNull(textReader, nameof(textReader));
            Guard.AgainstNull(writer, nameof(writer));
            Guard.AgainstEmpty(file, nameof(file));
            var (lines, newLine) = LineReader.ReadAllLines(textReader, null);
            writer.NewLine = newLine;
            var result = await Apply(lines, newLine, file);
            foreach (var line in lines)
            {
                await writer.WriteLineAsync(line.Current);
            }

            return result;
        }

        internal async Task<ProcessResult> Apply(List<Line> lines, string newLine, string? relativePath)
        {
            var missingSnippets = new List<MissingSnippet>();
            var missingIncludes = new List<MissingInclude>();
            var usedSnippets = new List<Snippet>();
            var usedIncludes = new List<Include>();
            var builder = new StringBuilder();
            Line? tocLine = null;
            var headerLines = new List<Line>();
            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];

                if (await includeProcessor.TryProcessInclude(lines, line, usedIncludes, index, missingIncludes))
                {
                    continue;
                }

                if (line.Current.StartsWith("#"))
                {
                    if (tocLine != null)
                    {
                        headerLines.Add(line);
                    }

                    continue;
                }

                if (line.Current == "toc")
                {
                    tocLine = line;
                    continue;
                }

                if (SnippetKeyReader.TryExtractKeyFromLine(line, out var key))
                {
                    builder.Clear();

                    Task AppendLine(string s)
                    {
                        builder.Append(s);
                        builder.Append(newLine);
                        return Task.CompletedTask;
                    }

                    await ProcessSnippetLine(AppendLine, missingSnippets, usedSnippets, key, line);
                    builder.TrimEnd();
                    line.Current = builder.ToString();
                }
            }

            if (writeHeader)
            {
                lines.Insert(0, new Line(HeaderWriter.WriteHeader(relativePath!, header, newLine), "", 0));
            }

            if (tocLine != null)
            {
                tocLine.Current = TocBuilder.BuildToc(headerLines, tocLevel, tocExcludes, newLine);
            }

            return new ProcessResult(
                missingSnippets: missingSnippets,
                usedSnippets: usedSnippets.Distinct().ToList(),
                usedIncludes: usedIncludes.Distinct().ToList(),
                missingIncludes: missingIncludes);
        }

        async Task ProcessSnippetLine(Func<string, Task> appendLine, List<MissingSnippet> missings, List<Snippet> used, string key, Line line)
        {
           await appendLine($"<!-- snippet: {key} -->");
            var snippetsForKey = await TryGetSnippets(key);
            if (snippetsForKey.Any())
            {
                await appendSnippetGroup(key, snippetsForKey, appendLine);
                await    appendLine("<!-- endsnippet -->");
                used.AddRange(snippetsForKey);
                return;
            }

            var missing = new MissingSnippet(key, line.LineNumber, line.Path);
            missings.Add(missing);
            await   appendLine($"** Could not find snippet '{key}' **");
        }

        async Task<IReadOnlyList<Snippet>> TryGetSnippets(string key)
        {
            if (snippets.TryGetValue(key, out var snippetsForKey))
            {
                return snippetsForKey;
            }

            if (key.StartsWith("http"))
            {
                var (success, path) = await Downloader.DownloadFile(key);
                if (!success)
                {
                    return Array.Empty<Snippet>();
                }

                return new List<Snippet>
                {
                    await FileToSnippet(key, path!, null)
                };
            }

            return await FilesToSnippets(key);
        }

        Task<Snippet[]> FilesToSnippets(string key)
        {
            string keyWithDirChar;
            if (key.StartsWith("/"))
            {
                keyWithDirChar = key;
            }
            else
            {
                keyWithDirChar = "/" + key;
            }

            var tasks = snippetSourceFiles
                .Where(file => file.EndsWith(keyWithDirChar, StringComparison.OrdinalIgnoreCase))
                .Select(file => FileToSnippet(key, file, file));
            return Task.WhenAll(tasks);
        }

        static async Task<Snippet> FileToSnippet(string key, string file, string? path)
        {
            var (text, lineCount) = await ReadNonStartEndLines(file);

            if (lineCount == 0)
            {
                lineCount++;
            }

            return Snippet.Build(
                startLine: 1,
                endLine: lineCount,
                value: text,
                key: key,
                language: Path.GetExtension(file).Substring(1),
                path: path);
        }

        static async Task<(string text, int lineCount)> ReadNonStartEndLines(string file)
        {
            var cleanedLines = (await FileEx.ReadAllLinesAsync(file))
                .Where(x => !StartEndTester.IsStartOrEnd(x.TrimStart())).ToList();
            return (string.Join(Environment.NewLine, cleanedLines), cleanedLines.Count);
        }
    }
}