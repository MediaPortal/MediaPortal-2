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
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The invented television: the backend that answers the TVHeadend questions out of channels and programmes
  /// nobody has ever broadcast, so that a box with no TVHeadend in the house can still be shown what its guide,
  /// its channel lists and its timers do.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Every test here goes through <see cref="TvHeadendApiClient"/> rather than at the demo's own innards, because
  /// that is the whole claim being made: the demo is a backend under the client, not a second guide beside it.
  /// So the questions are the client's real ones - with its real filter clauses, its real paging and its real
  /// reading of a hexadecimal content type - and a demo that answers them wrongly fails here in exactly the way a
  /// guide would look wrong on the television.
  /// </para>
  /// <para>
  /// Times are never fixed, because the demo builds its programmes out of the clock: an assertion about demo
  /// programmes is an assertion about "the evening that contains now", which is what the demo has to be full of at
  /// three in the afternoon and at four in the morning alike.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvDemoBackend
  {
    #region Asking the demo the way a screen does

    /// <summary>The demo asks for nothing but its own switch.</summary>
    private static TvBackendSettings DemoOn()
    {
      TvBackendSettings settings = new TvBackendSettings();
      settings.DemoData = true;
      return settings;
    }

    private static TvHeadendApiClient DemoClient()
    {
      return DemoTvBackend.Open(DemoOn());
    }

    private static IReadOnlyList<TvChannel> DemoChannels()
    {
      using (TvHeadendApiClient client = DemoClient())
        return client.GetChannelsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Every file a test here writes goes into its own name in the temporary folder, so that no test can read the
    /// <c>tv.json</c> of whoever is running these tests.
    /// </summary>
    private static string FileNamed(string name)
    {
      return Path.Combine(Path.GetTempPath(), "mp2-demo-tv-" + name + "-" +
          Guid.NewGuid().ToString("N") + ".json");
    }

    [SetUp]
    public void PutTheDemoBackToItsStartingEvening()
    {
      // The timers are the one thing the demo remembers, so a test that scheduled something would otherwise leave
      // it standing on the next test's timers screen.
      DemoTvBackend.ResetRecordings();
    }

    #endregion

    #region What the switch does to the settings

    [Test]
    public void DemoDataCountsAsAnAnswerToWhereTheTelevisionComesFrom()
    {
      TvBackendSettings settings = DemoOn();

      Assert.That(settings.IsConfigured, Is.True,
          "with demo data there is something to ask, so no screen is allowed to say 'no TV backend yet'");
      Assert.That(settings.BaseUrl, Is.Not.Null);
      Assert.That(settings.Describe(), Does.Contain("demo"));
      Assert.That(settings.Host, Is.Empty, "and none of this needs a machine named");
    }

    [Test]
    public void TheSwitchIsWrittenToTheFileLikeEveryOtherAnswer()
    {
      string file = FileNamed("switch");
      try
      {
        bool saved;
        DemoOn().Save(file, out saved);
        Assert.That(saved, Is.True);

        string? problem;
        TvBackendSettings read = TvBackendSettings.Load(file, out problem);

        Assert.That(problem, Is.Null);
        Assert.That(read.DemoData, Is.True);
        Assert.That(read.IsConfigured, Is.True);
      }
      finally
      {
        File.Delete(file);
      }
    }

    [Test]
    public void TheFactoryAsksTheOrdinaryBackendWhileTheSwitchIsOff()
    {
      TvBackendSettings settings = new TvBackendSettings();

      using (TvHeadendApiClient client = DemoTvBackend.Open(settings))
      {
        Assert.That(client.IsConfigured, Is.False,
            "a machine that was never named is still not there just because a demo exists");
      }

      settings.Host = "tvbox";
      using (TvHeadendApiClient real = DemoTvBackend.Open(settings))
      {
        Assert.That(real.BaseUrl!.ToString(), Is.EqualTo("http://tvbox:9981/"),
            "an address that was written down is still the address that is asked");
      }
    }

    #endregion

    #region Channels and programmes

    [Test]
    public async Task TheDemoBouquetsAndChannelsArriveTheWayABackendSendsThem()
    {
      using (TvHeadendApiClient client = DemoClient())
      {
        IReadOnlyList<TvChannelGroup> bouquets = await client.GetChannelGroupsAsync();
        Assert.That(bouquets.Select(b => b.Name),
            Is.EqualTo(new[] { "Alle Kanäle", "Favoriten", "HD", "Radio" }));
        Assert.That(bouquets[0].IsEmpty, Is.False);

        IReadOnlyList<TvChannel> channels = await client.GetChannelsAsync();
        Assert.That(channels.Count, Is.EqualTo(16));
        Assert.That(channels.Select(c => c.Number), Is.Ordered.Ascending);
        Assert.That(channels.Count(c => c.IsRadio), Is.EqualTo(4),
            "four of them are things you listen to, and that comes from the service bits like everywhere else");
        Assert.That(channels.Count(c => !c.IsWatchable), Is.Zero);
      }
    }

    [Test]
    public void TheStreamAddressOfADemoChannelBelongsToNobody()
    {
      IReadOnlyList<TvChannel> channels = DemoChannels();

      Assert.That(channels[0].StreamUrl!.Host, Is.EqualTo("tvbackend.invalid"),
          "a demo stream points at a name that cannot belong to a real machine");
    }

    [Test]
    public async Task TheGuideWindowIsFullWhicheverHourItIsAskedFor()
    {
      DateTimeOffset now = DateTimeOffset.Now;

      using (TvHeadendApiClient client = DemoClient())
      {
        IReadOnlyList<TvChannel> channels = await client.GetChannelsAsync();
        IReadOnlyList<TvChannelGroup> bouquets = await client.GetChannelGroupsAsync();

        IReadOnlyList<TvProgram> programs = await client.GetProgramsAsync(bouquets[0],
            now.AddMinutes(-30), now.AddHours(3));

        Assert.That(programs, Is.Not.Empty);
        foreach (TvChannel channel in channels)
        {
          Assert.That(programs.Any(p => p.ChannelId == channel.Id), Is.True,
              "channel " + channel.Name + " has nothing on, and a demo with an empty row is no demo");
        }

        foreach (TvProgram program in programs)
        {
          Assert.That(program.Stop > now.AddMinutes(-30) && program.Start < now.AddHours(3), Is.True,
              "everything that came back overlaps the window that was asked for");
        }
      }
    }

    [Test]
    public async Task AProgrammeThatBeganBeforeTheWindowIsStillAnswered()
    {
      TvChannel channel = DemoChannels().First(c => !c.IsRadio);

      using (TvHeadendApiClient client = DemoClient())
      {
        TvNowAndNext around = await client.GetNowAndNextAsync(channel, DateTimeOffset.Now);
        Assert.That(around.Now, Is.Not.Null, "a demo channel is always showing something");

        TvProgram running = around.Now!;
        IReadOnlyList<TvProgram> later = await client.GetProgramsAsync(channel,
            running.Stop.AddMinutes(-1), running.Stop);

        Assert.That(later.Select(p => p.Id), Does.Contain(running.Id),
            "a programme that started before a window and ends inside it is on the screen at the window's left edge");
      }
    }

    [Test]
    public async Task AQuestionAboutOneChannelNeverAnswersAnother()
    {
      TvChannel channel = DemoChannels().First(c => !c.IsRadio);

      using (TvHeadendApiClient client = DemoClient())
      {
        IReadOnlyList<TvProgram> programs = await client.GetProgramsAsync(channel,
            DateTimeOffset.Now.AddHours(-6), DateTimeOffset.Now.AddHours(6));

        Assert.That(programs, Is.Not.Empty);
        Assert.That(programs.Select(p => p.ChannelId).Distinct().ToList(),
            Is.EquivalentTo(new[] { channel.Id }));
      }
    }

    [Test]
    public async Task ATitleSearchFindsItsTitleAndNothingElse()
    {
      using (TvHeadendApiClient client = DemoClient())
      {
        IReadOnlyList<TvProgram> found = await client.FindProgramsAsync("Krimi",
            DateTimeOffset.Now.AddHours(-6), DateTimeOffset.Now.AddHours(18));

        Assert.That(found, Is.Not.Empty);
        Assert.That(found.All(p => p.Title.IndexOf("Krimi", StringComparison.OrdinalIgnoreCase) >= 0
            || (p.Subtitle ?? "").IndexOf("Krimi", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
      }
    }

    [Test]
    public async Task NoProgrammeIsAnsweredTwiceInADay()
    {
      using (TvHeadendApiClient client = DemoClient())
      {
        IReadOnlyList<TvChannelGroup> bouquets = await client.GetChannelGroupsAsync();
        IReadOnlyList<TvProgram> day = await client.GetProgramsAsync(bouquets[0],
            DateTimeOffset.Now.AddHours(-12), DateTimeOffset.Now.AddHours(12));

        Assert.That(day.Select(p => p.Id).Distinct().Count(), Is.EqualTo(day.Count),
            "the paging reads a table page by page, so an id answered twice would draw the same programme twice");
      }
    }

    [Test]
    public async Task EveryGenreColourTheSkinCarriesIsUsedSomewhereInTwoDays()
    {
      using (TvHeadendApiClient client = DemoClient())
      {
        IReadOnlyList<TvChannelGroup> bouquets = await client.GetChannelGroupsAsync();
        IReadOnlyList<TvProgram> twoDays = await client.GetProgramsAsync(bouquets[0],
            DateTimeOffset.Now.AddHours(-24), DateTimeOffset.Now.AddHours(24));

        HashSet<string> colours = new HashSet<string>();
        foreach (TvProgram program in twoDays)
        {
          Assert.That(program.Genres, Is.Not.Empty, "every demo programme says what it is: " + program.Title);
          colours.Add(TvGenreColors.ForGenre(program.Genres[0]));
        }

        Assert.That(colours, Is.EquivalentTo(new[]
        {
          TvGenreColors.MOVIE, TvGenreColors.SERIES, TvGenreColors.DOCUMENTARY, TvGenreColors.MUSIC,
          TvGenreColors.CHILDREN, TvGenreColors.NEWS, TvGenreColors.SPORT, TvGenreColors.OTHER
        }), "the demo is also the only way to see all eight colours without a real backend");
      }
    }

    [Test]
    public async Task AnEpisodeSaysWhichEpisodeItIsAndAFilmDoesNot()
    {
      using (TvHeadendApiClient client = DemoClient())
      {
        IReadOnlyList<TvChannelGroup> bouquets = await client.GetChannelGroupsAsync();
        IReadOnlyList<TvProgram> day = await client.GetProgramsAsync(bouquets[0],
            DateTimeOffset.Now, DateTimeOffset.Now.AddHours(24));

        TvProgram serial = day.First(delegate (TvProgram p)
        {
          return p.Genres.Contains("Series");
        });
        Assert.That(serial.Episode, Is.Not.Null);
        Assert.That(serial.Episode!.OnScreen, Is.Not.Null.And.Not.Empty);

        TvProgram film = day.First(delegate (TvProgram p)
        {
          return p.Genres.Contains("Movie");
        });
        Assert.That(film.Episode, Is.Null, "a film is not an episode of anything");
        Assert.That(film.StarRating, Is.Not.Null);
      }
    }

    #endregion

    #region The timers the demo remembers

    [Test]
    public async Task TheDemoStartsWithARecordingRunningAndOnesFinished()
    {
      using (TvHeadendApiClient client = DemoClient())
      {
        IReadOnlyList<TvRecording> planned = await client.GetRecordingsAsync(TvRecordingTable.Upcoming);
        IReadOnlyList<TvRecording> finished = await client.GetRecordingsAsync(TvRecordingTable.Finished);

        Assert.That(planned.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(planned.Any(r => r.IsRecording), Is.True);
        Assert.That(finished.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(finished.Any(r => r.FilePath != null), Is.True,
            "a finished recording points at a file, which is what the recorded table is for");
      }
    }

    [Test]
    public async Task ARecordingAskedForIsOnTheTimersAndInTheGuideAfterwards()
    {
      TvChannel channel = DemoChannels().First(c => !c.IsRadio);
      TvProgram future;

      using (TvHeadendApiClient client = DemoClient())
      {
        IReadOnlyList<TvProgram> ahead = await client.GetProgramsAsync(channel,
            DateTimeOffset.Now.AddMinutes(5), DateTimeOffset.Now.AddHours(6));
        future = ahead.First(p => p.Start > DateTimeOffset.Now);

        TvBackendAnswer answer = await client.ScheduleRecordingAsync(future, false);
        Assert.That(answer.Succeeded, Is.True, answer.Complaint);
        Assert.That(answer.Entry, Is.Not.Null);

        IReadOnlyList<TvRecording> planned = await client.GetRecordingsAsync(TvRecordingTable.Upcoming);
        Assert.That(planned.Select(r => r.Uuid), Does.Contain(answer.Entry!.Uuid));
        Assert.That(planned.First(r => r.Uuid == answer.Entry!.Uuid).Title, Is.EqualTo(future.Title),
            "the timer carries the programme's own words, which it got out of the same table the guide reads");

        IReadOnlyList<TvProgram> again = await client.GetProgramsAsync(channel, future.Start, future.Stop);
        TvProgram marked = again.First(p => p.Id == future.Id);
        Assert.That(marked.ScheduleId, Is.EqualTo(answer.Entry!.Uuid),
            "the guide has to see that this programme is already being recorded");

        TvBackendAnswer gone = await client.RemoveRecordingAsync(answer.Entry!.Uuid);
        Assert.That(gone.Succeeded, Is.True, gone.Complaint);

        planned = await client.GetRecordingsAsync(TvRecordingTable.Upcoming);
        Assert.That(planned.Select(r => r.Uuid), Does.Not.Contain(answer.Entry!.Uuid));
      }
    }

    [Test]
    public async Task AskingForTheSameProgrammeTwiceIsRefusedTheSecondTime()
    {
      TvChannel channel = DemoChannels().First(c => !c.IsRadio);

      using (TvHeadendApiClient client = DemoClient())
      {
        IReadOnlyList<TvProgram> ahead = await client.GetProgramsAsync(channel,
            DateTimeOffset.Now.AddMinutes(5), DateTimeOffset.Now.AddHours(6));
        TvProgram future = ahead.First(p => p.Start > DateTimeOffset.Now);

        Assert.That((await client.ScheduleRecordingAsync(future, false)).Succeeded, Is.True);

        TvBackendAnswer twice = await client.ScheduleRecordingAsync(future, false);
        Assert.That(twice.Succeeded, Is.False,
            "a demo that agreed to everything would teach nobody what a backend's refusal looks like");
        Assert.That(twice.Complaint, Is.Not.Empty);
      }
    }

    [Test]
    public async Task ARecordingCanBeCalledOffAndRenewedLikeAnyOther()
    {
      using (TvHeadendApiClient client = DemoClient())
      {
        IReadOnlyList<TvRecording> planned = await client.GetRecordingsAsync(TvRecordingTable.Upcoming);
        TvRecording waiting = planned.First(r => r.IsWaiting);

        TvBackendAnswer renewed = await client.SetSeriesRenewalAsync(waiting, true);
        Assert.That(renewed.Succeeded, Is.True, renewed.Complaint);

        planned = await client.GetRecordingsAsync(TvRecordingTable.Upcoming);
        Assert.That(planned.First(r => r.Uuid == waiting.Uuid).IsSeries, Is.True);

        TvBackendAnswer stopped = await client.StopRecordingAsync(waiting);
        Assert.That(stopped.Succeeded, Is.True, stopped.Complaint);

        IReadOnlyList<TvRecording> finished = await client.GetRecordingsAsync(TvRecordingTable.Finished);
        Assert.That(finished.Select(r => r.Uuid), Does.Contain(waiting.Uuid));
      }
    }

    [Test]
    public async Task AQuestionAboutARecordingThatDoesNotExistIsAnsweredWithARefusal()
    {
      using (TvHeadendApiClient client = DemoClient())
      {
        TvBackendAnswer answer = await client.RemoveRecordingAsync("demo-dvr-999");

        Assert.That(answer.Succeeded, Is.False);
        Assert.That(answer.Complaint, Does.Contain("demo"));
      }
    }

    #endregion

    #region The screens on top of it

    [Test]
    public async Task TheGuideIsFullAndSaysTheProgrammesAreInvented()
    {
      TvBackendSettings settings = DemoOn();

      using (TvGuideViewModel guide = new TvGuideViewModel(TvMediaType.Television,
          delegate { return settings; }))
      {
        await guide.ReloadAsync();

        Assert.That(guide.Groups, Is.Not.Empty);
        Assert.That(guide.RowCount, Is.GreaterThanOrEqualTo(10),
            "the whole point is that the grid is no longer empty");
        Assert.That(guide.Rows.Count, Is.GreaterThan(0).And.LessThanOrEqualTo(guide.VisibleRows),
            "what the guide shows is a page of the bouquet, not all of it");
        Assert.That(guide.Rows.Sum(delegate (TvGuideRow row) { return row.Blocks.Count; }),
            Is.GreaterThan(10));
        Assert.That(guide.Status, Does.Contain("invented"),
            "a full guide made of made-up channels has to say so on the screen");
      }
    }

    /// <summary>
    /// The demo is the only fixture around with more channels than fit on a screen, so it is where the arithmetic of
    /// paging is measured: a page has to be a screenful, the pages have to hand over to each other without skipping
    /// a channel, and the last page has to stop at the bottom of the bouquet rather than run off it
    /// (<c>EpgGrid.cs:494-506</c>, <c>IsViewPortAtTop</c> and <c>IsViewPortAtBottom</c>).
    /// </summary>
    [Test]
    public async Task ABouquetTallerThanTheScreenIsWalkedThroughWithoutSkippingAChannel()
    {
      TvBackendSettings settings = DemoOn();
      settings.VisibleRows = 3;

      using (TvGuideViewModel guide = new TvGuideViewModel(TvMediaType.Television,
          delegate { return settings; }))
      {
        await guide.ReloadAsync();

        Assert.That(guide.RowCount, Is.GreaterThan(3), "the demo has more channels than a screen of three");
        Assert.That(guide.Rows.Count, Is.EqualTo(3), "and the grid draws three of them, no more");

        List<int> offsets = new List<int>();
        List<string> seen = new List<string>();
        for (guide.FirstRow(); ; guide.PageRowsDown())
        {
          offsets.Add(guide.ViewOffset);
          seen.AddRange(guide.Rows.Select(delegate (TvGuideRow row) { return row.Name; }));
          if (!guide.CanScrollRowsDown)
            break;
        }

        List<int> wants = new List<int>();
        for (int at = 0; at + 3 <= guide.RowCount; at += 3)
          wants.Add(at);
        if (wants[wants.Count - 1] != guide.RowCount - 3)
          wants.Add(guide.RowCount - 3);

        Assert.That(offsets, Is.EqualTo(wants),
            "a page is a screenful of channels, and the last one stops at the bottom of the bouquet");
        Assert.That(seen.Distinct().Count(), Is.EqualTo(guide.RowCount),
            "page after page every channel of the bouquet passes the screen, and none is skipped");
        Assert.That(seen.Count - guide.RowCount, Is.LessThan(3),
            "the only channels shown twice are the last page sliding back to a full screen");

        guide.LastRow();
        Assert.That(guide.ViewOffset, Is.EqualTo(guide.RowCount - 3), "End stops at the last full screen");
        Assert.That(guide.Rows.Select(delegate (TvGuideRow row) { return row.Name; }),
            Is.EquivalentTo(seen.Skip(seen.Count - 3)), "and it is the last three channels of the bouquet");

        guide.PageRowsUp();
        Assert.That(guide.ViewOffset, Is.EqualTo(guide.RowCount - 6), "PageUp gives a screenful back");

        guide.FirstRow();
        Assert.That(guide.ViewOffset, Is.EqualTo(0));
        Assert.That(guide.CanScrollRowsUp, Is.False, "and Home stops at the top of it");
      }
    }

    /// <summary>
    /// Walking down with the arrow has to bring the new channel in at the edge it came from, which is what the
    /// reference's <c>SkipToChannelIndex</c> did (<c>EpgGrid.cs:998-1032</c>): a page that centred itself would
    /// lose the end of the list the viewer is walking towards, and a page that did not move at all would walk the
    /// cursor off the screen it is supposed to be looking at.
    /// </summary>
    [Test]
    public async Task WalkingDownToAChannelBelowTheScreenBringsItInAtTheBottom()
    {
      TvBackendSettings settings = DemoOn();
      settings.VisibleRows = 3;

      using (TvGuideViewModel guide = new TvGuideViewModel(TvMediaType.Television,
          delegate { return settings; }))
      {
        await guide.ReloadAsync();

        // A programme in the middle of the row, and a future one: pressing a running programme would send the guide
        // off to the tuner instead of leaving the cursor on it, and the last programme of a row stands at the very
        // edge of what the backend was asked for, where a shorter row's evening has legitimately run out.
        List<TvGuideBlock> blocks = guide.Rows[0].Blocks;
        TvGuideBlock standing = blocks[Math.Max(0, blocks.Count / 2)];
        Assert.That(standing.Program.Start, Is.GreaterThan(DateTimeOffset.Now),
            "the fixture stands the cursor on something still to come");
        guide.Press(standing);
        DateTimeOffset stoodAt = standing.Program.Start;
        string from = guide.SelectedRow!.Name;

        guide.MoveRowDown();
        Assert.That(guide.ViewOffset, Is.EqualTo(0), "the second channel still fits on the screen, so nothing slides");

        guide.MoveRowDown();
        guide.MoveRowDown();

        Assert.That(guide.SelectedRow!.Name, Is.Not.EqualTo(from));
        Assert.That(guide.ViewOffset, Is.EqualTo(1), "the fourth channel came in below, so the page went up by one");
        Assert.That(guide.Rows[guide.Rows.Count - 1].Name, Is.EqualTo(guide.SelectedRow.Name),
            "and the cursor stands on the bottom row it arrived on, not off the screen");

        Assert.That(guide.SelectedBlock, Is.Not.Null);
        Assert.That(guide.SelectedBlock!.Program.Start, Is.LessThanOrEqualTo(stoodAt),
            "the new channel is stood on at the time the viewer was looking at, not at its beginning");
        Assert.That(guide.SelectedBlock.Program.Stop, Is.GreaterThan(stoodAt));
      }
    }

    /// <summary>
    /// The column stays under the cursor while the channels move past it, which is what a grid is: a viewer who
    /// pressed something at half past eight and walks down to another channel expects to be looking at half past
    /// eight there, not at wherever that channel's current programme happens to begin. The drift this exists for
    /// came from re-anchoring on the programme the cursor stood on, so a channel of two-hour films pulled the
    /// cursor back an hour per step.
    /// </summary>
    [Test]
    public async Task WalkingBetweenChannelsKeepsTheTimeTheViewerStoodAt()
    {
      TvBackendSettings settings = DemoOn();

      using (TvGuideViewModel guide = new TvGuideViewModel(TvMediaType.Television,
          delegate { return settings; }))
      {
        await guide.ReloadAsync();

        List<TvGuideBlock> blocks = guide.Rows[0].Blocks;
        TvGuideBlock standing = blocks[Math.Max(0, blocks.Count / 2)];
        Assert.That(standing.Program.Start, Is.GreaterThan(DateTimeOffset.Now),
            "the fixture stands the cursor on something still to come");
        guide.Press(standing);
        DateTimeOffset stoodAt = standing.Program.Start;

        for (int step = 1; step < guide.Rows.Count; step++)
        {
          guide.MoveRowDown();
          Assert.That(guide.SelectedBlock, Is.Not.Null, "every demo channel has an evening to stand on");
          TvProgram at = guide.SelectedBlock!.Program;
          Assert.That(at.Start, Is.LessThanOrEqualTo(stoodAt),
              "step " + step + ": " + guide.SelectedRow!.Name + " is stood on at the time the viewer chose");
          Assert.That(at.Stop, Is.GreaterThan(stoodAt),
              "step " + step + ": and the programme under the cursor still covers that time, rather than the "
              + "cursor having slid back to the beginning of a longer film on the way");
        }
      }
    }

    [Test]
    public async Task TheWatchingScreenListsTheDemoChannels()
    {
      TvBackendSettings settings = DemoOn();

      using (TvTunerViewModel tuner = new TvTunerViewModel(TvMediaType.Radio, delegate { return settings; }))
      {
        await tuner.ReloadAsync();

        Assert.That(tuner.Channels.Count, Is.EqualTo(4), "a radio screen offers the radio half of the demo");
        Assert.That(tuner.Status, Does.Contain("invented"));
      }
    }

    [Test]
    public async Task TheTimersScreenHasSomethingToDraw()
    {
      TvBackendSettings settings = DemoOn();

      using (TvSchedulesViewModel schedules = new TvSchedulesViewModel(false, delegate { return settings; }))
      {
        await schedules.ReloadAsync();

        Assert.That(schedules.Entries, Is.Not.Empty);
        Assert.That(schedules.Status, Does.Not.Contain("No TV backend"));
      }
    }

    [Test]
    public void TheSettingsScreenCarriesTheSwitchIntoTheFieldsAndBack()
    {
      TvSettingsViewModel model = new TvSettingsViewModel();
      model.Show(DemoOn(), null);

      Assert.That(model.DemoData, Is.True);
      Assert.That(model.Typed().DemoData, Is.True);
      Assert.That(model.Status, Does.Contain("demo"));

      TvSettingsViewModel off = new TvSettingsViewModel();
      off.Show(new TvBackendSettings(), null);
      Assert.That(off.DemoData, Is.False);
      Assert.That(off.Typed().DemoData, Is.False);
    }

    [Test]
    public async Task AskingTheDemoFromTheSettingsScreenSaysWhatItFound()
    {
      TvSettingsViewModel model = new TvSettingsViewModel();
      model.Show(DemoOn(), null);

      await model.CheckAsync();

      Assert.That(model.Status, Does.Contain("answered"));
      Assert.That(model.Status, Does.Contain("12 television channel(s)"));
      Assert.That(model.Status, Does.Contain("4 radio channel(s)"));
      Assert.That(model.Status, Does.Contain("demo"), "the answer names the demo as the thing that answered");
    }

    #endregion
  }
}
