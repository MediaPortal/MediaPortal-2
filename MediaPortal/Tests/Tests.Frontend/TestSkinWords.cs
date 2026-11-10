/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;

namespace Tests.Frontend
{
  /// <summary>
  /// The words a viewer reads, read out of the same <c>strings_*.xml</c> files the reference's localization
  /// module read - since this front end owns no service to ask.  Which words are shipped, which file answers
  /// which language, and what is said when nothing answers, are all decided here.
  /// </summary>
  [TestFixture]
  public class TestSkinWords
  {
    private string? _folderBefore;

    [SetUp]
    public void StandInFrontOfTheShippedWords()
    {
      _folderBefore = SkinWords.WordsFolder;
    }

    [TearDown]
    public void PutTheWordsBack()
    {
      SkinWords.WordsFolder = _folderBefore;
      SkinWords.UseCurrentLanguage();
    }

    [Test]
    public void TheShippedLanguagesCarryTheWordsTheKeyboardAsksFor()
    {
      SkinWords.WordsFolder = Path.Combine(MetadataPath("FrontendSource"), "Language");

      SkinWords.Use(new CultureInfo("en-GB"));
      Assert.That(SkinWords.Count, Is.GreaterThan(200),
          "the English file is the SkinBase file as it ships, not a hand-cut slip of the few words the keys want");
      Assert.That(TheWordsTheKeyboardAsks(), Is.EqualTo(new[] { "Alt Gr", "Copy", "Paste" }));

      SkinWords.Use(new CultureInfo("de-DE"));
      Assert.That(TheWordsTheKeyboardAsks(), Is.EqualTo(new[] { "Alt Gr", "Kopieren", "Einfügen" }),
          "the German words are the ones this repository's translation cache holds, word for word");

      SkinWords.Use(new CultureInfo("fr-FR"));
      Assert.That(TheWordsTheKeyboardAsks(), Is.EqualTo(new[] { "Alt Gr", "Copier", "Coller" }),
          "and the French ones answer the French keyboard that was ported from the reference's French file");
    }

    [Test]
    public void ALanguageWithoutItsOwnFileIsAnsweredInEnglishRatherThanInBrackets()
    {
      SkinWords.WordsFolder = Path.Combine(MetadataPath("FrontendSource"), "Language");
      SkinWords.Use(new CultureInfo("it-IT"));

      Assert.That(SkinWords.Count, Is.GreaterThan(200),
          "a language nobody translated is still read over the English file, so it is not left without words");
      Assert.That(SkinWords.Say("[Keys.Copy]", "word from the markup"), Is.EqualTo("Copy"));
      Assert.That(SkinWords.Say("[Keys.Paste]", "word from the markup"), Is.EqualTo("Paste"));
    }

    [Test]
    public void AWordNobodyCarriesKeepsTheWordTheMarkupAlreadyCarries()
    {
      SkinWords.WordsFolder = Path.Combine(MetadataPath("FrontendSource"), "Language");
      SkinWords.Use(new CultureInfo("de-DE"));

      // The reference printed the label itself when its service knew nothing, brackets and all, which put
      // "[Keys.Copy]" on a key.  Here the English the markup carries is the answer instead, and a label that was
      // never a question is left alone entirely.
      Assert.That(SkinWords.Say("[Keys.NowhereToBeFound]", "Copy"), Is.EqualTo("Copy"));
      Assert.That(SkinWords.Say("Alt Gr", "Alt Gr"), Is.EqualTo("Alt Gr"),
          "a label without brackets was never asking about a file");
      Assert.That(SkinWords.Say("[SectionWithoutADot]", "as written"), Is.EqualTo("as written"));
    }

    [Test]
    public void ARegionCarriesItsOwnWordingOverTheWordingOfItsLanguage()
    {
      string folder = Path.Combine(Path.GetTempPath(), "mp2-words-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(folder);
      try
      {
        File.WriteAllText(Path.Combine(folder, "strings_en.xml"),
            "<resources><string name=\"Keys.Copy\">Copy</string></resources>");
        File.WriteAllText(Path.Combine(folder, "strings_de.xml"),
            "<resources><string name=\"Keys.Copy\">Kopieren</string>"
            + "<string name=\"Keys.Paste\">Einfügen</string></resources>");
        File.WriteAllText(Path.Combine(folder, "strings_de_AT.xml"),
            "<resources><string name=\"Keys.Copy\">Kopieren (AT)</string></resources>");

        SkinWords.WordsFolder = folder;
        SkinWords.Use(new CultureInfo("de-AT"));

        Assert.That(SkinWords.Say("[Keys.Copy]", "Copy"), Is.EqualTo("Kopieren (AT)"),
            "the file for the region is read over the file for the language, spelled the way this repository's "
            + "translation cache spells it (Tools/Transifex/Cache_MP2/SkinBase holds strings_pt_BR.xml)");
        Assert.That(SkinWords.Say("[Keys.Paste]", "Paste"), Is.EqualTo("Einfügen"),
            "and what the region does not say about is still answered by the language");
      }
      finally
      {
        Directory.Delete(folder, true);
      }
    }

    [Test]
    public void AnApostropheWrittenForAnAndroidFileIsAnApostropheOnTheScreen()
    {
      string folder = Path.Combine(Path.GetTempPath(), "mp2-words-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(folder);
      try
      {
        File.WriteAllText(Path.Combine(folder, "strings_en.xml"),
            "<resources><string name=\"Keys.Copy\">it\\'s copied</string></resources>");

        SkinWords.WordsFolder = folder;
        SkinWords.Use(new CultureInfo("en-GB"));

        Assert.That(SkinWords.Say("[Keys.Copy]", "word from the markup"), Is.EqualTo("it's copied"),
            "the same unescaping the reference's loader applied (LocalizationStrings.cs:200-208)");
      }
      finally
      {
        Directory.Delete(folder, true);
      }
    }

    [Test]
    public void AWordFolderThatWasAskedForAndIsNotThereIsSaidOutLoud()
    {
      string nowhere = Path.Combine(Path.GetTempPath(), "mp2-words-nowhere-" + Guid.NewGuid().ToString("N"));

      string? read = SkinWords.ResolveWordsFolder(nowhere, null);
      Assert.That(read, Is.Null, "a name that leads nowhere cannot be read from");
      Assert.That(SkinWords.WordsFolderProblem, Does.Contain("MP2_LANGUAGE_DIR"),
          "and the front end says which name it could not honour rather than going quiet");

      // A name set blank is an order rather than a shrug - the same distinction the skins make, and for the same
      // reason: a test or a machine that wants the markup's own words must be able to say so.
      read = SkinWords.ResolveWordsFolder("", Path.Combine(Path.GetTempPath(), "mp2-words-anyway"));
      Assert.That(read, Is.Null);
      Assert.That(SkinWords.WordsFolderProblem, Is.Null);
    }

    private static string[] TheWordsTheKeyboardAsks()
    {
      // Three names only.  What every keyboard draws - shift, caps lock, the space bar, the back key, the key at
      // the end of the row - the skin shapes say, and no language is asked about them.
      return new[]
      {
        SkinWords.Say("[Keys.AltGr]", "?"),
        SkinWords.Say("[Keys.Copy]", "?"),
        SkinWords.Say("[Keys.Paste]", "?")
      };
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestSkinWords).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(attribute => string.Equals(attribute.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }
  }
}
