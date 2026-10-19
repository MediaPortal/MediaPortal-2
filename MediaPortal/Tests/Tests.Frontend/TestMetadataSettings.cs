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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The metadata settings screen: rows built out of the server's answer, and a wish that carries only what
  /// the rows hold. The server itself is a stub - what is under test is the screen's arithmetic between the
  /// nullable answer and the nullable wish, which is exactly where a "false became true" bug would hide.
  /// </summary>
  [TestFixture]
  public class TestMetadataSettings
  {
    private class StubApi : IMetadataSettingsApi
    {
      internal MetadataSettingsAnswer ToAnswer = new MetadataSettingsAnswer();
      internal MetadataSettingsWish LastWish;
      internal int Gets;
      internal int Puts;

      public Task<MetadataSettingsAnswer> GetMetadataSettingsAsync(CancellationToken cancellation = default)
      {
        Gets++;
        return Task.FromResult(ToAnswer);
      }

      public Task<MetadataSettingsAnswer> PutMetadataSettingsAsync(MetadataSettingsWish wish,
          CancellationToken cancellation = default)
      {
        Puts++;
        LastWish = wish;
        return Task.FromResult(ToAnswer);
      }
    }

    private static MetadataSettingsViewModel ScreenOn(StubApi api)
    {
      return new MetadataSettingsViewModel(() => api);
    }

    [Test]
    public void TheRowsAreTheAnswerAndNothingElse()
    {
      StubApi api = new StubApi
      {
        ToAnswer = new MetadataSettingsAnswer
        {
          // A server that has the movie extractor answers two of its flags; the other fields stay null,
          // and the series extractor is not installed at all.
          Movie = new MetadataFlags { SkipOnlineSearches = true, CacheLocalFanArt = false },
          Audio = new MetadataFlags { SkipFanArtDownload = true },
          OnlineLibraries = new MetadataFlags { OnlyBasicFanArt = false, MovieLanguageCulture = "de-DE" }
        }
      };

      MetadataSettingsViewModel screen = ScreenOn(api);
      screen.ReloadCommand.Execute(null);

      Assert.That(screen.Rows.Select(row => row.Block + "." + row.Field), Is.EqualTo(new[]
      {
        "movie.SkipOnlineSearches", "movie.CacheLocalFanArt",
        "audio.SkipFanArtDownload",
        "online.OnlyBasicFanArt", "online.MovieLanguageCulture"
      }), "only the values the server actually spoke of become rows - the absent series extractor has no row, "
          + "and the movie extractor's silent fields have no switches to guess with");
      Assert.That(screen.Rows[0].IsOn, Is.True);
      Assert.That(screen.Rows[1].IsOn, Is.False,
          "a flag stored off is a row shown unticked, not a row left out");
      Assert.That(screen.Rows[4].IsText, Is.True);
      Assert.That(screen.Rows[4].Text, Is.EqualTo("de-DE"));
      Assert.That(screen.Status, Does.Contain("next import pass"),
          "the screen says when a change speaks, because saved-and-silent reads as broken");
    }

    [Test]
    public void TheWishCarriesTheRowsAndOnlyTheRows()
    {
      StubApi api = new StubApi
      {
        ToAnswer = new MetadataSettingsAnswer
        {
          Movie = new MetadataFlags { SkipOnlineSearches = true, CacheLocalFanArt = true },
          OnlineLibraries = new MetadataFlags { MovieLanguageCulture = "de-DE", SeriesLanguageCulture = "en-GB" }
        }
      };

      MetadataSettingsViewModel screen = ScreenOn(api);
      screen.ReloadCommand.Execute(null);

      screen.Rows.First(row => row.Field == "SkipOnlineSearches").IsOn = false;
      screen.Rows.First(row => row.Field == "MovieLanguageCulture").Text = "  fr-FR  ";

      screen.SaveCommand.Execute(null);

      Assert.That(api.Puts, Is.EqualTo(1));
      MetadataFlags movie = api.LastWish.Movie;
      Assert.That(movie.SkipOnlineSearches, Is.False,
          "the flipped row goes back flipped");
      Assert.That(movie.CacheLocalFanArt, Is.True,
          "and a row that was left alone goes back as it was read - the row's value is the server's value");
      Assert.That(movie.SkipFanArtDownload, Is.Null,
          "a field the screen never had a row for stays out of the wish - null means leave it as it is");
      Assert.That(api.LastWish.Series, Is.Null,
          "and a block with no rows at all is not sent in any shape");
      Assert.That(api.LastWish.OnlineLibraries.MovieLanguageCulture, Is.EqualTo("fr-FR"),
          "a language goes back trimmed; the padding a text box collects is nobody's culture name");
      Assert.That(api.LastWish.OnlineLibraries.SeriesLanguageCulture, Is.EqualTo("en-GB"),
          "the language that was not touched goes back exactly as it was read");
      Assert.That(screen.Status, Does.Contain("next import pass"));
    }

    [Test]
    public void SavingWithoutHavingReadIsRefused()
    {
      StubApi api = new StubApi();
      MetadataSettingsViewModel screen = ScreenOn(api);

      screen.SaveCommand.Execute(null);

      Assert.That(api.Puts, Is.Zero,
          "a form that never loaded must not save over whatever the server holds");
      Assert.That(screen.Status, Does.Contain("reload"));
    }

    [Test]
    public void AnOlderServerIsNamedNotImitated()
    {
      StubApi api = new StubApi();
      MetadataSettingsViewModel screen = ScreenOn(api);
      screen.ReloadCommand.Execute(null);
      Assert.That(api.Gets, Is.EqualTo(1), "opening the row asks once");

      // A provider that has no client yet - the viewer has not set a server address.
      MetadataSettingsViewModel homeless = new MetadataSettingsViewModel(() => null);
      homeless.ReloadCommand.Execute(null);
      Assert.That(homeless.Rows, Is.Empty);
      Assert.That(homeless.Status, Does.Contain("Server address"),
          "the way back is named, not just a complaint");
    }
  }
}
