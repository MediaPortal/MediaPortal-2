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
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One row of the feed list: what the feed is called on the news screen's left column, and where to ask.
  /// </summary>
  public sealed class NewsFeedEditorRow
  {
    public NewsFeedEditorRow(string name, string url)
    {
      Name = name;
      Url = url;
    }

    public string Name { get; }

    public string Url { get; }
  }

  /// <summary>
  /// One answer of the refresh row: the minutes as the file keeps them, and the words the picker shows.
  /// </summary>
  public sealed class NewsRefreshOption
  {
    public NewsRefreshOption(int minutes, string label)
    {
      Minutes = minutes;
      Label = label;
    }

    public int Minutes { get; }

    public string Label { get; }

    public override string ToString()
    {
      return Label;
    }
  }

  /// <summary>
  /// The news feeds as a settings page, the reference's feed management (News/plugin.xml files its branch at
  /// '/Configuration/Settings/Plugins/News'): the feeds the viewer added on top of MediaPortal's own list,
  /// and how often an open news screen asks again. Every answer saves itself the moment it is given, exactly
  /// like the weather's page - a couch viewer who walks away mid-edit leaves a file that says what they
  /// meant, and no "Apply" is waiting to be pressed by nobody.
  /// </summary>
  public partial class NewsFeedsEditorViewModel : ObservableObject
  {
    private readonly NewsFeedsSettings _settings;
    private readonly string? _settingsPath;

    [ObservableProperty]
    private NewsFeedEditorRow? _selectedFeed;

    [ObservableProperty]
    private string _newName = "";

    [ObservableProperty]
    private string _newUrl = "";

    [ObservableProperty]
    private NewsRefreshOption? _selectedRefresh;

    [ObservableProperty]
    private string _status = "";

    public NewsFeedsEditorViewModel(string? settingsPath = null)
    {
      _settingsPath = settingsPath;
      _settings = NewsFeedsSettings.Load(settingsPath, out string? problem);
      foreach (NewsFeedEntry entry in _settings.ExtraFeeds)
        Feeds.Add(new NewsFeedEditorRow(entry.Name, entry.Url));

      foreach (int minutes in NewsFeedsSettings.INTERVALS)
      {
        RefreshOptions.Add(new NewsRefreshOption(minutes, minutes == 0
            ? "Only when the screen opens"
            : "Every " + minutes + (minutes == 1 ? " minute" : " minutes")));
      }

      _selectedRefresh = RefreshOptions[OptionFor(_settings.IntervalMinutes)];
      Status = problem ?? Feeds.Count + (Feeds.Count == 1 ? " own feed" : " own feeds")
          + (Feeds.Count > 0 ? ", read after MediaPortal's own" : " - the news screen reads MediaPortal's own list alone");
    }

    /// <summary>The viewer's feeds, in the order they were added; the news screen reads them after its own.</summary>
    public ObservableCollection<NewsFeedEditorRow> Feeds { get; } = new ObservableCollection<NewsFeedEditorRow>();

    /// <summary>The answers the refresh row walks: zero first, then the reference's fifteen where it starts.</summary>
    public ObservableCollection<NewsRefreshOption> RefreshOptions { get; } = new ObservableCollection<NewsRefreshOption>();

    private bool CanAdd
    {
      // A feed without an address is half an answer, and half an answer saved would stand on the news
      // screen as a feed that refuses - so the button waits for the address. The name may stay out: the
      // address itself becomes the name on the screen then, like everywhere else the feeds are read.
      get { return _newUrl.Trim().Length > 0; }
    }

    private bool CanRemove
    {
      get { return SelectedFeed != null; }
    }

    /// <summary>Keeps the typed feed: onto the list, into the file, the news screen reads it from the next ask on.</summary>
    [RelayCommand(CanExecute = nameof(CanAdd))]
    private void AddFeed()
    {
      string url = NewUrl.Trim();
      if (url.Length == 0)
      {
        Status = "A feed without an address is no feed";
        return;
      }

      string name = NewName.Trim();
      if (name.Length == 0)
        name = url;

      bool duplicate = false;
      foreach (NewsFeedEntry entry in _settings.ExtraFeeds)
      {
        if (string.Equals(entry.Url.Trim(), url, StringComparison.OrdinalIgnoreCase))
        {
          duplicate = true;
          break;
        }
      }

      if (duplicate)
      {
        Status = "That address is already on the list";
        return;
      }

      Feeds.Add(new NewsFeedEditorRow(name, url));
      _settings.ExtraFeeds.Add(new NewsFeedEntry { Name = name, Url = url });
      string saved = SaveSettings(out bool wasSaved);
      Status = name + (wasSaved ? " added and saved" : " added") + (wasSaved ? "" : ", but " + saved);
      NewName = "";
      NewUrl = "";
    }

    /// <summary>Takes the picked feed off the list and out of the file. No confirmation: adding it back is one typing away.</summary>
    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void RemoveSelectedFeed()
    {
      NewsFeedEditorRow? feed = SelectedFeed;
      if (feed == null)
        return;

      Feeds.Remove(feed);
      _settings.ExtraFeeds.RemoveAll(entry =>
          string.Equals(entry.Url.Trim(), feed.Url.Trim(), StringComparison.OrdinalIgnoreCase));
      string saved = SaveSettings(out bool wasSaved);
      string gone = feed.Name + " is off the list";
      Status = wasSaved ? gone : gone + ", but " + saved;
      SelectedFeed = Feeds.Count > 0 ? Feeds[0] : null;
    }

    partial void OnNewUrlChanged(string value)
    {
      AddFeedCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFeedChanged(NewsFeedEditorRow? value)
    {
      RemoveSelectedFeedCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedRefreshChanged(NewsRefreshOption? value)
    {
      if (value == null || _settings.IntervalMinutes == value.Minutes)
        return;

      _settings.IntervalMinutes = value.Minutes;
      string saved = SaveSettings(out bool wasSaved);
      string answer = value.Minutes == 0
          ? "The news asks when the screen opens and when the viewer says so"
          : "The news asks every " + value.Minutes + (value.Minutes == 1 ? " minute" : " minutes");
      Status = wasSaved ? answer : answer + ", but " + saved;
    }

    private static int OptionFor(int minutes)
    {
      for (int i = 0; i < NewsFeedsSettings.INTERVALS.Length; i++)
      {
        if (NewsFeedsSettings.INTERVALS[i] == minutes)
          return i;
      }

      // A file that names a rhythm between the answers keeps standing as it is written; the picker stands
      // on the default the screen itself would ask with.
      return 0;
    }

    private string SaveSettings(out bool saved)
    {
      return _settings.Save(_settingsPath, out saved);
    }
  }
}
