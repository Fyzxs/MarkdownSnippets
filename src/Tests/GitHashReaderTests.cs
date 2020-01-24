using System.IO;
using System.Threading.Tasks;
using VerifyXunit;
using MarkdownSnippets;
using Xunit;
using Xunit.Abstractions;

public class GitHashReaderTests :
    VerifyBase
{
    [Fact]
    public async Task GetHash()
    {
        var path = GitRepoDirectoryFinder.FindForFilePath();
        var hash = await GitHashReader.GetHash(path);
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public Task NoRef()
    {
        return VerifyInner("GitDirs/NoRef");
    }

    [Fact]
    public Task WithRef()
    {
        return VerifyInner("GitDirs/WithRef");
    }

    async Task VerifyInner(string path)
    {
        var directory = Path.Combine(AssemblyLocation.CurrentDirectory, path);
        var hash = await GitHashReader.GetHashForGitDirectory(directory);
        await  Verify(hash);
    }

    public GitHashReaderTests(ITestOutputHelper output) :
        base(output)
    {
    }
}