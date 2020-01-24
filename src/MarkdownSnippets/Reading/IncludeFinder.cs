using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MarkdownSnippets
{
    public class IncludeFinder
    {
        IncludeFileFinder fileFinder;

        public IncludeFinder(DirectoryFilter? directoryFilter = null)
        {
            fileFinder = new IncludeFileFinder(directoryFilter);
        }

        public async IAsyncEnumerable<Include> ReadIncludes(params string[] directories)
        {
            Guard.AgainstNull(directories, nameof(directories));
            var files = fileFinder.FindFiles(directories).ToList();
            var keys = new List<string>();
            foreach (var file in files)
            {
                var key = Path.GetFileName(file).Replace(".include.md", "");
                if (keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    throw new Exception($"Duplicate include: {key}");
                }

                keys.Add(key);

                yield return Include.Build(key, await FileEx.ReadAllLinesAsync(file), file);
            }
        }
    }
}