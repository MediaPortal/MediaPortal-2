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
using NUnit.Framework;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The client's own answers: the file that remembers them, the culture they name, and the screen rows
  /// that write them. Every test names its own file - the machine's real client.json is nobody's test data.
  /// </summary>
  [TestFixture]
  public class TestClientSettings
  {
    private string _file = "";

    [SetUp]
    public void NameAFile()
    {
      _file = Path.Combine(Path.GetTempPath(), "mp2-client-" + Guid.NewGuid().ToString("N") + ".json");
    }

    [TearDown]
    public void TakeTheFileAway()
    {
      if (File.Exists(_file))
        File.Delete(_file);
    }

    [Test]
    public void AFileNobodyWroteAnswersWithTheShippedDefaults()
    {
      ClientSettings settings = ClientSettings.Load(_file, out string? problem);

      Assert.That(problem, Is.Null, "a machine that was never configured is not a broken machine");
      Assert.That(settings.ClientName, Is.EqualTo(Environment.MachineName));
      Assert.That(settings.FullScreen, Is.True, "a media centre fills the television until told otherwise");
      Assert.That(settings.StartScreen, Is.EqualTo(ClientSettings.START_HOME));
      Assert.That(settings.LastShelf, Is.Empty);
      Assert.That(settings.Culture, Is.Empty, "nobody named a language; the operating system's own speaks");
    }

    [Test]
    public void AnswersWrittenComeBackOutOfTheFile()
    {
      ClientSettings written = new ClientSettings();
      written.ClientName = "Wohnzimmer";
      written.FullScreen = false;
      written.StartScreen = ClientSettings.START_LAST_SHELF;
      written.LastShelf = "Series";
      written.Culture = "de-DE";
      written.Save(_file, out bool saved);
      Assert.That(saved, Is.True);

      ClientSettings read = ClientSettings.Load(_file, out string? problem);
      Assert.That(problem, Is.Null);
      Assert.That(read.ClientName, Is.EqualTo("Wohnzimmer"));
      Assert.That(read.FullScreen, Is.False);
      Assert.That(read.StartScreen, Is.EqualTo(ClientSettings.START_LAST_SHELF));
      Assert.That(read.LastShelf, Is.EqualTo("Series"));
      Assert.That(read.Culture, Is.EqualTo("de-DE"));
    }

    [Test]
    public void ABrokenFileIsAnsweredWithDefaultsAndASentence()
    {
      File.WriteAllText(_file, "this is not json at all {{{");

      ClientSettings settings = ClientSettings.Load(_file, out string? problem);

      Assert.That(problem, Is.Not.Null.And.Contains(_file));
      Assert.That(settings.StartScreen, Is.EqualTo(ClientSettings.START_HOME));
    }

    [Test]
    public void TheCalendarFollowsTheNamedLanguageAndSurvivesAnUnknownOne()
    {
      Assert.That(ClientSettings.UiCulture("de-DE").Name, Is.EqualTo("de-DE"));
      Assert.That(ClientSettings.UiCulture("").Name, Is.EqualTo(CultureInfo.CurrentUICulture.Name));
      Assert.That(ClientSettings.UiCulture("kl-77"), Is.SameAs(CultureInfo.InvariantCulture),
          "a language this machine cannot pronounce is answered with a calendar that still works");
    }

    [Test]
    public void TheLanguageRowWalksTheThreeAnswersTheReferenceShipped()
    {
      ClientSettingsViewModel screen = new ClientSettingsViewModel(_file);
      SkinSettingOption language = OptionNamed(screen, "Language");

      Assert.That(language.ValueText, Is.EqualTo("System default"));
      language.Press();
      Assert.That(language.ValueText, Is.EqualTo("English"));
      language.Press();
      Assert.That(language.ValueText, Is.EqualTo("Deutsch"));
      language.Press();
      Assert.That(language.ValueText, Is.EqualTo("System default"), "the row is a round trip, not a one-way door");

      ClientSettings saved = ClientSettings.Load(_file, out _);
      Assert.That(saved.Culture, Is.Empty, "every stop on the row is written where it is reached");
    }

    [Test]
    public void TheWakeUpRowPromisesItsTwoAnswersAndSaysWhenItHasNothingToReopen()
    {
      string previous = Environment.GetEnvironmentVariable("MP2_CLIENT_FILE") ?? "";
      Environment.SetEnvironmentVariable("MP2_CLIENT_FILE", _file);
      try
      {
        ClientSettingsViewModel screen = new ClientSettingsViewModel(_file);
        SkinSettingOption start = OptionNamed(screen, "Start screen");

        Assert.That(start.ValueText, Is.EqualTo(ClientSettings.START_HOME));
        start.Press();
        Assert.That(start.ValueText, Is.EqualTo(ClientSettings.START_LAST_SHELF + " (nothing remembered yet)"),
            "a wake-up promise the library cannot keep yet is said out loud, not hidden");

        ClientSettings.RecordShelf("Movies");
        screen = new ClientSettingsViewModel(_file);
        Assert.That(OptionNamed(screen, "Start screen").ValueText, Is.EqualTo(ClientSettings.START_LAST_SHELF));
      }
      finally
      {
        Environment.SetEnvironmentVariable("MP2_CLIENT_FILE", previous.Length == 0 ? null : previous);
      }
    }

    [Test]
    public void TheNameRowWritesTheNameAsItIsTyped()
    {
      ClientSettingsViewModel screen = new ClientSettingsViewModel(_file);
      screen.ClientNameText = "Schlafzimmer-Fernseher";

      Assert.That(screen.Status, Does.Contain("Schlafzimmer-Fernseher").And.Contain("Saved"),
          "a settings screen whose saves are silent is a settings screen whose saves are doubted");
      Assert.That(ClientSettings.Load(_file, out _).ClientName, Is.EqualTo("Schlafzimmer-Fernseher"));
    }

    [Test]
    public void TheRememberedShelfIsWrittenOnceAndLeftAloneAfterwards()
    {
      string previous = Environment.GetEnvironmentVariable("MP2_CLIENT_FILE") ?? "";
      Environment.SetEnvironmentVariable("MP2_CLIENT_FILE", _file);
      try
      {
        ClientSettings.RecordShelf("Movies");
        Assert.That(ClientSettings.Load(null, out _).LastShelf, Is.EqualTo("Movies"));

        DateTime written = File.GetLastWriteTimeUtc(_file);
        ClientSettings.RecordShelf("movies");
        Assert.That(File.GetLastWriteTimeUtc(_file), Is.EqualTo(written),
            "walking to the same shelf again is not a change worth a write");

        ClientSettings.RecordShelf("Series");
        Assert.That(ClientSettings.Load(null, out _).LastShelf, Is.EqualTo("Series"));
      }
      finally
      {
        Environment.SetEnvironmentVariable("MP2_CLIENT_FILE", previous.Length == 0 ? null : previous);
      }
    }

    private static SkinSettingOption OptionNamed(ClientSettingsViewModel screen, string label)
    {
      foreach (SkinSettingOption option in screen.Options)
        if (option.Label == label)
          return option;

      Assert.Fail("the client settings screen offers no row called '" + label + "'");
      throw new InvalidOperationException("unreachable");
    }
  }
}
