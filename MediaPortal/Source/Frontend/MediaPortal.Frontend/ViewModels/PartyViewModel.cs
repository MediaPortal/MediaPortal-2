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
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The party: the music shelf shuffled, played from the front, and reshuffled when it runs out.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the old party plugin's promise - press one thing and music keeps arriving - kept with the pieces
  /// this front end actually has: the server's own <c>AudioItem</c> shelf for what to play, and the shared
  /// player for playing it. The plugin kept playlists of folders of its own; a media centre whose library
  /// already knows its music has nothing to gain from a second, hand-kept list of files next to it, so here
  /// the shelf is the playlist.
  /// </para>
  /// <para>
  /// The screen chooses; it never plays. Like the title list it raises <see cref="PlayRequested"/> and the
  /// application - the one thing that owns the player - opens the file, which keeps a party track and a track
  /// started from the library the same playback with the same reports to the library. The application also
  /// says when the chosen song ended (<see cref="TrackFinished"/>) or would not open
  /// (<see cref="TrackRefused"/>): a party that cannot hear its own music is a slideshow, so both ears are
  /// part of the screen's contract with the application.
  /// </para>
  /// <para>
  /// The queue survives closing the window on purpose, which is why the application keeps one view model
  /// rather than building one per visit: the music carries on while the viewer is out of the room, and the
  /// screen they come back to should show the songs that are left, not a fresh shuffle that has forgotten
  /// what is playing.
  /// </para>
  /// </remarks>
  public partial class PartyViewModel : ObservableObject, IDisposable
  {
    /// <summary>How many pages of the shelf one party reads; enough for an evening and no more.</summary>
    private const int READ_PAGES = 5;

    /// <summary>Titles one of those pages brings.</summary>
    private const int PAGE = 60;

    /// <summary>
    /// Refusals in a row after which the party stops by itself. A song that will not open is an evening saved
    /// by walking on; several in a row say the shelf or the player is the problem, and that is a fact a
    /// person should hear once rather than a machine failing forever in a shuffle.
    /// </summary>
    private const int REFUSALS_TO_STOP = 3;

    private readonly ServerApiClient? _client;
    private readonly Random _random;
    private CancellationTokenSource? _running;

    /// <summary>Every playable song the shelf answered, in read order - what a new round shuffles from.</summary>
    private readonly List<TitleViewModel> _pool = new List<TitleViewModel>();

    /// <summary>The song the screen last asked the application to play, or null between songs.</summary>
    private TitleViewModel? _playing;

    [ObservableProperty]
    private TitleViewModel? _selected;

    [ObservableProperty]
    private bool _isOn;

    [ObservableProperty]
    private string _status = "Not started.";

    [ObservableProperty]
    private string _currentLine = "";

    /// <summary>Refusals in a row; a song that plays resets it.</summary>
    private int _refusals;

    /// <summary>Set by <see cref="Head"/> when it had to start a new round; the status line reads it once.</summary>
    private bool _justRefilled;

    /// <summary>
    /// What the last shelf read had to complain about beyond the songs it queued - the titles that cannot be
    /// played from here. Carried to the line start prints, because "6 songs shuffled" from a shelf of seven
    /// is only honest together with the missing one.
    /// </summary>
    private string? _shelfNote;

    /// <param name="client">
    /// Where the music shelf lives, or null for a screen that says it has no server - the same bargain the
    /// other front-end screens make with a viewer who started the application without one.
    /// </param>
    /// <param name="shuffleSeed">
    /// A seed for the shuffle, named so that a test can watch a known order unfold; null - what the
    /// application passes - means a different party every time, which is the entire point of the screen.
    /// </param>
    public PartyViewModel(ServerApiClient? client = null, int? shuffleSeed = null)
    {
      _client = client;
      _random = shuffleSeed.HasValue ? new Random(shuffleSeed.Value) : new Random();
      if (client == null)
        Status = "No server to ask for music";
    }

    /// <summary>The songs still to come, the one playing or next first.</summary>
    public ObservableCollection<TitleViewModel> Upcoming { get; } = new ObservableCollection<TitleViewModel>();

    /// <summary>
    /// Raised for the application to play - the same title shape the title list raises, so the same
    /// application code opens both and the library hears about both.
    /// </summary>
    public event EventHandler<TitleViewModel>? PlayRequested;

    private bool CanStart
    {
      get { return _client != null && !IsOn; }
    }

    private bool CanStop
    {
      get { return IsOn; }
    }

    private bool CanSkip
    {
      get { return Upcoming.Count > 0; }
    }

    private bool CanPlaySelected
    {
      get { return Selected != null && Upcoming.Contains(Selected); }
    }

    /// <summary>
    /// Starts the party: reads the shelf if nothing is queued yet, and asks for the first song.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
      if (_client == null)
        return;

      if (Upcoming.Count == 0)
        await ReadShelfAsync();

      if (Upcoming.Count == 0)
        return; // ReadShelfAsync said why there is nothing.

      _refusals = 0;
      IsOn = true;
      AskFor(Upcoming[0]);
      Status = "The party is on - " + _playing!.Name + ", " + (Upcoming.Count - 1) + " more to go"
          + (_shelfNote != null ? " - " + _shelfNote : "");
    }

    /// <summary>
    /// Stops the party: the queue stays, and so does whatever song is playing - the viewer who stops the
    /// party said "no more songs after this", not "silence now".
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
      IsOn = false;
      Status = _playing == null
          ? "The party is stopped - " + Upcoming.Count + " songs stay queued"
          : "The party is stopped - the queue stays, and the song that is playing is left alone";
    }

    /// <summary>
    /// Now the next one. The song being skipped leaves the queue and the application opens the new head over
    /// it - the same replacement the library screen makes when a viewer picks a different film midway,
    /// including the library's report of how far the skipped one got.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSkip))]
    private void Skip()
    {
      if (Upcoming.Count == 0)
        return;

      TitleViewModel skipped = Upcoming[0];
      if (ReferenceEquals(_playing, skipped))
      {
        Upcoming.RemoveAt(0);
        _playing = null;
      }

      TitleViewModel? next = Head(skipped);
      if (next == null)
        return;

      AskFor(next);
      Status = skipped.Name + " skipped - next up " + next.Name;
    }

    /// <summary>
    /// Puts a picked song at the front of the queue and plays it now; the songs behind it keep their order,
    /// and the song it replaces leaves the queue like the skip it is.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPlaySelected))]
    private void PlaySelected()
    {
      TitleViewModel? picked = Selected;
      if (picked == null || !Upcoming.Contains(picked))
        return;

      DropPlaying();
      Upcoming.Remove(picked);
      Upcoming.Insert(0, picked);
      _refusals = 0;
      AskFor(picked);
      Status = picked.Name + ", on your ask";
    }

    /// <summary>
    /// The application's ear for finished music: the song with this id reached its end. Only the song the
    /// screen is waiting on answers to this - a report about one the queue let go is history, and advancing
    /// twice from one ending is the bug this check exists to keep out.
    /// </summary>
    public void TrackFinished(Guid mediaItemId)
    {
      if (_playing == null || _playing.Title.Id != mediaItemId)
        return;

      TitleViewModel done = _playing;
      Upcoming.Remove(done);
      _playing = null;
      _refusals = 0;

      if (!IsOn)
      {
        // Stop said no more songs after the ones already asked for. A song ending now leaves the queue but
        // starts nothing; the viewer who wants more presses start or presses a row.
        NotifyQueue();
        Status = Upcoming.Count > 0
            ? done.Name + " played - the party is stopped, so nothing is asked for next"
            : done.Name + " played, and the queue is through";
        return;
      }

      TitleViewModel? next = Head(done);
      if (next == null)
      {
        Stop(); // Head() said nothing could be asked for; the queue keeps the words.
        return;
      }

      bool refilled = _justRefilled;
      AskFor(next);
      Status = refilled
          ? "The whole shelf has been played once - starting again, shuffled, with " + next.Name
          : done.Name + " played - next up " + next.Name + ", " + (Upcoming.Count - 1) + " to go";
    }

    /// <summary>
    /// The application's ear for music that will not open. The song leaves the queue, the reason is said, and
    /// the party walks on - until <see cref="REFUSALS_TO_STOP"/> in a row say the shelf itself is the problem.
    /// </summary>
    public void TrackRefused(Guid mediaItemId, string reason)
    {
      if (_playing == null || _playing.Title.Id != mediaItemId)
        return;

      TitleViewModel dead = _playing;
      Upcoming.Remove(dead);
      _playing = null;
      _refusals++;

      if (_refusals >= REFUSALS_TO_STOP)
      {
        IsOn = false;
        Status = dead.Name + " refused: " + reason + " - " + REFUSALS_TO_STOP
            + " songs in a row would not open, so the party stops here";
        return;
      }

      TitleViewModel? next = Head(dead);
      if (next == null)
      {
        IsOn = false;
        Status = dead.Name + " refused: " + reason + " - and there is no other song to ask for";
        return;
      }

      AskFor(next);
      Status = dead.Name + " refused: " + reason + " - the party walks on";
    }

    public void Dispose()
    {
      CancellationTokenSource? running = _running;
      _running = null;
      running?.Cancel();
      running?.Dispose();
    }

    partial void OnIsOnChanged(bool value)
    {
      StartCommand.NotifyCanExecuteChanged();
      StopCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedChanged(TitleViewModel? value)
    {
      PlaySelectedCommand.NotifyCanExecuteChanged();
    }

    private void NotifyQueue()
    {
      SkipCommand.NotifyCanExecuteChanged();
      PlaySelectedCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Takes the song to ask for when the head of the queue left it: the new head or, when the shelf ran out
    /// mid-party, a fresh round of it - a party that ends because the shuffle finished is a party that
    /// misread its own job. The song that just left (<paramref name="last"/>) moves to the back of a new
    /// round, so two rounds in a row do not open on the same song.
    /// </summary>
    private TitleViewModel? Head(TitleViewModel? last = null)
    {
      _justRefilled = false;
      if (Upcoming.Count == 0 && _pool.Count > 0 && IsOn)
      {
        List<TitleViewModel> round = new List<TitleViewModel>(_pool);
        Shuffle(round);
        if (_pool.Count > 1 && last != null)
        {
          round.Remove(last);
          round.Add(last);
        }

        foreach (TitleViewModel row in round)
          Upcoming.Add(row);
        _justRefilled = true;
      }

      NotifyQueue();
      return Upcoming.Count > 0 ? Upcoming[0] : null;
    }

    /// <summary>
    /// Lets go of the song being played without queueing anything: the replacement for a jump uses this
    /// before putting its own song at the front.
    /// </summary>
    private void DropPlaying()
    {
      if (_playing == null)
        return;

      Upcoming.Remove(_playing);
      _playing = null;
    }

    /// <summary>
    /// One song asked for: the line on the panel and the event the application plays.
    /// </summary>
    private void AskFor(TitleViewModel row)
    {
      _playing = row;
      CurrentLine = row.Name;
      NotifyQueue();
      PlayRequested?.Invoke(this, row);
    }

    /// <summary>
    /// Reads the music shelf, a page at a time, and puts every song that can be played from here into the
    /// pool. Every way this can end badly has its own words: the shelf not existing at all is a different
    /// evening from the shelf being empty, and both differ from a shelf whose files neither this machine can
    /// open nor the server can stream.
    /// </summary>
    private async Task ReadShelfAsync()
    {
      CancellationTokenSource? previous = _running;
      if (previous != null)
      {
        previous.Cancel();
        previous.Dispose();
      }

      CancellationTokenSource cancellation = new CancellationTokenSource();
      _running = cancellation;

      List<TitleViewModel> read = new List<TitleViewModel>();
      string? complaint = null;
      int namedCount = 0;
      int pagesRead = 0;
      int total = 0;

      try
      {
        for (int page = 0; page < READ_PAGES; page++)
        {
          MediaTitles answer = await _client!.GetTitlesAsync("AudioItem", null, false,
              PAGE, page * PAGE,
              null, null, null, null, null, null, cancellation.Token);

          if (answer.Error != null)
          {
            complaint = "the music shelf answered: " + answer.Error;
            break;
          }

          pagesRead++;
          total = answer.Total;
          foreach (MediaTitle title in answer.Titles ?? Array.Empty<MediaTitle>())
          {
            namedCount++;
            TitleViewModel row = new TitleViewModel(title, false, _client);
            if (row.Playable)
              read.Add(row);
          }

          if (answer.Remaining <= 0)
            break;
        }
      }
      catch (OperationCanceledException)
      {
        return;
      }
      catch (ServerApiException ex)
      {
        complaint = "the music shelf refused: " + ex.Message;
      }
      catch (Exception ex)
      {
        complaint = "the music shelf did not answer: " + ex.Message;
      }

      if (complaint != null)
      {
        Status = complaint;
        return;
      }

      _pool.Clear();
      _pool.AddRange(read);
      Shuffle(_pool);
      foreach (TitleViewModel row in _pool)
        Upcoming.Add(row);
      NotifyQueue();

      if (_pool.Count == 0)
      {
        Status = namedCount > 0
            ? "the shelf holds " + total + " titles and none of them can be played from here"
            : "the music shelf is empty";
        return;
      }

      int unreadable = namedCount - read.Count;
      _shelfNote = unreadable > 0
          ? unreadable + (unreadable == 1 ? " title the shelf named is" : " titles the shelf named are")
              + " not playable from here"
          : null;
      Status = _pool.Count + " songs shuffled"
          + (unreadable > 0 ? " - " + _shelfNote : "")
          + (total > namedCount
              ? ", read from the first " + pagesRead + (pagesRead == 1 ? " page" : " pages") + " of " + total
                  + " titles"
              : "");
    }

    private void Shuffle(List<TitleViewModel> rows)
    {
      for (int at = rows.Count - 1; at > 0; at--)
      {
        int swap = _random.Next(at + 1);
        (rows[at], rows[swap]) = (rows[swap], rows[at]);
      }
    }
  }
}
