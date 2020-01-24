using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VerifyXunit;
using MarkdownSnippets;
using Xunit;
using Xunit.Abstractions;

public class SnippetMarkdownHandlingTests :
    VerifyBase
{
    [Fact]
    public async Task AppendGroup()
    {
        var builder = new StringBuilder();
        var snippets = new List<Snippet> {Snippet.Build(1, 2, "theValue", "thekey", "thelanguage", "c:/dir/thePath")};
        var markdownHandling = new SnippetMarkdownHandling("c:/dir/", LinkFormat.GitHub);
        await using (var writer = new StringWriter(builder))
        {
            await markdownHandling.AppendGroup("key1", snippets, writer.WriteLineAsync);
        }

        await Verify(builder.ToString());
    }

    public SnippetMarkdownHandlingTests(ITestOutputHelper output) :
        base(output)
    {
    }
}