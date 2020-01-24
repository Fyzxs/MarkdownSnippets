using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MarkdownSnippets;

class Usage
{
    void ReadingFiles()
    {
        #region ReadingFilesSimple

        var files = Directory.EnumerateFiles(@"C:\path", "*.cs", SearchOption.AllDirectories);

        var snippets = FileSnippetExtractor.Read(files);

        #endregion
    }

    async Task DirectoryMarkdownProcessorRun()
    {
        #region DirectoryMarkdownProcessorRun

        var processor = new DirectoryMarkdownProcessor("targetDirectory");
        await processor.Run();

        #endregion
    }

    async Task DirectoryMarkdownProcessorRunMaxWidth()
    {
        #region DirectoryMarkdownProcessorRunMaxWidth

        var processor = new DirectoryMarkdownProcessor(
            "targetDirectory",
            maxWidth: 80);
        await processor.Run();

        #endregion
    }

    async Task ReadingDirectory()
    {
        #region ReadingDirectorySimple

        // extract snippets from files
        var snippetExtractor = new DirectorySnippetExtractor(
            // all directories except bin and obj
            directoryFilter: dirPath => !dirPath.EndsWith("bin") &&
                                        !dirPath.EndsWith("obj"));
        var snippets = await snippetExtractor.ReadSnippets(@"C:\path");

        #endregion
    }

    async Task Basic()
    {
        #region markdownProcessingSimple

        var directory = @"C:\path";

        // extract snippets from files
        var snippetExtractor = new DirectorySnippetExtractor();
        var snippets = await snippetExtractor.ReadSnippets(directory);

        // extract includes from files
        var includeFinder = new IncludeFinder();
        var includes = await includeFinder.ReadIncludes(directory).ToListAsync();

        // Merge with some markdown text
        var markdownProcessor = new MarkdownProcessor(
            snippets: snippets.Lookup,
            includes: includes,
            appendSnippetGroup: SimpleSnippetMarkdownHandling.AppendGroup,
            snippetSourceFiles: new List<string>(),
            tocLevel: 2,
            writeHeader: true,
            rootDirectory: directory);

        var path = @"C:\path\inputMarkdownFile.md";
        using var reader = File.OpenText(path);
        await using var writer = File.CreateText(@"C:\path\outputMarkdownFile.md");
        var result = await markdownProcessor.Apply(reader, writer, path);
        // snippets that the markdown file expected but did not exist in the input snippets
        var missingSnippets = result.MissingSnippets;

        // snippets that the markdown file used
        var usedSnippets = result.UsedSnippets;

        #endregion
    }
}