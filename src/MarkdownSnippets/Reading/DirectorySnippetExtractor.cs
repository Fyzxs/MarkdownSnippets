using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarkdownSnippets
{
    public class DirectorySnippetExtractor
    {
        int maxWidth;
        SnippetFileFinder fileFinder;

        public DirectorySnippetExtractor(
            DirectoryFilter? directoryFilter = null,
            int maxWidth = int.MaxValue)
        {
            Guard.AgainstNegativeAndZero(maxWidth, nameof(maxWidth));
            this.maxWidth = maxWidth;
            fileFinder = new SnippetFileFinder(directoryFilter);
        }

        public async Task<ReadSnippets> ReadSnippets(params string[] directories)
        {
            Guard.AgainstNull(directories, nameof(directories));
            var files = fileFinder.FindFiles(directories).ToList();
            var snippets = new List<Snippet>();
            foreach (var file in files)
            {
                foreach (var snippet in await Read(file, maxWidth))
                {
                    snippets.Add(snippet);
                }
            }
            return new ReadSnippets(snippets, files);
        }

        static async ValueTask<List<Snippet>> Read(string file, int maxWidth)
        {
            using var reader = File.OpenText(file);
            return await FileSnippetExtractor.Read(reader, file, maxWidth).ToListAsync();
        }
    }
}