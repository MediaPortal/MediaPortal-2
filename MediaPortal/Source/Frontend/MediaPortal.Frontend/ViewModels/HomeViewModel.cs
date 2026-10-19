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

    /// <summary>
    /// The menu entry the viewer is standing on, which is the entry whose lists the content pane next to the menu
    /// shows. Set by the screen's selection, and by <see cref="RebuildMenu"/> when the menu was rebuilt under it.
    /// </summary>
    [ObservableProperty]
    private MainMenuEntry? _selectedEntry;

    [ObservableProperty]
    private string _signInName = string.Empty;

    [ObservableProperty]
    private string? _signedInAs;

    /// <summary>
    /// Whether this server has refused the front end for not carrying a token. The reference's living rooms
    /// asked nobody who they were - login is a switch a household turns on for its server, not a turnstile in
    /// front of the library - so the sign-in boxes wait for the server to actually ask for them.
    /// </summary>
    [ObservableProperty]
    private bool _serverRefusesStrangers;

    /// <summary>
    /// Whether the sign-in boxes have a place on the screen: only while the server asks for a token and nobody
    /// has handed one over yet.
    /// </summary>
    public bool SignInOffered => ServerRefusesStrangers && SignedInAs == null;

    partial void OnSignedInAsChanged(string? value)
    {
      OnPropertyChanged(nameof(SignInOffered));
    }

    partial void OnServerRefusesStrangersChanged(bool value)
    {
      OnPropertyChanged(nameof(SignInOffered));
    }

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

      // The menu before anything has been read. A viewer who turns a television on before the server has finished
      // starting up is looking at this state, and it has to say what a media centre is - and what is missing from
      // it - rather than offering nothing to press.
      RebuildMenu(false);
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
    /// The main menu: the parts a media centre is made of, each one either openable or saying what is missing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what the home screen shows, and it is on screen before any server has answered, which is the whole
    /// point of it. The tiles used to be the server's own aspect list filtered down to the watchable ones, so a
    /// front end that had not reached a server - a server still booting, a machine whose address changed, a
    /// television that was switched on first - offered nothing to press and looked broken rather than idle.
    /// </para>
    /// <para>
    /// The names and the order are MP2's (see <see cref="MainMenu"/>), and the counts come from the same read that
    /// fills <see cref="Sections"/>, so a tile saying "4 films" is still the server's own number rather than one
    /// this screen kept around.
    /// </para>
    /// </remarks>
    public ObservableCollection<MainMenuEntry> Menu { get; } = new ObservableCollection<MainMenuEntry>();

    /// <summary>
    /// Pressing a menu entry: the part of the library to open, or null when this entry cannot be opened - in which
    /// case the status line says why.
    /// </summary>
    /// <remarks>
    /// Returning the section rather than raising an event keeps the decision and its reason in one place: the entry
    /// knows what it is missing, and this is the screen that knows what the viewer pressed. The window opens what
    /// comes back and does not have to work out whether a null meant "no" or "not yet".
    /// </remarks>
    public HomeSection? Open(MainMenuEntry? entry)
    {
      if (entry == null)
      {
        Status = "Nothing selected";
        return null;
      }

      HomeSection? section = entry.Section;
      if (section == null)
      {
        // A tile that opens a screen instead of a shelf has nothing for the status line to explain - the window
        // opens the screen, and a screen with no folders on it says more than this line could.
        if (entry.ScreenKey == null)
          Status = entry.Label + " - " + entry.Detail;
        return null;
      }

      return section;
    }

    /// <summary>
    /// Rebuilds the menu from what the server holds. Called wherever <see cref="Sections"/> is refilled, so that a
    /// tile's number and a tile's aspect can never come from two different readings.
    /// </summary>
    private void RebuildMenu(bool connected)
    {
      // Which entry the viewer was standing on, remembered across the rebuild. A refresh that left the selection at
      // the top of the menu would also refill the content pane with the wrong entry's lists, which is a long way
      // from what the viewer was looking at a second ago.
      string? wasSelected = SelectedEntry?.Mp2Action;

      Menu.Clear();
      foreach (MainMenuEntry entry in MainMenu.Build(Aspects, connected))
        Menu.Add(entry);

      if (wasSelected != null)
      {
        foreach (MainMenuEntry entry in Menu)
        {
          if (entry.Mp2Action == wasSelected)
          {
            // Going through the property, so the pane is refilled from the fresh reading; a menu rebuilt behind a
            // viewer who is standing on "Movies" has to show the counts that arrived in this reading.
            SelectedEntry = entry;
            return;
          }
        }
      }

      SelectedEntry = null;
    }

    /// <summary>
    /// The content pane: the lists of the menu entry the viewer is standing on, one after another, with their
    /// headings.
    /// </summary>
    /// <remarks>
    /// This is the half of MP2's home screen that the tiles never carried. <see cref="Menu"/> answers "which part of
    /// the library?", and the pane answers "what is in it, and what would you watch tonight?" - the same split the
    /// old client made between <c>MainMenu</c> and the <c>HomeContent</c> lists of the highlighted entry. It is one
    /// flat list of <see cref="HomeRow"/>s rather than four lists, because a remote can only stop on something it
    /// can walk into, and a heading nobody can stop on is decoration.
    /// </remarks>
    public ObservableCollection<HomeRow> ContentRows { get; } = new ObservableCollection<HomeRow>();

    /// <summary>Whether the pane has anything to say, which is what keeps its heading off an empty screen.</summary>
    public bool HasContent
    {
      get { return ContentRows.Count > 0; }
    }

    /// <summary>
    /// Which reading of the pane is the current one. Only the newest is allowed to keep writing, because walking
    /// the menu with a remote fires a load per stop, and an older answer arriving late would append its titles under
    /// a newer entry's heading.
    /// </summary>
    private int _contentGeneration;

    /// <summary>
    /// Refills the content pane for <paramref name="entry"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The headings go up before anything has been read, and the titles are appended under them as each answer
    /// arrives. A pane that stayed blank until the last of four requests came back would look like an empty library
    /// for as long as the slowest one took.
    /// </para>
    /// <para>
    /// One list refusing leaves its own line and does not hide the others, which is the rule every other read on
    /// this screen follows. A 403 is worth naming out loud though: three refusals in a row on a server that wants a
    /// sign-in read as an empty library unless the refusal says what it is.
    /// </para>
    /// </remarks>
    public async Task ShowContentAsync(MainMenuEntry? entry)
    {
      int generation = ++_contentGeneration;
      ContentRows.Clear();

      if (entry == null)
      {
        OnPropertyChanged(nameof(HasContent));
        return;
      }

      IReadOnlyList<HomeContentList> lists = HomeContentList.For(entry);
      if (lists.Count == 0)
      {
        // An entry that opens a whole screen, or one nothing backs. Saying what pressing does is the useful thing;
        // the pane would otherwise be an empty column next to a menu that is anything but empty.
        ContentRows.Add(HomeRow.Note(entry.Openable ? "Press Enter: " + entry.Action : entry.Detail));
        OnPropertyChanged(nameof(HasContent));
        return;
      }

      // The headings - and the lines saying which lists cannot be filled - go up before anything is read, each
      // list remembering the row position its own titles will land at once an answer arrives. Without those
      // positions, a title that arrives late would be appended after every later heading instead of under its
      // own, and a pane whose newest films stand below the word "Favorites" is a pane that lies.
      var rowPositions = new int[lists.Count];
      int position = 0;
      for (int i = 0; i < lists.Count; i++)
      {
        ContentRows.Add(HomeRow.Heading(lists[i].Heading));
        position++;
        if (lists[i].Missing != null)
        {
          ContentRows.Add(HomeRow.Note(lists[i].Missing));
          position++;
        }
        rowPositions[i] = position;
      }

      OnPropertyChanged(nameof(HasContent));

      if (_client == null)
        return;

      HomeSection section = entry.Section!;
      int askedFor = -1;

      // How many rows earlier lists have written. Everything is read in order and one list at a time, so when an
      // answer arrives, every row to the left of the insertion point is already final - and the point a later
      // list's titles have to land at has moved right by exactly this many rows.
      int writtenBefore = 0;

      try
      {
        for (int i = 0; i < lists.Count; i++)
        {
          HomeContentList list = lists[i];
          if (list.Missing != null)
            continue;

          askedFor = i;
          MediaTitles titles = await _client.GetTitlesAsync(section.Aspect.Name, list.Sort, list.Descending,
              HomeContentList.CONTENT_LIST_LIMIT, 0, null, list.Seen, null);

          if (generation != _contentGeneration)
            return;

          int at = rowPositions[i] + writtenBefore;
          int first = at;
          foreach (MediaTitle title in titles.Titles ?? Array.Empty<MediaTitle>())
            ContentRows.Insert(at++, HomeRow.For(new TitleViewModel(title, false, _client)));

          if (at == first)
            ContentRows.Insert(at++, HomeRow.Note("nothing here"));

          writtenBefore += at - first;
          OnPropertyChanged(nameof(HasContent));
        }

        // The list arrived without a token, so this server keeps its doors open to the room; whatever refusal
        // this screen saw before belonged to another server or to the switch before someone turned it off.
        ServerRefusesStrangers = false;
      }
      catch (ServerApiException ex)
      {
        if (generation != _contentGeneration)
          return;

        // A refusal of the titles is the server asking who is calling; the sign-in boxes appear because of it,
        // and the note below says so.
        if (ex.StatusCode == 403)
          ServerRefusesStrangers = true;

        // The headings are already up, so the line has to appear under the list that was asked for rather than at
        // the end of the pane; the loop stops, because every remaining list would be refused for the same reason.
        ContentRows.Insert(askedFor < 0 ? ContentRows.Count : rowPositions[askedFor] + writtenBefore,
            HomeRow.Note(DescribeContentFailure(ex, askedFor < 0 ? null : lists[askedFor])));
        OnPropertyChanged(nameof(HasContent));
      }
      catch (Exception ex)
      {
        if (generation != _contentGeneration)
          return;

        ContentRows.Add(HomeRow.Note("reading failed: " + ex.Message));
        OnPropertyChanged(nameof(HasContent));
      }
    }

    /// <summary>
    /// What one refused list says about itself, in the words a viewer needs: the refusal, and - when the server was
    /// refusing everybody rather than this list - the fact that signing in is the way past it.
    /// </summary>
    private static string DescribeContentFailure(ServerApiException failure, HomeContentList? list)
    {
      if (failure.StatusCode == 403)
        return "this server wants you to sign in first";
      return failure.Message;
    }

    /// <summary>
    /// Walking the menu with a remote: every stop refills the pane.
    /// </summary>
    partial void OnSelectedEntryChanged(MainMenuEntry? value)
    {
      // Deliberately not awaited: the key press that moved the selection must not wait on four HTTP requests, and
      // the pane is designed to fill itself in as they arrive.
      _ = ShowContentAsync(value);
    }

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
    /// through, the distinction the shelf marks with a tick (section <i>ac</i>). Each row also carries how far this
    /// caller got, because the request named a profile (section <i>ae</i>), which is what lets pressing a row go
    /// back into the film rather than starting it again; a server that was not told who is asking answers without
    /// those figures, and the rows then offer the beginning.
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
    /// How many parts of the library refused to answer the last time the strip was read, so that the line above it
    /// can be said again in the same words after a row left it. A note about a silent part must not survive one
    /// fewer row, or disappear because something was watched.
    /// </summary>
    private int _unfinishedSilent;

    /// <summary>
    /// Tells the strip what the library now says about one of the titles it is showing, so that a film the viewer
    /// has just watched does not stay on a list of things still to watch.
    /// </summary>
    /// <param name="after">The library's answer to the report of that viewing.</param>
    /// <returns>
    /// True when the strip was showing that title. False means it was not - a film picked in the library, or one
    /// that was not on the strip to begin with - and the strip is left as it was.
    /// </returns>
    /// <remarks>
    /// A finished title leaves the strip instead of staying on it with a tick, because the strip's whole promise
    /// is "press this to go back into it" and a title that is over is not that. A title stopped again keeps its row
    /// and gains the newer figure, which is the pair the strip exists for: the row is the offer, the figure says
    /// how much of it is already gone.
    /// </remarks>
    public bool ApplyPlayback(MediaPlayback after)
    {
      TitleViewModel? row = null;
      foreach (TitleViewModel candidate in Unfinished)
      {
        if (candidate.Title.Id == after.Id)
        {
          row = candidate;
          break;
        }
      }

      if (row == null)
        return false;

      if (after.Watched)
        Unfinished.Remove(row);
      else
        row.ApplyPlayback(after);

      ReportUnfinished(_unfinishedSilent);
      return true;
    }

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

      // The old stream belongs to the old server. Cancelling rather than disposing the source, and clearing the
      // field rather than waiting for the watch to notice: the watch's own cleanup compares what it finds against
      // the source it was started with, so a newer watch that is already in place here is left alone.
      bool wasWatching = _watchCancellation != null;
      _watchCancellation?.Cancel();
      _watchCancellation = null;

      _client = client;
      SignedInAs = null;
      ServerRefusesStrangers = false;
      Aspects.Clear();
      Sections.Clear();
      RebuildMenu(false);
      Unfinished.Clear();
      ReportUnfinished(0);
      SelectedAspect = null;
      ServerDescription = null;
      IsBusy = false;
      Status = "Server address set to " + client.BaseUrl;
      OnPropertyChanged(nameof(ServerUrl));

      if (wasWatching)
        WatchServerEvents();
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

        // And so does the stream: a server that wants to know its callers refused this connection a minute ago and
        // stopped the watch. Starting it again is the difference between a screen that updates once, after the
        // sign-in, and one that goes on updating afterwards by itself.
        WatchServerEvents();
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

        // Whether an entry of the menu can be filled at all is settled by this list alone, so the menu stops
        // saying "no server connected" the moment the answer is in; the counts come later, in this same run.
        RebuildMenu(true);

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

        // From the same aspects, in the same breath, so that the menu's "4 films" is the number that arrived in
        // this reading and not the one that was on screen before the server was switched.
        RebuildMenu(true);

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
              UNFINISHED_ROWS, 0, null, "started", null, null, null, cancellation);
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
        Unfinished.Add(new TitleViewModel(title, false, _client));

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
      _unfinishedSilent = silent;

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

    #region What the server says without being asked

    /// <summary>
    /// How long a burst of news from the server is allowed to gather before the screen asks again.
    /// </summary>
    /// <remarks>
    /// Longer than the server's own half-second flush, or the debounce would fire once per server frame and the
    /// whole point - one question to the server instead of one per directory the importer walked through - would be
    /// lost. Short enough that a viewer who added a film sees it appear while still standing near the television.
    /// </remarks>
    public static TimeSpan EVENT_REFRESH_DEBOUNCE = TimeSpan.FromSeconds(2);

    [ObservableProperty]
    private string? _serverActivity;

    /// <summary>
    /// Whether there is anything to say about what the server is doing, which is what puts the line on the screen
    /// and what takes it off again.
    /// </summary>
    public bool HasServerActivity
    {
      get { return !string.IsNullOrEmpty(ServerActivity); }
    }

    partial void OnServerActivityChanged(string? value)
    {
      OnPropertyChanged(nameof(HasServerActivity));
    }

    private Action<Action> _screenThread = delegate (Action action) { action(); };
    private CancellationTokenSource? _watchCancellation;
    private int _refreshArmed;

    /// <summary>
    /// Starts listening to the server's event stream, which is how the library on screen changes while nobody
    /// presses anything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Without this the front end is correct but slow: it shows what it read last, and the only way to make it
    /// look again is to press refresh. Everything the server does on its own - an import that finishes at two in
    /// the morning, a share added from another client, a file deleted from the disk - happened silently here.
    /// </para>
    /// <para>
    /// Called by whoever owns the screen, once, because only that code knows how to get back onto the thread the
    /// visual tree belongs to; like the playback screen, the running front end passes
    /// <c>action =&gt; Dispatcher.UIThread.Post(action)</c>. Left out, what is heard is applied where it arrived,
    /// which is what a test without a screen wants.
    /// </para>
    /// </remarks>
    public void WatchServerEvents(Action<Action>? screenThread = null)
    {
      if (_client == null || _watchCancellation != null)
        return;

      if (screenThread != null)
        _screenThread = screenThread;

      CancellationTokenSource cancellation = new CancellationTokenSource();
      _watchCancellation = cancellation;
      ServerActivity = "Connecting to the server";

      // Deliberately not awaited and not stored: the watch is the lifetime of the screen, the only thing that ends
      // it is UseServer or the process, and every way it can fail is reported through ServerActivity rather than
      // through a task nobody collects.
      _ = WatchAsync(cancellation.Token, cancellation);
    }

    /// <summary>Whether the screen is currently hearing the server's news.</summary>
    public bool IsWatchingEvents
    {
      get { return _watchCancellation != null; }
    }

    /// <summary>
    /// Stops listening. Called when the front end closes; switching servers does it on its own.
    /// </summary>
    public void StopWatchingEvents()
    {
      CancellationTokenSource? source = _watchCancellation;
      _watchCancellation = null;
      ServerActivity = null;
      source?.Cancel();
    }

    private async Task WatchAsync(CancellationToken cancellation, CancellationTokenSource source)
    {
      ServerApiClient client = _client!;
      try
      {
        await client.WatchMediaEventsAsync(OnEventAsync, OnConnection, cancellation).ConfigureAwait(false);
      }
      catch (ServerApiException failure)
      {
        // Refused at the door: the server wants to know its callers and this one has not signed in. Saying that
        // is the difference between a screen that quietly stops updating and one that says what to do about it.
        _screenThread(delegate
        {
          ServerActivity = "The server would not let this screen listen. Sign in to see changes as they happen.";
        });
      }
      catch (OperationCanceledException)
      {
        // The viewer switched servers. The expected ending.
      }
      catch (Exception ex)
      {
        _screenThread(delegate { ServerActivity = "Stopped listening: " + ex.Message; });
      }
      finally
      {
        // Only the watch this one started, or a watch started by UseServer while this was unwinding would have its
        // own cancellation thrown away by the older run.
        lock (_gateLock)
        {
          if (ReferenceEquals(_watchCancellation, source))
          {
            _watchCancellation = null;
            source.Dispose();
          }
        }
      }
    }

    /// <summary>
    /// One frame, on the watcher's thread. Nothing here touches the screen; what it decides is posted.
    /// </summary>
    private Task OnEventAsync(MediaEvent mediaEvent)
    {
      _screenThread(delegate { ApplyEvent(mediaEvent); });
      return Task.CompletedTask;
    }

    private void OnConnection(MediaEventConnection connection)
    {
      _screenThread(delegate
      {
        if (connection.IsConnected)
          ServerActivity = "Listening to the server";
        else
          ServerActivity = "Lost the server. Trying again in " +
              (int)Math.Round(connection.RetryAfter.TotalSeconds) + " seconds.";
      });
    }

    /// <summary>
    /// What a frame means on screen: a line about what the server is doing, and - when the frame says the titles
    /// may have changed - one refresh, after the burst around it has finished.
    /// </summary>
    private void ApplyEvent(MediaEvent mediaEvent)
    {
      if (mediaEvent.Kind == MediaEvent.KIND_PROGRESS && mediaEvent.Jobs.Count > 0)
        ServerActivity = DescribeJob(mediaEvent.Jobs[0]);
      else if (mediaEvent.Kind == MediaEvent.KIND_IMPORT)
      {
        if (mediaEvent.State == "started" || mediaEvent.State == "scheduled")
          ServerActivity = "The server is filing its library";
        else if (mediaEvent.State == "completed" || mediaEvent.State == "canceled")
          ServerActivity = "Listening to the server";
      }
      else if (mediaEvent.Kind == MediaEvent.KIND_GAP && mediaEvent.Dropped > 0)
        ServerActivity = "The server had " + mediaEvent.Dropped + " changes to report at once";

      if (mediaEvent.ShouldRefresh)
        ArmRefresh();
    }

    private static string DescribeJob(MediaImportJob job)
    {
      string where = job.Path ?? "";
      string local;
      string? reason;
      if (MediaLibraryPath.TryAsLocalFile(job.Path, out local, out reason))
        where = local;

      return where.Length == 0
          ? "Filing " + job.Completed + " of " + job.Identified
          : "Filing " + job.Completed + " of " + job.Identified + " in " + where;
    }

    /// <summary>
    /// Asks the server once, a moment after its news stopped arriving.
    /// </summary>
    /// <remarks>
    /// One pending refresh at a time, counted with an <see cref="Interlocked"/> rather than a timer: an importer
    /// finishing produces a handful of frames inside a second, and each of them asking the library for its whole
    /// shelf would be a screen that hammers the server because something happened on the wire. The flag is cleared
    /// before the refresh starts, so news that arrives while a read is in flight arms the next one.
    /// </remarks>
    private void ArmRefresh()
    {
      if (Interlocked.CompareExchange(ref _refreshArmed, 1, 0) != 0)
        return;

      _ = ArmAfterBurstAsync();
    }

    private async Task ArmAfterBurstAsync()
    {
      try
      {
        await Task.Delay(EVENT_REFRESH_DEBOUNCE).ConfigureAwait(false);
      }
      catch (Exception)
      {
        // A delay does not fail. Reaching the reset below is the only thing that matters.
      }

      Interlocked.Exchange(ref _refreshArmed, 0);
      _screenThread(delegate { _ = RefreshAsync(); });
    }

    #endregion
  }
}
