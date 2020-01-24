using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MarkdownSnippets;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class DirectoryMarkdownProcessorTests :
    VerifyBase
{
    [Fact]
    public Task Run()
    {
        var root = GitRepoDirectoryFinder.FindForFilePath();

        var processor = new DirectoryMarkdownProcessor(
            targetDirectory: root,
            tocLevel: 1,
            tocExcludes: new List<string>
            {
                "Icon",
                "Credits",
                "Release Notes"
            },
            directoryFilter: path =>
                !path.Contains("IncludeFileFinder") &&
                !path.Contains("DirectoryMarkdownProcessor"));
        return processor.Run();
    }

    [Fact]
    public async Task ReadOnly()
    {
        var root = Path.GetFullPath("DirectoryMarkdownProcessor/Readonly");
        try
        {
            var processor = new DirectoryMarkdownProcessor(
                root,
                writeHeader: false,
                readOnly: true);
            processor.AddSnippets(
                SnippetBuild("snippet1"),
                SnippetBuild("snippet2")
            );
            await processor.Run();

            var fileInfo = new FileInfo(Path.Combine(root, "one.md"));
            Assert.True(fileInfo.IsReadOnly);
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(root))
            {
                FileEx.ClearReadOnly(file);
            }
        }
    }

    [Fact]
    public async Task UrlSnippetMissing()
    {
        var root = Path.GetFullPath("DirectoryMarkdownProcessor/UrlSnippetMissing");
        var processor = new DirectoryMarkdownProcessor(root, writeHeader: false);
        var exception = await Assert.ThrowsAsync<MissingSnippetsException>(() => processor.Run());
        await Verify(
            new
            {
                exception.Missing,
                exception.Message
            });
    }

    [Fact]
    public async Task UrlIncludeMissing()
    {
        var root = Path.GetFullPath("DirectoryMarkdownProcessor/UrlIncludeMissing");
        var processor = new DirectoryMarkdownProcessor(root, writeHeader: false);
        var exception = await Assert.ThrowsAsync<MissingIncludesException>(() => processor.Run());
        await Verify(
            new
            {
                exception.Missing,
                exception.Message
            });
    }

    [Fact]
    public async Task UrlSnippet()
    {
        var root = Path.GetFullPath("DirectoryMarkdownProcessor/UrlSnippet");
        var processor = new DirectoryMarkdownProcessor(root, writeHeader: false);
        await processor.Run();

        var result = Path.Combine(root,"one.md");

        await Verify(await File.ReadAllTextAsync(result));
    }

    [Fact]
    public async Task UrlInclude()
    {
        var root = Path.GetFullPath("DirectoryMarkdownProcessor/UrlInclude");
        var processor = new DirectoryMarkdownProcessor(root, writeHeader: false);
        await processor.Run();

        var result = Path.Combine(root,"one.md");

        await Verify(await File.ReadAllTextAsync(result));
    }

    [Fact]
    public async Task Convention()
    {
        var root = Path.GetFullPath("DirectoryMarkdownProcessor/Convention");
        var processor = new DirectoryMarkdownProcessor(root, writeHeader: false);
        processor.AddSnippets(
            SnippetBuild("snippet1"),
            SnippetBuild("snippet2")
        );
       await processor.Run();

        var builder = new StringBuilder();
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            builder.AppendLine(file.Replace(root, ""));
            builder.AppendLine(File.ReadAllText(file));
            builder.AppendLine();
        }

        await Verify(builder.ToString());
    }

    [Fact]
    public Task MustErrorByDefaultWhenIncludesAreMissing()
    {
        var root = Path.GetFullPath("DirectoryMarkdownProcessor/MissingInclude");
        var processor = new DirectoryMarkdownProcessor(root, writeHeader: false);
        return Assert.ThrowsAsync<MissingIncludesException>(() => processor.Run());
    }

    [Fact]
    public Task MustNotErrorForMissingIncludesIfConfigured()
    {
        var root = Path.GetFullPath("DirectoryMarkdownProcessor/MissingInclude");
        var processor = new DirectoryMarkdownProcessor(
            root,
            writeHeader: false,
            treatMissingIncludeAsWarning: true);
        return processor.Run();
    }

    [Fact]
    public Task MustErrorByDefaultWhenSnippetsAreMissing()
    {
        var root = Path.GetFullPath("DirectoryMarkdownProcessor/Convention");
        var processor = new DirectoryMarkdownProcessor(root, writeHeader: false);
        return Assert.ThrowsAsync<MissingSnippetsException>(() => processor.Run());
    }

    [Fact]
    public Task MustNotErrorForMissingSnippetsIfConfigured()
    {
        var root = Path.GetFullPath("DirectoryMarkdownProcessor/Convention");
        var processor = new DirectoryMarkdownProcessor(
            root,
            writeHeader: false,
            treatMissingSnippetAsWarning: true);
        return processor.Run();
    }

    static Snippet SnippetBuild(string key)
    {
        return Snippet.Build(
            language: ".cs",
            startLine: 1,
            endLine: 2,
            value: "the code from " + key,
            key: key,
            path: null);
    }

    public DirectoryMarkdownProcessorTests(ITestOutputHelper output) :
        base(output)
    {
    }
}