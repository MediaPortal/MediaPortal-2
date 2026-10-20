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
*/

using System;
using System.Collections.Generic;

namespace MediaPortal.Frontend.Skins.WMC
{
  /// <summary>
  /// One group of WMC's home menu: a main menu of the cross's vertical arm, holding the submenus that form its
  /// horizontal arm while the group is stood on.
  /// </summary>
  /// <remarks>
  /// The reference spells this <c>HomeMenuGroup</c> (Incubator's HomeEditor) and builds the standing set from
  /// <c>DefaultGroups.Create()</c>. The name is prefixed here because the two rescued skins each arrange the home
  /// menu their own way - WMC into groups for its cross, Nereus into a flat rail with per-entry content - and those
  /// two arrangements are skin property, not base property, so nothing of one leaks into the other or into the
  /// shared front end.
  /// </remarks>
  public sealed class WmcHomeMenuGroup
  {
    public WmcHomeMenuGroup(string label)
    {
      Label = label;
    }

    /// <summary>What the vertical arm shows for this group: the leading action's own word.</summary>
    public string Label { get; }

    /// <summary>The group's submenus, in the reference's own order - the horizontal arm when this group is stood on.</summary>
    public List<ViewModels.MainMenuEntry> Entries { get; } = new List<ViewModels.MainMenuEntry>();
  }

  /// <summary>
  /// WMC's own home menu model - the grouping and the two cursors the cross walks. Ported from the reference
  /// <c>WMC\Models\HomeMenuModel.cs</c> and the default group table of
  /// <c>MediaPortal\Incubator\HomeEditor\Groups\DefaultGroups.cs</c>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A skin is not only its pictures: each rescued skin ships its home <i>model</i> too, and this is WMC's.
  /// The reference cross stands the <b>main</b> menus up and down (<c>MainMenu</c> ← <c>NestedMenuItems</c>, the
  /// groups; Up/Down walk <c>_navigationList</c>) and the <b>sub</b>menus of the standing group left and right
  /// (<c>SubItemsListViewHome</c> ← <c>SubItems</c>; Left/Right call <c>MoveNextSubItem</c>/<c>MovePreviousSubItem</c>).
  /// Both walks clamp at their ends - the reference has no wrap at either the top or the bottom of the groups or of
  /// a group's subitems - and every group remembers the submenu it was last left standing on
  /// (<c>KEY_ITEM_SELECTED_ACTION_ID</c>), so a walk back returns to where the viewer was, not to the group's first
  /// entry. All three behaviors live here and nowhere else.
  /// </para>
  /// <para>
  /// Nereus does not group its home at all (its own <c>Nereus\Models\HomeMenuModel.cs</c> is a flat list whose
  /// entries carry their content), so none of this is offered to it and none of it belongs to the base front end:
  /// the flat <c>Menu</c> in <c>HomeViewModel</c> stays the shared fact, and this class is one skin's reading of it.
  /// The model only ever reads entries and asks for one to become the selected one (<c>selectEntry</c>), so the
  /// pane, the fanart and Enter keep answering to the same selection the cross walks - WMC moves the cursor, the
  /// front end keeps the meaning.
  /// </para>
  /// </remarks>
  public sealed class WmcHomeMenuModel
  {
    /// <param name="menu">The flat home menu as the front end built it; the model groups it, never edits it.</param>
    /// <param name="selectedEntry">The entry the front end selects right now - the same one the pane and fanart answer to.</param>
    /// <param name="selectEntry">Makes one entry the selection - the same door a key press in the list walks through.</param>
    public WmcHomeMenuModel(Func<IList<ViewModels.MainMenuEntry>> menu,
        Func<ViewModels.MainMenuEntry?> selectedEntry, Action<ViewModels.MainMenuEntry?> selectEntry)
    {
      _menu = menu;
      _selectedEntry = selectedEntry;
      _selectEntry = selectEntry;
    }

    private readonly Func<IList<ViewModels.MainMenuEntry>> _menu;
    private readonly Func<ViewModels.MainMenuEntry?> _selectedEntry;
    private readonly Action<ViewModels.MainMenuEntry?> _selectEntry;

    private readonly List<WmcHomeMenuGroup> _groups = new List<WmcHomeMenuGroup>();

    /// <summary>The entry last stood on within each group - the reference's per-group remembered action.</summary>
    private readonly Dictionary<WmcHomeMenuGroup, ViewModels.MainMenuEntry> _rememberedSubItem =
        new Dictionary<WmcHomeMenuGroup, ViewModels.MainMenuEntry>();

    /// <summary>WMC's main menus: the groups, in the reference's order, empty ones dropped, "Others" last.</summary>
    public IReadOnlyList<WmcHomeMenuGroup> Groups { get { return _groups; } }

    /// <summary>The group the vertical arm stands on.</summary>
    public WmcHomeMenuGroup? SelectedGroup { get; private set; }

    /// <summary>The standing group's submenus - the horizontal arm, and what left and right walk.</summary>
    public IReadOnlyList<ViewModels.MainMenuEntry> SubItems
    {
      get { return SelectedGroup?.Entries ?? (IReadOnlyList<ViewModels.MainMenuEntry>)Array.Empty<ViewModels.MainMenuEntry>(); }
    }

    /// <summary>
    /// WMC's default groups, in the reference's own order, each keyed by the workflow Id of its leading action and
    /// holding the Ids of every action that belongs to it - a line-for-line copy of <c>DefaultGroups.Create()</c>
    /// restricted to the actions this front end's menu can carry (the TV and radio groups' guide, recording and
    /// search actions belong to a TV backend this port does not grow, so they have no line here). "Others" is not
    /// in the table: like the reference, it is assembled last from whatever the groups above left unplaced.
    /// </summary>
    private static readonly (string Leader, string[] Members)[] GROUP_TABLE =
    {
      // Pictures - the reference's very first group, its one action the images shelf.
      ("55556593-9fe9-436c-a3b6-a971e10c9d44",
          new[] { "55556593-9fe9-436c-a3b6-a971e10c9d44" }),
      // Music - the album shelf and the party player.
      ("30715d73-4205-417f-80aa-e82f0834171f",
          new[] { "30715d73-4205-417f-80aa-e82f0834171f", "e00b8442-8230-4d7b-b871-6ac77755a0d5" }),
      // Video - every shelf that shows moving pictures, the online shelf included where a server contributes it.
      ("a4df2df6-8d66-479a-9930-d7106525eb07",
          new[] { "a4df2df6-8d66-479a-9930-d7106525eb07", "80d2e2cc-baaa-4750-807b-f37714153751",
              "30f57cba-459c-4202-a587-09fff5098251", "c33e39cc-910e-41c8-bffd-9eccd340b569",
              "93442df7-186d-42e5-a0f5-cf1493e68f49" }),
      // TV - the live television action; its guide and recordings siblings wait for a TV backend.
      ("b4a9199f-6dd4-4bda-a077-de9c081f7703",
          new[] { "b4a9199f-6dd4-4bda-a077-de9c081f7703" }),
      // Radio - same shape as TV.
      ("e3bbc989-99db-40e9-a15f-ccb50b17a4c8",
          new[] { "e3bbc989-99db-40e9-a15f-ccb50b17a4c8" }),
      // News - the headlines and the weather under one main menu.
      ("bb49a591-7705-408f-8177-45d633fdfad0",
          new[] { "bb49a591-7705-408f-8177-45d633fdfad0", "e34fdb62-1f3e-4aa9-8a61-d143e0af77b5" }),
      // Configuration - the settings door; shutdown and the skin settings would join it as their screens arrive.
      ("f6255762-c52a-4231-9e67-14c28735216e",
          new[] { "f6255762-c52a-4231-9e67-14c28735216e" }),
    };

    /// <summary>
    /// Re-lays the groups from the flat menu, keeping the group that was stood on stood on. Called whenever the
    /// menu is rebuilt, the reference's model doing the same whenever its actions were re-read. Never moves the
    /// selection: the selection is the fact, and the standing group is aligned to wherever it already is.
    /// </summary>
    public void Rebuild(ViewModels.MainMenuEntry? selected)
    {
      string? wasGroup = SelectedGroup?.Label;
      _rememberedSubItem.Clear();
      _groups.Clear();

      IList<ViewModels.MainMenuEntry> menu = _menu();
      var placed = new HashSet<ViewModels.MainMenuEntry>();
      foreach ((string leader, string[] members) in GROUP_TABLE)
      {
        var collected = new List<ViewModels.MainMenuEntry>();
        foreach (string guid in members)
        {
          ViewModels.MainMenuEntry? match = FindByHomeImageId(menu, guid);
          if (match != null && placed.Add(match))
            collected.Add(match);
        }

        if (collected.Count == 0)
          continue;

        // A group is named by its leading action, exactly as the reference constructs each HomeMenuGroup with the
        // same language key as its first action; where the leader is not itself on this menu, the first member names it.
        ViewModels.MainMenuEntry leading = FindByHomeImageId(menu, leader) ?? collected[0];
        var group = new WmcHomeMenuGroup(leading.Label);
        group.Entries.AddRange(collected);
        _groups.Add(group);
      }

      // "Others" last, holding every entry the groups above left unplaced - the reference's OthersMenuItem group,
      // which its own model likewise appends behind the configured ones.
      var others = new WmcHomeMenuGroup("Others");
      foreach (ViewModels.MainMenuEntry entry in menu)
      {
        if (placed.Add(entry))
          others.Entries.Add(entry);
      }
      if (others.Entries.Count > 0)
        _groups.Add(others);

      // Stand on the group that holds the selection; failing that on the group that was stood on before the
      // rebuild; failing that on the first. Aligning rather than navigating, so the selection stays where it is.
      WmcHomeMenuGroup? stand = null;
      foreach (WmcHomeMenuGroup group in _groups)
      {
        if (selected != null && group.Entries.Contains(selected))
        {
          stand = group;
          break;
        }
      }
      if (stand == null)
      {
        foreach (WmcHomeMenuGroup group in _groups)
        {
          if (group.Label == wasGroup)
          {
            stand = group;
            break;
          }
        }
      }
      SelectedGroup = stand ?? (_groups.Count > 0 ? _groups[0] : null);
    }

    /// <summary>
    /// Stepping the vertical arm one group up or down - the reference's Up/Down on <c>_navigationList</c> -
    /// remembering where in the group being left the viewer stood. Clamped, not wrapped, like the reference.
    /// </summary>
    public void MoveGroup(int delta)
    {
      if (_groups.Count == 0)
        return;

      int standing = SelectedGroup == null ? -1 : _groups.IndexOf(SelectedGroup);
      if (standing < 0)
      {
        SelectedGroup = _groups[0];
        _selectEntry(_groups[0].Entries.Count > 0 ? _groups[0].Entries[0] : null);
        return;
      }

      int stepped = standing + delta;
      if (stepped < 0 || stepped >= _groups.Count)
        return;

      if (_selectedEntry() is ViewModels.MainMenuEntry was && _groups[standing].Entries.Contains(was))
        _rememberedSubItem[_groups[standing]] = was;

      SelectedGroup = _groups[stepped];

      // Navigating onto a group lands on the submenu last stood on in it, or its first - the reference restored
      // the group's remembered action whenever a new group was selected.
      _rememberedSubItem.TryGetValue(_groups[stepped], out ViewModels.MainMenuEntry? remembered);
      if (remembered == null || !_groups[stepped].Entries.Contains(remembered))
        remembered = _groups[stepped].Entries.Count > 0 ? _groups[stepped].Entries[0] : null;
      _selectEntry(remembered);
    }

    /// <summary>
    /// Stepping the horizontal arm one submenu left or right within the standing group - the reference's
    /// <c>MoveNextSubItem</c>/<c>MovePreviousSubItem</c>. Clamped at the group's ends rather than wrapping.
    /// </summary>
    public void MoveSubItem(int delta)
    {
      IReadOnlyList<ViewModels.MainMenuEntry> subItems = SubItems;
      if (subItems.Count == 0)
        return;

      ViewModels.MainMenuEntry? current = _selectedEntry();
      int standing = current == null ? -1 : IndexOf(subItems, current);
      if (standing < 0)
      {
        _selectEntry(subItems[0]);
        return;
      }

      int stepped = standing + delta;
      if (stepped < 0 || stepped >= subItems.Count)
        return;

      _selectEntry(subItems[stepped]);
    }

    /// <summary>
    /// Points the vertical arm at the group holding the current selection, without laying the groups out again -
    /// the answer the view asks for when it picked a standing entry the model had not been aligned to (a menu
    /// that appeared while the cross was already worn). Navigation never goes through here.
    /// </summary>
    public void AlignToSelection()
    {
      ViewModels.MainMenuEntry? selected = _selectedEntry();
      if (selected == null)
        return;
      foreach (WmcHomeMenuGroup group in _groups)
      {
        if (group.Entries.Contains(selected))
        {
          SelectedGroup = group;
          return;
        }
      }
    }

    /// <summary>Where an item stands in a read-only list - the lists here are small, and a walk is all the search they need.</summary>
    private static int IndexOf<T>(IReadOnlyList<T> items, T item)
    {
      for (int i = 0; i < items.Count; i++)
      {
        if (ReferenceEquals(items[i], item))
          return i;
      }
      return -1;
    }

    private static ViewModels.MainMenuEntry? FindByHomeImageId(IList<ViewModels.MainMenuEntry> menu, string guid)
    {
      foreach (ViewModels.MainMenuEntry entry in menu)
      {
        if (string.Equals(entry.HomeImageId, guid, StringComparison.OrdinalIgnoreCase))
          return entry;
      }
      return null;
    }
  }
}
