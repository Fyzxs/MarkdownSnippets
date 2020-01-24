using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MarkdownSnippets;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class DirectorySnippetExtractorTests :
    VerifyBase
{
    [Fact]
    public async Task Case()
    {
        var directory = Path.Combine(AssemblyLocation.CurrentDirectory, "DirectorySnippetExtractor/Case");
        var extractor = new DirectorySnippetExtractor();
        var snippets = await extractor.ReadSnippets(directory);
        AssertCaseInsensitive(snippets.Lookup);

        await Verify(snippets);
    }

    static void AssertCaseInsensitive(IReadOnlyDictionary<string, IReadOnlyList<Snippet>> dictionary)
    {
        Assert.True(dictionary.ContainsKey("Snippet"));
        Assert.True(dictionary.ContainsKey("snippet"));
    }

    [Fact]
    public async Task Nested()
    {
        var directory = Path.Combine(AssemblyLocation.CurrentDirectory, "DirectorySnippetExtractor/Nested");
        var extractor = new DirectorySnippetExtractor();
        var snippets = await extractor.ReadSnippets(directory);
        await Verify(snippets);
    }

    [Fact]
    public async Task Simple()
    {
        var directory = Path.Combine(AssemblyLocation.CurrentDirectory, "DirectorySnippetExtractor/Simple");
        var extractor = new DirectorySnippetExtractor();
        var snippets = await extractor.ReadSnippets(directory);
        await Verify(snippets);
    }

    [Fact]
    public async Task VerifyLambdasAreCalled()
    {
        var directories = new ConcurrentBag<string>();
        var targetDirectory = Path.Combine(AssemblyLocation.CurrentDirectory,
            "DirectorySnippetExtractor/VerifyLambdasAreCalled");
        var extractor = new DirectorySnippetExtractor(
            directoryFilter: path =>
            {
                directories.Add(path);
                return true;
            }
        );
        await extractor.ReadSnippets(targetDirectory);
        await Verify(directories.OrderBy(file => file));
    }

    public DirectorySnippetExtractorTests(ITestOutputHelper output) :
        base(output)
    {
    }
}