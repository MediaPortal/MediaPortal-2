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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The programme guide: which bouquet it opens on, which channel stands where, which column a programme stands
  /// in, and what the screen says when the backend has nothing to say.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The grid's arithmetic is the whole of this screen, so that is what is measured here: a programme has to come
  /// out in the column its own beginning falls in and span the columns its own length covers, a programme running
  /// over the edge of the window has to keep the end that is on the screen where that end really is, and the line
  /// at now has to disappear when the window walks away from now. Those are the ways a guide goes wrong without
  /// anybody being able to say why it looks odd.
  /// </para>
  /// <para>
  /// The backend is scripted in this file. Its programmes are anchored to the running clock rather than to a
  /// fixed date, because half of what a guide is about is that the evening is happening now - the programme that
  /// is supposed to be running has to still be running when the assertion is made a moment later. The anchor is
  /// snapped down to the same two-minute grid the guide floors its own window to, so that the columns a programme
  /// is expected in come out as whole numbers.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvGuideScreen
  {
    #region The scripted backend

    /// <summary>
    /// Four bouquets: one whose name says television, one that says radio, and two that say nothing either way.
    /// The two that say nothing are the point - a guide that discarded them would be guessing at names, and a
    /// guide that kept the ones that do speak would put radio stations on the television grid.
    /// </summary>
    private const string BOUQUETS_JSON = "{\"total\":4,\"entries\":["
        + "{\"uuid\":\"all-tv\",\"name\":\"All headends\",\"services\":[\"s1\",\"s2\"]},"
        + "{\"uuid\":\"family\",\"name\":\"Family\",\"services\":[\"s3\"]},"
        + "{\"uuid\":\"nur-tv\",\"name\":\"Nur TV\",\"services\":[\"s1\"]},"
        + "{\"uuid\":\"radio-all\",\"name\":\"Radio\",\"services\":[\"s4\"]}]}";

    private const string CHANNELS_JSON = "{\"total\":4,\"entries\":["
        + "{\"number\":3,\"name\":\"Das Erste\",\"epgchannelid\":\"epg-ard\",\"streamurl\":\"//stream/channelid/3\","
        + "\"tags\":[\"all-tv\",\"family\",\"nur-tv\"],\"services\":[\"s1\"]},"
        + "{\"number\":12,\"name\":\"ZDF\",\"epgchannelid\":\"epg-zdf\",\"streamurl\":\"//stream/channelid/12\","
        + "\"tags\":[\"all-tv\",\"nur-tv\"],\"services\":[\"s2\"]},"
        + "{\"number\":44,\"name\":\"Sparten\",\"epgchannelid\":\"epg-sparten\","
        + "\"streamurl\":\"//stream/channelid/44\",\"tags\":[\"family\"],\"services\":[\"s3\"]},"
        + "{\"number\":101,\"name\":\"Antenne\",\"epgchannelid\":\"epg-ant\","
        + "\"streamurl\":\"//stream/channelid/101\",\"tags\":[\"radio-all\"],\"services\":[\"s4\"]}]}";

    /// <summary>The same four channels from a backend that never says which bouquet a channel belongs to.</summary>
    private const string CHANNELS_WITHOUT_BOUQUETS_JSON = "{\"total\":4,\"entries\":["
        + "{\"number\":3,\"name\":\"Das Erste\",\"epgchannelid\":\"epg-ard\",\"tags\":[],\"services\":[\"s1\"]},"
        + "{\"number\":12,\"name\":\"ZDF\",\"epgchannelid\":\"epg-zdf\",\"tags\":[],\"services\":[\"s2\"]},"
        + "{\"number\":44,\"name\":\"Sparten\",\"epgchannelid\":\"epg-sparten\",\"tags\":[],\"services\":[\"s3\"]},"
        + "{\"number\":101,\"name\":\"Antenne\",\"epgchannelid\":\"epg-ant\",\"tags\":[],\"services\":[\"s4\"]}]}";

    private const string SERVICES_JSON = "{\"total\":4,\"entries\":["
        + "{\"uuid\":\"s1\",\"type\":1},{\"uuid\":\"s2\",\"type\":1},"
        + "{\"uuid\":\"s3\",\"type\":1},{\"uuid\":\"s4\",\"type\":2}]}";

    /// <summary>
    /// The evening, anchored to now: a bulletin that began eleven minutes ago and runs an hour, the film after it
    /// for ninety minutes, half an hour of music an hour on, a broadcast that begins two and a half hours on and
    /// so runs off the right-hand end of the window, and one that finished an hour ago and should never be drawn.
    /// </summary>
    private static string EventsFor(string query)
    {
      long base2 = Anchor();

      if (query.Contains("channelTag=radio-all", StringComparison.Ordinal))
      {
        return "{\"total\":1,\"entries\":["
            + "{\"event_id\":9001,\"start\":" + (base2 - 300) + ",\"stop\":" + (base2 + 3300)
            + ",\"title\":\"Nachtprogramm\",\"channel\":\"epg-ant\",\"channelname\":\"Antenne\","
            + "\"contentLabel\":\"Music\"}]}";
      }

      return "{\"total\":5,\"entries\":["
          + "{\"event_id\":7001,\"start\":" + (base2 - 660) + ",\"stop\":" + (base2 + 2940)
          + ",\"title\":\"Tagesschau\",\"summary\":\"The hour's news.\",\"description\":\"News from the country.\","
          + "\"contentLabel\":\"News\",\"genre\":[1],\"channel\":\"epg-ard\",\"channelname\":\"Das Erste\","
          + "\"dvrUuid\":\"dvr-1\",\"dvrState\":\"scheduled\"},"
          + "{\"event_id\":7002,\"start\":" + (base2 + 2940) + ",\"stop\":" + (base2 + 8340)
          + ",\"title\":\"Tatort\",\"summary\":\"A murder in Hamburg.\",\"contentLabel\":\"Movie\",\"genre\":[6],"
          + "\"seasonNumber\":7,\"episodeNumber\":3,\"episodeOnscreen\":\"S07 E03\","
          + "\"channel\":\"epg-ard\",\"channelname\":\"Das Erste\"},"
          + "{\"event_id\":7003,\"start\":" + (base2 + 3600) + ",\"stop\":" + (base2 + 5400)
          + ",\"title\":\"Kaminkonzert\",\"contentLabel\":\"Music\",\"channel\":\"epg-zdf\","
          + "\"channelname\":\"ZDF\"},"
          + "{\"event_id\":7005,\"start\":" + (base2 + 9000) + ",\"stop\":" + (base2 + 16200)
          + ",\"title\":\"Nachtspendung\",\"contentLabel\":\"Film\",\"channel\":\"epg-zdf\",\"channelname\":\"ZDF\"},"
          + "{\"event_id\":7004,\"start\":" + (base2 - 7200) + ",\"stop\":" + (base2 - 3600)
          + ",\"title\":\"Gestern\",\"channel\":\"epg-ard\",\"channelname\":\"Das Erste\"}"
          + "]}";
    }

    /// <summary>
    /// Now, snapped down to the two-minute grid the guide floors its own window to. Without the snap the first
    /// column of the grid would sit a stray minute away from the fixtures and every expectation would need a
    /// tolerance, which is how arithmetic tests stop proving anything.
    /// </summary>
    private static long Anchor()
    {
      long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      return now - now % (TvGuideViewModel.MINUTES_PER_COLUMN * 60L);
    }

    private class ScriptedGuide : HttpMessageHandler
    {
      internal readonly List<string> AskedPaths = new List<string>();
      internal readonly List<string> AskedQueries = new List<string>();

      /// <summary>A status to answer everything with, standing for a backend that will not talk.</summary>
      internal HttpStatusCode? RefuseEverything;

      /// <summary>A table the backend does not have at all.</summary>
      internal readonly List<string> Refuses = new List<string>();

      /// <summary>A backend that lists channels without ever saying which bouquet they are in.</summary>
      internal bool ChannelsCarryNoBouquets;

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        AskedPaths.Add(path);
        AskedQueries.Add(request.RequestUri.Query);

        if (RefuseEverything.HasValue)
          return Answer(request, RefuseEverything.Value, "");

        if (Refuses.Any(r => path.EndsWith(r, StringComparison.Ordinal)))
          return Answer(request, HttpStatusCode.NotFound, "");

        string body = path.EndsWith("/api/channeltag/list", StringComparison.Ordinal) ? BOUQUETS_JSON
            : path.EndsWith("/api/channel/grid", StringComparison.Ordinal)
                ? (ChannelsCarryNoBouquets ? CHANNELS_WITHOUT_BOUQUETS_JSON : CHANNELS_JSON)
            : path.EndsWith("/api/service/grid", StringComparison.Ordinal) ? SERVICES_JSON
            : path.EndsWith("/api/epg/events", StringComparison.Ordinal) ? EventsFor(request.RequestUri.Query)
            : "{\"total\":0,\"entries\":[]}";

        return Answer(request, HttpStatusCode.OK, body);
      }

      private static Task<HttpResponseMessage> Answer(HttpRequestMessage request, HttpStatusCode code, string body)
      {
        return Task.FromResult(new HttpResponseMessage(code)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
      }
    }

    private static TvGuideViewModel GuideWith(ScriptedGuide backend, TvMediaType kind, int rows)
    {
      TvBackendSettings settings = new TvBackendSettings();
      settings.Host = "tvbox";
      settings.Port = 9981;
      settings.VisibleRows = rows;
      return new TvGuideViewModel(kind, delegate { return settings; },
          delegate (TvBackendSettings s) { return new TvHeadendApiClient(s, new HttpClient(backend)); });
    }

    /// <summary>
    /// Opens the guide, which means waiting until the grid is actually there. How many channels fit on the screen
    /// at once is asked for as well, because what the grid does with a bouquet taller than that is half of what it
    /// is for, and a screen that is taller than any fixture in this file would never show it.
    /// </summary>
    private static TvGuideViewModel Opened(ScriptedGuide backend, TvMediaType kind = TvMediaType.Television,
        int rows = TvBackendSettings.DEFAULT_VISIBLE_ROWS)
    {
      TvGuideViewModel guide = GuideWith(backend, kind, rows);
      guide.ReloadAsync().GetAwaiter().GetResult();
      return guide;
    }

    /// <summary>
    /// Waits for the question a press started. The presses that move the window or change the bouquet answer to a
    /// button and so cannot hand their question back to whoever pressed it, which is why the guide keeps it where
    /// it can be waited for.
    /// </summary>
    private static void Settle(TvGuideViewModel guide)
    {
      guide.InFlight.GetAwaiter().GetResult();
    }

    private static TvGuideBlock Block(TvGuideViewModel guide, string channel, string title)
    {
      TvGuideRow? row = guide.Rows.FirstOrDefault(r => r.Name == channel);
      Assert.That(row, Is.Not.Null, "no row for " + channel + "; the grid has "
          + string.Join(", ", guide.Rows.Select(r => r.Name)));
      TvGuideBlock? block = row!.Blocks.FirstOrDefault(b => b.Title == title);
      Assert.That(block, Is.Not.Null, "no '" + title + "' on " + channel + "; the row has "
          + string.Join(", ", row.Blocks.Select(b => b.Title)));
      return block!;
    }

    private static string FilterOf(string query)
    {
      int at = query.IndexOf("filter=", StringComparison.Ordinal);
      Assert.That(at, Is.GreaterThanOrEqualTo(0), "the question asked about no window: " + query);
      return Uri.UnescapeDataString(query.Substring(at + 7));
    }

    #endregion

    #region What arrives

    [Test]
    public void ABoxWithNoBackendNamedAsksNothingAndSaysWhereToSayIt()
    {
      TvGuideViewModel guide = new TvGuideViewModel(TvMediaType.Television,
          delegate { return new TvBackendSettings(); });

      guide.ReloadAsync().GetAwaiter().GetResult();

      Assert.That(guide.Rows, Is.Empty);
      Assert.That(guide.Status, Does.Contain("television settings"));
    }

    [Test]
    public void TheFirstBouquetIsReadAndItsChannelsBecomeRowsInTheOrderAViewerWouldSayThem()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      Assert.That(guide.SelectedGroup, Is.Not.Null);
      Assert.That(guide.SelectedGroup!.Name, Is.EqualTo("All headends"),
          "the television guide opens on the bouquet the backend listed first, not on a guess");
      Assert.That(guide.Rows.Select(r => r.Name), Is.EqualTo(new[] { "Das Erste", "ZDF" }),
          "the bouquet's own channels by their own numbers - a guide whose rows jump about is unreadable");
      Assert.That(guide.Rows[0].Number, Is.EqualTo("3"));
    }

    [Test]
    public void TheTelevisionGuideLeavesTheBouquetsThatSayRadioAlone()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      Assert.That(guide.Groups.Select(g => g.Name), Is.EqualTo(new[] { "All headends", "Family", "Nur TV" }));
    }

    [Test]
    public void TheRadioGuideOpensOnTheBouquetThatSaysRadioAndKeepsTheOnesThatSayNothing()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend, TvMediaType.Radio);

      // "Nur TV" is the only bouquet a radio guide may not offer. The two that name no kind stay in the list,
      // because deciding that a bouquet called "Family" is television would take channels off somebody's guide.
      Assert.That(guide.Groups.Select(g => g.Name), Is.EqualTo(new[] { "All headends", "Family", "Radio" }));
      Assert.That(guide.SelectedGroup!.Name, Is.EqualTo("Radio"),
          "a radio guide that opens on a television bouquet opens on a grid of channels nobody hears on the radio");
      Assert.That(guide.Rows.Select(r => r.Name), Is.EqualTo(new[] { "Antenne" }));
      Assert.That(Block(guide, "Antenne", "Nachtprogramm").Fill, Is.EqualTo(TvGenreColors.MUSIC));
    }

    [Test]
    public void APickedBouquetReadsItsOwnChannelsAndTheirProgrammes()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      guide.SelectedGroup = guide.Groups.First(g => g.Name == "Family");
      Settle(guide);

      Assert.That(guide.Rows.Select(r => r.Name), Is.EqualTo(new[] { "Das Erste", "Sparten" }));
      Assert.That(backend.AskedQueries.Last(), Does.Contain("channelTag=family"),
          "the programmes asked for are the bouquet's own, not every channel the backend happens to have");
    }

    [Test]
    public void TheProgrammesAskedForCoverTheWindowTheGridIsShowing()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      string filter = FilterOf(backend.AskedQueries.Last(q => q.Contains("channelTag=", StringComparison.Ordinal)));
      Assert.That(filter, Does.Contain(guide.Start.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
      Assert.That(filter, Does.Contain(guide.End.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
      Assert.That(TvGuideViewModel.WINDOW_MINUTES, Is.EqualTo(180),
          "three hours is what a flat screen has room for, which is the reference's two hours widened");
    }

    #endregion

    #region Where a programme stands

    [Test]
    public void AProgrammeStandsInTheColumnItBeginsInAndSpansTheColumnsItLasts()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      TvGuideBlock film = Block(guide, "Das Erste", "Tatort");
      int expectedColumn = (int) Math.Round((film.Program.Start - guide.Start).TotalMinutes
          / TvGuideViewModel.MINUTES_PER_COLUMN, MidpointRounding.AwayFromZero);

      Assert.That(film.Column, Is.EqualTo(expectedColumn));
      Assert.That(film.ColumnSpan, Is.EqualTo(45), "ninety minutes is forty-five two-minute columns");
      Assert.That(film.Column + film.ColumnSpan, Is.LessThanOrEqualTo(TvGuideViewModel.COLUMNS));
    }

    [Test]
    public void HalfAnHourIsFifteenColumns()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      TvGuideBlock music = Block(guide, "ZDF", "Kaminkonzert");

      Assert.That(music.Program.Duration.TotalMinutes, Is.EqualTo(30));
      Assert.That(music.ColumnSpan, Is.EqualTo(15));
    }

    [Test]
    public void AProgrammeAlreadyRunningWhenTheWindowOpensStartsAtTheLeftEdgeAndSaysSo()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      TvGuideBlock news = Block(guide, "Das Erste", "Tagesschau");

      Assert.That(news.Column, Is.EqualTo(0));
      Assert.That(news.StartsBeforeWindow, Is.True,
          "its beginning is off the left of the screen, and a block that pretended otherwise would stand in the "
          + "wrong column and take every bar to its right with it");
      Assert.That(news.EndsAfterWindow, Is.False);
      Assert.That(news.IsRunning, Is.True);
      Assert.That(news.IsRecorded, Is.True, "a recording is attached to it, and the guide marks that");
    }

    [Test]
    public void AProgrammeThatEndedBeforeTheWindowOpenedIsNotDrawnAtAll()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      Assert.That(guide.Rows.SelectMany(r => r.Blocks).Select(b => b.Title), Does.Not.Contain("Gestern"));
    }

    [Test]
    public void AProgrammeRunningPastTheRightEdgeIsCutAtItAndSaysSo()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      TvGuideBlock late = Block(guide, "ZDF", "Nachtspendung");

      Assert.That(late.Program.Start < guide.End, Is.True, "it begins inside the window");
      Assert.That(late.Program.Stop > guide.End, Is.True, "and ends outside it");
      Assert.That(late.EndsAfterWindow, Is.True);
      Assert.That(late.Column + late.ColumnSpan, Is.EqualTo(TvGuideViewModel.COLUMNS),
          "the block stops at the edge it is cut at, however much longer the broadcast turns out to be");
    }

    [Test]
    public void TheHoursAlongTheTopAreTheHoursInsideTheWindow()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      Assert.That(guide.Hours.Count, Is.InRange(2, 3),
          "a three-hour window holds the hours it crosses and no others");
      foreach (TvGuideHour hour in guide.Hours)
      {
        Assert.That(hour.Moment.Minute, Is.EqualTo(0), "a ruler beginning at 20:04 is a ruler nobody reads");
        Assert.That(hour.Moment >= guide.Start && hour.Moment < guide.End, Is.True);
        Assert.That(hour.Column, Is.InRange(0, TvGuideViewModel.COLUMNS - 1));
      }
    }

    [Test]
    public void TheLineAtNowStandsWhereNowIsAndVanishesWhenTheWindowWalksAwayFromIt()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      Assert.That(guide.NowIsInside, Is.True);
      Assert.That(guide.NowColumn, Is.GreaterThanOrEqualTo(0));

      guide.ScrollForwardDay();
      Settle(guide);
      Assert.That(guide.NowIsInside, Is.False, "a window over tomorrow holds no now");
      Assert.That(guide.NowColumn, Is.EqualTo(-1));

      guide.ScrollToNow();
      Settle(guide);
      Assert.That(guide.NowIsInside, Is.True);
    }

    #endregion

    #region Walking the evening

    [Test]
    public void HalfAScreenOnMovesTheWindowByHalfAScreenAndAsksAgain()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);
      int askedBefore = backend.AskedQueries.Count;
      DateTimeOffset before = guide.Start;

      guide.ScrollForward();
      Settle(guide);

      Assert.That(guide.Start, Is.EqualTo(before.AddMinutes(TvGuideViewModel.WINDOW_MINUTES / 2)),
          "the reference moved half its visible hours when the viewer walked off the edge of the grid");
      Assert.That(backend.AskedQueries.Count, Is.GreaterThan(askedBefore));
    }

    [Test]
    public void AProgrammeStillOnTheGridAfterAStepIsStillTheOneBeingTalkedAbout()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      guide.Press(Block(guide, "Das Erste", "Tatort"));

      guide.ScrollForward();
      Settle(guide);

      Assert.That(guide.SelectedBlock, Is.Not.Null);
      Assert.That(guide.SelectedBlock!.Title, Is.EqualTo("Tatort"),
          "the arrows must not throw the viewer's own selection away on the way past");
      Assert.That(guide.DetailsTitle, Is.EqualTo("Tatort"));
    }

    [Test]
    public void SteppingThroughTheBouquetsWrapsAroundAtBothEnds()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);
      string first = guide.SelectedGroup!.Name;

      guide.NextGroup();
      Settle(guide);
      guide.NextGroup();
      Settle(guide);
      guide.NextGroup();
      Settle(guide);
      Assert.That(guide.SelectedGroup!.Name, Is.EqualTo(first), "past the last bouquet comes the first again");

      guide.PreviousGroup();
      Settle(guide);
      Assert.That(guide.SelectedGroup!.Name, Is.Not.EqualTo(first),
          "and one step back from the first is the last, not a dead end");
    }

    [Test]
    public void TheGuideEndsUpShowingTheWindowTheViewerStoppedOn()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      guide.ScrollForward();
      Settle(guide);
      guide.ScrollForward();
      Settle(guide);
      guide.ScrollForwardDay();
      Settle(guide);

      Assert.That(guide.End, Is.EqualTo(guide.Start.AddMinutes(TvGuideViewModel.WINDOW_MINUTES)));
      string filter = FilterOf(backend.AskedQueries.Last(q => q.Contains("channelTag=", StringComparison.Ordinal)));
      Assert.That(filter, Does.Contain(guide.Start.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
          "the guide shows the window it last asked about, whatever the earlier questions answered with");
    }

    #endregion

    #region What the screen says about a programme

    [Test]
    public void TheDetailsSayTheTimesTheKindTheChannelAndTheWords()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      guide.Press(Block(guide, "Das Erste", "Tagesschau"));

      Assert.That(guide.DetailsTitle, Is.EqualTo("Tagesschau"));
      Assert.That(guide.DetailsTimes, Does.Contain("-").And.Contain("1 h"),
          "start, dash, end, and how long it runs, which is how the reference's details led");
      Assert.That(guide.DetailsGenre, Is.EqualTo("News"));
      Assert.That(guide.DetailsChannel, Is.EqualTo("Das Erste"));
      Assert.That(guide.DetailsWords, Is.EqualTo("News from the country."),
          "the long description wins over the summary when the backend gave both");
    }

    [Test]
    public void WhenOnlyASummaryWasGivenTheSummaryIsSaid()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      guide.Press(Block(guide, "Das Erste", "Tatort"));
      Assert.That(guide.DetailsWords, Is.EqualTo("A murder in Hamburg."));
    }

    [Test]
    public void WhenNothingWasSaidTheScreenSaysThatRatherThanInventingSomething()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      guide.Press(Block(guide, "ZDF", "Kaminkonzert"));

      Assert.That(guide.DetailsWords, Does.Contain("said nothing"));
      Assert.That(guide.DetailsSeries, Is.Empty);
    }

    [Test]
    public void ASeriesLineArrivesWhenTheBackendToldOneAndStaysAwayWhenItDidNot()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      Assert.That(Block(guide, "Das Erste", "Tatort").SeriesLine, Is.EqualTo("S07 E03"),
          "what the broadcaster wrote on the screen itself wins over the numbers it also sent");
      Assert.That(Block(guide, "Das Erste", "Tagesschau").SeriesLine, Is.Empty);
    }

    [Test]
    public void ABlockIsFilledWithItsKindsColourInTheReferencePalette()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      Assert.That(Block(guide, "Das Erste", "Tagesschau").Fill, Is.EqualTo(TvGenreColors.NEWS));
      Assert.That(Block(guide, "Das Erste", "Tatort").Fill, Is.EqualTo(TvGenreColors.MOVIE));
      Assert.That(Block(guide, "ZDF", "Kaminkonzert").Fill, Is.EqualTo(TvGenreColors.MUSIC));
    }

    #endregion

    #region When the backend does not answer

    [Test]
    public void ARefusedBackendIsSaidOnTheGuideRatherThanShownAsAnEmptyEvening()
    {
      ScriptedGuide backend = new ScriptedGuide();
      backend.RefuseEverything = HttpStatusCode.Unauthorized;
      TvGuideViewModel guide = Opened(backend);

      Assert.That(guide.Rows, Is.Empty);
      Assert.That(guide.Status, Does.Contain("name and password"));
    }

    [Test]
    public void ABackendWithChannelsButNoProgrammeDataStillShowsItsChannels()
    {
      ScriptedGuide backend = new ScriptedGuide();
      backend.Refuses.Add("/api/epg/events");
      TvGuideViewModel guide = Opened(backend);

      Assert.That(guide.Rows.Select(r => r.Name), Is.EqualTo(new[] { "Das Erste", "ZDF" }),
          "a channel that exists belongs on the list even when nobody linked it to a broadcaster");
      Assert.That(guide.Rows.Sum(r => r.Blocks.Count), Is.EqualTo(0));
      Assert.That(guide.Status, Does.Contain("404"),
          "and the screen says where the programmes are missing from rather than saying nothing");
    }

    [Test]
    public void ABouquetThatNamedNoChannelsFallsBackToEveryChannelOfItsOwnKindAndSaysSo()
    {
      TvGuideViewModel guide = Opened(new ScriptedGuide { ChannelsCarryNoBouquets = true });

      Assert.That(guide.Rows.Select(r => r.Name), Is.EqualTo(new[] { "Das Erste", "ZDF", "Sparten" }),
          "a backend that never groups its channels is no reason to draw an empty grid");
      Assert.That(guide.Status, Does.Contain("every television channel"));

      TvGuideViewModel radio = Opened(new ScriptedGuide { ChannelsCarryNoBouquets = true }, TvMediaType.Radio);
      Assert.That(radio.Rows.Select(r => r.Name), Is.EqualTo(new[] { "Antenne" }),
          "and the guess is still only ever about the half of the set this guide is for");
    }

    [Test]
    public void ABouquetThatReallyIsEmptySaysSoRatherThanShowingOtherBouquetsChannels()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);

      guide.SelectedGroup = new TvChannelGroup("nowhere", "Nowhere", 3);
      Settle(guide);

      Assert.That(guide.Rows, Is.Empty,
          "every channel here names the bouquets it belongs to, so a bouquet none of them name is empty");
      Assert.That(guide.Status, Does.Contain("holds no channels"));
    }

    #endregion

    #region What the screen draws

    /// <summary>
    /// The model can be right about a column and the screen still draw the block somewhere else, because the two
    /// agree only through the attached properties the code-behind sets ninety times per row. These tests look at
    /// the controls that came out, not at the numbers that went in.
    /// </summary>
    private static Window ShowOnScreen(TvGuideViewModel guide)
    {
      Window window = new Window
      {
        Content = new TvGuideView(guide),
        Width = 1280,
        Height = 720
      };
      window.Show();
      Pump();
      return window;
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
      Dispatcher.UIThread.RunJobs();
    }

    private static List<Border> DrawnBoxes(Window window)
    {
      return window.GetVisualDescendants().OfType<Border>().Where(b => b.Tag is TvGuideBlock).ToList();
    }

    [Test]
    [AvaloniaTest]
    public void TheScreenDrawsOneBoxPerProgrammeInTheColumnTheModelPutItIn()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);
      Window window = ShowOnScreen(guide);

      List<Border> drawn = DrawnBoxes(window);
      Assert.That(drawn.Count, Is.EqualTo(guide.Rows.Sum(r => r.Blocks.Count)),
          "every programme the model has is a box on the screen, and nothing else is one");

      foreach (TvGuideRow row in guide.Rows)
      {
        foreach (TvGuideBlock block in row.Blocks)
        {
          Border box = drawn.First(b => ReferenceEquals(b.Tag, block));
          Assert.That(Grid.GetColumn(box), Is.EqualTo(block.Column), block.Title + " stands in the wrong column");
          Assert.That(Grid.GetColumnSpan(box), Is.EqualTo(block.ColumnSpan),
              block.Title + " is drawn across the wrong number of columns");
        }
      }
    }

    [Test]
    [AvaloniaTest]
    public void TheBoxBeingTalkedAboutIsTheOneWearingTheWhiteFrame()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);
      Window window = ShowOnScreen(guide);

      guide.Press(Block(guide, "ZDF", "Kaminkonzert"));
      Pump();

      List<Border> framed = DrawnBoxes(window)
          .Where(b => b.BorderBrush != null && b.BorderThickness.Left >= 2)
          .ToList();

      Assert.That(framed.Count, Is.EqualTo(1), "exactly one block wears the frame, or the guide is ambiguous");
      Assert.That(ReferenceEquals(framed[0].Tag, Block(guide, "ZDF", "Kaminkonzert")), Is.True,
          "and it is the one the viewer pressed");
    }

    [Test]
    [AvaloniaTest]
    public void TheRulerWritesTheHoursTheModelNamed()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);
      Window window = ShowOnScreen(guide);

      List<string> written = window.GetVisualDescendants().OfType<TextBlock>()
          .Select(t => t.Text ?? "")
          .ToList();

      foreach (TvGuideHour hour in guide.Hours)
        Assert.That(written, Does.Contain(hour.Text), "the hour " + hour.Text + " is missing from the ruler");
    }

    [Test]
    [AvaloniaTest]
    public void WalkingTheWindowOnDrawsTheNewEveningRatherThanTheOldOne()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);
      Window window = ShowOnScreen(guide);

      guide.ScrollForward();
      Settle(guide);
      Pump();

      List<Border> drawn = DrawnBoxes(window);
      Assert.That(drawn.Count, Is.EqualTo(guide.Rows.Sum(r => r.Blocks.Count)),
          "the screen followed the window instead of keeping the boxes it had");
      foreach (TvGuideRow row in guide.Rows)
      {
        foreach (TvGuideBlock block in row.Blocks)
        {
          Border box = drawn.First(b => ReferenceEquals(b.Tag, block));
          Assert.That(Grid.GetColumn(box), Is.EqualTo(block.Column));
        }
      }
    }

    /// <summary>
    /// The words the details band along the bottom is saying. The band's own sentences are what is looked at here -
    /// a title would also be found inside the block it belongs to, and the point of this test is the difference
    /// between a screen that follows the cursor and one that stopped listening.
    /// </summary>
    private static List<string> BandOf(Window window)
    {
      return window.GetVisualDescendants().OfType<TextBlock>()
          .Where(t => t.GetVisualAncestors().All(a => a.IsVisible))
          .Select(t => t.Text ?? "")
          .ToList();
    }

    /// <summary>
    /// The screen's words about the programme under the cursor have to change when the cursor moves. A model that
    /// raises a plain <c>PropertyChanged</c> event without carrying <c>INotifyPropertyChanged</c> is enough for
    /// the code beside it - which is why the grid redrew, the box was framed, and every model test passed - while
    /// Avalonia's bindings quietly attach to nothing and the screen goes on telling the viewer about the programme
    /// that happened to be under the cursor when the screen opened. That is invisible from the model, so it is
    /// measured here.
    /// </summary>
    [Test]
    [AvaloniaTest]
    public void TheBandAlongTheBottomChangesWhenTheViewerWalksToAnotherProgramme()
    {
      ScriptedGuide backend = new ScriptedGuide();
      TvGuideViewModel guide = Opened(backend);
      Window window = ShowOnScreen(guide);

      guide.Press(Block(guide, "Das Erste", "Tagesschau"));
      Pump();
      List<string> first = BandOf(window);
      Assert.That(first, Does.Contain("News from the country."),
          "the words of the programme the viewer is standing on are said along the bottom");

      guide.Press(Block(guide, "Das Erste", "Tatort"));
      Pump();
      List<string> then = BandOf(window);
      Assert.That(then, Does.Contain("A murder in Hamburg."),
          "and it changes when they walk to the next one");
      Assert.That(then, Has.None.EqualTo("News from the country."),
          "the old programme's words leave the screen with the old programme");
    }

    #endregion

    #region Paging and walking the grid with a remote

    /// <summary>
    /// A television has more channels than a screen has room for, so the grid holds a page of them and the viewer
    /// walks the rest. The reference's guide was the same shape (<c>EpgGrid.cs:494-506</c> drew
    /// <c>EpgNumberOfRows</c> rows out of a list it kept an offset into), and the shape is not decoration: on a
    /// real backend of three hundred channels a grid that drew all of them would be carrying twenty-seven thousand
    /// controls for a screen with room for eight rows.
    /// </summary>
    [Test]
    public void AScreenfulIsWhatTheGridShowsAndAPageWalksAScreenful()
    {
      ScriptedGuide backend = new ScriptedGuide();
      using (TvGuideViewModel guide = Opened(backend, TvMediaType.Television, 1))
      {
        Assert.That(guide.RowCount, Is.EqualTo(2), "the bouquet has two channels");
        Assert.That(guide.Rows.Count, Is.EqualTo(1), "and the grid shows the one that fits");
        Assert.That(guide.Rows[0].Name, Is.EqualTo("Das Erste"));
        Assert.That(guide.CanScrollRowsDown, Is.True, "with the other one below the bottom of the screen");

        guide.PageRowsDown();
        Assert.That(guide.ViewOffset, Is.EqualTo(1), "a page is a screenful of channels, not a row");
        Assert.That(guide.Rows[0].Name, Is.EqualTo("ZDF"));
        Assert.That(guide.CanScrollRowsDown, Is.False, "and the last channel is the last page");

        guide.PageRowsDown();
        Assert.That(guide.ViewOffset, Is.EqualTo(1), "walking past the last page does nothing rather than falling off");

        guide.LastRow();
        guide.FirstRow();
        Assert.That(guide.ViewOffset, Is.EqualTo(0), "Home goes back to the top of the bouquet");
        Assert.That(guide.Rows[0].Name, Is.EqualTo("Das Erste"));
        Assert.That(guide.CanScrollRowsUp, Is.False);
      }
    }

    /// <summary>
    /// Walking down a guide is asking "what is on at half past eight over there", which is why the reference put
    /// its focus back by the time it was standing at (<c>EpgGrid.cs:1216-1240</c>) rather than at the beginning of
    /// whatever row it landed on.
    /// </summary>
    [Test]
    public void WalkingToAnotherChannelStandsOnTheSameTimeThere()
    {
      ScriptedGuide backend = new ScriptedGuide();
      using (TvGuideViewModel guide = Opened(backend))
      {
        guide.Press(Block(guide, "ZDF", "Kaminkonzert"));

        guide.MoveRowUp();
        Assert.That(guide.SelectedRow!.Name, Is.EqualTo("Das Erste"));
        Assert.That(guide.SelectedBlock!.Title, Is.EqualTo("Tatort"),
            "the walk lands on the programme covering the time the viewer was looking at, not on the first one listed");

        guide.MoveRowDown();
        Assert.That(guide.SelectedRow!.Name, Is.EqualTo("ZDF"));
        Assert.That(guide.SelectedBlock!.Title, Is.EqualTo("Kaminkonzert"),
            "and walking back lands where they came from");
        Assert.That(guide.ViewOffset, Is.EqualTo(0), "nothing slid, because both channels fit on the screen");
      }
    }

    [Test]
    public void WalkingPastTheBottomOfTheScreenSlidesThePageAndKeepsTheCursorOnIt()
    {
      ScriptedGuide backend = new ScriptedGuide();
      using (TvGuideViewModel guide = Opened(backend, TvMediaType.Television, 1))
      {
        guide.Press(Block(guide, "Das Erste", "Tatort"));

        Assert.That(guide.CanMoveRowUp, Is.False,
            "at the first channel the Up arrow belongs to whatever is above the grid, which is how a viewer walks out");

        guide.MoveRowDown();
        Assert.That(guide.SelectedRow!.Name, Is.EqualTo("ZDF"));
        Assert.That(guide.ViewOffset, Is.EqualTo(1));
        Assert.That(guide.Rows[0].Name, Is.EqualTo("ZDF"),
            "the page slid along with the cursor instead of walking it off the screen");
        Assert.That(guide.CanMoveRowDown, Is.False, "and the last channel of the bouquet says so");

        guide.MoveRowUp();
        Assert.That(guide.ViewOffset, Is.EqualTo(0), "walking back up slides it back");
        Assert.That(guide.Rows[0].Name, Is.EqualTo("Das Erste"));
      }
    }

    /// <summary>
    /// A row of a guide is three hours long and there are two buttons for walking it, so at the end of a row the
    /// reference moved the window half an hour on (<c>EpgGrid.cs:965-986</c>) rather than leaving the press dead.
    /// </summary>
    [Test]
    public void WalkingOffTheEndOfAChannelWalksTheEveningHalfAnHourOn()
    {
      ScriptedGuide backend = new ScriptedGuide();
      using (TvGuideViewModel guide = Opened(backend))
      {
        DateTimeOffset was = guide.Start;
        int asked = backend.AskedQueries.Count(q => q.Contains("channelTag=", StringComparison.Ordinal));

        guide.Press(Block(guide, "Das Erste", "Tatort"));
        guide.MoveProgramForward();

        Assert.That(guide.Start, Is.EqualTo(was.AddMinutes(30)),
            "the row had nothing after the programme being stood on, so the window moved rather than the press dying");
        Settle(guide);
        Assert.That(backend.AskedQueries.Count(q => q.Contains("channelTag=", StringComparison.Ordinal)),
            Is.GreaterThan(asked), "and the programmes of the new window were asked for");
        Assert.That(guide.Rows.Count, Is.EqualTo(guide.RowCount), "and the grid refilled");

        // The other button does not simply undo the walk: a row that has a programme left to the left of the
        // cursor is walked along first, and only at its left-hand end does the window go back the way it came.
        TvGuideRow walked = guide.Rows.First(r => r.Blocks.Count > 1);
        guide.Press(walked.Blocks[0]);
        guide.MoveProgramBackward();
        Assert.That(guide.Start, Is.EqualTo(was), "at the left-hand end of a row the arrow walks the window back");
        Settle(guide);
      }
    }

    /// <summary>
    /// Sends a key to a control the way a control being stood on receives it, and says whether the screen took it.
    /// Whether the key was taken is half of what is measured here: a grid that claims every arrow leaves the viewer
    /// shut inside it, with no way back out to the buttons above.
    /// </summary>
    private static bool PressKey(InputElement target, Key key)
    {
      KeyEventArgs args = new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = key,
        Source = target
      };
      target.RaiseEvent(args);
      return args.Handled;
    }

    /// <summary>
    /// The keys arriving at the screen rather than at the model. The grid is a scroller, and a scroller answers
    /// its own arrows by scrolling; the guide answers them by walking its channels and its evening, and takes the
    /// keys off the scroller to do it. That is only proved by sending a key to the control the viewer would be
    /// stood on.
    /// </summary>
    [Test]
    [AvaloniaTest]
    public void TheRemoteWalksTheGridThroughTheScreen()
    {
      ScriptedGuide backend = new ScriptedGuide();
      using (TvGuideViewModel guide = Opened(backend, TvMediaType.Television, 1))
      {
        Window window = ShowOnScreen(guide);
        ScrollViewer grid = window.GetVisualDescendants().OfType<ScrollViewer>()
            .First(s => s.Name == "gridScroller");

        // The first channel of the bouquet has nothing above it, so the grid hands the Up arrow on: whether the
        // key was taken is not what is looked at here (the ring of buttons above the grid takes it, which is how a
        // viewer walks out of a guide), but the grid must not have moved either.
        PressKey(grid, Key.Up);
        Assert.That(guide.SelectedRow!.Name, Is.EqualTo("Das Erste"));
        Assert.That(guide.ViewOffset, Is.EqualTo(0));

        Assert.That(PressKey(grid, Key.Down), Is.True);
        Assert.That(guide.SelectedRow!.Name, Is.EqualTo("ZDF"));
        Assert.That(guide.ViewOffset, Is.EqualTo(1), "and the page came along with the cursor");

        PressKey(grid, Key.End);
        Assert.That(guide.ViewOffset, Is.EqualTo(1), "there is nothing below the last channel of the bouquet");

        Assert.That(PressKey(grid, Key.Right), Is.True);
        Assert.That(guide.SelectedBlock!.Title, Is.EqualTo("Nachtspendung"),
            "the next programme on the row being stood on");

        Assert.That(PressKey(grid, Key.Home), Is.True);
        Assert.That(guide.ViewOffset, Is.EqualTo(0));

        Assert.That(PressKey(grid, Key.PageDown), Is.True);
        Assert.That(guide.ViewOffset, Is.EqualTo(1));

        Assert.That(PressKey(grid, Key.Enter), Is.True, "and the key that opens what can be done with a programme");
        Pump();

        Assert.That(PressKey(grid, Key.F5), Is.False, "a key the grid has no use for stays on its way");
      }
    }

    #endregion
  }
}
