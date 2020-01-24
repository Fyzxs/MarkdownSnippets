using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarkdownSnippets
{
    /// <summary>
    /// Simple markdown handling to be passed to <see cref="MarkdownProcessor"/>.
    /// </summary>
    public static class SimpleSnippetMarkdownHandling
    {
        public static async Task AppendGroup(string key, IEnumerable<Snippet> snippets, Func<string, Task> appendLine)
        {
            Guard.AgainstNull(snippets, nameof(snippets));
            Guard.AgainstNull(appendLine, nameof(appendLine));

            foreach (var snippet in snippets)
            {
                await WriteSnippet(appendLine, snippet);
            }
        }

        static async Task WriteSnippet(Func<string, Task> appendLine, Snippet snippet)
        {
            await appendLine($@"```{snippet.Language}");
            await   appendLine(snippet.Value);
            await   appendLine(@"```");
        }
    }
}