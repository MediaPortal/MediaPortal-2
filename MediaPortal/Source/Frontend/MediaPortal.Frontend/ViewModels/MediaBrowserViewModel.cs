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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One row of the title list.
  /// </summary>
  /// <remarks>
  /// The row decides here whether it can be watched, because the answer belongs to the row: a library of two
  /// hundred titles on a machine that can reach only half of them is not a screen that should let the viewer
  /// press their way through the other half to find out.
  /// </remarks>
  public sealed class TitleViewModel
  {
    private readonly string? _unplayableReason;

    public TitleViewModel(MediaTitle title)
    {
      Title = title;
      if (!MediaLibraryPath.TryAsLocalFile(title.Path, out string localPath, out string? reason))
        _unplayableReason = reason;
      else
        LocalPath = localPath;
    }

    /// <summary>The answer from the server, unchanged.</summary>
    public MediaTitle Title { get; }

    /// <summary>The file to open, when this machine can open it.</summary>
    public string? LocalPath { get; }

    public bool Playable
    {
      get { return LocalPath != null; }
    }

    /// <summary>Why this title cannot be watched here, or <c>null</c> when it can.</summary>
    public string? UnplayableReason
    {
      get { return _unplayableReason; }
    }

    /// <summary>
    /// The name, or a word that says there is none. An empty row reads as a rendering bug; "Untitled" reads as
    /// a file nobody named, which is what it is.
    /// </summary>
    public string Name
    {
      get { return string.IsNullOrWhiteSpace(Title.Title) ? "Untitled" : Title.Title!.Trim(); }
    }

    /// <summary>The name a viewer scans for, with the article moved to the back - what the server sorted by.</summary>
    public string SortName
    {
      get
      {
        string? sort = Title.SortTitle;
        return string.IsNullOrWhiteSpace(sort) ? Name : sort!.Trim();
      }
    }

    /// <summary>
    /// Whether the library was told this title was watched to the end. The play count is the only evidence there
    /// is: it is the number this server itself wrote when a front end reported the end of the film, so a row is
    /// marked seen exactly when the library says it was finished and not when a file happens to exist.
    /// </summary>
    public bool Seen
    {
      get { return Title.PlayCount > 0; }
    }

    /// <summary>
    /// The mark in front of a row that was watched, nothing in front of one that was not. An empty string rather
    /// than a "not seen" mark, because ninety unmarked rows out of a shelf of three thousand are the information;
    /// a cross on every one of them would be a wall of ink saying nothing.
    /// </summary>
    public string SeenMark
    {
      // Written as an escape rather than as the character, so that the mark does not depend on the encoding of
      // this source file surviving a checkout on another machine.
      get { return Seen ? "\u2713" : ""; }
    }

    /// <summary>What the left column of the row says: the name, then the year it is known by.</summary>
    public string Line
    {
      get { return Title.Year.HasValue ? Name + " (" + Title.Year.Value + ")" : Name; }
    }

    /// <summary>
    /// The right column: how long it is, and what is wrong with it if something is.
    /// </summary>
    public string Detail
    {
      get
      {
        if (!Playable)
          return "not playable here";
        string length = Title.RuntimeMinutes.HasValue
            ? Title.RuntimeMinutes.Value + " min"
            : "";
        if (Title.Stub)
          return string.IsNullOrEmpty(length) ? "stub" : length + " · stub";
        return length;
      }
    }
  }

  /// <summary>
  /// The list of titles behind one home tile, and the reading that fills it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Pages are read one at a time and never all at once. A library of 40 000 items is a real household, and a
  /// screen that fetched it to show twenty would be waiting minutes for a list nobody scrolls through in one
  /// evening; "more" is a row on the screen because the viewer is the one who decides whether to ask again.
  /// </para>
  /// <para>
  /// Like the home screen, this view model knows nothing about Avalonia, so the whole of it is testable against
  /// a server that is really answering.
  /// </para>
  /// </remarks>
  public partial class MediaBrowserViewModel : ObservableObject
  {
    /// <summary>
    /// Titles per request. A television shows around ten rows, and reading ahead of what fits on the screen twice
    /// over keeps the remote from waiting on the network without fetching a whole season.
    /// </summary>
    public const int PAGE_SIZE = 60;

    private readonly ServerApiClient? _client;
    private CancellationTokenSource? _running;

    [ObservableProperty]
    private string _status;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _canLoadMore;

    [ObservableProperty]
    private TitleViewModel? _selected;

    /// <summary>
    /// Whether the details panel is on screen. Off until a viewer asks for one title, because the list is the
    /// screen - the panel is a question about one row, not a second place to navigate to.
    /// </summary>
    [ObservableProperty]
    private bool _detailsVisible;

    [ObservableProperty]
    private string _detailsHeading = "";

    [ObservableProperty]
    private string _detailsLine = "";

    [ObservableProperty]
    private string _detailsText = "";

    [ObservableProperty]
    private string _detailsPath = "";

    /// <summary>The title the last details read was for, so that opening it again costs nothing.</summary>
    private Guid _detailsFor;

    /// <summary>
    /// What the viewer has typed into the search box, which is not yet what the list is filtered by - the list
    /// moves when they press Enter or the Find button, not on every keystroke, because every keystroke would be a
    /// round trip to the server while the box is still being written in.
    /// </summary>
    [ObservableProperty]
    private string _searchText = "";

    /// <summary>The words the list on screen was read with, or "" when the whole shelf is showing.</summary>
    [ObservableProperty]
    private string _activeSearch = "";

    /// <summary>
    /// Which half of the shelf is on screen: "" for both halves, <c>unwatched</c> for the titles nobody has
    /// finished, <c>watched</c> for the ones somebody has. The server's own two words, so that what a log line
    /// says, what the answer echoes back and what this screen are one and the same thing.
    /// </summary>
    /// <remarks>
    /// Like <see cref="_activeSearch"/> this is what the list on screen answers, not what a button was last
    /// pressed with - a filter that failed to load is not a filter the viewer is looking through.
    /// </remarks>
    [ObservableProperty]
    private string _activeSeen = "";

    public MediaBrowserViewModel()
        : this(null, null)
    {
    }

    public MediaBrowserViewModel(ServerApiClient? client, HomeSection? section)
    {
      _client = client;
      Section = section;
      AspectName = section?.Aspect.Name ?? "";
      Heading = section?.Label ?? "Titles";
      _status = client == null ? "No server configured"
          : section == null ? "No section chosen"
          : "Reading " + section.Label;
    }

    /// <summary>The tile this list came from, or <c>null</c> in the designer.</summary>
    public HomeSection? Section { get; }

    /// <summary>The aspect the server is asked for, in its own spelling.</summary>
    public string AspectName { get; }

    /// <summary>What the screen is called at the top.</summary>
    public string Heading { get; }

    public ObservableCollection<TitleViewModel> Titles { get; } = new ObservableCollection<TitleViewModel>();

    /// <summary>
    /// Raised when a viewer asked to watch a title and this view model decided it is watchable. The application
    /// owns the player, so it decides what that means; this class only says which file it handed over.
    /// </summary>
    public event EventHandler<TitleViewModel>? PlayRequested;

    /// <summary>
    /// Reads the first page. Called when the screen appears, so that opening a tile and pressing Refresh on the
    /// home screen both lead to a filled list rather than an empty one.
    /// </summary>
    [RelayCommand]
    public async Task OpenAsync()
    {
      Titles.Clear();
      Selected = null;
      CanLoadMore = false;
      await ReadAsync(0);
    }

    /// <summary>
    /// Reads the page after the ones already on screen.
    /// </summary>
    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        await ReadAsync(Titles.Count);
    }

    /// <summary>
    /// Whether a search is on this list, which is what the "Clear" button appears and disappears for.
    /// </summary>
    public bool HasSearch
    {
      get { return !string.IsNullOrEmpty(ActiveSearch); }
    }

    partial void OnActiveSearchChanged(string value)
    {
      OnPropertyChanged(nameof(HasSearch));
    }

    /// <summary>
    /// Filters the list to the titles carrying the words in the search box. Enter on the box and the Find button
    /// both lead here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The search is sent as a question about the whole aspect rather than run over the titles already on screen,
    /// because a shelf of two thousand films has the one the viewer means on a page nobody has reached yet. Paging
    /// then walks through the matches, not through the shelf: <c>More</c> asks for the next page of the same
    /// question.
    /// </para>
    /// <para>
    /// An empty box means the whole shelf again. Someone who deleted the words to look at everything should not
    /// have to hunt for a second control to say so, which is also why this does not treat empty as "no matches".
    /// </para>
    /// </remarks>
    [RelayCommand]
    public async Task FindAsync()
    {
      string words = (SearchText ?? "").Trim();
      if (words.Length == 0)
      {
        await ClearSearchAsync();
        return;
      }

      if (!string.Equals(words, ActiveSearch, StringComparison.Ordinal))
      {
        ActiveSearch = words;
        ClearDetailsForNewList();
      }

      await ReadAsync(0);
    }

    /// <summary>Puts the whole shelf back. Escape on the search box comes here.</summary>
    [RelayCommand]
    public async Task ClearSearchAsync()
    {
      SearchText = "";
      if (HasSearch)
      {
        ActiveSearch = "";
        ClearDetailsForNewList();
      }

      await ReadAsync(0);
    }

    /// <summary>
    /// What the watched filter on the button in front of the list says. The three states are named here rather than
    /// in the markup because the same three words belong in a log line, in a test and in a screenshot.
    /// </summary>
    public string SeenLabel
    {
      get
      {
        if (ActiveSeen == "unwatched")
          return "Unwatched only";
        if (ActiveSeen == "watched")
          return "Watched only";
        return "All titles";
      }
    }

    /// <summary>Whether the list is being looked through, which is what puts a word about it in the status line.</summary>
    public bool HasSeenFilter
    {
      get { return !string.IsNullOrEmpty(ActiveSeen); }
    }

    partial void OnActiveSeenChanged(string value)
    {
      OnPropertyChanged(nameof(SeenLabel));
      OnPropertyChanged(nameof(HasSeenFilter));
    }

    /// <summary>
    /// Walks the watched filter one step round: everything, then what nobody has finished, then what somebody
    /// has, then everything again. The key <c>W</c> and the button above the list both lead here.
    /// </summary>
    /// <remarks>
    /// Three states on one control rather than two checkboxes, because a remote has no way of pressing two things
    /// at once and "watched and unwatched" is the same question as "everything", which a viewer can always reach
    /// with one more press.
    /// <para>
    /// The filter is asked of the server rather than applied to the rows on screen, for the same reason the search
    /// is: on a shelf of three thousand films the twenty the viewer has not finished are on pages nobody has read.
    /// </para>
    /// </remarks>
    [RelayCommand]
    public async Task CycleSeenAsync()
    {
      ActiveSeen = ActiveSeen == "" ? "unwatched"
          : ActiveSeen == "unwatched" ? "watched"
          : "";
      await ReaskAsync();
    }

    /// <summary>
    /// Takes the watched filter off the list, which is what Escape does when there is nothing newer on the screen
    /// to close. Nothing is re-read when no filter was on, because the list already answers that question.
    /// </summary>
    [RelayCommand]
    public async Task ClearSeenAsync()
    {
      if (!HasSeenFilter)
        return;

      ActiveSeen = "";
      await ReaskAsync();
    }

    /// <summary>
    /// Asks the server the current question again from the first page, because the question changed under the list.
    /// </summary>
    private async Task ReaskAsync()
    {
      ClearDetailsForNewList();
      await ReadAsync(0);
    }

    /// <summary>
    /// Puts the list back to its beginning because a different question was asked of the server.
    /// </summary>
    private void ClearDetailsForNewList()
    {
      // A details panel left open is about a title from the list that is on its way away, and a row a viewer can no
      // longer see is not a row they are reading about.
      DetailsVisible = false;
      Selected = null;
      Titles.Clear();
      CanLoadMore = false;
    }

    /// <summary>
    /// The viewer pressed OK on a row: either the application is told to play it, or the row's own reason why not
    /// is put on the status line. Both are said in the same place, so "nothing happened" is never the answer.
    /// </summary>
    [RelayCommand]
    public void Play()
    {
      Play(Selected);
    }

    public void Play(TitleViewModel? title)
    {
      if (title == null)
      {
        Status = "Nothing selected";
        return;
      }

      if (!title.Playable)
      {
        Status = title.Name + " cannot be watched from here: " + title.UnplayableReason;
        return;
      }

      Selected = title;
      PlayRequested?.Invoke(this, title);
    }

    /// <summary>
    /// Opens the details of the selected title, or closes the panel again. The screen's "info" key, and the only
    /// way a viewer learns that the server knows more about a film than its name and its length.
    /// </summary>
    /// <remarks>
    /// The read is per title and remembered: pressing the key twice on the same row costs nothing, which is what
    /// a viewer who pressed it to look and again to think expects. A failure leaves the panel closed - an empty
    /// panel would be a second, worse answer to the question than the status line.
    /// </remarks>
    [RelayCommand]
    public async Task ToggleDetailsAsync()
    {
      if (DetailsVisible)
      {
        DetailsVisible = false;
        return;
      }

      TitleViewModel? title = Selected;
      if (title == null)
      {
        Status = "Nothing selected";
        return;
      }
      if (_client == null)
      {
        Status = "No server configured";
        return;
      }
      if (_detailsFor == title.Title.Id)
      {
        DetailsVisible = true;
        return;
      }

      await ReadDetailsAsync(title);
    }

    /// <summary>Closes the panel. Escape on the screen, and what a viewer presses to get the list back.</summary>
    public void CloseDetails()
    {
      DetailsVisible = false;
    }

    /// <summary>
    /// Forgets what was read about one title, so that the next look at it asks the server again.
    /// </summary>
    /// <remarks>
    /// A viewing that has just been reported changes the two numbers the panel shows - how often the title was
    /// played and when it was last watched - so a panel that answers from the read made before the film started is
    /// showing something the viewer knows to be wrong. Called from whichever thread the report came back on, which
    /// is why this only touches a field and says nothing to the screen; the next <see cref="ToggleDetailsAsync"/>
    /// does the talking.
    /// </remarks>
    public void ForgetDetails(Guid mediaItemId)
    {
      if (_detailsFor == mediaItemId)
        _detailsFor = Guid.Empty;
    }

    private async Task ReadDetailsAsync(TitleViewModel title)
    {
      if (_client == null)
        return;

      try
      {
        MediaItemDetails details = await _client.GetItemAsync(title.Title.Id);
        _detailsFor = title.Title.Id;

        string name = string.IsNullOrWhiteSpace(details.Title) ? title.Name : details.Title!.Trim();
        DetailsHeading = details.Year.HasValue ? name + " (" + details.Year.Value + ")" : name;
        DetailsLine = DescribeLine(details, title);
        DetailsText = details.Description ?? "Nothing was written about this title on this server.";
        DetailsPath = details.Path ?? title.Title.Path ?? "This title has no resource the server can name.";
        DetailsVisible = true;
      }
      catch (ServerApiException ex)
      {
        if (ex.StatusCode == 403)
          Status = "The details are only for signed-in users on this server. Sign in on the server screen.";
        else if (ex.NotReady)
          Status = "The server is not ready yet: " + ex.Message;
        else
          Status = ex.Message;
      }
      catch (Exception ex)
      {
        Status = "Reading the details failed: " + ex.Message;
      }
    }

    /// <summary>
    /// The one line under the title: what a viewer decides with, in the order they decide with it - which episode
    /// it is, how long, what it costs to watch in years, and whether they have seen it.
    /// </summary>
    private static string DescribeLine(MediaItemDetails details, TitleViewModel title)
    {
      List<string> parts = new List<string>();

      if (details.Season.HasValue || details.Episode.HasValue)
      {
        string number = "S" + (details.Season ?? 0).ToString("00", CultureInfo.InvariantCulture)
            + "E" + (details.Episode ?? 0).ToString("00", CultureInfo.InvariantCulture);
        parts.Add(string.IsNullOrWhiteSpace(details.EpisodeName) ? number : number + " · " + details.EpisodeName!.Trim());
      }
      else if (!string.IsNullOrWhiteSpace(details.SeriesName))
      {
        parts.Add(details.SeriesName!.Trim());
      }

      if (!string.IsNullOrWhiteSpace(details.Collection))
        parts.Add(details.Collection!.Trim());

      int? minutes = details.RuntimeMinutes ?? title.Title.RuntimeMinutes;
      if (minutes.HasValue && minutes.Value > 0)
        parts.Add(minutes.Value + " min");

      if (!string.IsNullOrWhiteSpace(details.Certification))
        parts.Add(details.Certification!.Trim());

      if (details.Rating.HasValue && details.Rating.Value > 0)
        parts.Add("rated " + details.Rating.Value + "/10");

      if (details.PlayCount.HasValue && details.PlayCount.Value > 0)
      {
        string played = "watched " + details.PlayCount.Value + "×";
        if (details.LastPlayed.HasValue)
          played += ", last " + details.LastPlayed.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        parts.Add(played);
      }

      if (details.Stub || title.Title.Stub)
        parts.Add("stub");

      return parts.Count == 0 ? "No more is known about this title" : string.Join(" · ", parts);
    }

    private async Task ReadAsync(int offset)
    {
      if (_client == null)
      {
        Status = "No server configured";
        return;
      }

      // One read at a time. A viewer holding down the arrow key on the "more" row can queue a dozen requests;
      // the older ones are cancelled rather than allowed to append pages out of order.
      CancellationTokenSource? previous = _running;
      CancellationTokenSource cancellation = new CancellationTokenSource();
      _running = cancellation;
      previous?.Cancel();

      IsBusy = true;
      try
      {
        MediaTitles answer = await _client.GetTitlesAsync(AspectName, "title", false, PAGE_SIZE, offset,
            string.IsNullOrEmpty(ActiveSearch) ? null : ActiveSearch,
            string.IsNullOrEmpty(ActiveSeen) ? null : ActiveSeen, cancellation.Token);

        if (offset == 0)
          Titles.Clear();

        foreach (MediaTitle title in answer.Titles ?? Array.Empty<MediaTitle>())
          Titles.Add(new TitleViewModel(title));

        Selected = Selected ?? (Titles.Count > 0 ? Titles[0] : null);
        CanLoadMore = answer.Remaining > 0;

        // The filter in front of the count rather than behind it, because the number is the answer and the filter
        // is the reason the answer is that small: "12 of 40 unwatched films" reads as one statement, while
        // "12 of 40 films, unwatched" reads as two and can be misread as a description of the twelve.
        string seeing = ActiveSeen == "unwatched" ? "unwatched "
            : ActiveSeen == "watched" ? "watched "
            : "";

        if (answer.Total == 0)
        {
          // Two different nothings. "no films" and "no films called 'alien'" need different next moves from a
          // viewer, and only the second one has a search box to clear.
          Status = string.IsNullOrEmpty(ActiveSearch)
              ? "This server has no " + seeing + Heading.ToLowerInvariant()
              : "Nothing in " + Heading.ToLowerInvariant() + " matches '" + ActiveSearch + "'";
        }
        else if (string.IsNullOrEmpty(ActiveSearch))
        {
          Status = string.Format("{0} of {1} {2}{3}, sorted by {4}", Titles.Count, answer.Total, seeing,
              Heading.ToLowerInvariant(), answer.Sort ?? "the server's order");
        }
        else if (Titles.Count < answer.Total)
        {
          Status = string.Format("{0} of {1} {2}{3} match '{4}', sorted by {5}", Titles.Count, answer.Total,
              seeing, Heading.ToLowerInvariant(), ActiveSearch, answer.Sort ?? "the server's order");
        }
        else
        {
          // Every match is already on screen, so "1 of 1" would be a stranger's way of putting it.
          Status = string.Format("{0} {1}{2} {3} {4} '{5}'", answer.Total, seeing,
              answer.Total == 1 ? "title in" : "titles in", Heading.ToLowerInvariant(),
              answer.Total == 1 ? "matches" : "match", ActiveSearch);
        }
        if (!string.IsNullOrEmpty(answer.Error))
          Status = answer.Error;
      }
      catch (ServerApiException ex)
      {
        if (ex.StatusCode == 403)
        {
          // MP2 asks for a token when the server was configured to know who is asking. Saying so is the whole
          // answer available from this screen, and it is a better one than an empty list.
          Status = Heading + " is only for signed-in users on this server. Sign in on the server screen.";
        }
        else if (ex.NotReady)
          Status = "The server is not ready yet: " + ex.Message;
        else
          Status = ex.Message;
      }
      catch (OperationCanceledException)
      {
        // A newer read was asked for; it reports its own status.
      }
      catch (Exception ex)
      {
        // Reported rather than thrown: this method is reached from a button on screen, and an exception that
        // escapes an async command ends the process, not just the read.
        Status = "Reading failed: " + ex.Message;
      }
      finally
      {
        if (ReferenceEquals(_running, cancellation))
        {
          _running = null;
          IsBusy = false;
        }
        cancellation.Dispose();
      }
    }
  }
}
