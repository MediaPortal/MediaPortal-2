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
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The recording timers: what a viewer asks a TVHeadend to keep, what comes back when the screens ask what it
  /// has been asked to keep, and what is said when it refuses.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference kept these two halves apart. What was still to come lived in schedules it could cancel
  /// (<c>IScheduleControl</c>), and what had already been written lived in the media library as an ordinary file
  /// it could play; two screens, two providers, two sets of words. A TVHeadend answers both out of one table and
  /// says which is which in the entry's state, so the tests below read the same shape of row from three different
  /// tables and ask the four questions a person asks about one of them: record this, stop that, call it off, keep
  /// it running as a series.
  /// </para>
  /// <para>
  /// As everywhere else in this corner of the front end, no test waits for a real box. The backend is scripted
  /// here and answers the way the real one does - including the two things it is known for: the recording tables
  /// call their rows <c>dlrEntries</c> and their count <c>totalCount</c>, and a yes arrives either as a real JSON
  /// boolean or as the number 1 depending on which endpoint is answering.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvRecordings
  {
    /// <summary>One round hour, so that every time in an assertion is an offset from something checkable.</summary>
    private const long ANCHOR = 1800000000;

    /// <summary>Midnight of the day the anchor stands in, for the rows that carry a day and an hour.</summary>
    private const long ANCHOR_DAY = ANCHOR - ANCHOR % 86400;

    #region The scripted backend

    /// <summary>
    /// Three timers, one of them rubbish. The first is a plain plan that has not begun, the second is being
    /// written right now as part of a renewed series with a file growing behind it, and the third has no uuid -
    /// which is what a row the backend cannot be asked anything about looks like.
    /// </summary>
    private static readonly string UPCOMING_JSON = "{\"totalCount\":3,\"limit\":2000,\"start\":0,\"dlrEntries\":["
        + "{\"uuid\":\"dvr-1\",\"entryId\":11,\"channel\":\"epg-zdf\",\"name\":\"ZDF\",\"title\":\"Tatort\","
        + "\"subtitle\":\"Der neue Tag\",\"description\":\"Mord in Hamburg.\",\"start\":" + ANCHOR
        + ",\"stop\":" + (ANCHOR + 5400) + ",\"state\":\"Scheduled\",\"enabled\":true,\"autoscheduled\":false,"
        + "\"duplicate\":0,\"prio\":0,\"retention\":0,\"size\":0,\"dir\":\"/mnt/tv\",\"autorenew\":0},"
        + "{\"uuid\":\"dvr-2\",\"entryId\":12,\"channel\":\"epg-ard\",\"name\":\"Das Erste\","
        + "\"title\":\"Tagesschau\",\"start\":" + (ANCHOR + 7200) + ",\"stop\":" + (ANCHOR + 7500)
        + ",\"state\":\"Recording\",\"enabled\":1,\"autoscheduled\":1,\"prio\":1,\"retention\":7,"
        + "\"size\":2400000000,\"url\":\"file:///mnt/tv/tagesschau.ts\",\"autorenew\":1,"
        + "\"disabledText\":\"no card free\"},"
        + "{\"uuid\":\"\",\"name\":\"Rauschen\",\"title\":\"noise\",\"start\":" + ANCHOR + ",\"stop\":" + ANCHOR
        + ",\"state\":\"Scheduled\"}]}";

    /// <summary>
    /// Two finished recordings: one whose backend version only ever said a day and an hour, and one that failed.
    /// </summary>
    private static readonly string FINISHED_JSON = "{\"totalCount\":2,\"limit\":2000,\"start\":0,\"dlrEntries\":["
        + "{\"uuid\":\"dvr-9\",\"name\":\"ZDF\",\"title\":\"Film\",\"date\":" + ANCHOR_DAY
        + ",\"timevalue\":3600,\"stop\":" + (ANCHOR_DAY + 7200) + ",\"state\":\"Completed\",\"enabled\":true,"
        + "\"size\":100,\"url\":\"file:///mnt/tv/film.ts\"},"
        + "{\"uuid\":\"dvr-8\",\"name\":\"ZDF\",\"title\":\"Kaputt\",\"start\":" + ANCHOR + ",\"stop\":"
        + (ANCHOR + 60) + ",\"state\":\"Failed\",\"enabled\":true,\"size\":0}]}";

    /// <summary>The guide, so that a test can ask for a programme the way a viewer meets one.</summary>
    private static readonly string EVENTS_JSON = "{\"total\":1,\"entries\":["
        + "{\"event_id\":5002,\"start\":" + (ANCHOR + 1800) + ",\"stop\":" + (ANCHOR + 5400)
        + ",\"title\":\"Tatort\",\"channel\":\"epg-zdf\",\"channelname\":\"ZDF\"}]}";

    /// <summary>The answer to a question that was granted, carrying the entry the backend made.</summary>
    private static readonly string GRANTED_JSON = "{\"success\":true,\"data\":{\"uuid\":\"dvr-77\",\"entryId\":77,"
        + "\"name\":\"ZDF\",\"title\":\"Tatort\",\"start\":" + (ANCHOR + 1800) + ",\"stop\":" + (ANCHOR + 5400)
        + ",\"state\":\"Scheduled\",\"enabled\":true}}";

    /// <summary>A refusal written the way the newer backends write it: a list of objects.</summary>
    private const string REFUSED_JSON = "{\"success\":false,\"errors\":[{\"text\":\"Programm existiert bereits\"},"
        + "{\"text\":\"belegt durch Serienaufnahme\"}]}";

    /// <summary>The same refusal written the way an older one writes it: plain sentences.</summary>
    private const string REFUSED_PLAIN_JSON = "{\"success\":false,\"errors\":[\"no free card\"]}";

    /// <summary>
    /// A TvHeadend, scripted. It remembers every address and every query it was asked, because most of what is
    /// under test here is which question a screen asks rather than what comes back.
    /// </summary>
    private class ScriptedDvr : HttpMessageHandler
    {
      internal readonly List<string> AskedPaths = new List<string>();
      internal readonly List<string> AskedQueries = new List<string>();

      /// <summary>
      /// A status to answer every question with, standing for a machine that is not there or will not answer.
      /// </summary>
      internal HttpStatusCode? RefuseEverything;

      /// <summary>The body the recording endpoints answer with, when a test is about a refusal.</summary>
      internal string? AnswerToWrite;

      /// <summary>What the finished table holds, so that a test can hand a table nothing at all.</summary>
      internal string FinishedBody = FINISHED_JSON;

      internal string BodyFor(string path)
      {
        if (path.EndsWith("/api/epg/events", StringComparison.Ordinal))
          return EVENTS_JSON;
        if (path.EndsWith("/api/dvr/entry/grid_upcoming", StringComparison.Ordinal))
          return UPCOMING_JSON;
        if (path.EndsWith("/api/dvr/entry/grid_finished", StringComparison.Ordinal))
          return FinishedBody;
        if (path.EndsWith("/api/dvr/entry/grid_all", StringComparison.Ordinal))
          return UPCOMING_JSON;

        // Everything a viewer asks to be done, answered with a yes and the entry it made - unless the test put a
        // refusal in its place.
        if (path.Contains("/api/dvr/entry/", StringComparison.Ordinal))
          return AnswerToWrite ?? GRANTED_JSON;

        return "{\"total\":0,\"entries\":[]}";
      }

      /// <summary>The query of the last question asked, which is what the questions-about-questions read.</summary>
      internal string LastQuery
      {
        get { return AskedQueries.Count == 0 ? "" : AskedQueries[AskedQueries.Count - 1]; }
      }

      internal string LastPath
      {
        get { return AskedPaths.Count == 0 ? "" : AskedPaths[AskedPaths.Count - 1]; }
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        AskedPaths.Add(path);
        AskedQueries.Add(request.RequestUri.Query);

        if (RefuseEverything.HasValue)
          return Answer(request, RefuseEverything.Value, "");

        return Answer(request, HttpStatusCode.OK, BodyFor(path));
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

    private static TvHeadendApiClient ClientWith(ScriptedDvr backend)
    {
      TvBackendSettings settings = new TvBackendSettings();
      settings.Host = "tvbox";
      settings.Port = 9981;
      return new TvHeadendApiClient(settings, new HttpClient(backend));
    }

    private static IReadOnlyList<TvRecording> Asked(ScriptedDvr backend, TvRecordingTable table)
    {
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        return client.GetRecordingsAsync(table).GetAwaiter().GetResult();
      }
    }

    /// <summary>
    /// The one programme the scripted guide holds, fetched the way a screen fetches it. The tests that ask for a
    /// recording are asked about a programme a viewer stood in front of, not about nineteen arguments.
    /// </summary>
    private static TvProgram OneProgramme(ScriptedDvr backend)
    {
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        TvChannel channel = new TvChannel(12, "ZDF", "epg-zdf", null, Array.Empty<string>(),
            TvMediaType.Television);
        IReadOnlyList<TvProgram> found = client.GetProgramsAsync(channel,
                DateTimeOffset.FromUnixTimeSeconds(ANCHOR - 3600),
                DateTimeOffset.FromUnixTimeSeconds(ANCHOR + 86400))
            .GetAwaiter().GetResult();
        Assert.That(found, Has.Count.EqualTo(1), "the scripted guide holds one programme");
        return found[0];
      }
    }

    #endregion

    #region Reading the tables

    [Test]
    public void TheTimersOneTableHoldsAreReadWithTheirTimesTheirChannelsAndTheirStates()
    {
      ScriptedDvr backend = new ScriptedDvr();

      IReadOnlyList<TvRecording> timers = Asked(backend, TvRecordingTable.Upcoming);

      Assert.That(backend.AskedPaths[0], Does.EndWith("/api/dvr/entry/grid_upcoming"),
          "the upcoming timers are asked of the table that holds them");
      Assert.That(timers, Has.Count.EqualTo(2), "the row the backend gave no id for is no recording");

      TvRecording waiting = timers[0];
      Assert.That(waiting.Uuid, Is.EqualTo("dvr-1"));
      Assert.That(waiting.Title, Is.EqualTo("Tatort"));
      Assert.That(waiting.Heading, Is.EqualTo("Tatort - Der neue Tag"),
          "a screen writes the episode behind the title, which is what the reference's rows did");
      Assert.That(waiting.ChannelName, Is.EqualTo("ZDF"),
          "the channel of a recording arrives under the name field, not under a channel name");
      Assert.That(waiting.Start, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(ANCHOR)));
      Assert.That(waiting.Duration, Is.EqualTo(TimeSpan.FromMinutes(90)));
      Assert.That(waiting.TimeRange, Is.EqualTo(waiting.StartLocal + " - " + waiting.StopLocal),
          "the two ends of it read as one line, which is the column the reference gave them");
      Assert.That(waiting.IsWaiting, Is.True);
      Assert.That(waiting.IsRecording, Is.False);
      Assert.That(waiting.IsCompleted, Is.False);
      Assert.That(waiting.IsSeries, Is.False);
      Assert.That(waiting.HasFile, Is.False);
      Assert.That(waiting.Directory, Is.EqualTo("/mnt/tv"));
    }

    [Test]
    public void ATimerThatIsBeingWrittenSaysThatAndWhereItsFileIsGrowing()
    {
      IReadOnlyList<TvRecording> timers = Asked(new ScriptedDvr(), TvRecordingTable.Upcoming);

      TvRecording running = timers[1];
      Assert.That(running.Title, Is.EqualTo("Tagesschau"));
      Assert.That(running.IsRecording, Is.True, "a state of Recording is a recording happening now");
      Assert.That(running.IsWaiting, Is.False);
      Assert.That(running.IsSeries, Is.True, "a renewed event is the reference's series recording");
      Assert.That(running.Automatic, Is.True, "the backend made this one itself, off the series it was given");
      Assert.That(running.Enabled, Is.True, "a backend that writes 1 for yes still means yes");
      Assert.That(running.HasFile, Is.True);
      Assert.That(running.Size, Does.Contain("GB"), "a file of two billion bytes is said in gigabytes");
      Assert.That(running.Problem, Is.EqualTo("no card free"),
          "what the backend complains about an entry travels to the screen that shows it");
    }

    [Test]
    public void TheFinishedOnesAreAskedOfTheirOwnTableAndEverythingOfTheOneThatHoldsBoth()
    {
      ScriptedDvr onlyOnce = new ScriptedDvr();
      Asked(onlyOnce, TvRecordingTable.Finished);
      Assert.That(onlyOnce.AskedPaths, Has.Count.EqualTo(1), "one table holds enough for one screen");
      Assert.That(onlyOnce.AskedPaths[0], Does.EndWith("/api/dvr/entry/grid_finished"));

      ScriptedDvr everything = new ScriptedDvr();
      Asked(everything, TvRecordingTable.Everything);
      Assert.That(everything.AskedPaths[0], Does.EndWith("/api/dvr/entry/grid_all"),
          "a screen that wants the lot does not ask the two other tables one after the other");
    }

    [Test]
    public void ARecordingThatOnlyEverSaidItsDayAndItsHourIsStillPutOnTheScreen()
    {
      IReadOnlyList<TvRecording> finished = Asked(new ScriptedDvr(), TvRecordingTable.Finished);

      TvRecording film = finished[0];
      Assert.That(film.Title, Is.EqualTo("Film"));
      Assert.That(film.Start, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(ANCHOR_DAY).AddHours(1)),
          "a day counted from the epoch and an hour counted from that midnight are one instant");
      Assert.That(film.Stop, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(ANCHOR_DAY + 7200)));
      Assert.That(film.IsCompleted, Is.True);
    }

    [Test]
    public void ARecordingThatWentWrongIsKeptAndSaidToBeWrong()
    {
      IReadOnlyList<TvRecording> finished = Asked(new ScriptedDvr(), TvRecordingTable.Finished);

      TvRecording broken = finished[1];
      Assert.That(broken.IsFailed, Is.True);
      Assert.That(broken.IsCompleted, Is.False);
      Assert.That(broken.HasFile, Is.False, "a recording that never wrote anything has nothing to play");
      Assert.That(broken.Size, Is.Empty);
    }

    [Test]
    public void ATableThatAnswersWithNothingIsAnEmptyScreenAndNotAFault()
    {
      ScriptedDvr empty = new ScriptedDvr { FinishedBody = "{\"totalCount\":0,\"dlrEntries\":[]}" };

      Assert.That(Asked(empty, TvRecordingTable.Finished), Is.Empty);
    }

    #endregion

    #region Asking for a recording

    [Test]
    public void PlanningARecordingAsksTheBackendForTheProgrammeRatherThanForATime()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvProgram programme = OneProgramme(backend);

      TvBackendAnswer answer;
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        answer = client.ScheduleRecordingAsync(programme, false).GetAwaiter().GetResult();
      }

      Assert.That(backend.LastPath, Does.EndWith("/api/dvr/entry/create"));
      Assert.That(backend.LastQuery, Does.Contain("event_id=5002"),
          "a TVHeadend knows the channel and both ends of the window from its own programme data");
      Assert.That(backend.LastQuery, Does.Contain("autorenew=0"), "one programme was asked for as one programme");
      Assert.That(answer.Succeeded, Is.True);
      Assert.That(answer.Entry, Is.Not.Null);
      Assert.That(answer.Entry!.Uuid, Is.EqualTo("dvr-77"),
          "the entry the backend made comes back with the answer, so the screen can mark its programme at once");
    }

    [Test]
    public void ARecordingAskedForAsASeriesIsAskedForInTheBackendsOwnWordForKeepingItRenewed()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvProgram programme = OneProgramme(backend);

      using (TvHeadendApiClient client = ClientWith(backend))
      {
        client.ScheduleRecordingAsync(programme, true).GetAwaiter().GetResult();
      }

      Assert.That(backend.LastQuery, Does.Contain("autorenew=1"),
          "the reference's eight series types are this backend's one switch");
      Assert.That(backend.LastQuery, Does.Contain("dupdetect=0"),
          "somebody who asked for a series wants the repeats too, unless they said otherwise on the box itself");
    }

    [Test]
    public void CallingATimerOffAsksTheDeleteOfThatEntryAndOfNoOther()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvRecording timer = Asked(backend, TvRecordingTable.Upcoming)[0];

      using (TvHeadendApiClient client = ClientWith(backend))
      {
        client.RemoveRecordingAsync(timer).GetAwaiter().GetResult();
      }

      Assert.That(backend.LastPath, Does.EndWith("/api/dvr/entry/delete"));
      Assert.That(backend.LastQuery, Does.Contain("uuid=dvr-1"));
      Assert.That(backend.LastQuery, Does.Contain("entryId=11"),
          "the row number travels with the uuid because some versions read one and some the other");
    }

    [Test]
    public void StoppingARecordingThatIsWritingDoesNotThrowTheEntryAway()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvRecording running = Asked(backend, TvRecordingTable.Upcoming)[1];

      using (TvHeadendApiClient client = ClientWith(backend))
      {
        client.StopRecordingAsync(running).GetAwaiter().GetResult();
      }

      Assert.That(backend.LastPath, Does.EndWith("/api/dvr/entry/cancel"),
          "the reference's stopping of a running recording ends the recording and leaves the entry standing");
      Assert.That(backend.LastQuery, Does.Contain("uuid=dvr-2"));
    }

    [Test]
    public void TurningASeriesOnCoversEveryEpisodeTheBoxAlreadyKnowsAbout()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvRecording timer = Asked(backend, TvRecordingTable.Upcoming)[0];

      using (TvHeadendApiClient client = ClientWith(backend))
      {
        client.SetSeriesRenewalAsync(timer, true).GetAwaiter().GetResult();
      }

      Assert.That(backend.LastPath, Does.EndWith("/api/dvr/entry/autorenew"));
      Assert.That(backend.LastQuery, Does.Contain("active=1"));
      Assert.That(backend.LastQuery, Does.Contain("all=1"),
          "asking for a series means the episodes the box listed already, not only the one that was standing there");
    }

    #endregion

    #region When the backend says no

    [Test]
    public void ARefusalIsAnAnswerTheScreenCanSayRatherThanAFaultItHasToCatch()
    {
      ScriptedDvr backend = new ScriptedDvr { AnswerToWrite = REFUSED_JSON };
      TvProgram programme = OneProgramme(backend);

      TvBackendAnswer answer;
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        answer = client.ScheduleRecordingAsync(programme, false).GetAwaiter().GetResult();
      }

      Assert.That(answer.Succeeded, Is.False);
      Assert.That(answer.Complaint, Does.Contain("Programm existiert bereits"));
      Assert.That(answer.Complaint, Does.Contain("belegt durch Serienaufnahme"),
          "both of the backend's sentences travel, because the second one is usually the interesting one");
    }

    [Test]
    public void ARefusalWrittenAsPlainSentencesIsStillSaid()
    {
      ScriptedDvr backend = new ScriptedDvr { AnswerToWrite = REFUSED_PLAIN_JSON };
      TvRecording timer = Asked(backend, TvRecordingTable.Upcoming)[0];

      TvBackendAnswer answer;
      using (TvHeadendApiClient client = ClientWith(backend))
      {
        answer = client.RemoveRecordingAsync(timer).GetAwaiter().GetResult();
      }

      Assert.That(answer.Succeeded, Is.False);
      Assert.That(answer.Complaint, Is.EqualTo("no free card"));
    }

    [Test]
    public void ABackendThatIsNotThereIsStillTheOneKindOfFaultScreensCatch()
    {
      ScriptedDvr backend = new ScriptedDvr { RefuseEverything = HttpStatusCode.InternalServerError };

      using (TvHeadendApiClient client = ClientWith(backend))
      {
        Assert.That(delegate
        {
          client.GetRecordingsAsync(TvRecordingTable.Upcoming).GetAwaiter().GetResult();
        }, Throws.TypeOf<TvBackendException>());
      }
    }

    [Test]
    public void ARecordingWithNoIdCannotBePointedAtAndSaysSoRatherThanAskTheWrongQuestion()
    {
      using (TvHeadendApiClient client = ClientWith(new ScriptedDvr()))
      {
        TvRecording nothing = new TvRecording("", null, null, "ZDF", DateTimeOffset.Now,
            DateTimeOffset.Now.AddHours(1), "Titel", null, null, "Scheduled", true, false, false, false,
            0, 0, 0, null, null, null);

        Assert.That(delegate { client.RemoveRecordingAsync(nothing).GetAwaiter().GetResult(); },
            Throws.ArgumentException,
            "a question about an entry nobody can identify would be answered about some other entry");
      }
    }

    #endregion
  }
}
