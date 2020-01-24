using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

static class FileEx
{
    public static FileStream OpenRead(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public static string GetRelativePath(string file, string directory)
    {
        var fileUri = new Uri(file);
        // Folders must end in a slash
        if (!directory.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            directory += Path.DirectorySeparatorChar;
        }

        var directoryUri = new Uri(directory);
        return Uri.UnescapeDataString(directoryUri.MakeRelativeUri(fileUri).ToString().Replace('/', Path.DirectorySeparatorChar));
    }

    public static IEnumerable<string> FindFiles(string directory, string pattern)
    {
        var files = new List<string>();
        try
        {
            files.AddRange(Directory.EnumerateFiles(directory, pattern));
        }
        catch (UnauthorizedAccessException)
        {
        }

        return files;
    }

    public static void ClearReadOnly(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        new FileInfo(path)
        {
            IsReadOnly = false
        };
    }

    public static void MakeReadOnly(string path)
    {
        new FileInfo(path)
        {
            IsReadOnly = true
        };
    }

    public static async Task<List<string>> ReadAllLinesAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        using var reader = File.OpenText(path);
        cancellationToken.ThrowIfCancellationRequested();
        var lines = new List<string>();
        while (true)
        {
            string line;
            if ((line = await reader.ReadLineAsync()) != null)
            {
                lines.Add(line);
                cancellationToken.ThrowIfCancellationRequested();
            }
            else
            {
                break;
            }
        }
        return lines;
    }
}