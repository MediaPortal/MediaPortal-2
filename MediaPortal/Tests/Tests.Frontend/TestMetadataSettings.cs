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

    [Test]
    public void TheDetailFlagsAndTheCountsBecomeRowsAfterTheOlderSwitches()
    {
      StubApi api = new StubApi
      {
        ToAnswer = new MetadataSettingsAnswer
        {
          Movie = new MetadataFlags
          {
            SkipOnlineSearches = false,
            IncludeActorDetails = false,
            IncludeWriterDetails = true,
            MaximumActorCount = 7,
            MaximumCharacterCount = 0
          },
          Audio = new MetadataFlags { IncludeArtistDetails = true }
        }
      };

      MetadataSettingsViewModel screen = ScreenOn(api);
      screen.ReloadCommand.Execute(null);

      Assert.That(screen.Rows.Select(row => row.Block + "." + row.Field), Is.EqualTo(new[]
      {
        "movie.SkipOnlineSearches", "movie.IncludeActorDetails", "movie.IncludeWriterDetails",
        "movie.MaximumActorCount", "movie.MaximumCharacterCount", "audio.IncludeArtistDetails"
      }), "the switches the screen has always shown, then whose details to fetch, then how many people to keep");

      MetadataRow actors = screen.Rows.First(row => row.Field == "MaximumActorCount");
      Assert.That(actors.Text, Is.EqualTo("7"));
      Assert.That(actors.IsNumber, Is.True, "a count is a box the screen reads back as a number");
      Assert.That(actors.IsToggle, Is.False, "and never a checkbox a viewer ticks by habit");
      Assert.That(actors.Label, Does.Contain("between 0 and 100"),
          "the range the server stores stands in the row's own words, where the number is typed");
      Assert.That(actors.Label, Does.Contain("Movies"), "and says which kind of media it limits");

      Assert.That(screen.Rows.First(row => row.Field == "MaximumCharacterCount").Text, Is.EqualTo("0"),
          "a count stored as none is a row showing zero, not a row left out");
      Assert.That(screen.Rows.First(row => row.Field == "IncludeActorDetails").IsOn, Is.False,
          "a detail flag stored off is a box shown unticked");
      Assert.That(screen.Rows.First(row => row.Field == "IncludeArtistDetails").Label,
          Does.Contain("Music - fetch the artists' details"),
          "the music block is asked for the people a record credits, not for a film's cast");
    }

    [Test]
    public void ACountTheServerWouldRefuseNeverLeavesTheScreen()
    {
      StubApi api = new StubApi
      {
        ToAnswer = new MetadataSettingsAnswer
        {
          Movie = new MetadataFlags { MaximumActorCount = 7, MaximumCharacterCount = 5 }
        }
      };

      MetadataSettingsViewModel screen = ScreenOn(api);
      screen.ReloadCommand.Execute(null);

      MetadataRow actors = screen.Rows.First(row => row.Field == "MaximumActorCount");
      foreach (string refused in new[] { "101", "-1", "many", "" })
      {
        actors.Text = refused;
        screen.SaveCommand.Execute(null);

        Assert.That(api.Puts, Is.Zero,
            "\"" + refused + "\" is no number the endpoint stores, so the screen keeps it to itself");
        Assert.That(screen.Status, Does.Contain("Nothing was sent"),
            "the viewer is told the form went nowhere, rather than reading a silent save");
        Assert.That(screen.Status, Does.Contain("between 0 and 100"),
            "and the sentence says what would have been accepted");
      }

      actors.Text = " 12 ";
      screen.SaveCommand.Execute(null);

      Assert.That(api.Puts, Is.EqualTo(1), "a number the endpoint stores is sent after all");
      Assert.That(api.LastWish.Movie.MaximumActorCount, Is.EqualTo(12),
          "the padding a text box collects is not part of the number");
      Assert.That(api.LastWish.Movie.MaximumCharacterCount, Is.EqualTo(5),
          "the count beside it goes back as it was read");
      Assert.That(screen.Status, Does.Contain("next import pass"));
    }

    [Test]
    public void ADetailFlagOrCountTheServerNeverNamedIsNotSent()
    {
      StubApi api = new StubApi
      {
        // A server whose movie extractor answers the switches the screen has always shown and nothing of the new
        // ones - an older build, or an extractor whose settings class carries none of them.
        ToAnswer = new MetadataSettingsAnswer { Movie = new MetadataFlags { SkipOnlineSearches = true } }
      };

      MetadataSettingsViewModel screen = ScreenOn(api);
      screen.ReloadCommand.Execute(null);
      screen.SaveCommand.Execute(null);

      Assert.That(api.Puts, Is.EqualTo(1));
      Assert.That(api.LastWish.Movie.IncludeActorDetails, Is.Null,
          "a field the screen had no row for is not sent in any shape - null tells the server to leave it");
      Assert.That(api.LastWish.Movie.MaximumActorCount, Is.Null,
          "and a count nobody was shown is not invented with a default of its own");
      Assert.That(api.LastWish.Series, Is.Null, "a block with no rows at all is not sent in any shape");
    }
  }
}
