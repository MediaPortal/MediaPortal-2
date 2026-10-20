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
