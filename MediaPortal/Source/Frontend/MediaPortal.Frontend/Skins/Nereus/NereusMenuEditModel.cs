using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Skins.Nereus
{
  /// <summary>The button of an editor row that the skin is asked to hand the focus to.</summary>
  public enum NereusMenuEditButton
  {
    Up,
    Down,
    Remove,
    Config
  }

  /// <summary>
  /// Says which row's button the editor wants focused, and in which of its two lists that row is standing.
  /// </summary>
  public sealed class NereusMenuEditFocusEventArgs : EventArgs
  {
    internal NereusMenuEditFocusEventArgs(NereusMenuEditRow row, NereusMenuEditButton button, bool isMediaList)
    {
      Row = row;
      Button = button;
      IsMediaList = isMediaList;
    }

    /// <summary>The row whose button is wanted.</summary>
    public NereusMenuEditRow Row { get; }

    /// <summary>Which of the row's buttons is wanted.</summary>
    public NereusMenuEditButton Button { get; }

    /// <summary>Whether the row belongs to the media list editor rather than to the menu editor.</summary>
    public bool IsMediaList { get; }
  }

  /// <summary>
  /// One line of the home menu editor: a tile of the home menu, or one media list of a tile.
  /// </summary>
  /// <remarks>
  /// MP2 gave both dialogs the same face - <c>Skin\Nereus\themes\default\styles\EditMenuStyles.xaml</c> - and told
  /// the two uses apart by flags kept in <c>ListItem.AdditionalProperties</c>. Here the flags are properties of a
  /// line and there is still only one kind of line, which is why the row is written once: a line names an action
  /// when it is a tile, and names a list key on top when it is one of a tile's media lists.
  /// </remarks>
  public sealed partial class NereusMenuEditRow : ObservableObject
  {
    private readonly NereusMenuEditModel _model;

    internal NereusMenuEditRow(NereusMenuEditModel model, Guid actionId, string? listKey, string label,
        bool canConfigure)
    {
      _model = model;
      ActionId = actionId;
      ListKey = listKey;
      Label = label;
      IsUpEnabled = true;
      IsDownEnabled = true;
      IsConfigEnabled = canConfigure;
    }

    /// <summary>The home menu action this line is about. A media list line names the tile its list belongs to.</summary>
    public Guid ActionId { get; }

    /// <summary>
    /// The key the settings file stores this list under, or null when the line is a tile of the home menu. It is
    /// also the one thing the editor needs to tell its two lists apart.
    /// </summary>
    public string? ListKey { get; }

    /// <summary>What the line says, in the words the tile itself uses.</summary>
    public string Label { get; }

    /// <summary>Whether the line can be moved up, which is everywhere but where it already stands at the top.</summary>
    [ObservableProperty]
    private bool _isUpEnabled;

    /// <summary>Whether the line can be moved down, which is everywhere but where it already stands at the bottom.</summary>
    [ObservableProperty]
    private bool _isDownEnabled;

    /// <summary>Whether the line has media lists to choose, which is what the reference read off the home content.</summary>
    [ObservableProperty]
    private bool _isConfigEnabled;

    /// <summary>The four buttons the skin is asked to put the focus on; see <see cref="NereusMenuEditModel.FocusRequested"/>.</summary>
    [ObservableProperty]
    private bool _isUpFocused;

    [ObservableProperty]
    private bool _isDownFocused;

    [ObservableProperty]
    private bool _isRemoveFocused;

    [ObservableProperty]
    private bool _isConfigFocused;

    [RelayCommand]
    private void MoveUp()
    {
      _model.MoveUp(this);
    }

    [RelayCommand]
    private void MoveDown()
    {
      _model.MoveDown(this);
    }

    [RelayCommand]
    private void Remove()
    {
      _model.Remove(this);
    }

    [RelayCommand]
    private void Config()
    {
      _model.Config(this);
    }
  }

  /// <summary>
  /// The home menu editor's model, moved over from Nereus' own <c>Models\MenuEditModel.cs</c>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Everything the reference model could do, it can do, and in the same way: the tiles a viewer arranged are kept
  /// in order, a tile can be moved up or down, taken out, or opened to say which of its media lists the content
  /// pane may pull previews from, and a tile that was taken out can be put back at the end. The flags the reference
  /// kept per line for its face - which button is enabled, which one is to be focused next - are the properties of
  /// <see cref="NereusMenuEditRow"/>, set by the same rules in the same places.
  /// </para>
  /// <para>
  /// Three things are spelled differently here because this front end spells them differently: the catalogue of
  /// tiles is the home menu's own entries rather than the workflow's actions for the home state, a tile's lists
  /// come from <see cref="HomeContentList.AvailableFor"/> rather than from an <c>AbstractHomeContent</c>, and
  /// nothing is written to the settings file - the reference wrote when the dialog was closed with OK, and so does
  /// the code in front of this model, once, for both settings together.
  /// </para>
  /// </remarks>
  public sealed partial class NereusMenuEditModel : ObservableObject
  {
    private readonly List<NereusMenuEditRow> _catalogue = new List<NereusMenuEditRow>();
    private readonly Dictionary<Guid, NereusMenuEditRow> _rowsByActionId = new Dictionary<Guid, NereusMenuEditRow>();
    private readonly List<NereusMenuEditRow> _allMediaListRows = new List<NereusMenuEditRow>();
    private readonly Dictionary<Guid, List<string>> _mediaLists;

    /// <param name="available">
    /// Every home menu entry the front end can offer, in the order the home menu itself lists them. Entries without
    /// an action id behind them are not tiles a viewer can arrange, so the editor does not pretend otherwise.
    /// </param>
    /// <param name="configuredActionIds">The tiles as the viewer arranged them, which is what is being edited.</param>
    /// <param name="startActionId">The tile the viewer was standing on when the editor was asked for.</param>
    /// <param name="configuredMediaLists">
    /// The stored <c>"&lt;action id&gt;:&lt;key&gt;,&lt;key&gt;"</c> lines, read straight out of
    /// <c>NereusSkinSettings.HomeMenuActionMediaLists</c>.
    /// </param>
    public NereusMenuEditModel(IEnumerable<MainMenuEntry> available, IEnumerable<Guid> configuredActionIds,
        Guid? startActionId, IEnumerable<string>? configuredMediaLists)
    {
      foreach (MainMenuEntry entry in available)
      {
        if (!Guid.TryParse(entry.HomeImageId, out Guid actionId) || _rowsByActionId.ContainsKey(actionId))
          continue;

        NereusMenuEditRow row = new NereusMenuEditRow(this, actionId, null, entry.Label,
            HomeContentList.AvailableFor(actionId).Count > 0);
        _catalogue.Add(row);
        _rowsByActionId[actionId] = row;
      }

      _mediaLists = ReadMediaLists(configuredMediaLists);
      StartActionId = startActionId;
      LoadRows(configuredActionIds);
    }

    /// <summary>The tiles being edited, in the order the viewer put them there.</summary>
    public ObservableCollection<NereusMenuEditRow> Rows { get; } = new ObservableCollection<NereusMenuEditRow>();

    /// <summary>The tiles that are not on the menu, which is what "add menu item" offers.</summary>
    public ObservableCollection<NereusMenuEditRow> OtherRows { get; } = new ObservableCollection<NereusMenuEditRow>();

    /// <summary>The lists the open tile may show, in the order the content pane reads them.</summary>
    public ObservableCollection<NereusMenuEditRow> MediaListRows { get; } = new ObservableCollection<NereusMenuEditRow>();

    /// <summary>The lists the open tile is not showing, which is what "add home content" offers.</summary>
    public ObservableCollection<NereusMenuEditRow> OtherMediaListRows { get; } =
        new ObservableCollection<NereusMenuEditRow>();

    /// <summary>Whether "add menu item" has anything to offer.</summary>
    [ObservableProperty]
    private bool _hasOtherRows;

    /// <summary>Whether "add home content" has anything to offer.</summary>
    [ObservableProperty]
    private bool _hasOtherMediaListRows;

    /// <summary>
    /// The tile the viewer was standing on when the editor was asked for. The reference opened its dialog with that
    /// line selected and its up button ready, so that the viewer did not have to walk to the tile they meant.
    /// </summary>
    public Guid? StartActionId { get; }

    /// <summary>The tile the media list editor is about, once one has been opened.</summary>
    public Guid SelectedActionId { get; private set; }

    /// <summary>What the tile the media list editor is about is called, for that dialog's heading.</summary>
    public string? SelectedLabel { get; private set; }

    /// <summary>
    /// Raised whenever the list moved underneath the viewer and the skin has to put the focus back onto the button
    /// they are pressing. The reference needed no such notice - its face was bound to the focus flag of the line and
    /// MP2's GUI engine polled it - while here a button has to be asked for the focus once, by name.
    /// </summary>
    public event EventHandler<NereusMenuEditFocusEventArgs>? FocusRequested;

    /// <summary>
    /// Raised when a tile's configure button was pressed, which is the reference's <c>Config</c> opening
    /// <c>DialogEditMediaListMenu</c>. The model only says that the lists of this tile are wanted; which screen
    /// that is, and how the viewer gets back from it, is what the screen in front of it decides.
    /// </summary>
    public event EventHandler? MediaListEditRequested;

    /// <summary>The tiles as they stand now, in their order, which is what the settings file stores.</summary>
    public IList<Guid> CurrentActionIds()
    {
      return Rows.Select(row => row.ActionId).ToList();
    }

    /// <summary>
    /// The media lists as they stand now, written the way the reference wrote them: one line per tile, the tile's
    /// id, a colon, and the list keys comma separated. A tile whose lists were all taken out is written as a colon
    /// with nothing behind it, which is how the reference said "this tile shows nothing" - leaving the tile out
    /// entirely would mean "every list", which is the opposite answer.
    /// </summary>
    public string[] MediaListSettings()
    {
      return _mediaLists.Select(pair => pair.Key + ":" + string.Join(",", pair.Value)).ToArray();
    }

    #region Tiles

    /// <summary>
    /// Puts the open dialog's lines on the given tile's lists, which is what the reference did every time a tile was
    /// opened for editing - so a media list dialog left without saving has no effect on the next opening.
    /// </summary>
    public bool Config(NereusMenuEditRow row)
    {
      int index = Rows.IndexOf(row);
      if (index < 0 || !row.IsConfigEnabled)
        return false;

      SelectedActionId = row.ActionId;
      SelectedLabel = row.Label;
      ResetRowProperties(Rows);
      row.IsConfigFocused = true;
      AskForFocus(row, NereusMenuEditButton.Config, false);
      LoadMediaListRows();
      MediaListEditRequested?.Invoke(this, EventArgs.Empty);
      return true;
    }

    /// <summary>Moves a tile one place up, or does nothing where the tile already stands at the top.</summary>
    public void MoveUp(NereusMenuEditRow row)
    {
      ObservableCollection<NereusMenuEditRow> rows = ListFor(row);
      int index = rows.IndexOf(row);
      if (index < 1)
        return;

      rows.RemoveAt(index);
      rows.Insert(index - 1, row);
      ResetRowProperties(rows);
      if (index > 1)
        row.IsUpFocused = true;
      else
        row.IsDownFocused = true;
      AskForFocus(row, index > 1 ? NereusMenuEditButton.Up : NereusMenuEditButton.Down, row.ListKey != null);
    }

    /// <summary>Moves a tile one place down, or does nothing where the tile already stands at the bottom.</summary>
    public void MoveDown(NereusMenuEditRow row)
    {
      ObservableCollection<NereusMenuEditRow> rows = ListFor(row);
      int index = rows.IndexOf(row);
      if (index < 0 || index > rows.Count - 2)
        return;

      rows.RemoveAt(index);
      rows.Insert(index + 1, row);
      ResetRowProperties(rows);
      if (index < rows.Count - 2)
        row.IsDownFocused = true;
      else
        row.IsUpFocused = true;
      AskForFocus(row, index < rows.Count - 2 ? NereusMenuEditButton.Down : NereusMenuEditButton.Up,
          row.ListKey != null);
    }

    /// <summary>Puts a tile at the end of the menu, unless it is on the menu already.</summary>
    public void Add(NereusMenuEditRow row)
    {
      ObservableCollection<NereusMenuEditRow> rows = ListFor(row);
      if (rows.Contains(row))
        return;

      rows.Add(row);
      ResetRowProperties(rows);
      UpdateOtherRows(row.ListKey != null);
    }

    /// <summary>
    /// Takes a tile out of the menu, and hands the focus to the remove button of the tile that took its place -
    /// the neighbour below, or the one above where the bottom tile was removed. A viewer who is clearing tiles one
    /// after the other never has to look for the button again.
    /// </summary>
    public void Remove(NereusMenuEditRow row)
    {
      ObservableCollection<NereusMenuEditRow> rows = ListFor(row);
      int index = rows.IndexOf(row);
      if (index < 0)
        return;

      rows.Remove(row);
      if (index >= rows.Count && rows.Count > 0)
        index = rows.Count - 1;
      ResetRowProperties(rows);
      if (index >= 0 && index < rows.Count)
        rows[index].IsRemoveFocused = true;
      if (index >= 0 && index < rows.Count)
        AskForFocus(rows[index], NereusMenuEditButton.Remove, row.ListKey != null);
      UpdateOtherRows(row.ListKey != null);
    }

    /// <summary>
    /// Lays the tiles out from stored ids, keeping only the ones this front end can actually offer. A stored id
    /// whose plugin is gone is skipped rather than shown as a dead tile, and a tile named twice is named once.
    /// </summary>
    private void LoadRows(IEnumerable<Guid> configuredActionIds)
    {
      Rows.Clear();
      HashSet<Guid> seen = new HashSet<Guid>();
      foreach (Guid actionId in configuredActionIds)
      {
        if (!seen.Add(actionId) || !_rowsByActionId.TryGetValue(actionId, out NereusMenuEditRow? row))
          continue;
        Rows.Add(row);
      }

      ResetRowProperties(Rows);

      if (StartActionId.HasValue && _rowsByActionId.TryGetValue(StartActionId.Value, out NereusMenuEditRow? start)
          && Rows.Contains(start))
      {
        start.IsUpFocused = true;
      }

      UpdateOtherRows(false);
    }

    private void UpdateOtherRows(bool mediaList)
    {
      if (mediaList)
      {
        UpdateOtherMediaListRows();
        return;
      }

      OtherRows.Clear();
      foreach (NereusMenuEditRow row in _catalogue)
      {
        if (!Rows.Contains(row))
          OtherRows.Add(row);
      }
      HasOtherRows = OtherRows.Count > 0;
    }

    #endregion

    #region Media lists

    /// <summary>
    /// Fills the media list dialog with the tile's own lists. A tile the viewer never touched is shown with all of
    /// its lists, because that is what the content pane reads from it - the stored line only exists once somebody
    /// has chosen, and until then "nothing stored" means "everything", exactly as in the reference.
    /// </summary>
    private void LoadMediaListRows()
    {
      _allMediaListRows.Clear();
      foreach (HomeContentList list in HomeContentList.AvailableFor(SelectedActionId))
        _allMediaListRows.Add(new NereusMenuEditRow(this, SelectedActionId, list.Key, list.Heading, false));

      MediaListRows.Clear();
      if (_mediaLists.TryGetValue(SelectedActionId, out List<string>? keys))
      {
        foreach (string key in keys)
        {
          NereusMenuEditRow? row = _allMediaListRows.FirstOrDefault(candidate => candidate.ListKey == key);
          if (row != null)
            MediaListRows.Add(row);
        }
      }
      else
      {
        foreach (NereusMenuEditRow row in _allMediaListRows)
          MediaListRows.Add(row);
      }

      ResetRowProperties(MediaListRows);
      UpdateOtherMediaListRows();
    }

    private void UpdateOtherMediaListRows()
    {
      OtherMediaListRows.Clear();
      foreach (NereusMenuEditRow row in _allMediaListRows)
      {
        if (!MediaListRows.Contains(row))
          OtherMediaListRows.Add(row);
      }
      HasOtherMediaListRows = OtherMediaListRows.Count > 0;
    }

    /// <summary>
    /// Keeps the lists the dialog shows for the tile it is about. The reference did this when the dialog was closed
    /// with OK and wrote nothing to disk; the disk is written once, later, when the whole editor is closed with OK.
    /// </summary>
    public void SaveMediaListEdit()
    {
      if (SelectedActionId == Guid.Empty)
        return;
      _mediaLists[SelectedActionId] = MediaListRows.Select(row => row.ListKey ?? "").ToList();
    }

    #endregion

    private ObservableCollection<NereusMenuEditRow> ListFor(NereusMenuEditRow row)
    {
      return row.ListKey == null ? Rows : MediaListRows;
    }

    /// <summary>
    /// Which buttons of a line are enabled, and which one of them is holding the focus: nowhere but the first line
    /// can a line go up, nowhere but the last can it go down, and a line that is not being moved holds no focus.
    /// These are the two lines the reference's <c>ResetActionItemProperties</c> wrote per item.
    /// </summary>
    private static void ResetRowProperties(IList<NereusMenuEditRow> rows)
    {
      for (int i = 0; i < rows.Count; i++)
      {
        NereusMenuEditRow row = rows[i];
        row.IsUpFocused = false;
        row.IsDownFocused = false;
        row.IsRemoveFocused = false;
        row.IsConfigFocused = false;
        row.IsUpEnabled = i > 0;
        row.IsDownEnabled = i < rows.Count - 1;
      }
    }

    private void AskForFocus(NereusMenuEditRow row, NereusMenuEditButton button, bool isMediaList)
    {
      FocusRequested?.Invoke(this, new NereusMenuEditFocusEventArgs(row, button, isMediaList));
    }

    /// <summary>
    /// Reads the stored lines into the dictionary the editor works with, key for key the way the reference's
    /// <c>GetActionMediaListDictionary</c> read them - including that a line ending in a colon is a tile with one
    /// nameless list behind it, which matches no list at all and so shows nothing.
    /// </summary>
    private static Dictionary<Guid, List<string>> ReadMediaLists(IEnumerable<string>? settings)
    {
      Dictionary<Guid, List<string>> mediaLists = new Dictionary<Guid, List<string>>();
      foreach (string setting in settings ?? Array.Empty<string>())
      {
        string[] parts = setting.Split(':');
        if (parts.Length == 2 && Guid.TryParse(parts[0].Trim(), out Guid actionId))
          mediaLists[actionId] = parts[1].Split(',').Select(key => key.Trim()).ToList();
      }
      return mediaLists;
    }
  }
}
