using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MarkdownSnippets;
using Verify;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class SnippetExtractorTests :
    VerifyBase
{
    [Fact]
    public async Task AppendUrlAsSnippet()
    {
        var snippets = new List<Snippet>();
        await snippets.AppendUrlAsSnippet("https://raw.githubusercontent.com/SimonCropp/MarkdownSnippets/master/src/appveyor.yml");
        await Verify(snippets);
    }

    [Fact]
    public async Task AppendFileAsSnippet()
    {
        var temp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(temp, "Foo");
            var snippets = new List<Snippet>();
            snippets.AppendFileAsSnippet(temp);
            var settings = new VerifySettings();
            settings.AddScrubber(x =>
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(temp);
                return x
                    .Replace(temp, "FilePath.txt")
                    .Replace(nameWithoutExtension, "File", StringComparison.OrdinalIgnoreCase);
            });
            await Verify(snippets, settings);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public async Task CanExtractWithInnerWhiteSpace()
    {
        var input = @"
  #region CodeKey

  BeforeWhiteSpace

  AfterWhiteSpace

  #endregion";
        var snippets =await  FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task NestedBroken()
    {
        var input = @"
  #region KeyParent
  a
  #region KeyChild
  b
  c
  #endregion";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task NestedRegion()
    {
        var input = @"
  #region KeyParent
  a
  #region KeyChild
  b
  #endregion
  c
  #endregion";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task NestedMixed2()
    {
        var input = @"
  #region KeyParent
  a
  <!-- begin-snippet: KeyChild -->
  b
  <!-- end-snippet -->
  c
  #endregion";
        var snippets =await  FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task RemoveDuplicateNewlines()
    {
        var input = @"


  <!-- begin-snippet: KeyParent -->


  a


  <!-- begin-snippet: KeyChild -->


  b


  <!-- end-snippet -->


  c


  <!-- end-snippet -->


";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task NestedStartCode()
    {
        var input = @"
  <!-- begin-snippet: KeyParent -->
  a
  <!-- begin-snippet: KeyChild -->
  b
  <!-- end-snippet -->
  c
  <!-- end-snippet -->";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task NestedMixed1()
    {
        var input = @"
  <!-- begin-snippet: KeyParent -->
  a
  #region KeyChild
  b
  #endregion
  c
  <!-- end-snippet -->";
        var snippets =await  FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task CanExtractFromXml()
    {
        var input = @"
  <!-- begin-snippet: CodeKey -->
  <configSections/>
  <!-- end-snippet -->";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    ValueTask<List<Snippet>> FromText(string contents)
    {
        using var stringReader = new StringReader(contents);
        return FileSnippetExtractor.Read(stringReader, "path.cs", 80).ToListAsync();
    }

    [Fact]
    public async Task UnClosedSnippet()
    {
        var input = @"
  <!-- begin-snippet: CodeKey -->
  <configSections/>";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task UnClosedRegion()
    {
        var input = @"
  #region CodeKey
  <configSections/>";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task TooWide()
    {
        var input = @"
  #region CodeKey
  caaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaab
  #endregion";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task CanExtractFromRegion()
    {
        var input = @"
  #region CodeKey
  The Code
  #endregion";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task CanExtractWithNoTrailingCharacters()
    {
        var input = @"
  // begin-snippet: CodeKey
  the code
  // end-snippet ";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task CanExtractWithMissingSpaces()
    {
        var input = @"
  <!--begin-snippet: CodeKey-->
  <configSections/>
  <!--end-snippet-->";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    [Fact]
    public async Task CanExtractWithTrailingWhitespace()
    {
        var input = @"
  // begin-snippet: CodeKey
  the code
  // end-snippet   ";
        var snippets = await FromText(input);
        await  Verify(snippets);
    }

    public SnippetExtractorTests(ITestOutputHelper output) :
        base(output)
    {
    }
}