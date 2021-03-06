﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MarkdownSnippets;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class MarkdownProcessorTests :
    VerifyBase
{
    [Fact]
    public Task WithSingleInclude()
    {
        var content = @"
before

include: theKey

after
";
        var lines = new List<string> {"theValue1"};
        return this.VerifySnippets(
            content,
            availableSnippets: new List<Snippet>(),
            snippetSourceFiles: new List<string>(),
            includes: new[] {Include.Build("theKey", lines, "c:/root/thePath")});
    }

    [Fact]
    public Task WithDoubleInclude()
    {
        var content = @"
before

include: theKey

after
";
        var lines = new List<string> {"theValue1", "theValue2"};
        return this.VerifySnippets(
            content,
            availableSnippets: new List<Snippet>(),
            snippetSourceFiles: new List<string>(),
            includes: new[] {Include.Build("theKey", lines, "c:/root/thePath")});
    }

    [Fact]
    public Task WithMultipleInclude()
    {
        var content = @"
before

include: theKey

after
";
        var lines = new List<string> {"theValue1", "theValue2", "theValue3"};
        return this.VerifySnippets(
            content,
            availableSnippets: new List<Snippet>(),
            snippetSourceFiles: new List<string>(),
            includes: new[] {Include.Build("theKey", lines, "c:/root/thePath")});
    }

    [Fact]
    public Task MissingInclude()
    {
        var content = @"
before

include: theKey

after
";
        return this.VerifySnippets(content,
            availableSnippets: new List<Snippet>(),
            snippetSourceFiles: new List<string>(),
            includes: new List<Include>());
    }

    [Fact]
    public Task SkipHeadingBeforeToc()
    {
        var content = @"
## Heading 1

toc

Text1

## Heading 2

Text2

";
        return this.VerifySnippets(content, new List<Snippet>(), new List<string>());
    }

    [Fact]
    public Task Toc()
    {
        var content = @"
# Title

toc

## Heading 1

Text1

## Heading 2

Text2

";
        return this.VerifySnippets(content, new List<Snippet>(), new List<string>());
    }

    [Fact]
    public Task Simple()
    {
        var availableSnippets = new List<Snippet>
        {
            SnippetBuild(
                language: "cs",
                key: "snippet1"
            ),
            SnippetBuild(
                language: "cs",
                key: "snippet2"
            )
        };
        var content = @"
snippet: snippet1

some text

snippet: snippet2

some other text

snippet: FileToUseAsSnippet.txt

some other text

snippet: /FileToUseAsSnippet.txt

";
        return this.VerifySnippets(
            content,
            availableSnippets,
            new List<string>
            {
                Path.Combine(GitRepoDirectoryFinder.FindForFilePath(), "src/Tests/FileToUseAsSnippet.txt")
            });
    }

    [Fact]
    public Task SnippetInInclude()
    {
        var availableSnippets = new List<Snippet>
        {
            SnippetBuild(
                language: "cs",
                key: "snippet1"
            )
        };
        var content = @"
some text

include: theKey

some other text
";
        var lines = new List<string> {"snippet: snippet1"};
        return this.VerifySnippets(
            content,
            availableSnippets,
            new List<string>(),
            includes: new[] {Include.Build("theKey", lines, "thePath")});
    }
    [Fact]
    public Task SnippetInIncludeLast()
    {
        var availableSnippets = new List<Snippet>
        {
            SnippetBuild(
                language: "cs",
                key: "snippet1"
            )
        };
        var content = @"
some text

include: theKey

some other text
";
        var lines = new List<string> {"line1","snippet: snippet1"};
        return this.VerifySnippets(
            content,
            availableSnippets,
            new List<string>(),
            includes: new[] {Include.Build("theKey", lines, "thePath")});
    }

    static Snippet SnippetBuild(string language, string key)
    {
        return Snippet.Build(
            language: language,
            startLine: 1,
            endLine: 2,
            value: "Snippet",
            key: key,
            path: "thePath");
    }

    public MarkdownProcessorTests(ITestOutputHelper output) :
        base(output)
    {
    }
}