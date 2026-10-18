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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The home screen: which server this is, and what its media database is made of.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The view model is the half of the front end that can be tested without a screen, which is why the reading
  /// happens here and nothing about Avalonia appears in this file. It takes a <see cref="ServerApiClient"/> and
  /// nothing else, so a test can hand it a client pointed at a server that is really running.
  /// </para>
  /// <para>
  /// The awaits deliberately keep their context instead of calling <c>ConfigureAwait(false)</c>: the code after
  /// them touches <see cref="Aspects"/>, which the list on screen is bound to, and that has to happen on the
  /// thread that owns the visual tree.
  /// </para>
  /// </remarks>
  public partial class HomeViewModel : ObservableObject
  {
    private ServerApiClient? _client;
    private CancellationTokenSource? _runningRefresh;

    // One run at a time may touch Aspects. Cancelling the older run is not enough for that: a cancelled run
    // carries on until its next check, and until then it is clearing and adding to the same collection the newer
    // run is writing. This is a chain of tasks rather than a SemaphoreSlim, because a Task needs no disposal -
    // a semaphore owned by a view model that gets thrown away is a fresh way to hand ObjectDisposedException to
    // a run that is still waiting on it, which is the very failure mode this class already documents.
    private readonly object _gateLock = new object();
    private Task _gate = Task.CompletedTask;

    [ObservableProperty]
    private string _status;

    [ObservableProperty]
    private string? _serverDescription;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private AspectViewModel? _selectedAspect;

    [ObservableProperty]
    private string _signInName = string.Empty;

    [ObservableProperty]
    private string? _signedInAs;

    /// <summary>
    /// For the XAML designer and for the tests that only check what the screen looks like before anything was
    /// read. A view model built this way says so in its status line instead of pretending to be connected.
    /// </summary>
    public HomeViewModel()
        : this(null)
    {
    }

    public HomeViewModel(ServerApiClient? client)
    {
      _client = client;
      _status = client == null ? "No server configured" : "Not connected yet";
    }

    public ObservableCollection<AspectViewModel> Aspects { get; } = new ObservableCollection<AspectViewModel>();

    /// <summary>
    /// The parts of this server a viewer can sit down and watch, in the order the tiles are arranged.
    /// </summary>
    /// <remarks>
    /// Filled at the end of a refresh, after the counts arrived, because a tile that said "?" until somebody
    /// looked at the aspect list below it would be a tile that lies about the library half the time.
    /// </remarks>
    public ObservableCollection<HomeSection> Sections { get; } = new ObservableCollection<HomeSection>();

    /// <summary>
    /// Titles somebody started and did not finish, most recently stopped first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the one thing a home screen can say that no tile can: not "here is a shelf of three thousand
    /// films" but "you were in this one". It is the same row type the library list uses, so a title reads the same
    /// in both places and the play path underneath is the one the library already proved.
    /// </para>
    /// <para>
    /// Read with the <c>started</c> filter, which is the server's answer to "was there a last-played date and no
    /// finished viewing behind it" - so it holds films that were stopped half way and none that were watched
    /// through, the distinction the shelf marks with a tick (section <i>ac</i>). What it does not hold is a
    /// position to jump back to: MP2 stores that per profile, and this front end has no profile yet, so pressing
    /// a row starts the film again. That is why the heading says "left half way" and not "continue watching".
    /// </para>
    /// </remarks>
    public ObservableCollection<TitleViewModel> Unfinished { get; } = new ObservableCollection<TitleViewModel>();

    /// <summary>
    /// Whether the strip is on screen: rows to offer, or a library that refused to answer about them.
    /// </summary>
    /// <remarks>
    /// A refusal keeps the heading on screen with its reason even though there is nothing to press. Hiding the
    /// strip completely would make "the server wouldn't tell me" look like "you have nothing to watch", and those
    /// two look identical to nobody but the viewer.
    /// </remarks>
    public bool HasUnfinished
    {
      get { return Unfinished.Count > 0 || !string.IsNullOrEmpty(_unfinishedStatus); }
    }

    /// <summary>
    /// What the strip says above its rows: how many titles are in it, or why it could not be filled.
    /// </summary>
    public string UnfinishedStatus
    {
      get { return _unfinishedStatus ?? ""; }
    }

    private string? _unfinishedStatus;

    /// <summary>
    /// Said when the viewer presses OK on a row of the strip.
    /// </summary>
    /// <remarks>
    /// The same shape as the library's <c>PlayRequested</c>, so the application opens the film through the one
    /// path it already has; the home screen does not know what a player is.
    /// </remarks>
    public event EventHandler<TitleViewModel>? ResumeRequested;

    /// <summary>
    /// The viewer pressed OK on one row of the strip.
    /// </summary>
    [RelayCommand]
    private void Resume(TitleViewModel? title)
    {
      if (title == null)
        return;
      ResumeRequested?.Invoke(this, title);
    }

    /// <summary>
    /// The client the screen reads through, or <c>null</c> before a server was chosen.
    /// </summary>
    /// <remarks>
    /// Screens that open from this one read from the same server, and "the same server" has to mean the client
    /// that is on screen rather than one built again from a setting that may have changed since.
    /// </remarks>
    public ServerApiClient? Client
    {
      get { return _client; }
    }

    /// <summary>
    /// Where the client is looking, shown on the screen so that "it says no media" can be answered without
    /// guessing which server was being talked to.
    /// </summary>
    public string ServerUrl
    {
      get { return _client == null ? "(none)" : _client.BaseUrl.ToString(); }
    }

    /// <summary>
    /// Points the screen at a different server. The library shown is thrown away with the old address - keeping the
    /// aspects of one server under the name of another is how a viewer ends up clicking a film that is not there.
    /// </summary>
    /// <remarks>
    /// The client being replaced is cancelled but deliberately <em>not</em> disposed: a refresh from it may be
    /// between awaits, and taking the <c>HttpClient</c> away from under it is exactly the
    /// <c>ObjectDisposedException</c> this class already documents twice. Its handler is released when the client is
    /// collected, and a viewer changes the server address a handful of times per installation, so the one client that
    /// is not disposed is not a leak that ever grows.
    /// </remarks>
    public void UseServer(ServerApiClient client)
    {
      CancellationTokenSource? running = _runningRefresh;
      running?.Cancel();

      _client = client;
      SignedInAs = null;
      Aspects.Clear();
      Sections.Clear();
      Unfinished.Clear();
      ReportUnfinished(0);
      SelectedAspect = null;
      ServerDescription = null;
      IsBusy = false;
      Status = "Server address set to " + client.BaseUrl;
      OnPropertyChanged(nameof(ServerUrl));
    }

    /// <summary>
    /// What was typed into the password box, for as long as it takes to hand it over.
    /// </summary>
    /// <remarks>
    /// An ordinary property and not an observable one: nothing on screen displays it, and a screen that could
    /// display it would be a screen that shows the password. It is cleared as soon as the server has answered,
    /// so the front end does not go on holding a household's password while somebody is choosing a film.
    /// </remarks>
    public string SignInPassword
    {
      get;
      set;
    } = string.Empty;

    /// <summary>
    /// Tells the server who this is, so that the parts of the library it keeps for known callers open.
    /// </summary>
    /// <remarks>
    /// MP2's server asks for this by default: <c>ServerSettings.WebAutorizationEnabled</c> is on, and the
    /// endpoints that name titles answer a stranger with 403. The switch for a household that does not want it is
    /// on the server, not here; what the front end can do is say which of the two situations it is standing in.
    /// </remarks>
    [RelayCommand]
    public async Task SignInAsync()
    {
      if (_client == null)
      {
        Status = "No server configured";
        return;
      }

      string name = SignInName.Trim();
      if (name.Length == 0)
      {
        Status = "Say who you are first";
        return;
      }

      string password = SignInPassword;
      SignInPassword = string.Empty;
      OnPropertyChanged(nameof(SignInPassword));

      try
      {
        Status = "Asking " + _client.BaseUrl + " who " + name + " is";
        string signedIn = await _client.SignInAsync(name, password, CancellationToken.None);
        SignedInAs = signedIn;
        Status = "Signed in as " + signedIn;

        // The library was refused to a stranger a moment ago, so the screen has to ask again now that it is not.
        await RefreshAsync();
      }
      catch (ServerApiException ex)
      {
        SignedInAs = null;
        Status = DescribeSignInFailure(ex);
      }
      catch (OperationCanceledException)
      {
        Status = "Sign-in cancelled";
      }
      catch (Exception ex)
      {
        // Same reason as in RefreshAsync: this runs on a continuation from a button, and what escapes here would
        // end the process rather than the attempt.
        SignedInAs = null;
        Status = "Sign-in failed: " + ex.Message;
      }
    }

    /// <summary>
    /// What a refused sign-in means to the person standing in front of the screen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MP2's token endpoint has two failure answers, and one of them is a lie by omission: a wrong password gets
    /// "The user name or password is incorrect.", while a name that does not exist - and a server on which
    /// nobody has ever set profiles up - both fall through to "User management not available."
    /// (<c>ResourceServer.GrantResourceOwnerCredentials</c>). A viewer told the latter cannot tell a typo from a
    /// server that has no accounts at all, and the difference is the difference between typing again and going
    /// to the server to configure it.
    /// </para>
    /// <para>
    /// So that one answer is translated into what can actually be done about it. Turning off "Web authorization"
    /// is offered as the equal alternative rather than as a workaround, because on a home network it is a
    /// supported configuration, not a mistake - it is the setting the server itself suggests when it cannot
    /// identify anybody.
    /// </para>
    /// </remarks>
    public static string DescribeSignInFailure(ServerApiException failure)
    {
      if (failure.Message.IndexOf("User management not available", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        return "The server could not find that profile. MP2 says the same thing when a profile was never set up, "
            + "so either add one in the server's configuration, or switch \"Web authorization\" off there and the "
            + "library is open to the whole house.";
      }

      return failure.StatusCode == 400 || failure.StatusCode == 401
          ? "The server does not accept that name and password: " + failure.Message
          : failure.Message;
    }

    /// <summary>
    /// Reads the server again: what it is, which aspects it has, and then how many items each aspect holds.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
      if (_client == null)
      {
        Status = "No server configured";
        return;
      }

      // Pressing the button twice should not leave two runs writing to the same list, so the older run is
      // cancelled. It is deliberately *not* disposed here: every run disposes its own source in its finally
      // below, and disposing a source that another run is still between awaits on makes the next
      // `cancellation.Token` in that run throw ObjectDisposedException on the dispatcher. Measured: starting a
      // second refresh while the first was in flight killed the process with exactly that. The assignment also
      // happens before the cancel for the same family of reason - the older run's finally must no longer see its
      // own source as the current one, or it would clear IsBusy while the new run is still going.
      CancellationTokenSource? previous = _runningRefresh;
      CancellationTokenSource cancellation = new CancellationTokenSource();
      _runningRefresh = cancellation;
      previous?.Cancel();

      IsBusy = true;

      // Cancelling the older run is not enough to keep two runs out of the same list, so each run waits for the
      // one before it. No `ConfigureAwait(false)` anywhere in here - the body raises collection-changed events
      // that a bound screen listens to, so it has to resume the way the rest of the method does.
      Task earlierRun;
      TaskCompletionSource runFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      lock (_gateLock)
      {
        earlierRun = _gate;
        _gate = runFinished.Task;
      }

      try
      {
        await earlierRun;

        // Replaced by a newer press before it ever got the list. Checked here rather than relying on a token
        // throwing, because a run that was cancelled while it queued behind the gate never reached an await
        // that could throw.
        if (cancellation.IsCancellationRequested)
          return;

        Status = "Reading " + _client.BaseUrl;

        // Gone before the new numbers arrive. Rows of the previous server sitting under the name of the current
        // one would be the one thing on this screen that lies on its own.
        Unfinished.Clear();
        ReportUnfinished(0);

        ServerInfo info = await _client.GetServerInfoAsync(cancellation.Token);
        ServerDescription = info.Describe();

        LibraryAspects aspects = await _client.GetLibraryAspectsAsync(cancellation.Token);
        Aspects.Clear();
        foreach (MediaAspect aspect in aspects.Aspects)
          Aspects.Add(new AspectViewModel(aspect));
        if (SelectedAspect == null && Aspects.Count > 0)
          SelectedAspect = Aspects[0];

        Status = string.Format("{0} aspects on {1}", aspects.Count, info.Name);

        // Each count is its own request, and one that fails leaves that row at "?" rather than failing the
        // screen: an aspect the server refuses to count is information, not a reason to show nothing.
        foreach (AspectViewModel aspect in Aspects)
        {
          try
          {
            MediaCount count = await _client.GetMediaCountAsync(aspect.Name, cancellation.Token);
            aspect.Count = count.Count;
          }
          catch (ServerApiException)
          {
            aspect.Count = null;
          }
        }

        // The tiles come last: they are the first thing a viewer looks at, and a tile that already knows how
        // many titles it holds answers the question the viewer was about to ask.
        Sections.Clear();
        foreach (HomeSection section in HomeSection.For(Aspects))
          Sections.Add(section);

        // Last, because it is the only reading on this screen that is a convenience rather than the point of it:
        // a server that answers the tiles but not this has cost the viewer nothing they came for.
        await ReadUnfinishedAsync(cancellation.Token);
      }
      catch (ServerApiException ex)
      {
        Status = ex.NotReady
            ? "The server is not ready yet: " + ex.Message
            : ex.Message;
      }
      catch (OperationCanceledException)
      {
        // Replaced by a newer press of the same button, which reports its own status.
      }
      catch (Exception ex)
      {
        // The last resort is here for one specific reason: the command hands this method to an `async void`
        // continuation that rethrows whatever escapes it on the thread that pressed the button. Anything nobody
        // thought of therefore does not end the read, it ends the front end - measured once already, with the
        // cancellation source above. Reporting it costs one status line and keeps the screen alive.
        Status = "Reading failed: " + ex.Message;
      }
      finally
      {
        // Lets the next press in. The chain never faults - every exception is reported in Status above - so a
        // waiting run is never handed someone else's failure through `await earlierRun`.
        runFinished.SetResult();
        if (ReferenceEquals(_runningRefresh, cancellation))
        {
          _runningRefresh = null;
          IsBusy = false;
        }
        cancellation.Dispose();
      }
    }

    /// <summary>
    /// Rows the strip offers at most. Eight is what fits in a line and a half of a television screen; a household
    /// that stopped more than eight films at once is not choosing from this list anyway.
    /// </summary>
    private const int UNFINISHED_ROWS = 8;

    /// <summary>
    /// Asks every part of the library on screen what it holds that somebody left half way, and puts the newest
    /// stopped titles on the strip.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Asked per section rather than once for the whole library, because the endpoint needs an aspect and a
    /// viewer who stopped an episode and a film wants both back. Each part is asked for the same handful of rows
    /// the strip can hold and the newest first, so taking the newest of the merged pages afterwards cannot lose a
    /// title: the newest nine stopped titles overall are among the newest eight of each part.
    /// </para>
    /// <para>
    /// One part refusing is reported and does not hide what the others answered, in the same spirit as the counts
    /// above - a library that will not say is information, not a reason to show nothing.
    /// </para>
    /// </remarks>
    private async Task ReadUnfinishedAsync(CancellationToken cancellation)
    {
      if (_client == null)
        return;

      List<MediaTitle> found = new List<MediaTitle>();
      int silent = 0;

      foreach (HomeSection section in Sections)
      {
        if (cancellation.IsCancellationRequested)
          return;

        try
        {
          MediaTitles titles = await _client.GetTitlesAsync(section.Aspect.Name, "lastplayed", true,
              UNFINISHED_ROWS, 0, null, "started", cancellation);
          if (titles.Titles != null)
            found.AddRange(titles.Titles);
        }
        catch (ServerApiException)
        {
          silent++;
        }
      }

      foreach (MediaTitle title in found
          .OrderByDescending(t => t.LastPlayed)
          .ThenBy(t => t.SortTitle ?? t.Title, StringComparer.OrdinalIgnoreCase)
          .Take(UNFINISHED_ROWS))
        Unfinished.Add(new TitleViewModel(title));

      ReportUnfinished(silent);
    }

    /// <summary>
    /// The line above the strip: how many rows it holds, and how many parts of the library did not answer.
    /// </summary>
    /// <remarks>
    /// An empty strip with nothing wrong leaves the line empty, which takes the strip off screen; an empty strip
    /// behind a refusal keeps its reason, because "nothing to watch" and "the server wouldn't say" have to look
    /// different.
    /// </remarks>
    private void ReportUnfinished(int silent)
    {
      string line;
      if (Unfinished.Count == 0 && silent == 0)
        line = "";
      else
      {
        line = Unfinished.Count == 0
            ? "Could not check what you left half way"
            : Unfinished.Count == 1 ? "1 title left half way" : Unfinished.Count + " titles left half way";
        if (silent > 0)
          line += silent == 1 ? " - 1 part of the library did not answer" : " - " + silent + " parts of the library did not answer";
      }

      _unfinishedStatus = string.IsNullOrEmpty(line) ? null : line;
      OnPropertyChanged(nameof(UnfinishedStatus));
      OnPropertyChanged(nameof(HasUnfinished));
    }
  }
}
