using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

static class FileEx
{
    public static FileStream OpenRead(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public static async Task<string> ReadAllTextAsync(
        string path,
        CancellationToken cancellation = default)
    {
        char[]? buffer = null;
        var reader = File.OpenText(path);
        try
        {
            cancellation.ThrowIfCancellationRequested();
            buffer = ArrayPool<char>.Shared.Rent(reader.CurrentEncoding.GetMaxCharCount(4096));
            var builder = new StringBuilder();
            while (true)
            {
                var charCount = await reader.ReadAsync(buffer, 0, buffer.Length);
                if (charCount != 0)
                {
                    builder.Append(buffer, 0, charCount);
                }
                else
                {
                    break;
                }
            }

            return builder.ToString();
        }
        finally
        {
            reader.Dispose();
            if (buffer != null)
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }
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