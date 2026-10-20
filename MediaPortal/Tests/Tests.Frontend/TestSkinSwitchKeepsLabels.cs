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
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The home menu's labels through a skin change. The viewer's complaint was exact and repeated: after taking
  /// Nereus off for the default skin, the main menu stood there without its labels - the tiles spelling out the
  /// entry's type name where "Movies" should stand, which is what an Avalonia list does when its item template
  /// has been handed back as <c>null</c> and nothing else is left to draw a row with.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Every ordering the remote can produce gets a test here, because the code under test swaps templates at
  /// moments that are not obvious from the outside: while the window is being built, while it is being torn
  /// down, and while a screen is off between navigations. A skin change that reaches a list sitting on screen,
  /// a change that happens while the home list is detached behind another screen and is only noticed when the
  /// viewer walks back, and a change that happens after the viewer has already walked back once - all three
  /// have to leave the labels on, and all three go through a different line of the swap's bookkeeping.
  /// </para>
  /// <para>
  /// These tests keep a copy of the template each skin answers with so that "restored" is not proven by the
  /// labels alone - both rescued skins draw labels too - but by the list demonstrably wearing the view's own
  /// tree again once no skin answers the key.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestSkinSwitchKeepsLabels
  {
    /// <summary>
    /// Nereus on, home shown, skin taken off: the list wears the view's own template again and every realized
    /// tile says its entry's label - nothing else. This is the complaint in one sentence: the type name of the
    /// entry is the exact thing that must not appear, because it is what the television drew instead.
    /// </summary>
    [AvaloniaTest]
    public void TakingTheSkinOffHandsTheHomeMenuBackToTheView()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
        HomeViewModel home = new HomeViewModel();
        window = new ShellWindow(new HomeView(home, new WorkflowManager()));
        window.Show();
        Pump();

        ListBox menu = HomeMenu(window);
        IDataTemplate? worn = menu.ItemTemplate;
        Assert.That(worn, Is.Not.Null, "with Nereus worn the home list draws the skin's tiles");
        AssertTilesSayTheirLabels(window, home);

        SkinEngine.Clear(app);
        Pump();

        Assert.That(menu.ItemTemplate, Is.Not.Null,
            "taking the skin off gave the home list its own template back - a null template is the television "
            + "spelling out the entry's type name where a label should stand");
        Assert.That(menu.ItemTemplate, Is.Not.SameAs(worn),
            "and it is the view's own tree again, not the skin's left hanging there");
        AssertTilesSayTheirLabels(window, home);
      }
      finally
      {
        CloseAndClean(app, window);
        SkinEngine.SkinsFolder = previous;
      }
    }

    /// <summary>
    /// Skin over skin - Nereus off is the risky direction, skin onto skin is not: with WMC put on over Nereus
    /// the list keeps wearing a skin's tiles (WMC's), and the labels stay because WMC's tiles carry labels too.
    /// A swap that quietly falls back to nothing here would strip the labels one skin change earlier than the
    /// viewer ever reaches the default skin.
    /// </summary>
    [AvaloniaTest]
    public void SwappingOneSkinForAnotherKeepsTheHomeMenuDressed()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
        HomeViewModel home = new HomeViewModel();
        window = new ShellWindow(new HomeView(home, new WorkflowManager()));
        window.Show();
        Pump();

        ListBox menu = HomeMenu(window);
        IDataTemplate? nereus = menu.ItemTemplate;

        Assert.That(SkinEngine.Apply(app, "wmc", out problem), Is.True, problem ?? "");
        Pump();

        Assert.That(menu.ItemTemplate, Is.Not.Null.And.Not.SameAs(nereus),
            "WMC's tiles replaced Nereus' without a gap in between");
        AssertTilesSayTheirLabels(window, home);

        SkinEngine.Clear(app);
        Pump();

        Assert.That(menu.ItemTemplate, Is.Not.Null,
            "and the way out of the second skin works as well as out of the first");
        AssertTilesSayTheirLabels(window, home);
      }
      finally
      {
        CloseAndClean(app, window);
        SkinEngine.SkinsFolder = previous;
      }
    }

    /// <summary>
    /// The ordering the settings screen actually produces: the viewer leaves home (its list leaves the visual
    /// tree and lets go of the skin), the skin goes off while no one is looking at home, and the viewer walks
    /// back onto the same window. A list that woke up on the way back has to have made its peace with the skin
    /// that is worn now - and has to keep answering the skin again afterwards, because a subscription that woke
    /// up without re-listening answers this one change from its attach handler and then stops hearing the skin
    /// forever.
    /// </summary>
    [AvaloniaTest]
    public void AHomeScreenThatCameBackStillListensToTheSkin()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
        HomeViewModel home = new HomeViewModel();
        Control homeView = new HomeView(home, new WorkflowManager());
        window = new ShellWindow(homeView);
        window.Show();
        Pump();

        ListBox menu = HomeMenu(window);
        IDataTemplate? nereus = menu.ItemTemplate;

        // Off home and back, the way a navigation does it: the list leaves the tree, the subscription lets go,
        // and the list comes back. The skin has not changed in between, so nothing visibly moves.
        Control? chrome = window.Content as Control;
        Assert.That(chrome, Is.Not.Null);
        window.Content = new Border();
        Pump();
        window.Content = chrome;
        Pump();

        menu = HomeMenu(window);
        Assert.That(menu.ItemTemplate, Is.SameAs(nereus),
            "walking back onto home without a skin change leaves the skin's tiles standing there");

        // And now the skin comes off, while home sits on screen again. This is the change that a subscription
        // which re-attached itself without re-listening to the engine never hears: the list would keep drawing
        // the skin's tree from a skin that stopped existing.
        SkinEngine.Clear(app);
        Pump();

        Assert.That(menu.ItemTemplate, Is.Not.Null.And.Not.SameAs(nereus),
            "a home screen the viewer walked back to still follows the skin after the walk back - taking the "
            + "skin off has to reach the list even when the list has been off the tree once");
        AssertTilesSayTheirLabels(window, home);
      }
      finally
      {
        CloseAndClean(app, window);
        SkinEngine.SkinsFolder = previous;
      }
    }

    // ---- the two things every ordering has to leave behind -------------------------------------------------

    /// <summary>
    /// Every realized tile of the home menu holds a text that is its entry's label, and no text anywhere in the
    /// window is the entry's type name. The first half is the feature; the second half is the failure the
    /// viewer reported, caught at its source rather than inferred from the missing half. The virtualized list
    /// realizes only the tiles that fit the viewport - the grown rail tiles are tall, and a headless window's
    /// menu can hold exactly one of them - so the walk goes over the containers that exist rather than over
    /// the whole menu; the tiles standing there are plenty to see a template that stopped drawing labels.
    /// </summary>
    private static void AssertTilesSayTheirLabels(ShellWindow window, HomeViewModel home)
    {
      ListBox menu = HomeMenu(window);
      menu.UpdateLayout();

      List<ListBoxItem> realized = menu.GetVisualDescendants().OfType<ListBoxItem>()
          .Where(c => c.DataContext is MainMenuEntry).ToList();
      // The headless window shows its screen inside a 1280x720 head, and since the rail grew both the view's
      // tiles and Nereus' are big pictures - Nereus' tile is taller than the headless menu's whole viewport -
      // so the virtualizing list realizes only the tiles that fit, which can be exactly one. One labeled tile
      // is still the proof this fixture is after: the list drew, and what it drew says the entry's name.
      Assert.That(realized.Count, Is.GreaterThanOrEqualTo(1),
          "a shown home screen realizes at least the tile it stands on - none of them means the list did not "
          + "at all (items source count: " + (menu.Items?.Count ?? -1) + ", template: "
          + (menu.ItemTemplate?.GetType().Name ?? "null") + ")");

      List<string> complaints = new List<string>();
      foreach (ListBoxItem tile in realized)
      {
        MainMenuEntry entry = (MainMenuEntry) tile.DataContext!;
        bool labeled = tile.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == entry.Label);
        if (!labeled)
          complaints.Add(entry.Mp2Action + " realized a tile that shows none of its label '" + entry.Label + "'");
      }

      string[] typeNames = window.GetVisualDescendants().OfType<TextBlock>()
          .Select(t => t.Text ?? string.Empty)
          .Where(t => t.StartsWith("MediaPortal.Frontend.ViewModels.MainMenuEntry", StringComparison.Ordinal))
          .ToArray();

      Assert.That(complaints, Is.Empty, string.Join("; ", complaints));
      Assert.That(typeNames, Is.Empty,
          "a tile that spells the entry's type name is Avalonia's last resort when the list holds no template "
          + "at all - this is the shape of the label-less home menu the viewer keeps photographing");
    }

    /// <summary>The home screen's main menu list - the one the skin dresses.</summary>
    private static ListBox HomeMenu(ShellWindow window)
    {
      ListBox? menu = window.GetVisualDescendants().OfType<ListBox>()
          .FirstOrDefault(l => l.Name == "menuList");
      Assert.That(menu, Is.Not.Null, "the home screen shows no main menu list");
      return menu!;
    }

    private static void CloseAndClean(Application app, ShellWindow? window)
    {
      if (window != null)
      {
        try { window.Close(); } catch (ObjectDisposedException) { }
      }
      SkinEngine.Clear(app);
      Pump();
    }

    /// <summary>Let the dispatcher build what showing queued - the same pump the window tests run on.</summary>
    private static void Pump()
    {
      for (int round = 0; round < 10; round++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(10);
      }
      Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// A directory the test project was told about at build time - see <c>TestSkinFromDisk</c> for why a test
    /// cannot derive these from the working directory.
    /// </summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestSkinSwitchKeepsLabels).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }
  }
}
