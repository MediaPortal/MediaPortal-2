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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The television backend: what is asked of a TvHeadend, what comes back, and what is said when nothing does.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Nothing here waits for a TV server. The backend is scripted inside this file, answering with two bouquets,
  /// three channels - one of them radio through the DVB type bits of the service behind it - and a handful of
  /// programmes whose times are the seconds since 1970 in UTC that a real backend really answers with. What is
  /// under test is this front end's half: the query that has to say "anything overlapping this window" in the
  /// backend's own filter language, the paging a table forces on every question, the two-slash stream address
  /// that has to become a real URL, the guide colours the reference chose, and the three different sentences for
  /// nobody being there, being refused, and being pointed at a machine that is not a TV backend at all.
  /// </para>
  /// <para>
  /// No test reads or writes the household's <c>tv.json</c>: each one names its own file in the temporary
  /// folder, and the one test that cares about <c>MP2_TV_FILE</c> puts the variable back afterwards.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvBackend
  {
    #region The scripted backend

    /// <summary>
    /// One round hour of programmes, so that every assertion about a time can be written as an offset from the
    /// same anchor instead of as a magic number nobody can check.
    /// </summary>
    private const long ANCHOR = 1800000000;

    private const string BOUQUETS_JSON = "{\"total\":2,\"entries\":["
        + "{\"uuid\":\"tag-tv\",\"name\":\"All headends\",\"services\":[\"svc-1\",\"svc-2\"]},"
        + "{\"uuid\":\"tag-radio\",\"name\":\"Radio bouquet\",\"services\":[\"svc-3\"]}]}";

    /// <summary>
    /// Three channels, deliberately out of order and with the stream address written the way TVHeadend writes
    /// it - two slashes and no host - on one of them, and left out entirely on another.
    /// </summary>
    private const string CHANNELS_JSON = "{\"total\":3,\"entries\":["
        + "{\"number\":12,\"name\":\"ZDF\",\"epgchannelid\":\"epg-zdf\",\"streamurl\":\"//stream/channelid/12\","
        + "\"tags\":[\"tag-tv\"],\"services\":[\"svc-1\"]},"
        + "{\"number\":3,\"name\":\"Das Erste\",\"epgchannelid\":\"epg-ard\",\"tags\":[\"tag-tv\"],"
        + "\"services\":[\"svc-2\"]},"
        + "{\"number\":101,\"name\":\"Antenne\",\"epgchannelid\":\"epg-ant\",\"tags\":[\"tag-radio\"],"
        + "\"services\":[\"svc-3\"]}]}";

    /// <summary>The DVB type bits: 1 is a thing you watch, 2 is a thing you listen to.</summary>
    private const string SERVICES_JSON = "{\"total\":3,\"entries\":["
        + "{\"uuid\":\"svc-1\",\"type\":1},"
        + "{\"uuid\":\"svc-2\",\"type\":1},"
        + "{\"uuid\":\"svc-3\",\"type\":2}]}";

    /// <summary>
    /// A news bulletin, a film still to come, and - on a second page - a late programme. Only the news carries a
    /// recording, and carries it as "recording" rather than as a mere plan. The film has no end but a length,
    /// which is how an answer from an older backend looks.
    /// </summary>
    private static string EventsJson(long anchor, int total, bool secondPage)
    {
      if (secondPage)
      {
        return "{\"total\":" + total.ToString(CultureInfo.InvariantCulture) + ",\"entries\":["
            + "{\"event_id\":5003,\"start\":" + (anchor + 7200) + ",\"stop\":" + (anchor + 10800)
            + ",\"title\":\"Spätausgabe\",\"channel\":\"epg-zdf\"}]}";
      }

      return "{\"total\":" + total.ToString(CultureInfo.InvariantCulture) + ",\"entries\":["
          + "{\"event_id\":5001,\"start\":" + (anchor - 600) + ",\"stop\":" + (anchor + 1800)
          + ",\"title\":\"Tagesschau\",\"summary\":\"The hour's news\",\"description\":\"News from the country.\","
          + "\"contentType\":\"0x11\",\"contentLabel\":\"News\",\"genre\":[1],\"ageRating\":12,"
          + "\"nextEventId\":5002,\"dvrUuid\":\"dvr-77\",\"dvrState\":\"recording\",\"channel\":\"epg-zdf\"},"
          + "{\"event_id\":5002,\"start\":" + (anchor + 1800) + ",\"duration\":5400,\"title\":\"Tatort\","
          + "\"summary\":\"A murder in Hamburg.\",\"contentType\":\"0x01\",\"contentLabel\":\"Movie\","
          + "\"genre\":[6],\"seasonNumber\":7,\"episodeNumber\":3,\"episodeOnscreen\":\"S07 E03\","
          + "\"starRating\":4,\"channel\":\"epg-zdf\"}]}";
    }

    private const string KINDS_JSON = "{\"total\":2,\"entries\":["
        + "{\"id\":\"0x01\",\"val\":\"Movie\"},{\"id\":\"0x11\",\"val\":\"News\"}]}";

    /// <summary>
    /// A TvHeadend, scripted. It remembers every address it was asked and every <c>Authorization</c> header it
    /// was shown, because four of the tests below are about exactly those two things rather than about the
    /// answer.
    /// </summary>
    private class ScriptedBackend : HttpMessageHandler
    {
      internal readonly List<string> AskedPaths = new List<string>();
      internal readonly List<string> AskedQueries = new List<string>();
      internal readonly List<string?> Authorizations = new List<string?>();

      /// <summary>A path that answers nothing at all, standing for a service the backend does not have.</summary>
      internal readonly List<string> Refuses = new List<string>();

      /// <summary>A path that answers with nonsense, standing for a port that is not a TV backend.</summary>
      internal string? BreaksOn;

      /// <summary>A status to answer every question with, standing for a refusal or a missing machine.</summary>
      internal HttpStatusCode? RefuseEverything;

      /// <summary>The moment the scripted programmes are built around. The clock, when a screen asks "what is on now".</summary>
      internal long EventsAnchor = ANCHOR;

      /// <summary>How many rows the programme table claims to hold, which is what makes the client page.</summary>
      internal int EventsTotal = 2;

      /// <summary>A guide that answers with nothing in it, standing for a backend without programme data.</summary>
      internal bool GuideStaysEmpty;

      /// <summary>A backend that has been started but never set up, which answers every table with nothing.</summary>
      internal bool HasNoChannelsAtAll;

      internal string BodyFor(string path, string query)
      {
        if (HasNoChannelsAtAll && (path.EndsWith("/api/channeltag/list", StringComparison.Ordinal)
            || path.EndsWith("/api/channel/grid", StringComparison.Ordinal)))
          return "{\"total\":0,\"entries\":[]}";

        if (path.EndsWith("/api/channeltag/list", StringComparison.Ordinal))
          return BOUQUETS_JSON;
        if (path.EndsWith("/api/channel/grid", StringComparison.Ordinal))
          return CHANNELS_JSON;
        if (path.EndsWith("/api/service/grid", StringComparison.Ordinal))
          return SERVICES_JSON;
        if (path.EndsWith("/api/epg/content_type/list", StringComparison.Ordinal))
          return KINDS_JSON;
        if (path.EndsWith("/api/epg/events", StringComparison.Ordinal))
        {
          if (GuideStaysEmpty)
            return "{\"total\":0,\"entries\":[]}";

          long start = 0;
          int at = query.IndexOf("start=", StringComparison.Ordinal);
          if (at >= 0)
          {
            string said = query.Substring(at + 6);
            int until = said.IndexOf('&');
            if (until >= 0)
              said = said.Substring(0, until);
            long.TryParse(said, out start);
          }
          return EventsJson(EventsAnchor, EventsTotal, start > 0);
        }
        return "{\"total\":0,\"entries\":[]}";
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        AskedPaths.Add(path);
        AskedQueries.Add(request.RequestUri.Query);
        Authorizations.Add(request.Headers.Authorization == null
            ? null
            : request.Headers.Authorization.Scheme + " " + request.Headers.Authorization.Parameter);

        if (RefuseEverything.HasValue)
          return Answer(request, RefuseEverything.Value, "");

        if (Refuses.Any(r => path.EndsWith(r, StringComparison.Ordinal)))
          return Answer(request, HttpStatusCode.NotFound, "");

        if (BreaksOn != null && path.EndsWith(BreaksOn, StringComparison.Ordinal))
          return Answer(request, HttpStatusCode.OK, "<html><body>a router's own web page</body></html>");

        return Answer(request, HttpStatusCode.OK, BodyFor(path, request.RequestUri.Query));
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

    private static TvBackendSettings NamedBackend()
    {
      TvBackendSettings settings = new TvBackendSettings();
      settings.Host = "tvbox";
      settings.Port = 9981;
      return settings;
    }

    private static TvHeadendApiClient ClientWith(ScriptedBackend backend, TvBackendSettings? settings = null)
    {
      return new TvHeadendApiClient(settings ?? NamedBackend(), new HttpClient(backend));
    }

    private static TvChannel FirstChannel()
    {
      return new TvChannel(12, "ZDF", "epg-zdf", new Uri("http://tvbox:9981/stream/channelid/12"),
          new List<string> { "tag-tv" }, TvMediaType.Television);
    }

    #endregion

    #region What is asked

    [Test]
    public async Task TheBouquetsArriveInTheOrderTheBackendOfferedThem()
    {
      ScriptedBackend backend = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        IReadOnlyList<TvChannelGroup> groups = await client.GetChannelGroupsAsync();

        Assert.That(backend.AskedPaths[0], Is.EqualTo("/api/channeltag/list"));
        Assert.That(groups.Count, Is.EqualTo(2));
        Assert.That(groups[0].Name, Is.EqualTo("All headends"));
        Assert.That(groups[0].ChannelCount, Is.EqualTo(2), "a bouquet is its services, counted");
        Assert.That(groups[1].IsEmpty, Is.False);
      }
    }

    [Test]
    public async Task TheChannelsComeBackInTheOrderAViewerWouldSayThem()
    {
      ScriptedBackend backend = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        IReadOnlyList<TvChannel> channels = await client.GetChannelsAsync();

        Assert.That(channels.Select(c => c.Number), Is.EqualTo(new[] { 3, 12, 101 }));
        Assert.That(channels[0].Name, Is.EqualTo("Das Erste"));
        Assert.That(channels[1].Id, Is.EqualTo("epg-zdf"),
            "programmes are filed under the EPG channel, so that is the id a guide question needs");
      }
    }

    [Test]
    public async Task ATwoSlashStreamAddressBecomesARealAddressAndAnAbsentOneIsBuiltFromTheNumber()
    {
      ScriptedBackend backend = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        IReadOnlyList<TvChannel> channels = await client.GetChannelsAsync();

        Assert.That(channels[1].StreamUrl!.ToString(), Is.EqualTo("http://tvbox:9981/stream/channelid/12"),
            "the backend writes //stream/channelid/12, and combining that the ordinary way would make "
            + "\"stream\" the machine's name");
        Assert.That(channels[0].StreamUrl!.ToString(), Is.EqualTo("http://tvbox:9981/stream/channelid/3"),
            "a channel with no stream URL of its own is still watchable at its documented address");
        Assert.That(channels[0].IsWatchable, Is.True);
      }
    }

    [Test]
    public async Task AChannelIsRadioWhenTheServiceBehindItSaysSo()
    {
      ScriptedBackend backend = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        IReadOnlyList<TvChannel> channels = await client.GetChannelsAsync();

        Assert.That(channels[2].IsRadio, Is.True, "Antenne's service carries the radio bit");
        Assert.That(channels[1].IsRadio, Is.False);
        Assert.That(backend.AskedPaths, Does.Contain("/api/service/grid"));
      }
    }

    [Test]
    public async Task WhenTheServiceGridStaysSilentAChannelCalledRadioIsRadio()
    {
      ScriptedBackend backend = new ScriptedBackend();
      backend.Refuses.Add("/api/service/grid");
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        IReadOnlyList<TvChannel> channels = await client.GetChannelsAsync();

        // Nothing here may fail because one table was missing; the channels still arrive, and the one in the
        // "Radio bouquet" is called radio by its bouquet.
        Assert.That(channels.Count, Is.EqualTo(3));
        Assert.That(channels[2].IsRadio, Is.True);
      }
    }

    [Test]
    public async Task TheLoginTravelsOnlyWhenANameWasWrittenDown()
    {
      ScriptedBackend silent = new ScriptedBackend();
      using (TvHeadendApiClient anonymous = ClientWith(silent))
      {
        await anonymous.GetChannelGroupsAsync();
        Assert.That(silent.Authorizations[0], Is.Null,
            "a backend that asks for nothing must not be handed a header built out of two empty fields");
      }

      TvBackendSettings withLogin = NamedBackend();
      withLogin.Username = "viewer";
      withLogin.Password = "geheim";

      ScriptedBackend guarded = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(guarded, withLogin))
      {
        await client.GetChannelGroupsAsync();
        string expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("viewer:geheim"));
        Assert.That(guarded.Authorizations[0], Is.EqualTo(expected));
      }
    }

    [Test]
    public async Task ATableIsWalkedPageByPageUntilTheRowsRunOut()
    {
      ScriptedBackend backend = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        IReadOnlyList<TvProgram> programs = await client.GetProgramsAsync(FirstChannel(),
            DateTimeOffset.FromUnixTimeSeconds(ANCHOR - 86400), DateTimeOffset.FromUnixTimeSeconds(ANCHOR));

        Assert.That(backend.AskedQueries[0], Does.Contain("start=0").And.Contain("limit="),
            "a table answers at most its limit and says how many there really were, so every question pages");
        Assert.That(programs.Count, Is.EqualTo(2));
      }
    }

    [Test]
    public async Task WhenTheTableSaysThereWereMoreRowsTheNextPageIsAskedForToo()
    {
      ScriptedBackend backend = new ScriptedBackend();
      backend.EventsTotal = 2500;
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        IReadOnlyList<TvProgram> programs = await client.GetProgramsAsync(FirstChannel(),
            DateTimeOffset.FromUnixTimeSeconds(ANCHOR - 86400), DateTimeOffset.FromUnixTimeSeconds(ANCHOR));

        Assert.That(backend.AskedPaths.Where(p => p.EndsWith("/api/epg/events", StringComparison.Ordinal)).Count(),
            Is.GreaterThan(1), "the answer said there were more rows than it sent");
        Assert.That(backend.AskedQueries[1], Does.Contain("start=2000"));
        Assert.That(programs.Count, Is.EqualTo(3));
        Assert.That(programs[2].Title, Is.EqualTo("Spätausgabe"));
      }
    }

    [Test]
    public async Task AWindowIsAskedForAsAnythingOverlappingItAndNotAsAnythingInsideIt()
    {
      ScriptedBackend backend = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        DateTimeOffset from = DateTimeOffset.FromUnixTimeSeconds(ANCHOR);
        await client.GetProgramsAsync(FirstChannel(), from, from.AddHours(6));

        string query = backend.AskedQueries[0];
        Assert.That(query, Does.Contain("channel=epg-zdf"));
        string filter = Uri.UnescapeDataString(query.Substring(query.IndexOf("filter=", StringComparison.Ordinal) + 7));

        Assert.That(filter, Does.Contain("\"field\":\"start\"").And.Contain("\"comparison\":\"lt\""),
            "a programme that began before the window and ends inside it is on the screen when it opens");
        Assert.That(filter, Does.Contain("\"field\":\"stop\"").And.Contain("\"comparison\":\"gt\""));
        Assert.That(filter, Does.Contain(ANCHOR.ToString(CultureInfo.InvariantCulture)),
            "the window travels as the seconds since nineteen seventy, in UTC");
      }
    }

    [Test]
    public async Task AChannelWithoutAnEpgChannelIsAskedForByItsName()
    {
      ScriptedBackend backend = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        TvChannel unlinked = new TvChannel(44, "Third Channel", "", null, new List<string>(),
            TvMediaType.Television);
        await client.GetProgramsAsync(unlinked, DateTimeOffset.FromUnixTimeSeconds(ANCHOR),
            DateTimeOffset.FromUnixTimeSeconds(ANCHOR + 3600));

        string filter = Uri.UnescapeDataString(
            backend.AskedQueries[0].Substring(backend.AskedQueries[0].IndexOf("filter=", StringComparison.Ordinal) + 7));
        Assert.That(filter, Does.Contain("\"field\":\"channelName\"").And.Contain("Third Channel"));
      }
    }

    [Test]
    public async Task TheNamesOfProgrammeKindsComeFromTheBackendItself()
    {
      ScriptedBackend backend = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        IReadOnlyDictionary<int, string> kinds = await client.GetProgramKindNamesAsync();

        Assert.That(backend.AskedPaths, Does.Contain("/api/epg/content_type/list"));
        Assert.That(kinds[0x01], Is.EqualTo("Movie"));
        Assert.That(kinds[0x11], Is.EqualTo("News"));
      }
    }

    #endregion

    #region What comes back

    [Test]
    public async Task ProgrammesArriveAsInstantsAndKnowHowFarThroughTheyAre()
    {
      ScriptedBackend backend = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        IReadOnlyList<TvProgram> programs = await client.GetProgramsAsync(FirstChannel(),
            DateTimeOffset.FromUnixTimeSeconds(ANCHOR), DateTimeOffset.FromUnixTimeSeconds(ANCHOR + 7200));

        TvProgram news = programs[0];
        Assert.That(news.Title, Is.EqualTo("Tagesschau"));
        Assert.That(news.Start, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(ANCHOR - 600)));
        Assert.That(news.Stop, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(ANCHOR + 1800)));
        Assert.That(news.Duration, Is.EqualTo(TimeSpan.FromMinutes(40)));
        Assert.That(news.IsOnAt(DateTimeOffset.FromUnixTimeSeconds(ANCHOR)), Is.True);
        Assert.That(news.ProgressAt(DateTimeOffset.FromUnixTimeSeconds(ANCHOR)),
            Is.EqualTo(600.0 / 2400.0).Within(0.0001));
        Assert.That(news.PrimaryGenre, Is.EqualTo("News"));
        Assert.That(news.AgeRating, Is.EqualTo(12));
        Assert.That(news.IsRecorded, Is.True);
        Assert.That(news.IsBeingRecorded, Is.True);

        TvProgram film = programs[1];
        Assert.That(film.Title, Is.EqualTo("Tatort"));
        Assert.That(film.Start, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(ANCHOR + 1800)));
        Assert.That(film.Stop, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(ANCHOR + 1800 + 5400)),
            "an older answer gives a length instead of an end, counted in seconds");
        Assert.That(film.IsBeingRecorded, Is.False);
        Assert.That(film.Episode!.OnScreen, Is.EqualTo("S07 E03"));
        Assert.That(film.Episode.IsKnown, Is.True);
        Assert.That(film.StarRating, Is.EqualTo(4));
      }
    }

    [Test]
    public async Task NowIsTheProgrammeRunningAndNextIsTheOneAfterIt()
    {
      ScriptedBackend backend = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        TvNowAndNext nowAndNext = await client.GetNowAndNextAsync(FirstChannel(),
            DateTimeOffset.FromUnixTimeSeconds(ANCHOR));

        Assert.That(nowAndNext.HasAnything, Is.True);
        Assert.That(nowAndNext.Now!.Title, Is.EqualTo("Tagesschau"));
        Assert.That(nowAndNext.Next!.Title, Is.EqualTo("Tatort"));
      }
    }

    [Test]
    public async Task AChannelWithNothingInTheTableIsSaidAsNothingRatherThanAsAFailure()
    {
      ScriptedBackend backend = new ScriptedBackend();
      backend.GuideStaysEmpty = true;
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        TvNowAndNext nowAndNext = await client.GetNowAndNextAsync(FirstChannel(),
            DateTimeOffset.FromUnixTimeSeconds(ANCHOR));

        Assert.That(nowAndNext.HasAnything, Is.False);
        Assert.That(nowAndNext.NowTitle, Is.EqualTo("nothing on this channel right now"));
        Assert.That(nowAndNext.NextTitle, Is.Empty);
      }
    }

    [Test]
    public async Task AGuideThatIsRefusedIsRefusedRatherThanQuietlyEmpty()
    {
      ScriptedBackend backend = new ScriptedBackend();
      backend.Refuses.Add("/api/epg/events");
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        // An empty table and a refused one are different facts: the first means the backend has no programme
        // data, the second means it would not answer, and a screen that showed "nothing on" for both would be
        // hiding a broken backend behind an empty evening.
        TvBackendException thrown = Assert.ThrowsAsync<TvBackendException>(async delegate
        {
            await client.GetProgramsAsync(FirstChannel(), DateTimeOffset.FromUnixTimeSeconds(ANCHOR),
                DateTimeOffset.FromUnixTimeSeconds(ANCHOR + 60));
        })!;
        Assert.That(thrown.StatusCode, Is.EqualTo(404));
      }
    }

    [Test]
    public void TheGuideWearsTheEightColoursTheReferenceChose()
    {
      Assert.That(TvGenreColors.ForGenre("Movie"), Is.EqualTo(TvGenreColors.MOVIE));
      Assert.That(TvGenreColors.ForGenre("Movie/Drama"), Is.EqualTo(TvGenreColors.MOVIE));
      Assert.That(TvGenreColors.ForGenre("spielfilm"), Is.EqualTo(TvGenreColors.MOVIE));
      Assert.That(TvGenreColors.ForGenre("Sports"), Is.EqualTo(TvGenreColors.SPORT));
      Assert.That(TvGenreColors.ForGenre("Dokumentation"), Is.EqualTo(TvGenreColors.DOCUMENTARY));
      Assert.That(TvGenreColors.ForGenre("children's drama"), Is.EqualTo(TvGenreColors.CHILDREN),
            "a children's drama carries both words and the reference painted it blue, not grey");
      Assert.That(TvGenreColors.ForGenre("weather"), Is.EqualTo(TvGenreColors.OTHER));
      Assert.That(TvGenreColors.ForGenre(null), Is.EqualTo(TvGenreColors.OTHER));
    }

    [Test]
    public void TheGenreWordsTheReferenceUsedAllStillHaveTheirColour()
    {
      Assert.That(TvGenreColors.SERIES, Is.EqualTo("#ed7d31"));
      Assert.That(TvGenreColors.MOVIE, Is.EqualTo("#7532a8"));
      Assert.That(TvGenreColors.MUSIC, Is.EqualTo("#c89800"));
      Assert.That(TvGenreColors.NEWS, Is.EqualTo("#c03636"));
      Assert.That(TvGenreColors.CHILDREN, Is.EqualTo("#4e93d2"));
      Assert.That(TvGenreColors.DOCUMENTARY, Is.EqualTo("#4f7a32"));
      Assert.That(TvGenreColors.SPORT, Is.EqualTo("#00817e"));
    }

    [Test]
    public void TheDvbGenreCodesBehindABackendsNumberHaveWords()
    {
      Assert.That(TvHeadendApiClient.DvbGenreName(0x01), Is.EqualTo("News"));
      Assert.That(TvHeadendApiClient.DvbGenreName(0x11), Is.EqualTo("News"),
          "the broadcaster's genre is the four-bit nibble, whatever stands above it");
      Assert.That(TvHeadendApiClient.DvbGenreName(0x0F), Is.EqualTo("Children"));
      Assert.That(TvHeadendApiClient.DvbGenreName(0x00), Is.Null, "zero means nothing was said");
    }

    #endregion

    #region What is said when it goes wrong

    [Test]
    public void NobodyNamedABackendIsNotAFailureButASentence()
    {
      using (TvHeadendApiClient client = new TvHeadendApiClient(new TvBackendSettings()))
      {
        Assert.That(client.IsConfigured, Is.False);
        Assert.That(client.BaseUrl, Is.Null);
        Assert.That(client.NotConfiguredSays, Does.Contain("television settings"));

        Assert.That(delegate { return client.GetChannelGroupsAsync().GetAwaiter().GetResult(); },
            Throws.InstanceOf<InvalidOperationException>());
      }
    }

    [Test]
    public void APortThatIsNotAPortLeavesNoAddressToAskAt()
    {
      TvBackendSettings typed = NamedBackend();
      typed.Port = 9981;
      Assert.That(typed.BaseUrl!.ToString(), Is.EqualTo("http://tvbox:9981/"));

      typed.Port = 0;
      Assert.That(typed.BaseUrl, Is.Null);
      typed.Port = 70000;
      Assert.That(typed.BaseUrl, Is.Null);

      typed.Port = 9981;
      typed.Host = "  ";
      Assert.That(typed.IsConfigured, Is.False);
      Assert.That(typed.Describe(), Is.EqualTo("no TV backend named"));
    }

    [Test]
    public void ADescriptionNeverSaysThePassword()
    {
      TvBackendSettings typed = NamedBackend();
      typed.Username = "viewer";
      typed.Password = "geheim";
      Assert.That(typed.Describe(), Is.EqualTo("tvbox:9981"));
    }

    [Test]
    public async Task AWrongPortLandingOnSomeOtherWebPageIsNamedAsNotATVBackend()
    {
      ScriptedBackend backend = new ScriptedBackend();
      backend.BreaksOn = "/api/channel/grid";
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        TvBackendException? thrown = null;
        try
        {
          await client.GetChannelsAsync();
        }
        catch (TvBackendException ex)
        {
          thrown = ex;
        }

        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown!.Message, Does.Contain("not the answer a TV backend gives"));
        Assert.That(thrown.NeedsLogin, Is.False);
      }
    }

    [Test]
    public async Task AMachineThatAnswersFourOhFourIsNotATVBackendEither()
    {
      ScriptedBackend backend = new ScriptedBackend();
      backend.RefuseEverything = HttpStatusCode.NotFound;
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        TvBackendException thrown = Assert.ThrowsAsync<TvBackendException>(
            async delegate { await client.GetChannelGroupsAsync(); })!;
        Assert.That(thrown.StatusCode, Is.EqualTo(404));
        Assert.That(thrown.Message, Does.Contain("not a TV backend"));
        Assert.That(thrown.Url.Host, Is.EqualTo("tvbox"));
      }
    }

    [Test]
    public async Task ARefusalToAcceptTheNameIsTheOneFailureAScreenCanDoSomethingAbout()
    {
      ScriptedBackend backend = new ScriptedBackend();
      backend.RefuseEverything = HttpStatusCode.Unauthorized;
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        TvBackendException thrown = Assert.ThrowsAsync<TvBackendException>(
            async delegate { await client.GetChannelGroupsAsync(); })!;
        Assert.That(thrown.NeedsLogin, Is.True);
        Assert.That(thrown.Message, Does.Contain("name and password"));
      }
    }

    [Test]
    public async Task AnEmptyAnswerIsAnEmptyTableRatherThanAnError()
    {
      ScriptedBackend backend = new ScriptedBackend();
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        IReadOnlyList<TvProgram> none = await client.FindProgramsAsync("anything",
            DateTimeOffset.FromUnixTimeSeconds(ANCHOR), DateTimeOffset.FromUnixTimeSeconds(ANCHOR + 60));
        Assert.That(none, Is.Not.Null);
      }
    }

    #endregion

    #region The file beside frontend.json

    private string _folder = "";

    [SetUp]
    public void MakeFolder()
    {
      _folder = Path.Combine(Path.GetTempPath(), "mp2tv" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_folder);
    }

    [TearDown]
    public void ClearUp()
    {
      Environment.SetEnvironmentVariable("MP2_TV_FILE", null);
      try
      {
        if (Directory.Exists(_folder))
          Directory.Delete(_folder, true);
      }
      catch (IOException)
      {
        // A temporary folder that will not go away is not worth failing a test over.
      }
    }

    private string FileNamed(string name)
    {
      return Path.Combine(_folder, name);
    }

    [Test]
    public void ABoxNobodyHasToldAnythingComesBackAsNothingConfiguredAndNoComplaint()
    {
      string? problem;
      TvBackendSettings read = TvBackendSettings.Load(FileNamed("absent.json"), out problem);

      Assert.That(read.IsConfigured, Is.False);
      Assert.That(problem, Is.Null);
      Assert.That(read.Port, Is.EqualTo(TvBackendSettings.DEFAULT_PORT));
      Assert.That(read.GuideDays, Is.EqualTo(TvBackendSettings.DEFAULT_GUIDE_DAYS),
          "fourteen days is the reference's own horizon, and the answer to an empty field");
    }

    [Test]
    public void TheAddressSurvivesBeingWrittenAndReadAgain()
    {
      TvBackendSettings written = new TvBackendSettings();
      written.Host = "tvserver.lan";
      written.Port = 9982;
      written.Username = "viewer";
      written.Password = "geheim";
      written.GuideDays = 7;

      bool saved;
      string says = written.Save(FileNamed("tv.json"), out saved);
      Assert.That(saved, Is.True, says);

      string? problem;
      TvBackendSettings read = TvBackendSettings.Load(FileNamed("tv.json"), out problem);
      Assert.That(problem, Is.Null);
      Assert.That(read.Host, Is.EqualTo("tvserver.lan"));
      Assert.That(read.Port, Is.EqualTo(9982));
      Assert.That(read.Username, Is.EqualTo("viewer"));
      Assert.That(read.Password, Is.EqualTo("geheim"));
      Assert.That(read.GuideDays, Is.EqualTo(7));
      Assert.That(read.BaseUrl!.ToString(), Is.EqualTo("http://tvserver.lan:9982/"));
    }

    [Test]
    public void AFileWrittenByHandWithNonsenseInItIsCorrectedRatherThanRefused()
    {
      string file = FileNamed("hand.json");
      File.WriteAllText(file, "{ \"host\": \"tvbox\", \"port\": 0, \"guide_days\": 0, }");

      string? problem;
      TvBackendSettings read = TvBackendSettings.Load(file, out problem);
      Assert.That(problem, Is.Null);
      Assert.That(read.Host, Is.EqualTo("tvbox"));
      Assert.That(read.Port, Is.EqualTo(TvBackendSettings.DEFAULT_PORT));
      Assert.That(read.GuideDays, Is.EqualTo(TvBackendSettings.DEFAULT_GUIDE_DAYS));
    }

    [Test]
    public void AFileThatIsNotTheFileIsSaidAsOneSentenceAndReadAsNothing()
    {
      string file = FileNamed("broken.json");
      File.WriteAllText(file, "this is not json at all");

      string? problem;
      TvBackendSettings read = TvBackendSettings.Load(file, out problem);
      Assert.That(read.IsConfigured, Is.False);
      Assert.That(problem, Does.Contain(file).And.Contain("could not be read"));
    }

    [Test]
    public void TheEnvironmentSaysWhereTheFileLiesWhenTheTestsAndTheGatesWantToKnow()
    {
      string file = FileNamed("fromEnvironment.json");
      Environment.SetEnvironmentVariable("MP2_TV_FILE", file);
      Assert.That(TvBackendSettings.DefaultFilePath, Is.EqualTo(file));

      TvBackendSettings written = new TvBackendSettings();
      written.Host = "fromenvironment";
      bool saved;
      written.Save(null, out saved);
      Assert.That(saved, Is.True);
      Assert.That(File.Exists(file), Is.True);

      string? problem;
      Assert.That(TvBackendSettings.Load(null, out problem).Host, Is.EqualTo("fromenvironment"));
    }

    [Test]
    public void EveryAnswerAboutTheGuideAndTheZappingSurvivesBeingWrittenAndReadAgain()
    {
      TvBackendSettings written = new TvBackendSettings();
      written.Host = "tvbox";
      written.VisibleHours = 6.5;
      written.VisibleRows = 14;
      written.ZapTimeoutSeconds = 4.5;
      written.ShowChannelNames = false;
      written.ShowChannelNumbers = false;
      written.ShowGenreColors = false;
      written.ShowSeriesInfo = false;
      written.HideAllChannelsGroup = true;
      written.ZapFromGuide = false;
      written.ZapByChannelIndex = false;
      written.AutoStartTV = true;

      bool saved;
      string says = written.Save(FileNamed("guide.json"), out saved);
      Assert.That(saved, Is.True, says);

      string? problem;
      TvBackendSettings read = TvBackendSettings.Load(FileNamed("guide.json"), out problem);
      Assert.That(problem, Is.Null);
      Assert.That(read.VisibleHours, Is.EqualTo(6.5));
      Assert.That(read.VisibleRows, Is.EqualTo(14));
      Assert.That(read.ZapTimeoutSeconds, Is.EqualTo(4.5));
      Assert.That(read.ShowChannelNames, Is.False);
      Assert.That(read.ShowChannelNumbers, Is.False);
      Assert.That(read.ShowGenreColors, Is.False);
      Assert.That(read.ShowSeriesInfo, Is.False);
      Assert.That(read.HideAllChannelsGroup, Is.True);
      Assert.That(read.ZapFromGuide, Is.False);
      Assert.That(read.ZapByChannelIndex, Is.False);
      Assert.That(read.AutoStartTV, Is.True);
    }

    [Test]
    public void TheDefaultsAreTheOnesTheReferenceShipped()
    {
      TvBackendSettings plain = new TvBackendSettings();

      Assert.That(plain.ShowChannelNames, Is.True);
      Assert.That(plain.ShowChannelNumbers, Is.True);
      Assert.That(plain.ShowSeriesInfo, Is.True, "the reference showed a programme's episode unless told not to");
      Assert.That(plain.HideAllChannelsGroup, Is.False);
      Assert.That(plain.ZapFromGuide, Is.True);
      Assert.That(plain.ZapByChannelIndex, Is.True,
          "a number on a remote means a place in the list to the person holding it");
      Assert.That(plain.AutoStartTV, Is.False,
          "the reference would not start a picture on its own, and neither does this");
      Assert.That(plain.VisibleRows, Is.EqualTo(9), "EpgNumberOfRows defaulted to nine");
      Assert.That(plain.ZapTimeoutSeconds, Is.EqualTo(2), "ZapTimeout defaulted to two seconds");

      // The two this front end answers differently, on purpose, so that a difference is at least a known one.
      Assert.That(plain.VisibleHours, Is.EqualTo(3),
          "the reference's two hours were measured across seventy-five columns; this guide has ninety and started "
          + "at three, which the switch can turn back");
      Assert.That(plain.ShowGenreColors, Is.True,
          "the reference defaulted to off because its skin drew a colour strip of its own beside every block; "
          + "here the fill is the only place a genre is written");
    }

    [Test]
    public void AFileThatNamesImpossibleNumbersIsCorrectedRatherThanRefused()
    {
      string file = FileNamed("impossible.json");
      File.WriteAllText(file, "{ \"host\": \"tvbox\", \"visible_hours\": 99, \"visible_rows\": 0,"
          + " \"zap_timeout_seconds\": 900 }");

      string? problem;
      TvBackendSettings read = TvBackendSettings.Load(file, out problem);
      Assert.That(problem, Is.Null, "a number out of range is a typo, not a broken file");
      Assert.That(read.VisibleHours, Is.EqualTo(TvBackendSettings.DEFAULT_VISIBLE_HOURS),
          "a guide a day wide is not a guide, and the hour labels would lie on top of each other");
      Assert.That(read.VisibleRows, Is.EqualTo(TvBackendSettings.DEFAULT_VISIBLE_ROWS));
      Assert.That(read.ZapTimeoutSeconds, Is.EqualTo(TvBackendSettings.DEFAULT_ZAP_TIMEOUT_SECONDS),
          "half a minute of waiting after a digit is a television that appears to have stopped");

      File.WriteAllText(FileNamed("tiny.json"),
          "{ \"host\": \"tvbox\", \"visible_hours\": 0.5, \"visible_rows\": 40, \"zap_timeout_seconds\": 0.5 }");
      TvBackendSettings edge = TvBackendSettings.Load(FileNamed("tiny.json"), out problem);
      Assert.That(problem, Is.Null);
      Assert.That(edge.VisibleHours, Is.EqualTo(0.5), "half an hour is narrow but it is still a window");
      Assert.That(edge.VisibleRows, Is.EqualTo(40), "forty rows is a screenful of very thin channels");
      Assert.That(edge.ZapTimeoutSeconds, Is.EqualTo(0.5));
    }

    [Test]
    public void TheBouquetTheViewerCanAskToBeRiddenIsKnownByItsNameAlone()
    {
      TvBackendSettings hiding = new TvBackendSettings();
      Assert.That(hiding.IsHiddenGroup("All Channels"), Is.False,
          "off is off: the reference hid nothing until its switch was turned");

      hiding.HideAllChannelsGroup = true;
      Assert.That(hiding.IsHiddenGroup("All Channels"), Is.True);
      Assert.That(hiding.IsHiddenGroup("  all channels  "), Is.True,
          "the reference compared the name exactly; a bouquet somebody typed a name into deserves the same answer "
          + "however they spaced or capitalised it");
      Assert.That(hiding.IsHiddenGroup("Alle Kanäle"), Is.True);
      Assert.That(hiding.IsHiddenGroup("Family"), Is.False);
      Assert.That(hiding.IsHiddenGroup("All Channels Extra"), Is.False,
          "a bouquet that merely starts with those words is a bouquet of its own");
      Assert.That(hiding.IsHiddenGroup(null), Is.False);
    }

    #endregion

    #region The screen that asks

    [Test]
    public void TheScreenShowsWhatTheFileHolds()
    {
      TvBackendSettings stored = new TvBackendSettings();
      stored.Host = "tvbox";
      stored.Port = 9982;
      stored.GuideDays = 3;

      TvSettingsViewModel screen = new TvSettingsViewModel();
      screen.Show(stored, null);

      Assert.That(screen.Host, Is.EqualTo("tvbox"));
      Assert.That(screen.Port, Is.EqualTo("9982"));
      Assert.That(screen.GuideDays, Is.EqualTo("3"));
      Assert.That(screen.DaysAhead, Is.EqualTo(3));
      Assert.That(screen.Status, Does.Contain("has not been asked yet"));
    }

    [Test]
    public void HalfTypedAnswersAreStillAnswersAndNonsenseDaysFallBackToFourteen()
    {
      TvSettingsViewModel screen = new TvSettingsViewModel();
      screen.Show(new TvBackendSettings(), null);
      screen.Host = "tvbox";
      screen.Port = "99";
      screen.GuideDays = "four";

      Assert.That(screen.DaysAhead, Is.EqualTo(14));
      Assert.That(screen.Typed().Port, Is.EqualTo(99), "a number that was typed is the number that was meant");

      screen.Port = "99a";
      Assert.That(screen.Typed().Port, Is.EqualTo(9981), "a half-typed port is not a port, so the default is");

      screen.GuideDays = "31";
      Assert.That(screen.DaysAhead, Is.EqualTo(31));
      screen.GuideDays = "32";
      Assert.That(screen.DaysAhead, Is.EqualTo(14), "nobody keeps a month of guide on a television");
    }

    [Test]
    public void TheScreenCarriesTheGuidesOwnNumbersAndItsSwitches()
    {
      TvBackendSettings stored = new TvBackendSettings();
      stored.Host = "tvbox";
      stored.VisibleHours = 4.5;
      stored.VisibleRows = 12;
      stored.ZapTimeoutSeconds = 3;
      stored.ShowGenreColors = false;
      stored.HideAllChannelsGroup = true;
      stored.AutoStartTV = true;

      TvSettingsViewModel screen = new TvSettingsViewModel();
      screen.Show(stored, null);

      Assert.That(screen.VisibleHours, Is.EqualTo("4.5"));
      Assert.That(screen.VisibleRows, Is.EqualTo("12"));
      Assert.That(screen.ZapTimeout, Is.EqualTo("3"), "three seconds is written as 3, not as 3,0");
      Assert.That(screen.ShowGenreColors, Is.False);
      Assert.That(screen.HideAllChannelsGroup, Is.True);
      Assert.That(screen.AutoStartTV, Is.True);
      Assert.That(screen.ShowChannelNames, Is.True, "everything the file did not mention keeps its default");

      TvBackendSettings back = screen.Typed();
      Assert.That(back.VisibleHours, Is.EqualTo(4.5));
      Assert.That(back.VisibleRows, Is.EqualTo(12));
      Assert.That(back.ZapTimeoutSeconds, Is.EqualTo(3));
      Assert.That(back.ShowGenreColors, Is.False);
      Assert.That(back.HideAllChannelsGroup, Is.True);
      Assert.That(back.AutoStartTV, Is.True);
    }

    [Test]
    public void HalfTypedNumbersFallBackAndACommaIsTakenForThePointItMeans()
    {
      TvSettingsViewModel screen = new TvSettingsViewModel();
      screen.Show(new TvBackendSettings(), null);

      screen.VisibleHours = "two";
      Assert.That(screen.HoursToShow, Is.EqualTo(TvBackendSettings.DEFAULT_VISIBLE_HOURS),
          "a box that does not hold a number yet is not an answer, and the rest of the screen still works");

      screen.VisibleHours = "2.";
      Assert.That(screen.HoursToShow, Is.EqualTo(2),
          "a number that stops after the point is still the number that was typed");

      screen.VisibleHours = "2,5";
      Assert.That(screen.HoursToShow, Is.EqualTo(2.5),
          "the keyboard in front of a television is the one on the remote, and a household that writes 2,5 means "
          + "two and a half hours");

      screen.VisibleRows = "nonsense";
      Assert.That(screen.RowsToShow, Is.EqualTo(TvBackendSettings.DEFAULT_VISIBLE_ROWS));
      screen.VisibleRows = "500";
      Assert.That(screen.RowsToShow, Is.EqualTo(TvBackendSettings.DEFAULT_VISIBLE_ROWS),
          "five hundred channels on one screen is a grid of nothing");

      screen.ZapTimeout = "abc";
      Assert.That(screen.ZapSecondsToShow, Is.EqualTo(TvBackendSettings.DEFAULT_ZAP_TIMEOUT_SECONDS));

      Assert.That(screen.Typed().VisibleRows, Is.EqualTo(TvBackendSettings.DEFAULT_VISIBLE_ROWS),
          "what goes into the file is the corrected answer, not the typo");
    }

    [Test]
    public async Task AskingTheBackendSaysHowManyChannelsItHas()
    {
      ScriptedBackend backend = new ScriptedBackend();
      backend.EventsAnchor = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      TvSettingsViewModel screen = ScreenWith(backend);
      screen.Show(NamedBackend(), null);

      await screen.CheckAsync();

      Assert.That(screen.Status, Does.Contain("2 television channel(s)"));
      Assert.That(screen.Status, Does.Contain("1 radio channel(s)"));
      Assert.That(screen.Status, Does.Contain("Tagesschau"),
          "the check asks the guide too, because channels without programmes are not a working television");
    }

    [Test]
    public async Task AskingABackendThatRefusesSaysSoAndPointsAtThePassword()
    {
      ScriptedBackend backend = new ScriptedBackend();
      backend.RefuseEverything = HttpStatusCode.Unauthorized;
      TvSettingsViewModel screen = ScreenWith(backend);
      screen.Show(NamedBackend(), null);

      await screen.CheckAsync();

      Assert.That(screen.Status, Does.Contain("name and password"));
      Assert.That(screen.Checking, Is.False);
    }

    [Test]
    public async Task AskingBeforeAnythingWasWrittenDownAsksNothingAtAll()
    {
      ScriptedBackend backend = new ScriptedBackend();
      TvSettingsViewModel screen = ScreenWith(backend);
      screen.Show(new TvBackendSettings(), null);

      await screen.CheckAsync();

      Assert.That(backend.AskedPaths, Is.Empty);
      Assert.That(screen.Status, Does.Contain("nothing to ask yet"));
    }

    [Test]
    public async Task ABackendThatWasStartedButNeverSetUpIsSaidAsOneToSetUp()
    {
      ScriptedBackend backend = new ScriptedBackend();
      backend.HasNoChannelsAtAll = true;
      TvSettingsViewModel screen = ScreenWith(backend);
      screen.Show(NamedBackend(), null);

      await screen.CheckAsync();

      Assert.That(screen.Status, Does.Contain("tvbox:9981").And.Contain("no television channels"));
      Assert.That(screen.Status, Does.Contain("set up with channels"),
          "the answer has to say what to do next, not merely that nothing came");
    }

    private static TvSettingsViewModel ScreenWith(ScriptedBackend backend)
    {
      return new TvSettingsViewModel(delegate (TvBackendSettings settings)
      {
        return new TvHeadendApiClient(settings, new HttpClient(backend));
      });
    }

    #endregion
  }
}
