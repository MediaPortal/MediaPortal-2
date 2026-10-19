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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One row of the feeds column: a paper's name and what came of asking it for headlines.
  /// </summary>
  public sealed class NewsFeedRow
  {
    public NewsFeedRow(string name, string state)
    {
      Name = name;
      State = state;
    }

    /// <summary>The feed's name, or "All headlines" for the row that merges them.</summary>
    public string Name { get; }

    /// <summary>How many it answered with, or why it did not - both belong in the row, not in a footnote.</summary>
    public string State { get; }
  }

  /// <summary>
  /// One row of the headlines column.
  /// </summary>
  public sealed class NewsHeadlineRow
  {
    private readonly NewsHeadline _headline;

    public NewsHeadlineRow(NewsHeadline headline)
    {
      _headline = headline;
    }

    public string Title
    {
      get { return _headline.Title; }
    }

    /// <summary>Which paper it is from - shown on every row, because the merged list is where it gets lost.</summary>
    public string FeedName
    {
      get { return _headline.FeedName; }
    }

    /// <summary>The date in the row's small print, or the words that say the feed did not give one.</summary>
    public string DateText
    {
      get
      {
        if (_headline.Published == null)
          return "no date";
        // The feed's own offset kept while converting, so a headline published at nine in London reads as ten
        // here rather than as nine o'clock somewhere.
        return _headline.Published.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);
      }
    }

    /// <summary>The story's words, for the panel on the right.</summary>
    public string Summary
    {
      get { return _headline.Summary; }
    }
  }

  /// <summary>
  /// The news: MediaPortal's own feed list, read for the viewer's language, with the headlines of every feed in
  /// one list newest first.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the screen the old MP2 drew from the same plugin's collector, and its shape is the plugin's: a list
  /// of feeds, and their headlines. What changed is who counts as a source of truth. The old collector refetched
  /// on a background timer and kept a merged list in memory; here the feeds are read when the screen opens and
  /// when the viewer says so, because a screen that is open for ninety seconds of a television's evening does
  /// not need a fifteen-minute clock of its own.
  /// </para>
  /// <para>
  /// <b>A feed that refuses is said, not swallowed.</b> Each feed answers on its own and keeps its own row in
  /// the left column - a count or the refusal - and the merged list is exactly the headlines of the feeds that
  /// answered. The alternative, which is what a single try/catch around the whole refresh would give, is a screen
  /// that shows nothing when three of four papers were at fault.
  /// </para>
  /// <para>
  /// The awaits keep their context, as everywhere else in this front end: what follows them writes properties a
  /// bound screen is listening to.
  /// </para>
  /// </remarks>
  public partial class NewsViewModel : ObservableObject, IDisposable
  {
    /// <summary>The merged list, which is what the screen shows before anybody has picked a feed.</summary>
    private static readonly NewsFeedRow ALL = new NewsFeedRow("All headlines", "");

    private readonly NewsApiClient? _client;
    private readonly string _cultureName;
    private readonly List<NewsFeedAnswer> _answers = new List<NewsFeedAnswer>();
    private CancellationTokenSource? _running;

    [ObservableProperty]
    private NewsFeedRow? _selectedFeed;

    [ObservableProperty]
    private NewsHeadlineRow? _selectedHeadline;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _status = "Not read yet.";

    /// <param name="client">Where to fetch from, or null for a screen that says it has nowhere.</param>
    /// <param name="cultureName">
    /// Which language's feed list to ask for; the machine's own by default, and named here so that a test can be
    /// somebody from Vienna.
    /// </param>
    public NewsViewModel(NewsApiClient? client = null, string? cultureName = null)
    {
      _client = client;
      _cultureName = string.IsNullOrWhiteSpace(cultureName)
          ? CultureInfo.CurrentUICulture.Name
          : cultureName.Trim();
      Feeds.Add(ALL);
      if (_client == null)
        Status = "No news client";
    }

    /// <summary>The feeds - "All headlines" first, then one row per feed on the list.</summary>
    public ObservableCollection<NewsFeedRow> Feeds { get; } = new ObservableCollection<NewsFeedRow>();

    /// <summary>
    /// The headlines of the selected feed, newest first; of all the feeds, while "All headlines" is selected.
    /// </summary>
    public ObservableCollection<NewsHeadlineRow> Headlines { get; } = new ObservableCollection<NewsHeadlineRow>();

    private bool CanRefresh
    {
      get { return _client != null; }
    }

    /// <summary>Asks every feed on the list again.</summary>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
      if (_client == null)
      {
        Status = "No news client";
        return;
      }

      // One read at a time, same rule as the title lists: a held-down refresh key queues requests, and the last
      // answer that arrives would otherwise be whichever feed was slowest rather than the last one asked for.
      CancellationTokenSource? previous = _running;
      CancellationTokenSource cancellation = new CancellationTokenSource();
      _running = cancellation;
      previous?.Cancel();

      IsBusy = true;
      try
      {
        IReadOnlyList<NewsFeedBookmark> bookmarks;
        try
        {
          bookmarks = await _client.GetDefaultFeedsAsync(_cultureName, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
          throw;
        }
        catch (Exception ex)
        {
          // The list is MediaPortal's own address; not answering means the screen has no sources to name, which
          // is said here rather than leaving an empty feed column to be read as an empty world.
          Status = "MediaPortal's feed list did not answer: " + ex.Message;
          return;
        }

        if (bookmarks.Count == 0)
        {
          Status = "MediaPortal's feed list has no feeds for '" + _cultureName + "' and no English fallback";
          return;
        }

        NewsFeedAnswer[] answers = await Task.WhenAll(bookmarks.Select(feed =>
            _client.ReadFeedAsync(feed, cancellation.Token)).ToArray());

        _answers.Clear();
        _answers.AddRange(answers);

        string wasSelectedName = SelectedFeed?.Name ?? ALL.Name;
        Feeds.Clear();
        Feeds.Add(ALL);
        foreach (NewsFeedAnswer answer in answers)
          Feeds.Add(new NewsFeedRow(answer.Feed.Name,
              answer.Error == null ? CountText(answer) : answer.Error));

        // The viewer's place in the column survives a refresh; a feed that left the list because its paper left
        // MediaPortal's list is answered by falling back to the merged view.
        SelectedFeed = Feeds.FirstOrDefault(row => row.Name == wasSelectedName) ?? ALL;
        ShowHeadlines();
        Status = StatusOf(answers);
      }
      catch (OperationCanceledException)
      {
        // A newer read was asked for; it reports its own status.
      }
      catch (Exception ex)
      {
        Status = "Reading the news failed: " + ex.Message;
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

    partial void OnSelectedFeedChanged(NewsFeedRow? value)
    {
      ShowHeadlines();
    }

    public void Dispose()
    {
      _running?.Cancel();
      _running?.Dispose();
      // The screen owns the view model's whole world once it is built, and the view model owns the client it
      // was handed - nobody else reaches in here, and nobody else should have to remember.
      _client?.Dispose();
    }

    /// <summary>
    /// Fills the headlines column from what is already in memory: a refresh is the only moment anything is
    /// fetched, and walking the feed column only walks what arrived.
    /// </summary>
    private void ShowHeadlines()
    {
      Headlines.Clear();
      SelectedHeadline = null;
      bool all = SelectedFeed == null || SelectedFeed.Name == ALL.Name;

      // Newest first, and the undated ones last rather than first: a feed that gives no date is not claiming to
      // be the newest story on the internet, and "no date" printed above everything else would say it was.
      IEnumerable<NewsHeadline> merged = _answers
          .Where(answer => answer.Headlines != null && (all || answer.Feed.Name == SelectedFeed!.Name))
          .SelectMany(answer => answer.Headlines!);
      foreach (NewsHeadline headline in merged
          .OrderBy(headline => headline.Published == null ? 1 : 0)
          .ThenByDescending(headline => headline.Published))
        Headlines.Add(new NewsHeadlineRow(headline));

      SelectedHeadline = Headlines.Count > 0 ? Headlines[0] : null;
    }

    private static string CountText(NewsFeedAnswer answer)
    {
      int count = answer.Headlines?.Count ?? 0;
      return count == 1 ? "1 headline" : count.ToString("N0") + " headlines";
    }

    private string StatusOf(NewsFeedAnswer[] answers)
    {
      NewsFeedAnswer[] refused = answers.Where(answer => answer.Error != null).ToArray();
      int headlines = answers.Sum(answer => answer.Headlines?.Count ?? 0);
      int answered = answers.Length - refused.Length;

      if (headlines == 0 && refused.Length == answers.Length)
      {
        // Nothing at all, and every reason named - this is the case the status line exists for.
        return "No feed answered: " + string.Join("; ",
            refused.Select(answer => answer.Feed.Name + " - " + answer.Error));
      }

      string read = headlines + (headlines == 1 ? " headline" : " headlines") + " from " + answered + " of "
          + answers.Length + " feeds";
      if (refused.Length == 0)
        return read + ", newest first";
      return read + " - refused: " + string.Join(", ", refused.Select(answer =>
          answer.Feed.Name + " (" + answer.Error + ")"));
    }
  }
}
