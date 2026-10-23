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
using Avalonia.Controls.Presenters;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
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
  /// The home screen as the reference skins wrote it: not a screen in C# that knows a face, but a screen of bare
  /// hosts that a skin fills from its own folder.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Every rescued skin ships its home under its own roof - <c>home.xaml</c> beside its manifest - and the
  /// general home screen keeps none of it. That division is the whole argument of these tests, because it is
  /// the thing a port loses silently: put a face's layers back into the shared file and every screen still
  /// looks right while the skin folder stops being the place a person edits a face.
  /// </para>
  /// <para>
  /// So the first test reads the manifests off disk and asks only one question of each: does this face name a
  /// home of its own, and is that file there? The second puts a skin on and asks the three keys the home screen
  /// hosts - background, menu face, overlays - whether the worn skin answers them. The third walks the road a
  /// viewer walks: the corner buttons a skin hangs are the skin's markup, carry no names the general screen
  /// could hold, and say by their <c>Tag</c> what they open - so pressing one has to open exactly that.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestSkinHomeFiles
  {
    /// <summary>The keys the general home screen hangs hosts on, in the order it paints them.</summary>
    private static readonly string[] HOME_KEYS =
    {
      "HomeBackgroundTemplate",
      "HomeMenuFaceTemplate",
      "HomeOverlaysTemplate",
    };

    /// <summary>
    /// Every rescued skin names a <c>home.xaml</c> in its manifest, and the file is there beside it.
    /// </summary>
    [Test]
    public void EveryRescuedSkinNamesAHomeOfItsOwn()
    {
      string skins = Path.Combine(MetadataPath("FrontendSource"), "Skins");

      foreach (string folder in Directory.GetDirectories(skins))
      {
        string manifestPath = Path.Combine(folder, "skin.xml");
        if (!File.Exists(manifestPath))
          continue;

        using FileStream stream = File.OpenRead(manifestPath);
        SkinManifest? manifest = SkinManifest.Load(stream, manifestPath,
            Path.GetFileName(folder), out string? problem);
        Assert.That(manifest, Is.Not.Null, Path.GetFileName(folder) + ": " + (problem ?? ""));

        Assert.That(manifest!.HomeFile, Is.Not.Null.And.Not.Empty,
            Path.GetFileName(folder) + " ships no home of its own - its layers, its menu face and its overlays "
            + "would have to live in the general home screen, which is the arrangement the reference never had");
        Assert.That(File.Exists(Path.Combine(folder, manifest.HomeFile!)), Is.True,
            Path.GetFileName(folder) + " names " + manifest.HomeFile + " in its manifest but carries no such file");
      }
    }

    /// <summary>
    /// The worn Nereus answers the three home keys out of its own <c>home.xaml</c>.
    /// </summary>
    [AvaloniaTest]
    public void TheWornSkinAnswersTheHomeKeysTheScreenHosts()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      try
      {
        FrontendTheme.Apply(app, "nereus", out string? themeProblem);
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        foreach (string key in HOME_KEYS)
        {
          Assert.That(SkinEngine.WornTemplate(key), Is.Not.Null,
              "the worn Nereus answers no " + key + ", so the general home screen would paint its fallback "
              + "where this face's own markup should stand");
        }
      }
      finally
      {
        SkinEngine.SkinsFolder = previous;
      }
    }

    /// <summary>
    /// The corner buttons a face hangs in its overlays open the screen their <c>Tag</c> names, through the host
    /// and without a single name in the general screen.
    /// </summary>
    [AvaloniaTest]
    public void TheSkinsCornerButtonsOpenWhatTheirTagNames()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        FrontendTheme.Apply(app, "nereus", out string? themeProblem);
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        var opened = new List<string>();
        var home = new HomeViewModel();
        var workflow = new WorkflowManager();
        var view = new HomeView(home, workflow,
            sectionScreen: delegate (HomeSection section)
            {
              return null!;
            },
            screenFactory: delegate (string screenKey)
            {
              opened.Add(screenKey);
              return null!;
            });

        window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
        {
          return frame.StateId == "home" ? view : frame.ViewModel as Control;
        });
        workflow.Push(new WorkflowFrame("home", "Home", home));
        window.Show();
        Pump();

        ContentControl overlays = window.GetVisualDescendants().OfType<ContentControl>()
            .Single(host => host.Name == "homeOverlays");
        Assert.That(overlays.Template, Is.Not.Null,
            "the home's overlay host stands empty although the Nereus skin ships overlays");

        Button[] corner = overlays.GetVisualDescendants().OfType<Button>().ToArray();
        Assert.That(corner.Select(button => button.Tag as string),
            Is.EquivalentTo(new[] { "close", "viewer", "settings", "browse" }),
            "the corner buttons this face hangs are the reference's four ways out, each naming the door it opens");

        // Pressing one is the whole road: the button belongs to the skin's markup, the host answers the click,
        // and the word in the Tag is the screen that gets asked for.
        Button settings = corner.Single(button => (button.Tag as string) == "settings");
        settings.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, settings));
        Pump();

        Assert.That(opened, Is.EqualTo(new[] { "settings" }),
            "pressing the skin's settings button has to ask for the settings screen by the name its Tag carries");
      }
      finally
      {
        if (window != null)
        {
          try { window.Close(); } catch (ObjectDisposedException) { }
        }
        SkinEngine.SkinsFolder = previous;
      }
    }

    /// <summary>
    /// The four ways out of the home are shapes out of the skin's own vectors rather than pictures off its shelf,
    /// which is what lets them wear the palette's colour, stay sharp when the screen is scaled, and need no lit twin
    /// of a file for the moment something stands on them. They also stand on one line, at the size the head line's
    /// pictures use - and that last part is the guarantee a corner button loses the moment its markup hands the
    /// shape a box instead of a ceiling, because a shape stretched uniformly into a box of a told size keeps to the
    /// top of that box.
    /// </summary>
    [AvaloniaTest]
    public void TheSkinsCornerButtonsDrawShapesAndNotPictures()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        FrontendTheme.Apply(app, "nereus", out string? themeProblem);
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        HomeViewModel home = new HomeViewModel();
        WorkflowManager workflow = new WorkflowManager();
        HomeView view = new HomeView(home, workflow,
            sectionScreen: delegate (HomeSection section)
            {
              return null!;
            },
            screenFactory: delegate (string screenKey)
            {
              return null!;
            });
        window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
        {
          return frame.StateId == "home" ? view : frame.ViewModel as Control;
        });
        workflow.Push(new WorkflowFrame("home", "Home", home));
        window.Show();
        Pump();

        ContentControl overlays = window.GetVisualDescendants().OfType<ContentControl>()
            .Single(host => host.Name == "homeOverlays");
        Button[] corner = overlays.GetVisualDescendants().OfType<Button>().ToArray();
        Assert.That(corner, Has.Length.EqualTo(4), "this face hangs four ways out of the home");

        double? middle = null;
        foreach (Button button in corner)
        {
          string door = button.Tag as string ?? "corner";

          Assert.That(button.GetVisualDescendants().OfType<Image>(), Is.Empty,
              "the " + door + " button carries a picture again; these four are drawn out of the skin's vectors, "
              + "which is what lets them take the palette's colour and drop their lit twin file");

          Avalonia.Controls.Shapes.Path[] drawn = button.GetVisualDescendants()
              .OfType<Avalonia.Controls.Shapes.Path>()
              .Where(shape => shape.Data != null && shape.IsEffectivelyVisible)
              .ToArray();
          Assert.That(drawn, Has.Length.EqualTo(1),
              "the " + door + " button draws no shape, so the worn skin answered no vector for it and the corner of "
              + "the home stands empty");

          Avalonia.Controls.Shapes.Path mark = drawn[0];
          Assert.That(mark.Bounds.Width, Is.LessThanOrEqualTo(47),
              "and it is drawn at the head line's size rather than bigger than it");
          Assert.That(mark.Bounds.Height, Is.LessThanOrEqualTo(47),
              "and no taller than the head line's pictures either");

          double plate = mark.Bounds.Width / mark.Bounds.Height;
          double art = mark.Data!.Bounds.Width / mark.Data!.Bounds.Height;
          Assert.That(plate, Is.EqualTo(art).Within(0.05 * art),
              "the " + door + " mark was stretched into a plate it does not fill, so it is standing off the line "
              + "the four of them are meant to share");

          if (middle == null)
            middle = mark.Bounds.Center.Y;
          else
            Assert.That(mark.Bounds.Center.Y, Is.EqualTo(middle.Value).Within(1.0),
                "a person, a gear, a magnifier and a power ring are cut differently and still have to come to rest "
                + "on one line");
        }

        WornSkin worn = SkinEngine.Worn(app)!;
        foreach (string name in new[] { "NotificationMenu/power", "NotificationMenu/user",
            "NotificationMenu/settings", "NotificationMenu/search" })
        {
          Assert.That(worn.Vectors.Has(name), Is.True,
              "the worn skin carries no vector called " + name + ", so a corner of the home has nothing to draw");
        }
      }
      finally
      {
        if (window != null)
        {
          try { window.Close(); } catch (ObjectDisposedException) { }
        }
        SkinEngine.SkinsFolder = previous;
      }
    }

    /// <summary>
    /// What one of the four lights up as when something stands on it: a disc, not a square.
    /// </summary>
    /// <remarks>
    /// The palette paints a stood-on button's light in the button's own corner, and the face writes its buttons
    /// square. On these four that square shows as a box lying behind a round mark - which is what the viewer
    /// complained about - so the corner belongs to the button, and the template that paints the light has to
    /// carry it. The second half of this is the part that could go wrong quietly: a setter on the control is
    /// nothing until the thing drawing the light reads it.
    /// </remarks>
    [AvaloniaTest]
    public void ACornerButtonLightsUpAsADiscAndNotASquare()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      Window? window = null;
      try
      {
        FrontendTheme.Apply(app, "nereus", out string? themeProblem);
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        Button power = new Button();
        power.Classes.Add("nereus-corner-button");
        window = new Window { Content = power, Width = 200, Height = 200 };
        window.Show();
        Pump();

        Assert.That(power.Bounds.Height, Is.GreaterThanOrEqualTo(50), "the plate stood at its sixty-six");
        Assert.That(power.CornerRadius.TopLeft, Is.EqualTo(power.Bounds.Height / 2),
            "a corner button is lit as a disc: half its own width, so the light has no corner standing out beside "
            + "a round mark");

        ContentPresenter? plate = power.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault();
        Assert.That(plate, Is.Not.Null, "the button's template paints the light");
        Assert.That(plate!.CornerRadius.TopLeft, Is.EqualTo(power.CornerRadius.TopLeft),
            "and the element that paints it carries the disc's corner rather than the square the face writes "
            + "everywhere else");

        window.Close();
      }
      finally
      {
        if (window != null)
        {
          try { window.Close(); } catch (ObjectDisposedException) { }
        }
        SkinEngine.SkinsFolder = previous;
      }
    }

    /// <summary>Runs the dispatcher long enough for a screen to build and settle.</summary>
    private static void Pump()
    {
      for (int round = 0; round < 10; round++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(10);
      }
      Dispatcher.UIThread.RunJobs();
    }

    /// <summary>A path the test project handed over as assembly metadata.</summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestSkinHomeFiles).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      return value!.Value;
    }
  }
}
