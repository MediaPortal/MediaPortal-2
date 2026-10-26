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
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Styling;
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

        //  And the light has to actually come up on that disc. A corner nothing paints with would pass the two
        //  assertions above while the viewer still saw a square, so the pointer is laid on the button and the
        //  palette's raised colour is asked for where it is painted.
        IBrush resting = plate.Background;
        Point middle = power.TranslatePoint(new Point(power.Bounds.Width / 2, power.Bounds.Height / 2), window)
            ?? new Point(power.Bounds.Center.X, power.Bounds.Center.Y);
        window.MouseMove(middle);
        Pump();

        Assert.That(power.IsPointerOver, Is.True, "the pointer was laid on the button");
        Assert.That(plate.Background, Is.Not.SameAs(resting),
            "and a button that is stood on takes the palette's light in the plate that paints it");
        Assert.That(plate.Background, Is.Not.Null, "the plate is painted at all");

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

    /// <summary>
    /// What standing on one of the four does to the mark it carries: it is lit with a white glow that spreads further
    /// than the button's own square, and the plate under it is left as quiet as it was. Four things could go quietly
    /// wrong about this. A setter aimed through a button at the shape inside it only fires if the style really
    /// reaches into the button's content. An effect whose colour it could not resolve still exists afterwards - it
    /// simply draws in black, which over this face's pictures is the same as drawing no light at all - and a glow
    /// aimed at white that came back coloured is the same mistake in a nicer dress. A control that owns a template
    /// clips what it draws to its own rectangle, so the light was cut on the button's circle halfway out of the
    /// glyph, which is a ring under the mark and not a light out of it. And the glow was once written for the ring
    /// alone, so a viewer with a mouse saw the button lighten while the glyph stayed dark: what the two ways of
    /// standing on a button promise has to be read off the two rules, because no pointer can be pointed at a
    /// headless screen.
    /// </summary>
    [AvaloniaTest]
    public void StandingOnACornerButtonLightsItsMarkAndNotItsPlate()
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
        power.Content = new Avalonia.Controls.Shapes.Path
        {
          Data = Geometry.Parse("M 0 0 L 20 10 L 0 20 Z"),
          Fill = Brushes.White
        };
        window = new Window { Content = power, Width = 200, Height = 200 };
        window.Show();
        Pump();

        Avalonia.Controls.Shapes.Path mark = power.GetVisualDescendants()
            .OfType<Avalonia.Controls.Shapes.Path>().First();
        ContentPresenter? plate = power.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault();

        Assert.That(power.Effect, Is.Null,
            "at rest a corner button carries no glow - four quiet marks over the picture behind them is what the "
            + "face shows until something stands on one");
        IEffect? restingPlate = plate?.Effect;

        power.Focus();
        Pump();
        Assert.That(power.IsFocused, Is.True, "something stands on the button");

        DropShadowEffect? glow = power.Effect as DropShadowEffect;
        Assert.That(glow, Is.Not.Null,
            "standing on the button lit a glow, and lit it on the button whose glyph it is, where the plate it "
            + "stands on paints nothing and can therefore not swallow the light");
        Assert.That(glow!.BlurRadius, Is.GreaterThan(13.0),
            "the glow is light spread well out of the glyph rather than a hard edge behind it; it spreads "
            + glow.BlurRadius.ToString("0.#") + " lines");
        Assert.That(glow.Opacity, Is.GreaterThan(0.9),
            "and it is lit at full strength, not a hint of a hint; it carries "
            + glow.Opacity.ToString("0.00") + " of its light");
        Assert.That(glow.Color, Is.EqualTo(Colors.White),
            "the glow is white light: a coloured glow over this face's pictures reads as a tint laid over the "
            + "mark rather than as light coming out of it, and an effect whose colour could not be resolved "
            + "draws in black, which is no light at all; it came back " + glow.Color);

        // The cut the reader complained about: a sixty-six square with a forty-six glyph in it has ten lines of
        // room to its own edge, and a control that owns a template clips what it draws to that edge - so the
        // light stopped on the button's own circle, halfway out of the glyph, like a ring laid under the mark.
        Assert.That(power.ClipToBounds, Is.False,
            "and the button lets its own light out: nothing but that light leans outside the square, so the clip "
            + "that cut it in a circle is off");
        Assert.That(mark.ClipToBounds, Is.False,
            "and the glyph itself clips nothing either, which is where the light is drawn");

        Assert.That(mark.Effect, Is.Null,
            "the mark carries no glow of its own - it is the button that lights, and the mark is what the light "
            + "has to work on");
        Assert.That(plate?.Effect, Is.SameAs(restingPlate),
            "the plate the mark stands on carries no glow of its own, so the mark alone answers the key");

        // The hand and the ring are answered by one and the same glow. The pointer's half stands behind the
        // window's own gate, which is why the two rules are two rules and not one.
        Style? hand = EveryStyle(app.Styles).FirstOrDefault(s =>
            s.Selector.ToString() == "Window:not(.pointer-resting) Button.nereus-corner-button:pointerover");
        Assert.That(hand, Is.Not.Null,
            "a viewer who drags a mouse across the row is answered by a rule of their own, so that the mark lights "
            + "for the pointer as it does for the ring");
        DropShadowEffect? pointed = hand?.Setters.OfType<Setter>()
            .FirstOrDefault(s => s.Property.Name == "Effect")?.Value as DropShadowEffect;
        Assert.That(pointed, Is.Not.Null,
            "and that rule dresses the mark with a glow rather than only lightening the button it stands on");
        Assert.That(pointed!.BlurRadius, Is.EqualTo(glow.BlurRadius),
            "the pointer's glow spreads as far as the ring's; the ring's " + glow.BlurRadius.ToString("0.#")
            + ", the pointer's " + pointed.BlurRadius.ToString("0.#"));
        Assert.That(pointed.Color, Is.EqualTo(glow.Color),
            "and wears the same light; the ring's " + glow.Color + ", the pointer's " + pointed.Color);
        Assert.That(pointed.Opacity, Is.EqualTo(glow.Opacity),
            "at the same strength; the ring's " + glow.Opacity.ToString("0.00") + ", the pointer's "
            + pointed.Opacity.ToString("0.00"));
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

    /// <summary>
    /// Every single rule a face laid down, out of the groups it filed them in: the skin hands its files over as
    /// <see cref="Styles"/> collections, and a rule a test wants to compare with another one is inside one of
    /// those, not in the screen's own style list.
    /// </summary>
    private static IEnumerable<Style> EveryStyle(IEnumerable<IStyle> styles)
    {
      foreach (IStyle style in styles)
      {
        if (style is Style single)
          yield return single;

        if (style is Styles group)
        {
          foreach (Style nested in EveryStyle(group))
            yield return nested;
        }
      }
    }

    /// <summary>
    /// What a main menu tile's picture does about the viewer standing on it and about being the entry the screen
    /// is showing: the light comes up for either, but the growth belongs to the standing entry alone, and to that
    /// only while someone is actually on it. A tile that is merely the tile the arrow points at has to keep its
    /// picture in its frame, or the growth - which says "this entry is what the screen is about" - is said about
    /// every tile a viewer walks past on the way to the one they meant.
    /// </summary>
    [AvaloniaTest]
    public void OnlyTheStandingEntryGrowsItsPictureAndOnlyWhileSomeoneStandsOnIt()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      Window? window = null;
      try
      {
        FrontendTheme.Apply(app, "nereus", out string? themeProblem);
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        ListBox rail = new ListBox();
        rail.Classes.Add("nereus-home-menu");
        ListBoxItem standing = new ListBoxItem { Content = new Image() };
        ListBoxItem other = new ListBoxItem { Content = new Image() };
        ((Image) standing.Content).Classes.Add("nereus-menu-art");
        ((Image) other.Content).Classes.Add("nereus-menu-art");
        rail.Items.Add(standing);
        rail.Items.Add(other);
        window = new Window { Content = rail, Width = 320, Height = 420 };
        window.Show();
        Pump();

        Image art = (Image) standing.Content;
        Image elsewhere = (Image) other.Content;

        // The two pictures are measured with their animations taken off, because a headless screen never gets
        // the beats a transition runs on: an animation started here is read halfway and stays there. What this
        // test holds the face to is which rule wins for which tile, and a rule that wins half a second late is
        // still the rule that wins - the speed it arrives at is not what is being asked here.
        art.Transitions = new Avalonia.Animation.Transitions();
        elsewhere.Transitions = new Avalonia.Animation.Transitions();
        Pump();

        const double BASE_MENU_ART_LIGHT = 0.75;
        Assert.That(art.Opacity, Is.EqualTo(BASE_MENU_ART_LIGHT).Within(0.01),
            "a tile that waits wears three quarters of its picture");
        Assert.That(Growth(art), Is.EqualTo(1.0).Within(0.01), "and keeps it in its frame");

        // The entry the screen is about, with nobody standing on it yet: the light comes up, the picture stays
        // where it is.
        rail.SelectedIndex = 0;
        Pump();
        Assert.That(art.Opacity, Is.EqualTo(1.0).Within(0.01),
            "the standing entry wears the full light wherever the viewer is, because the screen is about it");
        Assert.That(Growth(art), Is.EqualTo(1.0).Within(0.01),
            "standing on the screen is not standing on the tile: the picture does not step nearer until "
            + "someone is on it; it stands at " + Growth(art).ToString("0.000"));

        // A viewer who walks the rail lights every tile they pass and grows none of them, because the growth
        // means "this is the entry the screen is showing" and is not to be said about the wrong one.
        Assert.That(other.Focus(), Is.True, "the viewer steps onto the entry below the standing one");
        Pump();
        Assert.That(elsewhere.Opacity, Is.EqualTo(1.0).Within(0.01),
            "the tile a viewer stands on wears the full light, whichever entry it is");
        Assert.That(Growth(elsewhere), Is.EqualTo(1.0).Within(0.01),
            "but a tile that is not the entry the screen is about keeps its picture in its frame even under the "
            + "focus; it stands at " + Growth(elsewhere).ToString("0.000"));
        Assert.That(Growth(art), Is.EqualTo(1.0).Within(0.02),
            "and the standing entry does not grow while the viewer is away out on the rail either; it stands at "
            + Growth(art).ToString("0.000"));

        // Now they come back onto the entry itself, and the growth the dimness was kept for arrives.
        Assert.That(standing.Focus(), Is.True, "the viewer steps onto the standing entry");
        Pump();
        Assert.That(Growth(art), Is.EqualTo(1.1).Within(0.01),
            "the standing entry under a viewer steps its picture a tenth nearer; it came to "
            + Growth(art).ToString("0.000"));

        // And the way off is the way back: still the entry the screen is about, nobody on it, and the picture
        // goes home to its frame.
        Assert.That(other.Focus(), Is.True, "the viewer steps off onto the entry below");
        Pump();
        Assert.That(rail.SelectedIndex, Is.EqualTo(0),
            "the first tile is still the entry the screen is showing, so what settles back has to be the "
            + "growth and not the selection");
        Assert.That(Growth(art), Is.EqualTo(1.0).Within(0.01),
            "and the picture settles back into its frame as soon as nobody stands on it; it came to "
            + Growth(art).ToString("0.000"));
        Assert.That(art.Opacity, Is.EqualTo(1.0).Within(0.01),
            "the standing entry keeps the light once it has it, wherever the viewer goes; it stands at "
            + art.Opacity.ToString("0.000") + " of its picture");
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

    /// <summary>How far a control's picture has grown out of its frame, read off its transform.</summary>
    private static double Growth(Control control)
    {
      return control.RenderTransform is ITransform growth ? growth.Value.M11 : 1.0;
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
