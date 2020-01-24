using System.Threading.Tasks;
using MarkdownSnippets;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class IncludeFinderTests :
    VerifyBase
{
    [Fact]
    public async Task Simple()
    {
        var finder = new IncludeFinder();
        var includes = await finder.ReadIncludes("IncludeFinder").ToListAsync();
        await Verify(includes);
    }

    public IncludeFinderTests(ITestOutputHelper output) :
        base(output)
    {
    }
}