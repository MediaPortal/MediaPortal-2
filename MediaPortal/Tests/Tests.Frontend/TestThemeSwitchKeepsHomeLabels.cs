/*
    This file is part of MediaPortal 2
    Licensed under the GNU General Public License v3 or later.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
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
  /// The skin change the way the theme screen performs it - through FrontendTheme.Apply and its "default"
  /// name, not by clearing the engine behind the theme's back. The viewer's complaint named exactly this
  /// route: after switching from Nereus to the default skin, the main menu stood there without its labels.
  /// </summary>
  [TestFixture]
  public class TestThemeSwitchKeepsHomeLabels
  {
    [TearDown]
    public void PutTheDefaultFaceBack()
    {
      Application? app = Application.Current;
      if (app != null)
      {
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
        SkinEngine.Clear(app);
      }
    }

    [AvaloniaTest]
    public void WalkingFromNereusToTheDefaultSkinThroughTheThemeLeavesTheLabelsStanding()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? problem), Is.EqualTo("nereus"), problem ?? "");
        HomeViewModel home = new HomeViewModel();
        window = new ShellWindow(new HomeView(home, new WorkflowManager()));
        window.Show();
        Pump();

        ListBox menu = HomeMenu(window);
        Assert.That(menu.ItemTemplate, Is.Not.Null, "with Nereus worn the home list draws the skin tiles");

        Assert.That(FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out problem),
            Is.EqualTo(FrontendTheme.DEFAULT_NAME), problem ?? "");
        Pump();

        Assert.That(SkinEngine.Worn(app), Is.Null, "the theme name default took the skin off");
        Assert.That(menu.ItemTemplate, Is.Not.Null,
            "and the home list wears its own template again, not a handed-back null");
        AssertTilesSayTheirLabels(window, home);
      }
      finally
      {
        window?.Close();
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previous;
      }
    }

    private static ListBox HomeMenu(ShellWindow window)
    {
      ListBox? menu = window.GetVisualDescendants().OfType<ListBox>()
          .FirstOrDefault(l => l.Name == "menuList");
      Assert.That(menu, Is.Not.Null, "the home screen shows no main menu list");
      return menu!;
    }

    private static void AssertTilesSayTheirLabels(ShellWindow window, HomeViewModel home)
    {
      ListBox menu = HomeMenu(window);
      menu.UpdateLayout();

      List<ListBoxItem> realized = menu.GetVisualDescendants().OfType<ListBoxItem>()
          .Where(i => i.IsVisible).ToList();
      Assert.That(realized, Is.Not.Empty, "the home menu has tiles on screen to read");
      foreach (ListBoxItem item in realized)
      {
        MainMenuEntry? entry = item.DataContext as MainMenuEntry;
        if (entry == null || string.IsNullOrEmpty(entry.Label))
          continue;
        Assert.That(item.GetVisualDescendants().OfType<TextBlock>()
            .Any(t => t.Text == entry.Label), Is.True,
            "a tile whose entry is named has to say that name with letters, not stand silent");
      }
    }

    private static void Pump()
    {
      for (int round = 0; round < 10; round++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(10);
      }
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestThemeSwitchKeepsHomeLabels).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");
      return Path.GetFullPath(value!.Value!);
    }
  }
}