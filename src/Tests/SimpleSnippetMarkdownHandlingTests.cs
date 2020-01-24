﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VerifyXunit;
using MarkdownSnippets;
using Xunit;
using Xunit.Abstractions;

public class SimpleSnippetMarkdownHandlingTests :
    VerifyBase
{
    [Fact]
    public async Task AppendGroup()
    {
        var builder = new StringBuilder();
        var snippets = new List<Snippet> {Snippet.Build(1, 2, "theValue", "thekey", "thelanguage", "thePath")};
        await using (var writer = new StringWriter(builder))
        {
            await SimpleSnippetMarkdownHandling.AppendGroup("key1", snippets, writer.WriteLineAsync);
        }

        await Verify(builder.ToString());
    }

    public SimpleSnippetMarkdownHandlingTests(ITestOutputHelper output) :
        base(output)
    {
    }
}