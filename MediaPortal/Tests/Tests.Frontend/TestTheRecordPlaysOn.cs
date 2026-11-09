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
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.ViewModels;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// Whether a record plays on by itself: which tune follows this one, what the list beside the equalizer shows
  /// of a record longer than the list, and what the two marks the viewer can press do to the walk.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The player has always been able to open one file and fall silent when it is through, and for a film that is
  /// the whole story. A song differs in one respect only - it arrives as a piece of a record - and that single
  /// difference is what makes a queue necessary: nobody wants to press OK twelve times to hear one album.
  /// </para>
  /// <para>
  /// Most of these tests hold no player at all. Which tune comes next is a decision, and a decision tested
  /// through an engine is a decision tested only as far as the engine cooperates. The tests that do want a
  /// player say so by name.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTheRecordPlaysOn
  {
    /// <summary>
    /// A row of the shelf inside a record: a file this machine can open, numbered as the library numbers the
    /// tunes of an album.
    /// </summary>
    private static TitleViewModel Tune(int number)
    {
      return new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Tune " + number, null, 2024, null,
          "file:///D:/Musik/" + number.ToString("00") + ".mp3", false,
          Track: number, Artist: "The Long Nights"));
    }

    private static List<TitleViewModel> Shelf(int tunes)
    {
      List<TitleViewModel> rows = new List<TitleViewModel>();
      for (int number = 1; number <= tunes; number++)
        rows.Add(Tune(number));
      return rows;
    }

    private static AudioQueue Loaded(int tunes, int pressedAt, int? seed = null)
    {
      AudioQueue queue = new AudioQueue(seed);
      Assert.That(queue.Load(Shelf(tunes), pressedAt), Is.True, "a shelf of tunes was refused");
      return queue;
    }

    private static string Names(AudioQueue queue)
    {
      return string.Join(",", queue.VisibleRows.Select(row => row.Name));
    }

    /// <summary>The names of the tunes the queue asked to be opened, in the order it asked for them.</summary>
    private static List<string> Asked(AudioQueue queue)
    {
      List<string> asked = new List<string>();
      queue.PlayRequested += (_, row) => asked.Add(row.Name);
      return asked;
    }

    // ---- what the list beside the equalizer shows ----------------------------------------------------

    [Test]
    public void TheTuneThatWasPressedIsTheOneTheListSaysIsPlaying()
    {
      AudioQueue queue = Loaded(12, 3);

      Assert.That(queue.Current!.Name, Is.EqualTo("Tune 4"), "a record opened on the wrong tune");
      Assert.That(queue.Current.IsCurrent, Is.True, "the list does not know which tune it is on");
      Assert.That(queue.PositionWords, Is.EqualTo("4 / 12"), "the list counted its tunes from the wrong end");
      Assert.That(queue.Rows.Count, Is.EqualTo(12), "a record was shortened by being opened in the middle");
    }

    [Test]
    public void AListBesideAPlayerLeadsWithWhatIsComing()
    {
      // Ten rows, twelve tunes, and the tunes that already played are the ones that fall away: the question a
      // list standing next to a player answers is what comes next.
      AudioQueue queue = Loaded(12, 3);

      Assert.That(Names(queue),
          Is.EqualTo("Tune 3,Tune 4,Tune 5,Tune 6,Tune 7,Tune 8,Tune 9,Tune 10,Tune 11,Tune 12"));
      Assert.That(queue.VisibleRows.ToList().IndexOf(queue.Current!), Is.EqualTo(1),
          "the tune that plays was not the second row of the list");
    }

    [Test]
    public void ATuneNearTheEndOfARecordGivesItsRoomToTheTunesThatAlreadyPlayed()
    {
      // Standing on the eleventh tune there is only one tune left to promise, so the list fills up backwards
      // rather than drawing nine empty rows.
      AudioQueue queue = Loaded(12, 10);

      Assert.That(Names(queue),
          Is.EqualTo("Tune 3,Tune 4,Tune 5,Tune 6,Tune 7,Tune 8,Tune 9,Tune 10,Tune 11,Tune 12"));
      Assert.That(queue.VisibleRows.ToList().IndexOf(queue.Current!), Is.EqualTo(8),
          "the list did not say which of its rows was playing");
    }

    [Test]
    public void AShortRecordDrawsEveryRowItHas()
    {
      AudioQueue queue = Loaded(4, 1);

      Assert.That(Names(queue), Is.EqualTo("Tune 1,Tune 2,Tune 3,Tune 4"),
          "a record of four tunes was drawn with something missing");
      Assert.That(queue.ShowsList, Is.True);
    }

    [Test]
    public void ARecordOfOneTuneHasNoListToDraw()
    {
      AudioQueue queue = new AudioQueue();
      Assert.That(queue.Load(Shelf(1), 0), Is.True);

      // A single row of a list is a claim that something follows, and nothing does. The screen reads this as
      // "put the panel away" through ShowsList.
      Assert.That(queue.ShowsList, Is.False, "a lone tune was offered as a list of tunes");
    }

    // ---- the two marks --------------------------------------------------------------------------------

    [Test]
    public void TheShuffleMarkMovesWhatFollowsAndLeavesTheTuneThatPlays()
    {
      AudioQueue queue = Loaded(12, 3, seed: 7);
      string playing = queue.Current!.Name;
      string[] before = queue.Rows.Select(row => row.Name).ToArray();

      queue.Shuffled = true;

      Assert.That(queue.Current!.Name, Is.EqualTo(playing), "pressing shuffle stopped the tune that was playing");
      Assert.That(queue.Walk.Select(row => row.Name), Is.EquivalentTo(before),
          "shuffle lost a tune or invented one");
      Assert.That(queue.Walk.Select(row => row.Name), Is.Not.EqualTo(before),
          "shuffle left the rest of the record in its own order");
      Assert.That(queue.VisibleRows.ToList().IndexOf(queue.Current!), Is.EqualTo(1),
          "shuffle moved the tune that was playing out of its place in the list");
    }

    [Test]
    public void TurningTheShuffleOffHandsTheRecordItsOwnOrderBack()
    {
      AudioQueue queue = Loaded(12, 3, seed: 7);
      queue.Shuffled = true;
      queue.Shuffled = false;

      Assert.That(queue.Rows.Select(row => row.Name), Is.EqualTo(
          new[] { "Tune 1", "Tune 2", "Tune 3", "Tune 4", "Tune 5", "Tune 6", "Tune 7", "Tune 8",
              "Tune 9", "Tune 10", "Tune 11", "Tune 12" }),
          "the record did not get its own order back");
      Assert.That(queue.Current!.Name, Is.EqualTo("Tune 4"), "the tune that played was moved out of the way");
      Assert.That(queue.PositionWords, Is.EqualTo("4 / 12"));
    }

    [Test]
    public void WalkingOffTheEndOfARecordThatDoesNotRepeatGoesNowhere()
    {
      AudioQueue queue = Loaded(3, 2);
      List<string> asked = Asked(queue);

      Assert.That(queue.Next(), Is.False, "a record that does not repeat promised a fourth tune");
      Assert.That(asked, Is.Empty, "a record with nothing left was asked for a tune anyway");
      Assert.That(queue.Current!.Name, Is.EqualTo("Tune 3"), "the last tune was left while nothing followed it");
    }

    [Test]
    public void ARecordThatRepeatsItsTunesStartsAgain()
    {
      AudioQueue queue = Loaded(3, 2);
      queue.Repeat = AudioRepeat.All;
      List<string> asked = Asked(queue);

      Assert.That(queue.Next(), Is.True, "a repeating record stopped at its last tune");
      Assert.That(asked, Is.EqualTo(new[] { "Tune 1" }), "the record did not begin again at its first tune");
    }

    [Test]
    public void ARecordRepeatingOneTuneAsksForThatTuneAgain()
    {
      AudioQueue queue = Loaded(3, 1);
      queue.Repeat = AudioRepeat.One;
      List<string> asked = Asked(queue);

      AudioQueueRow? again = queue.Finished(queue.Current!.Title.Title.Id);

      Assert.That(again!.Name, Is.EqualTo("Tune 2"), "the tune that repeats was not the tune that ended");
      Assert.That(asked, Is.EqualTo(new[] { "Tune 2" }));
    }

    [Test]
    public void ATuneThatRanOutWalksOnToTheNextOne()
    {
      AudioQueue queue = Loaded(3, 0);
      List<string> asked = Asked(queue);

      AudioQueueRow? next = queue.Finished(queue.Current!.Title.Title.Id);

      Assert.That(next!.Name, Is.EqualTo("Tune 2"));
      Assert.That(asked, Is.EqualTo(new[] { "Tune 2" }));
    }

    [Test]
    public void AnEndingNobodyWaitedForIsHistory()
    {
      // The engine reports the ending of the media it was opened on. If the queue moved on in the meantime - or
      // if the tune that ended belongs to the party, or to a record nobody opened - answering it would start a
      // second tune for one ending.
      AudioQueue queue = Loaded(3, 0);
      List<string> asked = Asked(queue);

      Assert.That(queue.Finished(Guid.NewGuid()), Is.Null);
      Assert.That(asked, Is.Empty);
    }

    [Test]
    public void ABrokenTuneLeavesTheListAndTheRecordWalksPastIt()
    {
      AudioQueue queue = Loaded(3, 1);
      Guid broken = queue.Current!.Title.Title.Id;
      List<string> asked = Asked(queue);

      AudioQueueRow? next = queue.Refused(broken);

      Assert.That(next!.Name, Is.EqualTo("Tune 3"), "the record stopped at its broken tune");
      Assert.That(Names(queue), Is.EqualTo("Tune 1,Tune 3"), "the panel went on showing a tune that cannot play");
      Assert.That(queue.Current!.Name, Is.EqualTo("Tune 3"), "the list never moved onto the tune it walked to");
    }

    [Test]
    public void ThreeBrokenTunesStopTheRecord()
    {
      // A file that will not open is a file. Three in a row is a share that is gone, and a queue that walked
      // on through a whole album of those would be a screen full of error lines.
      AudioQueue queue = Loaded(6, 0);
      List<string> asked = Asked(queue);

      Assert.That(queue.Refused(queue.Current!.Title.Title.Id), Is.Not.Null);
      Assert.That(queue.Refused(queue.Current!.Title.Title.Id), Is.Not.Null);
      Assert.That(queue.Refused(queue.Current!.Title.Title.Id), Is.Null,
          "a record whose files all fail kept being walked");
      Assert.That(asked.Count, Is.EqualTo(2), "tunes were asked for after the record gave up");
    }

    [Test]
    public void TheBackMarkAtTheBeginningStartsThisTuneAgain()
    {
      AudioQueue queue = Loaded(3, 0);
      List<string> asked = Asked(queue);

      Assert.That(queue.Previous(), Is.True, "the back mark did nothing on the first tune");
      Assert.That(asked, Is.EqualTo(new[] { "Tune 1" }), "the first tune was not started from its beginning");
    }

    [Test]
    public void PressingARowOfTheListPlaysThatTune()
    {
      AudioQueue queue = Loaded(4, 0);
      List<string> asked = Asked(queue);

      Assert.That(queue.PlayAt(2), Is.True);
      Assert.That(asked, Is.EqualTo(new[] { "Tune 3" }));
      Assert.That(queue.PositionWords, Is.EqualTo("3 / 4"));
      Assert.That(queue.PlayAt(9), Is.False, "a row the list does not have was accepted");
    }

    // ---- the lengths the shelf does not carry ---------------------------------------------------------

    [Test]
    public async Task EveryTuneIsAskedForItsOwnLength()
    {
      AudioQueue queue = Loaded(3, 0);
      await queue.ReadLengthsAsync(id => Task.FromResult(Length(id, 212)));

      Assert.That(queue.Rows[0].LengthWords, Is.EqualTo("3:32"), "a tune's minutes were not written out");
      Assert.That(queue.Rows[2].LengthWords, Is.EqualTo("3:32"));
    }

    [Test]
    public async Task ATuneWhoseLengthNobodyWroteDownStandsEmpty()
    {
      AudioQueue queue = Loaded(2, 0);
      await queue.ReadLengthsAsync(id => Task.FromResult(Length(id, null)));

      // "0:00" would be a lie about a length; an empty space is the truth about a missing one.
      Assert.That(queue.Rows[0].LengthWords, Is.Empty);
    }

    [Test]
    public async Task ALengthThatArrivesAfterTheRecordWasLeftLandsNowhere()
    {
      AudioQueue queue = Loaded(2, 0);
      TaskCompletionSource<bool> released = new TaskCompletionSource<bool>();
      Task reading = queue.ReadLengthsAsync(async id =>
      {
        await released.Task;
        return Length(id, 212);
      });

      // The viewer pressed another record while the questions were out. Writing the answers now would put this
      // record's minutes onto the rows of the next one.
      queue.Load(Shelf(5), 0);
      released.SetResult(true);
      await reading;

      Assert.That(queue.Rows[0].LengthWords, Is.Empty, "an answer for a record that was left was written down");
      Assert.That(queue.Rows.Count, Is.EqualTo(5));
    }

    [Test]
    public async Task NoQuestionIsAskedWithoutALibraryToAsk()
    {
      AudioQueue queue = Loaded(2, 0);
      await queue.ReadLengthsAsync(null);

      Assert.That(queue.Rows[0].LengthWords, Is.Empty);
    }

    // ---- together with a player -----------------------------------------------------------------------

    [Test]
    public void TheTuneAfterASongThatRanOutOpensByItself()
    {
      FakePlayer player = new FakePlayer();
      NowPlayingViewModel screen = new NowPlayingViewModel(player);
      try
      {
        List<TitleViewModel> shelf = Shelf(3);
        Assert.That(screen.Play(Playable(shelf[0]), shelf[0].Title.Id, 0, shelf[0].Name), Is.True);
        screen.PlayingAspect = "AudioItem";
        screen.LoadAudioQueue(shelf, 0, "AudioItem");

        player.RaiseEndOfMedia();

        Assert.That(screen.Title, Is.EqualTo("Tune 2"), "the record did not play on after its first tune");
        // The second tune is played onto a screen that has to still know it is showing music: opening a media
        // forgets which shelf it came from, and a screen that forgot would draw a film's face around a song.
        Assert.That(screen.IsAudio, Is.True, "the second tune of a record lost the audio arrangement");
        Assert.That(screen.PlayingArtist, Is.EqualTo("The Long Nights"), "the singer was lost between two tunes");
        Assert.That(screen.AudioQueueRows.Count, Is.GreaterThan(1), "the list went away with the first tune");
      }
      finally
      {
        screen.Dispose();
      }
    }

    [Test]
    public void ASongPlayedFromNowherePlaysOnToNothing()
    {
      FakePlayer player = new FakePlayer();
      NowPlayingViewModel screen = new NowPlayingViewModel(player);
      try
      {
        List<TitleViewModel> shelf = Shelf(3);
        screen.Play(Playable(shelf[0]), shelf[0].Title.Id, 0, shelf[0].Name);
        screen.PlayingAspect = "AudioItem";
        screen.LoadAudioQueue(shelf, 0, "AudioItem");
        screen.ClearAudioQueue();

        player.RaiseEndOfMedia();

        Assert.That(screen.Title, Is.EqualTo("Tune 1"), "a lone song was followed by a tune nobody asked for");
      }
      finally
      {
        screen.Dispose();
      }
    }

    [Test]
    public void StoppingThePlayerEndsTheRecord()
    {
      FakePlayer player = new FakePlayer();
      NowPlayingViewModel screen = new NowPlayingViewModel(player);
      try
      {
        List<TitleViewModel> shelf = Shelf(3);
        screen.Play(Playable(shelf[0]), shelf[0].Title.Id, 0, shelf[0].Name);
        screen.PlayingAspect = "AudioItem";
        screen.LoadAudioQueue(shelf, 0, "AudioItem");
        Assert.That(screen.ShowsAudioQueue, Is.True);

        screen.Stop();

        Assert.That(screen.ShowsAudioQueue, Is.False, "a stopped record went on offering its tunes");
        Assert.That(screen.AudioQueueRows, Is.Empty);
      }
      finally
      {
        screen.Dispose();
      }
    }

    [Test]
    public void TheMarksOfTheRecordAreAnsweredToTheViewer()
    {
      FakePlayer player = new FakePlayer();
      NowPlayingViewModel screen = new NowPlayingViewModel(player);
      try
      {
        List<TitleViewModel> shelf = Shelf(3);
        screen.Play(Playable(shelf[0]), shelf[0].Title.Id, 0, shelf[0].Name);
        screen.PlayingAspect = "AudioItem";
        screen.LoadAudioQueue(shelf, 0, "AudioItem");

        screen.CycleRepeat();
        Assert.That(screen.AudioQueueGoesOn, Is.True, "the repeat mark was not lit");
        Assert.That(screen.RepeatCaption, Does.Contain("all"));

        screen.CycleRepeat();
        Assert.That(screen.AudioQueueRepeatsOne, Is.True, "the second press did not repeat one tune");
        Assert.That(screen.RepeatCaption, Does.Contain("this tune"));

        screen.CycleRepeat();
        Assert.That(screen.AudioQueueGoesOn, Is.False);
        Assert.That(screen.RepeatCaption, Does.Contain("off"));

        screen.ToggleShuffle();
        Assert.That(screen.AudioQueueShuffled, Is.True, "the shuffle mark was not lit");
        Assert.That(screen.Title, Is.EqualTo("Tune 1"), "the shuffle mark stopped the tune that was playing");
      }
      finally
      {
        screen.Dispose();
      }
    }

    // ---- what a row of the list prints ---------------------------------------------------------------

    [Test]
    public void ARowCutsTheRecordOffTheNameTheLibraryGaveIt()
    {
      // The library writes a track's name with its record and its number in front of it, because on a shelf of
      // many records a bare number says nothing: TrackInfo.ToString() (TrackInfo.cs:305) spells the title either
      // "{record}: {number} - {name}" (line 59) or "{number} - {name}" (line 63). Both spellings are cut here.
      Assert.That(AudioQueueRow.TuneName("Nightglass: 3 - The Long Nights", 3), Is.EqualTo("The Long Nights"),
          "the record hung in front of the tune in the record's own list");
      Assert.That(AudioQueueRow.TuneName("9 - Anonymous Under Water", 9), Is.EqualTo("Anonymous Under Water"),
          "the short spelling left its number in the list");
    }

    [Test]
    public void OnlyTheLibrarysOwnPrefixIsCutOffAName()
    {
      // A record's name may carry a colon or a dash of its own, and a name nobody ever numbered has no prefix
      // to cut. Everything that does not read as the library's own spelling - including a number that is not
      // this row's number - has to come through untouched, because rewriting a name is worse than printing it
      // with a prefix.
      Assert.That(AudioQueueRow.TuneName("Rock - Best: 2 - Song For Both", 2), Is.EqualTo("Song For Both"),
          "the cut stopped at the record's own dash instead of the tune's");
      Assert.That(AudioQueueRow.TuneName("Nothing Like This", 7), Is.EqualTo("Nothing Like This"),
          "a name the library never prefixed was rewritten");
      Assert.That(AudioQueueRow.TuneName("Best: 5 - Wrong Number", 3), Is.EqualTo("Best: 5 - Wrong Number"),
          "a number that is not this row's was cut anyway");
      Assert.That(AudioQueueRow.TuneName("3 - Never Numbered", null), Is.EqualTo("3 - Never Numbered"),
          "a tune nobody numbered lost the number the list had nothing else to go by");
      Assert.That(AudioQueueRow.TuneName(null, 4), Is.Empty, "a tune with no name at all was given one");
    }

    // ---- whether the screen may step aside at the end of a tune ---------------------------------------

    [Test]
    public void ARecordIsOnlyThroughWithTheScreenWhenItsLastTuneRanOut()
    {
      // The application asks this before it closes the playback screen over the shelf it was opened from: a
      // record that is halfway through plays on, and the sound deserves the screen that names it.
      AudioQueue queue = Loaded(3, 0);

      Assert.That(queue.PlaysOn, Is.True, "the first tune of a record was called an ending");
      queue.Finished(queue.Current!.Title.Title.Id);
      Assert.That(queue.PlaysOn, Is.True, "the middle of a record was called an ending");
      queue.Finished(queue.Current!.Title.Title.Id);
      // Standing on the last tune of a record that does not repeat, nothing follows it: this is where the screen
      // is allowed to step aside, and it is the tune's own ending that says so, not the walk that follows it.
      Assert.That(queue.PlaysOn, Is.False, "the last tune of a record that does not repeat promised another one");
      queue.Finished(queue.Current!.Title.Title.Id);
      Assert.That(queue.PlaysOn, Is.False, "a record that had run out still promised a tune");
    }

    [Test]
    public void ARecordThatRepeatsNeverEndsItsScreenOnATune()
    {
      AudioQueue queue = Loaded(2, 1);

      queue.Repeat = AudioRepeat.All;
      Assert.That(queue.PlaysOn, Is.True, "the last tune of a record that starts again ended the screen");

      queue.Repeat = AudioRepeat.One;
      Assert.That(queue.PlaysOn, Is.True, "a tune that repeats alone was treated as an ending");
    }

    // ---- how many rows the screen has room for ---------------------------------------------------------

    [Test]
    public void ARecordLongerThanThePanelIsShownAsAWindowThatWalksWithThePlayer()
    {
      // The screen hands the list the number of rows its height has room for. Fewer rows must never mean a
      // shorter record: the head of the panel goes on counting all twelve, and the window keeps the current tune
      // with as many of the ones after it as fit.
      AudioQueue queue = Loaded(12, 5);
      queue.Slots = 4;

      Assert.That(queue.VisibleRows, Has.Count.EqualTo(4), "the panel showed more rows than it was given");
      Assert.That(queue.VisibleRows[0].IsCurrent, Is.True, "the window stopped leading with the tune that plays");
      Assert.That(queue.PositionWords, Is.EqualTo("6 / 12"), "the panel told the viewer the record was shorter");
      Assert.That(queue.Rows, Has.Count.EqualTo(12), "a record was shortened by a small window");

      queue.Slots = 100;
      Assert.That(queue.VisibleRows, Has.Count.EqualTo(AudioQueue.SLOTS),
          "a tall screen did not get all the rows the panel ever draws");
      queue.Slots = 0;
      Assert.That(queue.VisibleRows, Has.Count.EqualTo(1), "a screen with no room at all lost the tune it is on");
    }

    [Test]
    public void TheScreenStaysWhileTheRecordHasTunesLeft()
    {
      // The application reads this one answer before it closes the playback screen over the shelf it was opened
      // from, so it is asked of the screen and not only of the queue.
      FakePlayer player = new FakePlayer();
      NowPlayingViewModel screen = new NowPlayingViewModel(player);
      try
      {
        List<TitleViewModel> shelf = Shelf(2);
        screen.Play(Playable(shelf[0]), shelf[0].Title.Id, 0, shelf[0].Name);
        screen.PlayingAspect = "AudioItem";
        screen.LoadAudioQueue(shelf, 0, "AudioItem");
        Assert.That(screen.AudioQueuePlaysOn, Is.True,
            "the screen was told to step aside while the second tune was still waiting");

        screen.Stop();
        Assert.That(screen.AudioQueuePlaysOn, Is.False, "a stopped record went on promising its screen");
      }
      finally
      {
        screen.Dispose();
      }
    }

    [Test]
    public async Task TheStageIsAskedForItsPictureAgainForTheNextTune()
    {
      // Opening a tune wipes every picture the library had said - the next tune may come from another record -
      // so the screen's every-change question has to include the stage's picture. Asked once when the screen
      // opened and never again, the second tune of a record played on a bare shade.
      FakePlayer player = new FakePlayer();
      NowPlayingViewModel screen = new NowPlayingViewModel(player);
      try
      {
        List<TitleViewModel> shelf = Shelf(2);
        List<Guid> asked = new List<Guid>();
        screen.FanartReader = delegate (Guid id)
        {
          asked.Add(id);
          return Task.FromResult<byte[]?>(new byte[] { 1, 2, 3 });
        };

        screen.Play(Playable(shelf[0]), shelf[0].Title.Id, 0, shelf[0].Name);
        screen.PlayingAspect = "AudioItem";
        await screen.ReadFanartAsync();
        Assert.That(screen.Fanart, Is.Not.Null, "the stage of a song was left without its picture");

        screen.Play(Playable(shelf[1]), shelf[1].Title.Id, 0, shelf[1].Name);
        screen.PlayingAspect = "AudioItem";
        Assert.That(screen.Fanart, Is.Null, "the tune before was still being painted behind this one");
        await screen.ReadFanartAsync();

        Assert.That(asked, Is.EqualTo(new[] { shelf[0].Title.Id, shelf[1].Title.Id }),
            "the stage kept the first tune's picture or was never asked for the second");
        await screen.ReadFanartAsync();
        Assert.That(asked.Count, Is.EqualTo(2), "the same tune was asked for its picture twice over");
      }
      finally
      {
        screen.Dispose();
      }
    }

    // ---- helpers --------------------------------------------------------------------------------------

    private static MediaLocation Playable(TitleViewModel row)
    {
      MediaLocation? location = row.PlaybackLocation;
      Assert.That(location, Is.Not.Null, "a test row named no file");
      return location.Value;
    }

    private static MediaItemDetails Length(Guid id, int? seconds)
    {
      return new MediaItemDetails(id, "Tune", null, 2024, null, null, null, null, null, null, null,
          null, null, null, null, null, null, null, null, null, null, false, null,
          DurationSeconds: seconds);
    }
  }
}
