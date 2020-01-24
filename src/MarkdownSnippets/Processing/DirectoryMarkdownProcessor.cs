using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarkdownSnippets
{
    public class DirectoryMarkdownProcessor
    {
        bool writeHeader;
        string? header;
        string? urlPrefix;
        DirectoryFilter? directoryFilter;
        bool readOnly;
        int tocLevel;
        int maxWidth;
        IEnumerable<string>? tocExcludes;
        Action<string> log;
        string targetDirectory;
        List<string> sourceMdFiles = new List<string>();
        ConcurrentBag<Include> includes = new ConcurrentBag<Include>();
        ConcurrentBag<Snippet> snippets = new ConcurrentBag<Snippet>();
        public IReadOnlyCollection<Snippet> Snippets => snippets;
        List<string> snippetSourceFiles = new List<string>();
        AppendSnippetGroupToMarkdown appendSnippetGroup;
        bool treatMissingSnippetAsWarning;
        bool treatMissingIncludeAsWarning;
        Task? addSnippetsFromTask = null;
        Task? addIncludesFromTask;

        public DirectoryMarkdownProcessor(
            string targetDirectory,
            bool scanForMdFiles = true,
            bool scanForSnippets = true,
            bool scanForIncludes = true,
            Action<string>? log = null,
            AppendSnippetGroupToMarkdown? appendSnippetGroup = null,
            bool writeHeader = true,
            string? header = null,
            DirectoryFilter? directoryFilter = null,
            bool readOnly = false,
            LinkFormat linkFormat = LinkFormat.GitHub,
            int tocLevel = 2,
            IEnumerable<string>? tocExcludes = null,
            bool treatMissingSnippetAsWarning = false,
            bool treatMissingIncludeAsWarning = false,
            int maxWidth = int.MaxValue,
            string? urlPrefix = null)
        {
            this.writeHeader = writeHeader;
            this.header = header;
            this.urlPrefix = urlPrefix;
            this.directoryFilter = directoryFilter;
            this.readOnly = readOnly;
            this.tocLevel = tocLevel;
            this.tocExcludes = tocExcludes;
            this.maxWidth = maxWidth;
            this.treatMissingSnippetAsWarning = treatMissingSnippetAsWarning;
            this.treatMissingIncludeAsWarning = treatMissingIncludeAsWarning;

            this.appendSnippetGroup = appendSnippetGroup ?? new SnippetMarkdownHandling(targetDirectory, linkFormat, urlPrefix).AppendGroup;

            this.log = log ?? (s => { Trace.WriteLine(s); });

            Guard.DirectoryExists(targetDirectory, nameof(targetDirectory));
            this.targetDirectory = Path.GetFullPath(targetDirectory);
            if (scanForMdFiles)
            {
                AddMdFilesFrom(targetDirectory);
            }

            if (scanForSnippets)
            {
                addSnippetsFromTask = AddSnippetsFrom(targetDirectory);
            }

            if (scanForIncludes)
            {
                addIncludesFromTask = AddIncludeFilesFrom(targetDirectory);
            }
        }

        public void AddSnippets(List<Snippet> snippets)
        {
            Guard.AgainstNull(snippets, nameof(snippets));
            var files = snippets
                .Where(x => x.Path != null)
                .Select(x => x.Path!)
                .Distinct()
                .ToList();
            snippetSourceFiles.AddRange(files);
            log($"Added {files.Count} files for snippets");
            this.snippets.AddRange(snippets);
            log($"Added {snippets.Count} snippets");
        }

        public void AddSnippets(params Snippet[] snippets)
        {
            Guard.AgainstNull(snippets, nameof(snippets));
            AddSnippets(snippets.ToList());
        }

        public async Task AddSnippetsFrom(string directory)
        {
            directory = ExpandDirectory(directory);
            var finder = new SnippetFileFinder(directoryFilter);
            var files = finder.FindFiles(directory);
            snippetSourceFiles.AddRange(files);
            log($"Searching {files.Count} files for snippets");
            await foreach(var snippet in FileSnippetExtractor.Read(files, maxWidth))
            {
                snippets.Add(snippet);
            }
            log($"Snippet count: {snippets.Count}");
        }

        string ExpandDirectory(string directory)
        {
            Guard.AgainstNull(directory, nameof(directory));
            directory = Path.Combine(targetDirectory, directory);
            directory = Path.GetFullPath(directory);
            Guard.DirectoryExists(directory, nameof(directory));
            return directory;
        }

        public void AddMdFilesFrom(string directory)
        {
            directory = ExpandDirectory(directory);
            var finder = new MdFileFinder(directoryFilter);
            var files = finder.FindFiles(directory).ToList();
            sourceMdFiles.AddRange(files);
            log($"Added {files.Count} .source.md files");
        }

        public async Task AddIncludeFilesFrom(string directory)
        {
            directory = ExpandDirectory(directory);
            var finder = new IncludeFinder(directoryFilter);

            await foreach(var include in finder.ReadIncludes(directory))
            {
                includes.Add(include);
            }

            log($"Includes count: {includes.Count}");
        }

        public void AddMdFiles(params string[] files)
        {
            Guard.AgainstNull(files, nameof(files));
            foreach (var file in files)
            {
                sourceMdFiles.Add(file);
            }
        }

        public async Task Run()
        {
            if (addSnippetsFromTask != null)
            {
                await addSnippetsFromTask;
            }

            if (addIncludesFromTask != null)
            {
                await addIncludesFromTask;
            }

            Guard.AgainstNull(Snippets, nameof(snippets));
            Guard.AgainstNull(snippetSourceFiles, nameof(snippetSourceFiles));
            var processor = new MarkdownProcessor(
                Snippets.ToDictionary(),
                includes,
                appendSnippetGroup,
                snippetSourceFiles,
                tocLevel,
                writeHeader,
                targetDirectory,
                header,
                tocExcludes);
            await Task.WhenAll(sourceMdFiles.Select(x => ProcessFile(x, processor)));
        }

        async Task ProcessFile(string sourceFile, MarkdownProcessor markdownProcessor)
        {
            log($"Processing {sourceFile}");
            var target = GetTargetFile(sourceFile, targetDirectory);
            FileEx.ClearReadOnly(target);

            var (lines, newLine) = await ReadLines(sourceFile);

            var relativeSource = sourceFile
                .Substring(targetDirectory.Length)
                .Replace('\\', '/');
            var result = await markdownProcessor.Apply(lines, newLine, relativeSource);

            var missingSnippets = result.MissingSnippets;
            if (missingSnippets.Any())
            {
                // If the config value is set to treat missing snippets as warnings, then don't throw
                if (treatMissingSnippetAsWarning)
                {
                    foreach (var missing in missingSnippets)
                    {
                        log($"WARN: The source file:{missing.File} includes a key {missing.Key}, however the snippet is missing. Make sure that the snippet is defined.");
                    }
                }
                else
                {
                    throw new MissingSnippetsException(missingSnippets);
                }
            }

            var missingIncludes = result.MissingIncludes;
            if (missingIncludes.Any())
            {
                // If the config value is set to treat missing include as warnings, then don't throw
                if (treatMissingIncludeAsWarning)
                {
                    foreach (var missing in missingIncludes)
                    {
                        log($"WARN: The source file:{missing.File} includes a key {missing.Key}, however the include is missing. Make sure that the include is defined.");
                    }
                }
                else
                {
                    throw new MissingIncludesException(missingIncludes);
                }
            }

            await WriteLines(target, lines);

            if (readOnly)
            {
                FileEx.MakeReadOnly(target);
            }
        }

        static async Task WriteLines(string target, List<Line> lines)
        {
            using var writer = File.CreateText(target);
            foreach (var line in lines)
            {
                await writer.WriteLineAsync(line.Current);
            }
        }

        static async Task<(List<Line> lines, string newLine)> ReadLines(string sourceFile)
        {
            using var reader = File.OpenText(sourceFile);
            return await LineReader.ReadAllLines(reader, sourceFile);
        }

        static string GetTargetFile(string sourceFile, string rootDirectory)
        {
            var relativePath = FileEx.GetRelativePath(sourceFile, rootDirectory);

            var filtered = relativePath.Split(Path.DirectorySeparatorChar)
                .Where(x => !string.Equals(x, "mdsource", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var sourceTrimmed = Path.Combine(filtered);
            var targetFile = Path.Combine(rootDirectory, sourceTrimmed);
            // remove ".md" from ".source.md" then change ".source" to ".md"
            targetFile = Path.ChangeExtension(Path.ChangeExtension(targetFile, null), ".md");
            return targetFile;
        }
    }
}