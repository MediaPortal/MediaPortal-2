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
using MediaPortal.Frontend.Skins.WMC;
using MediaPortal.Frontend.ViewModels;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Tests.Frontend
{
  /// <summary>
  /// WMC's own home menu model - its grouping and its two cursors - checked against the reference it was ported
  /// from (<c>WMC\Models\HomeMenuModel.cs</c> and <c>Incubator\HomeEditor\Groups\DefaultGroups.cs</c>): the groups
  /// are the vertical arm and come in the reference's order with "Others" last holding the ungrouped actions; the
  /// standing group's submenus are the horizontal arm; both walks clamp instead of wrapping; and every group keeps
  /// the submenu it was last left standing on.
  /// </summary>
  /// <remarks>
  /// The model is one skin's reading of the menu, not a base facility - these tests are also the guard that it
  /// stays that way: they walk it through the flat menu <c>HomeViewModel</c> publishes and demand nothing of the
  /// view model beyond the flat menu and the selection the cross moves.
  /// </remarks>
  [TestFixture]
  public class TestWmcHomeMenuModel
  {
    private static (WmcHomeMenuModel model, HomeViewModel home) Wire()
    {
      HomeViewModel home = new HomeViewModel();
      var model = new WmcHomeMenuModel(() => home.Menu, () => home.SelectedEntry, entry => home.SelectedEntry = entry);
      model.Rebuild(home.SelectedEntry);
      return (model, home);
    }

    private static WmcHomeMenuGroup Group(WmcHomeMenuModel model, string label)
    {
      foreach (WmcHomeMenuGroup group in model.Groups)
      {
        if (group.Label == label)
          return group;
      }
      Assert.Fail("the cross has no group named \"" + label + "\"");
      return null;
    }

    [Test]
    public void GroupsStandInTheDocumentaryOrderWithOthersLast()
    {
      (WmcHomeMenuModel model, HomeViewModel _) = Wire();

      var labels = new List<string>();
      foreach (WmcHomeMenuGroup group in model.Groups)
        labels.Add(group.Label);

      CollectionAssert.AreEqual(
          new[] { "Images", "Audio", "Videos", "TV", "Radio", "News", "Settings", "Others" }, labels,
          "the vertical arm is DefaultGroups.Create()'s order, and the ungrouped actions stand last");
    }

    [Test]
    public void EachGroupHoldsTheReferenceSubmenusAndIsNamedByItsLeader()
    {
      (WmcHomeMenuModel model, HomeViewModel _) = Wire();

      CollectionAssert.AreEqual(new[] { "Images" }, Labels(Group(model, "Images")));
      CollectionAssert.AreEqual(new[] { "Audio", "Party music player" }, Labels(Group(model, "Audio")),
          "the music group is the album shelf and the party player, in the reference's order");
      CollectionAssert.AreEqual(new[] { "Videos", "Movies", "Series", "Browse media" }, Labels(Group(model, "Videos")),
          "the video group is its shelves; the online shelf has no action here and leaves no gap");
      CollectionAssert.AreEqual(new[] { "News", "Weather" }, Labels(Group(model, "News")),
          "the weather stands under the news, as the reference puts it");
      CollectionAssert.AreEqual(new[] { "Local media", "Latest media", "Utilities" }, Labels(Group(model, "Others")),
          "the actions DefaultGroups never claimed are the others group, in the menu's own order");
    }

    [Test]
    public void EveryEntryStandsInExactlyOneGroup()
    {
      (WmcHomeMenuModel model, HomeViewModel home) = Wire();

      var seen = new HashSet<MainMenuEntry>();
      int counted = 0;
      foreach (WmcHomeMenuGroup group in model.Groups)
      {
        foreach (MainMenuEntry entry in group.Entries)
        {
          Assert.That(seen.Add(entry), "the cross lists {0} in two groups", entry.Label);
          counted++;
        }
      }

      Assert.That(counted, Is.EqualTo(home.Menu.Count), "an entry vanished from the cross");
    }

    [Test]
    public void VerticalWalkIsGroupsAndClampsAtBothEnds()
    {
      (WmcHomeMenuModel model, HomeViewModel home) = Wire();

      Assert.That(model.SelectedGroup, Is.Not.Null, "a rebuilt cross always stands on a group");
      Assert.That(model.SelectedGroup.Label, Is.EqualTo("Images"));

      model.MoveGroup(+1);
      Assert.That(model.SelectedGroup.Label, Is.EqualTo("Audio"), "down is the next main menu, not the next flat entry");
      Assert.That(home.SelectedEntry.Label, Is.EqualTo("Audio"), "stepping onto a group stands on its remembered submenu");

      model.MoveGroup(-1);
      model.MoveGroup(-1);
      Assert.That(model.SelectedGroup.Label, Is.EqualTo("Images"), "up at the top holds - the reference does not wrap");

      for (int i = 0; i < 20; i++)
        model.MoveGroup(+1);
      Assert.That(model.SelectedGroup.Label, Is.EqualTo("Others"), "down at the bottom holds on the last main menu");
    }

    [Test]
    public void HorizontalWalkIsTheStandingGroupsSubmenusAndClamps()
    {
      (WmcHomeMenuModel model, HomeViewModel home) = Wire();

      model.MoveToGroup("Videos");
      Assert.That(home.SelectedEntry.Label, Is.EqualTo("Videos"), "a fresh group opens on its first submenu");

      model.MoveSubItem(+1);
      Assert.That(home.SelectedEntry.Label, Is.EqualTo("Movies"));
      model.MoveSubItem(+1);
      model.MoveSubItem(+1);
      Assert.That(home.SelectedEntry.Label, Is.EqualTo("Browse media"));
      model.MoveSubItem(+1);
      Assert.That(home.SelectedEntry.Label, Is.EqualTo("Browse media"), "right at the group's end holds - no wrap, no escape");
      model.MoveSubItem(-1);
      model.MoveSubItem(-1);
      model.MoveSubItem(-1);
      model.MoveSubItem(-1);
      Assert.That(home.SelectedEntry.Label, Is.EqualTo("Videos"), "left at the group's start holds");
    }

    [Test]
    public void EveryGroupRemembersTheSubmenuItWasLeftOn()
    {
      (WmcHomeMenuModel model, HomeViewModel home) = Wire();

      model.MoveToGroup("Videos");
      model.MoveSubItem(+1);
      Assert.That(home.SelectedEntry.Label, Is.EqualTo("Movies"));

      model.MoveGroup(+1);
      model.MoveGroup(-1);
      Assert.That(model.SelectedGroup.Label, Is.EqualTo("Videos"));
      Assert.That(home.SelectedEntry.Label, Is.EqualTo("Movies"),
          "walking back to a group returns to the submenu it was left on - the reference's per-group remembered action");

      model.MoveToGroup("News");
      model.MoveSubItem(+1);
      model.MoveToGroup("Others");
      model.MoveToGroup("News");
      Assert.That(home.SelectedEntry.Label, Is.EqualTo("Weather"), "and it is remembered for every group on its own");
    }

    [Test]
    public void RebuildKeepsTheStandingGroupAndFollowsTheSelection()
    {
      (WmcHomeMenuModel model, HomeViewModel home) = Wire();

      model.MoveToGroup("Radio");
      model.Rebuild(home.SelectedEntry);
      Assert.That(model.SelectedGroup.Label, Is.EqualTo("Radio"), "a refresh does not walk the viewer off their main menu");

      home.SelectedEntry = null;
      model.Rebuild(null);
      Assert.That(model.SelectedGroup.Label, Is.EqualTo("Radio"),
          "a selection that did not survive the rebuild still leaves the cross on the group it was stood on");
    }

    private static List<string> Labels(WmcHomeMenuGroup group)
    {
      var labels = new List<string>();
      foreach (MainMenuEntry entry in group.Entries)
        labels.Add(entry.Label);
      return labels;
    }
  }

  internal static class WmcHomeMenuModelTestExtensions
  {
    /// <summary>Walk to a group by name the way the viewer walks to it: one main menu at a time.</summary>
    public static void MoveToGroup(this WmcHomeMenuModel model, string label)
    {
      for (int guard = 0; guard < 32; guard++)
      {
        if (model.SelectedGroup != null && model.SelectedGroup.Label == label)
          return;
        int wanted = IndexOf(model.Groups, label);
        int standing = IndexOfLabels(model);
        model.MoveGroup(wanted > standing ? +1 : -1);
      }
      Assert.Fail("no group named " + label + " to walk to");
    }

    private static int IndexOfLabels(WmcHomeMenuModel model)
    {
      for (int i = 0; i < model.Groups.Count; i++)
      {
        if (ReferenceEquals(model.Groups[i], model.SelectedGroup))
          return i;
      }
      return -1;
    }

    private static int IndexOf(IReadOnlyList<WmcHomeMenuGroup> groups, string label)
    {
      for (int i = 0; i < groups.Count; i++)
      {
        if (groups[i].Label == label)
          return i;
      }
      return -1;
    }
  }
}
