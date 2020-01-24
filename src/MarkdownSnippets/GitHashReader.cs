using System.IO;
using System.Threading.Tasks;

static class GitHashReader
{
    public static Task<string> GetHash(string directory)
    {
        var gitDirectory = Path.Combine(directory, ".git");
        return GetHashForGitDirectory(gitDirectory);
    }

    public static async Task<string> GetHashForGitDirectory(string gitDirectory)
    {
        var headPath = Path.Combine(gitDirectory, "HEAD");
        var line = await ReadFirstLine(headPath);
        if (!line.StartsWith("ref: "))
        {
            return line;
        }
        var head = line.Substring(5);
        var @ref = Path.Combine(gitDirectory, head);
        return await ReadFirstLine(@ref);
    }

    static async Task<string> ReadFirstLine(string head)
    {
        using var stream = FileEx.OpenRead(head);
        using var reader = new StreamReader(stream);
        return await reader.ReadLineAsync();
    }
}