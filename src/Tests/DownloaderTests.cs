using System.Threading.Tasks;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class DownloaderTests :
    VerifyBase
{
    [Fact]
    public async Task Valid()
    {
        var result = await Downloader.DownloadContent("https://raw.githubusercontent.com/SimonCropp/MarkdownSnippets/master/license.txt");
        var content = await result.content!;
        await Verify(new {result.success, content});
    }

    [Fact]
    public async Task Missing()
    {
        var content = await Downloader.DownloadContent("https://raw.githubusercontent.com/SimonCropp/MarkdownSnippets/master/missing.txt");
        Assert.False(content.success);
        Assert.Null(content.content);
    }

    public DownloaderTests(ITestOutputHelper output) :
        base(output)
    {
    }
}