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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The wall that is parked beside the one the viewer reads has to be off the pointer, not only off the light.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Both hosts fill the same cell of one Grid, and the parked one stays realized on purpose - that is what
  /// makes walking back a flip and nothing else. But this engine keeps a control as a pointer target while it
  /// is only invisible to the eye: neither <c>Opacity = 0</c> nor <c>IsEnabled = false</c> takes a tile out of
  /// the hit test, and a disabled control still answers <c>:pointerover</c>. So the parked wall's covers sat
  /// over and between the covers being read and handed the pointer to themselves as the hand moved inside one
  /// tile, which started that tile's zoom curve and its focus frame from the beginning - over and over, on a
  /// square the viewer never left.
  /// </para>
  /// <para>
  /// The reference could not do this to its viewer: its second host was a hidden <c>ContentControl</c>, and a
  /// hidden element is no target there. These tests hold this port to that law on this engine's terms.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTheParkedWallKeepsOffThePointer
  {
    /// <summary>The music entry, whose home the wall tests build as the server would answer it.</summary>
    private const string AUDIO_ACTION = "30715d73-4205-417f-80aa-e82f0834171f";

    [AvaloniaTest]
    public void TheWallThatStoodBeforeTheStepTakesNoPointerOffTheStandingOne()
    {
      OnThePair(delegate (HomeViewModel home, ShellWindow window)
      {
        ItemsControl standing = Hosts(window).Single(host => host.IsEnabled);
        ItemsControl parked = Hosts(window).Single(host => !host.IsEnabled);

        Button[] parkedCovers = CoversOf(parked);
        Assert.That(parkedCovers, Is.Not.Empty,
            "the host the viewer walked away from keeps its wall realized - that is the point of the pair");

        // The claim's sharp edge: those covers still report themselves visible, because nothing hid them - only
        // the light was taken off them. Being off the light is therefore not what the tests are about.
        Assert.That(parkedCovers.Any(cover => cover.IsEffectivelyVisible), Is.True,
            "a parked cover still calls itself visible, so the eye alone cannot be the guard");
        Assert.That(parked.IsEnabled, Is.False, "the parked host is switched off as a whole");
        Assert.That(parkedCovers.All(cover => !cover.IsEffectivelyEnabled), Is.True,
            "and so are its covers, which the engine still counts as switched off below the switched host");

        foreach (Button cover in parkedCovers)
        {
          Assert.That(OffThePointer(cover), Is.True,
              "the parked wall's cover '" + NameOf(cover) + "' stands over the wall being read and takes the "
              + "pointer off it, which starts that tile's zoom and its focus frame again");
        }

        foreach (Button cover in CoversOf(standing))
        {
          Assert.That(OffThePointer(cover), Is.False,
              "the wall the viewer is reading has to answer the pointer, cover '" + NameOf(cover) + "'");
        }
      });
    }

    [AvaloniaTest]
    public void WalkingBackOntoTheParkedWallGivesThePointerBackToIt()
    {
      OnThePair(delegate (HomeViewModel home, ShellWindow window)
      {
        ItemsControl parked = Hosts(window).Single(host => !host.IsEnabled);
        MainMenuEntry back = home.DisplayMenu.Single(item => item.HomeImageId == AUDIO_ACTION);

        home.SelectedEntry = back;
        home.ShowContentAsync(back).GetAwaiter().GetResult();
        AddABand(home, "Back again");
        Pump();

        ItemsControl standingNow = Hosts(window).Single(host => host.IsEnabled);
        ItemsControl parkedNow = Hosts(window).Single(host => !host.IsEnabled);
        Assert.That(standingNow, Is.SameAs(parked),
            "walking back puts the light on the host that was parked, and the viewer stands on its wall");

        foreach (Button cover in CoversOf(standingNow))
        {
          Assert.That(OffThePointer(cover), Is.False,
              "a wall the viewer walked back onto answers the pointer, cover '" + NameOf(cover) + "'");
        }

        foreach (Button cover in CoversOf(parkedNow))
        {
          Assert.That(OffThePointer(cover), Is.True,
              "and the one they left goes off the pointer with the light, cover '" + NameOf(cover) + "'");
        }
      });
    }

    /// <summary>
    /// Whether the pointer can reach a control at all: the hit test walks down the visual tree and drops every
    /// branch whose trunk refuses it, so one ancestor off the pointer takes the whole cover away from it.
    /// </summary>
    private static bool OffThePointer(Visual visual)
    {
      return visual.GetVisualAncestors().OfType<InputElement>()
          .Any(ancestor => !ancestor.IsHitTestVisible);
    }

    private static Button[] CoversOf(ItemsControl host)
    {
      // Deliberately not filtered by IsEnabled or visibility: what is asked here is what the pointer would find.
      return host.GetVisualDescendants().OfType<Button>()
          .Where(button => button.Tag is HomeContentTile)
          .ToArray();
    }

    private static string NameOf(Button cover)
    {
      return cover.Tag is HomeContentTile tile && tile.Title != null ? tile.Title.Name : "(without a name)";
    }

    /// <summary>
    /// The shown home with both hosts wearing a wall: the music entry's wall stands, the viewer steps to another
    /// entry and its wall comes onto the other host, so one host is in the light and one is parked - both of
    /// them realized, which is the only state the pointer can be wrong about.
    /// </summary>
    private static void OnThePair(Action<HomeViewModel, ShellWindow> assert)
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        HomeViewModel home = new HomeViewModel();
        MainMenuEntry audio = home.DisplayMenu.Single(item => item.HomeImageId == AUDIO_ACTION);
        home.SelectedEntry = audio;
        home.ShowContentAsync(audio).GetAwaiter().GetResult();
        AddABand(home, "Latest added");
        Pump();

        MainMenuEntry other = home.DisplayMenu.First(item => item.HomeImageId != AUDIO_ACTION
            && !string.IsNullOrEmpty(item.HomeImageId));
        home.SelectedEntry = other;
        home.ShowContentAsync(other).GetAwaiter().GetResult();
        AddABand(home, "Unplayed");
        Pump();

        window = new ShellWindow(new HomeView(home, new WorkflowManager(),
            sectionScreen: delegate (HomeSection section) { return null!; }));
        window.Width = 1920;
        window.Height = 1080;
        window.Show();
        Pump();

        Assert.That(Hosts(window).Count(host => host.IsEnabled), Is.EqualTo(1),
            "one host wears the light and the other does not");
        assert(home, window);
      }
      finally
      {
        window?.Close();
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out _);
      }
    }

    private static void AddABand(HomeViewModel home, string heading)
    {
      HomeContentGroup band = new HomeContentGroup(heading, "square1", 760, 4);
      for (int index = 1; index <= 4; index++)
      {
        band.AddTitle(new TitleViewModel(
            new MediaTitle(Guid.NewGuid(), heading + " " + index, null, 2020, null, null, false)));
      }
      home.ContentGroups.Add(band);
    }

    private static ItemsControl[] Hosts(ShellWindow window)
    {
      ItemsControl[] hosts = window.GetVisualDescendants().OfType<ItemsControl>()
          .Where(host => host.Name == "nereusWall0" || host.Name == "nereusWall1")
          .ToArray();
      Assert.That(hosts, Has.Length.EqualTo(2), "the home wears the pair of wall hosts");
      return hosts;
    }

    private static void Pump()
    {
      for (int round = 0; round < 10; round++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(10);
      }
      Dispatcher.UIThread.RunJobs();
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestTheParkedWallKeepsOffThePointer).Assembly
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
