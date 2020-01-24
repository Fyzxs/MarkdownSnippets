using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarkdownSnippets
{
    public delegate Task AppendSnippetGroupToMarkdown(string key, IEnumerable<Snippet> snippets, Func<string, Task> appendLine);
}