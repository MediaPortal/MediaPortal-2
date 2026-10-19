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
  /// One row of the middle column: a way of grouping a shelf, in the server's own word and the viewer's.
  /// </summary>
  public sealed class BrowseCategory
  {
    public BrowseCategory(string word, string label)
    {
      Word = word;
      Label = label;
    }

    /// <summary>What the server calls this grouping; the <c>by</c> of the two endpoints that do the work.</summary>
    public string Word { get; }

    /// <summary>What the screen calls it.</summary>
    public string Label { get; }
  }

  /// <summary>
  /// One row of the right-hand column: a value a shelf can be grouped under, and how many titles carry it.
  /// </summary>
  public sealed class BrowseValue
  {
    public BrowseValue(string value, int count)
    {
      Value = value;
      Count = count;
    }

    /// <summary>The value as the database stored it - a genre name, a year, a person's name.</summary>
    public string Value { get; }

    /// <summary>How many titles in this shelf carry it, counted by the server rather than guessed at here.</summary>
    public int Count { get; }

    /// <summary>How the row reads: the value, and the number that justifies it being a row at all.</summary>
    public string Text
    {
      get { return Value + "  ·  " + Count.ToString("N0") + (Count == 1 ? " title" : " titles"); }
    }
  }

  /// <summary>
  /// The browse screen: a shelf, a way of grouping it, and the values that grouping takes in this library.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The old MP2 browse tree walked a hierarchy the importer had built - genre nodes, year nodes, person nodes,
  /// each a item in its own right - and none of that exists over HTTP, because MP2's current API stores media as
  /// titles carrying attributes rather than as a folder tree of category items. What does exist is the server's
  /// own grouping machinery, the one its skins' filter dialogs always used: ask which genres a shelf holds and
  /// how many titles carry each, pick one, and ask for the titles that have it. Both halves are one database
  /// query each; this screen is that, and nothing else.
  /// </para>
  /// <para>
  /// <b>The five groupings are the server's list, not a discovery.</b> Genre, year, actors, directors, writers -
  /// what <c>GET api/media/groups</c> answers for, and the same five the old filter dialogs offered. A shelf
  /// whose titles carry none of them (pictures have no genre, music has no director) answers with no groups, and
  /// the status line says so plainly instead of leaving an empty column to be read as a broken screen.
  /// </para>
  /// <para>
  /// <b>The counts are the server's.</b> A shelf of four thousand films is not fetched sixty pages deep to count
  /// its genres here; the database answers with the number, and this screen repeats it. That is also why the
  /// right-hand column arrives in the server's order - biggest group first - and is not re-sorted: the order is
  /// the answer's, and re-sorting it alphabetically would hide the fact that this library is mostly one genre.
  /// </para>
  /// <para>
  /// Pressing a value opens the ordinary titles screen wearing that filter - the same list, the same keys, the
  /// same play - so browsing is a way <i>into</i> the library rather than a separate room with its own rules.
  /// </para>
  /// </remarks>
  public partial class BrowseViewModel : ObservableObject
  {
    /// <summary>
    /// The five groupings, in the order the server lists them when it refuses an unknown one. Kept here in the
    /// open because the screen shows exactly these and no others: a grouping discovered from the schema would
    /// offer things like "recording time" that a viewer would never press.
    /// </summary>
    public static readonly IReadOnlyList<BrowseCategory> CATEGORIES = new List<BrowseCategory>
    {
        new BrowseCategory("genre", "By genre"),
        new BrowseCategory("year", "By year"),
        new BrowseCategory("actors", "By actor"),
        new BrowseCategory("directors", "By director"),
        new BrowseCategory("writers", "By writer")
    };

    private readonly ServerApiClient? _client;
    private CancellationTokenSource? _running;

    /// <summary>
    /// Whether the screen is up. The constructor picks the first shelf and the first grouping so that the columns
    /// are not blank when the screen appears, and those two picks must not start two counts before the screen
    /// that would watch them exists; the window asks for the first read itself when it opens, and only changes
    /// after that are worth a request.
    /// </summary>
    private bool _ready;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    private HomeSection? _selectedShelf;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    private BrowseCategory? _selectedCategory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    private BrowseValue? _selectedValue;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _status = "Not read yet.";

    /// <param name="client">The server to count, or null for a screen that says it has none.</param>
    /// <param name="shelves">
    /// The shelves on offer, which are the home screen's tiles - the aspects a viewer sits down to watch - rather
    /// than every aspect the server's schema happens to contain.
    /// </param>
    public BrowseViewModel(ServerApiClient? client = null, IEnumerable<HomeSection>? shelves = null)
    {
      _client = client;
      if (shelves != null)
        foreach (HomeSection shelf in shelves)
          Shelves.Add(shelf);
      foreach (BrowseCategory category in CATEGORIES)
        Categories.Add(category);
      // The viewer's hand is already on the arrows when this screen opens; a screen that makes them pick a shelf
      // and a category before the first arrow press does anything is three presses of setup.
      SelectedShelf = Shelves.Count > 0 ? Shelves[0] : null;
      SelectedCategory = CATEGORIES[0];
      _ready = true;
      if (_client == null)
        Status = "No server configured";
      else if (Shelves.Count == 0)
        Status = "This server has no titles to browse yet";
    }

    /// <summary>The shelves on offer: what the home screen had tiles for.</summary>
    public ObservableCollection<HomeSection> Shelves { get; } = new ObservableCollection<HomeSection>();

    /// <summary>The five groupings, always all five; a shelf that cannot answer one says so in the third column.</summary>
    public ObservableCollection<BrowseCategory> Categories { get; } = new ObservableCollection<BrowseCategory>();

    /// <summary>
    /// The values the picked grouping takes in the picked shelf, biggest first. Filled by <see cref="ReadAsync"/>,
    /// so a column that is empty either says why in the status line or is genuinely a shelf nobody has tagged.
    /// </summary>
    public ObservableCollection<BrowseValue> Values { get; } = new ObservableCollection<BrowseValue>();

    /// <summary>
    /// Raised when a viewer pressed a value: the application owns the windows, so it decides that this means
    /// opening the titles screen filtered to what was picked; this class only says which shelf, grouping and
    /// value made the section it hands over.
    /// </summary>
    public event EventHandler<HomeSection>? ValueChosen;

    private bool CanOpen
    {
      get { return SelectedShelf != null && SelectedCategory != null && SelectedValue != null; }
    }

    /// <summary>
    /// The picked grouping's word with the plural it actually takes: genres and years add one, and the three
    /// person groupings already are plural.
    /// </summary>
    private string PluralWord
    {
      get
      {
        string word = SelectedCategory?.Word ?? "";
        if (word.Equals("genre", StringComparison.OrdinalIgnoreCase))
          return "genres";
        if (word.Equals("year", StringComparison.OrdinalIgnoreCase))
          return "years";
        return word;
      }
    }

    partial void OnSelectedShelfChanged(HomeSection? value)
    {
      if (value != null && _ready)
        _ = RefreshCommand.ExecuteAsync(null);
    }

    partial void OnSelectedCategoryChanged(BrowseCategory? value)
    {
      if (value != null && _ready)
        _ = RefreshCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Count the picked shelf under the picked grouping. The window asks for this when it opens, and every change
    /// of shelf or grouping asks for it again by itself.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
      if (_client == null)
      {
        Status = "No server configured";
        return;
      }
      if (SelectedShelf == null || SelectedCategory == null)
        return;

      // One count at a time, for the same reason the titles list cancels: the arrow key held down on the category
      // column would otherwise let five answers arrive in any order and leave the last-written one not the last-
      // picked.
      CancellationTokenSource? previous = _running;
      CancellationTokenSource cancellation = new CancellationTokenSource();
      _running = cancellation;
      previous?.Cancel();

      IsBusy = true;
      try
      {
        MediaGroups answer = await _client.GetGroupsAsync(SelectedShelf.Aspect.Name, SelectedCategory.Word,
            cancellation.Token);

        Values.Clear();
        foreach (MediaGroup group in answer.Values)
          Values.Add(new BrowseValue(group.Value!, group.Count));
        SelectedValue = Values.Count > 0 ? Values[0] : null;

        if (!string.IsNullOrEmpty(answer.Error))
          Status = answer.Error;
        else if (Values.Count == 0)
        {
          // The honest nothing: the shelf exists and the grouping exists, but no title in it carries a value.
          // Naming both halves says which of the two the viewer is short of.
          Status = "None of the " + SelectedShelf.Label + " carries a " + SelectedCategory.Word +
              " the importer wrote.";
        }
        else
        {
          // Plural by hand because the grouping words are irregular between themselves - "genre" adds an s,
          // "actors" already has one - and "2 genre" is the sort of slip a screen should not make in its first
          // sentence.
          Status = Values.Count + " " + PluralWord + " among " + SelectedShelf.Label +
              ", biggest first - press OK for the titles of the selected one";
        }
      }
      catch (ServerApiException ex)
      {
        if (ex.StatusCode == 403)
          Status = SelectedShelf.Label + " is only for signed-in users on this server. Sign in on the server screen.";
        else if (ex.NotReady)
          Status = "The server is not ready yet: " + ex.Message;
        else
          Status = ex.Message;
      }
      catch (OperationCanceledException)
      {
        // A newer count was asked for; it reports its own status.
      }
      catch (Exception ex)
      {
        Status = "Counting failed: " + ex.Message;
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

    /// <summary>Opens the picked value as the titles screen, filtered to it.</summary>
    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
      HomeSection? shelf = SelectedShelf;
      BrowseCategory? category = SelectedCategory;
      BrowseValue? value = SelectedValue;
      if (shelf == null || category == null || value == null)
        return;

      // The section that opens is the shelf's own - same label, same order its tile promised - wearing the picked
      // grouping, so the titles screen says "Movies under Action" rather than silently showing a different list
      // under the same heading as the tile.
      ValueChosen?.Invoke(this, new HomeSection(shelf.Label, shelf.Aspect, shelf.Sort, shelf.SortDescending,
          category.Word, value.Value));
    }
  }
}
