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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
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
  /// The wall a session opens on and the screen that shows it. The wake reads the library and the opening wall
  /// behind its ring and hands the window over afterwards, so the model is regularly finished reading before a
  /// home screen exists - and the light a wall arrives in is written by its arrival.
  /// </summary>
  /// <remarks>
  /// A home view that only ever answered changes therefore took over a finished wall left standing in the dark it
  /// rises out of: the wall was read, put on the model and never brought into the light, so the home opened empty
  /// and filled itself on the first step along the menu, which is what the viewer reported - "nach dem initialen
  /// Laden ist er nicht befÃ¼llt, erst nach einer ersten Navigation".
  /// </remarks>
  [TestFixture]
  public class TestTheHomeOpensOnItsWall
  {
    private const string AUDIO_ACTION = "30715d73-4205-417f-80aa-e82f0834171f";

    /// <summary>
    /// The wall the wake left standing comes up with its screen: a viewer who has never touched a key finds the
    /// library's covers on the wall, in the light, with nothing folded away.
    /// </summary>
    [AvaloniaTest]
    public void AWallStandingBeforeTheScreenWasBuiltIsInLightWhenTheScreenArrives()
    {
      OnHomeWithAStandingWall(delegate (HomeViewModel home, Control pane, Control wall)
      {
        Assert.That(pane.IsEffectivelyVisible, Is.True,
            "the content area is off the screen although a wall stands on it");
        Assert.That(wall.Opacity, Is.EqualTo(1.0).Within(0.001),
            "the wall the home opened on is still in the dark it rises out of; the light is written by its "
            + "arrival, which nobody heard because the wall was read before this view was built");
        Assert.That(Squares(wall), Is.GreaterThan(0), "no square of the standing wall can be stood on");
      });
    }

    /// <summary>
    /// The other half of the same promise: a wall that is put up, taken down and put up again while the screen
    /// stands there keeps coming up - the answer this view has always given to a wall's arrival.
    /// </summary>
    [AvaloniaTest]
    public void AWallArrivingWhileTheScreenStandsThereComesUpToo()
    {
      OnHomeWithAStandingWall(delegate (HomeViewModel home, Control pane, Control wall)
      {
        home.ContentGroups.Clear();
        home.ContentGroups.Add(NewBand("Continue playing", "square1", 760, 3));
        Pump();

        Assert.That(pane.IsEffectivelyVisible, Is.True, "the wall that arrived later folded its area away");
        Assert.That(wall.Opacity, Is.EqualTo(1.0).Within(0.001),
            "a wall put on while the screen stands there never came into the light");
        Assert.That(Squares(wall), Is.EqualTo(3), "the wall that arrived later is not the one being shown");
      });
    }

    /// <summary>
    /// The wake's own order: the reading is through and its wall is standing before the screen is built, and the
    /// screen is then shown.
    /// </summary>
    private static void OnHomeWithAStandingWall(Action<HomeViewModel, Control, Control> assert)
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        WearNereus(app);
        HomeViewModel home = new HomeViewModel();
        MainMenuEntry entry = home.DisplayMenu.Single(item => item.HomeImageId == AUDIO_ACTION);
        home.SelectedEntry = entry;
        home.ShowContentAsync(entry).GetAwaiter().GetResult();
        home.ContentGroups.Clear();

        // What the server would have answered for this entry, put up the way the model puts a wall up.
        home.ContentGroups.Add(NewBand("Latest added", "square1", 760, 6));
        home.ContentGroups.Add(NewBand("Unplayed", "square2", 650, 4));

        window = new ShellWindow(new HomeView(home, new WorkflowManager(),
            sectionScreen: delegate (HomeSection section) { return null!; }));
        window.Width = 1920;
        window.Height = 1080;
        window.Show();
        Pump();

        assert(home, paneOf(window), WallOf(window));
      }
      finally
      {
        window?.Close();
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out string? _);
      }
    }

    private static void WearNereus(Application app)
    {
      Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
          themeProblem ?? "");
      Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");
    }

    private static HomeContentGroup NewBand(string heading, string layout, double width, int covers)
    {
      HomeContentGroup band = new HomeContentGroup(heading, layout, width, covers);
      for (int index = 1; index <= covers; index++)
      {
        band.AddTitle(new TitleViewModel(new MediaTitle(Guid.NewGuid(), heading + " " + index, null, 2020,
            null, null, false)));
      }
      return band;
    }

    private static ScrollViewer paneOf(ShellWindow window)
    {
      return window.GetVisualDescendants().OfType<ScrollViewer>()
          .Single(viewer => viewer.Name == "nereusContentPane");
    }

    private static Control WallOf(ShellWindow window)
    {
      return window.GetVisualDescendants().OfType<Control>().Single(control => control.Name == "nereusWallStanding");
    }

    private static int Squares(Control wall)
    {
      return wall.GetVisualDescendants().OfType<Button>()
          .Count(button => button.Focusable && button.IsEffectivelyVisible && button.IsEnabled);
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
      AssemblyMetadataAttribute? value = typeof(TestTheHomeOpensOnItsWall).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");
      return Path.GetFullPath(value!.Value!);
    }
  }
}
