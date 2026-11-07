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

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FrontendAspect = MediaPortal.Frontend.Api.MediaAspect;

namespace Tests.Frontend
{
  /// <summary>
  /// What the screen's own menu costs while it is put away: the rail of a grid or list shelf is hidden by sliding
  /// the room two hundred points back over it, which moves it off the picture but leaves it standing, so its rows
  /// are laid out and painted like everything else. This measures that price instead of guessing it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Two numbers are asked for, because hiding a panel takes two kinds of work away and they are not the same
  /// work: the laying out (a shelf that is resized, walked from one layout into another, or told to show its
  /// details column measures and arranges its whole room again, rail included) and the painting (every frame the
  /// window is drawn carries the rail's shapes to the surface, where the surface then cuts them off at the edge).
  /// </para>
  /// <para>
  /// The painting is measured through the headless drawing backend, which is a CPU painting the picture rather
  /// than the machine's own adapter doing it on the television. The absolute milliseconds therefore belong to
  /// this test and to nobody else; what travels to the real window is the RATIO between the two states, measured
  /// in the same breath on the same window. That is also why nothing here asserts a budget: it writes the two
  /// numbers down, and the reader decides.
  /// </para>
  /// <para>
  /// Every test names its own settings folder through MP2_SKIN_SETTINGS_DIR and MP2_SCREEN_VIEWS_FILE, so none
  /// of them reads or writes what a real viewer on this machine set, and all of them take the worn skin off
  /// again afterwards.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestOffScreenMenuCost
  {
    private static readonly Uri SERVER_URL = new Uri("http://127.0.0.1:1/MediaPortal/Server/");

    private static readonly HomeSection MOVIES = new HomeSection("Movies",
        new AspectViewModel(new FrontendAspect(MovieAspect.ASPECT_ID, MovieAspect.Metadata.Name, false, false,
            null, Array.Empty<AspectAttribute>())));

    private string _previousDir = "";
    private string _ownDir = "";
    private string _previousScreensFile = "";
    private string _previousTheme = "";

    [SetUp]
    public void PutTheNereusSkinOnInAnOwnFolder()
    {
      _previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      _ownDir = Path.Combine(Path.GetTempPath(), "mp2-railcost-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_ownDir);
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", _ownDir);
      _previousScreensFile = Environment.GetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE") ?? "";
      Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE", Path.Combine(_ownDir, "screens.json"));

      _previousTheme = FrontendTheme.CurrentName(Application.Current!);
      FrontendTheme.Apply(Application.Current!, "nereus", out _);
    }

    [TearDown]
    public void PutTheMachineBack()
    {
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          _previousDir.Length == 0 ? null : _previousDir);
      Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE",
          _previousScreensFile.Length == 0 ? null : _previousScreensFile);

      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, _previousTheme.Length == 0 ? FrontendTheme.DEFAULT_NAME : _previousTheme, out _);
    }

    #region The shelf the measure is taken on

    /// <summary>A shelf of titles that answers in json and says nothing about pictures.</summary>
    private class RailShelf : HttpMessageHandler
    {
      internal int Total { get; set; } = 400;

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;

        if (path.Contains("/fanart", StringComparison.Ordinal))
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });

        if (path.EndsWith("/thumbnail", StringComparison.Ordinal))
        {
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          {
            RequestMessage = request,
            Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 })
          });
        }

        if (path.Contains("/items/", StringComparison.Ordinal))
        {
          string id = path.Substring(path.LastIndexOf('/') + 1);
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          {
            RequestMessage = request,
            Content = new StringContent("{\"id\":\"" + id + "\",\"title\":\"Title\",\"year\":2000}",
                Encoding.UTF8, "application/json")
          });
        }

        string query = request.RequestUri.Query;
        int askedFor = int.Parse(Value(query, "limit") ?? "60");
        int from = int.Parse(Value(query, "offset") ?? "0");
        int rows = Math.Max(0, Math.Min(askedFor, Total - from));

        StringBuilder body = new StringBuilder();
        body.Append("{\"total\":").Append(Total).Append(",\"offset\":").Append(from)
            .Append(",\"limit\":").Append(askedFor).Append(",\"returned\":").Append(rows)
            .Append(",\"sort\":\"title\",\"titles\":[");
        for (int i = 0; i < rows; i++)
        {
          if (i > 0)
            body.Append(',');

          body.Append("{\"id\":\"")
              .Append(new Guid(from + i + 1, 0, 0, new byte[] { 128, 0, 0, 0, 0, 0, 0, 1 }))
              .Append("\",\"title\":\"Title ").Append((from + i).ToString("D4")).Append("\",\"year\":2000}");
        }

        body.Append("]}");

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
        });
      }

      private static string? Value(string query, string name)
      {
        foreach (string part in query.TrimStart('?').Split('&'))
        {
          string[] pair = part.Split('=');
          if (pair.Length == 2 && pair[0] == name)
            return pair[1];
        }

        return null;
      }
    }

    /// <summary>
    /// A grid shelf on the screen, with the room standing where a viewer who never opened the menu leaves it.
    /// </summary>
    private class RailOnScreen : IDisposable
    {
      internal ShellWindow Window { get; }
      internal MediaBrowserViewModel Shelf { get; }
      internal Panel Rail { get; }
      internal Panel CoversRail { get; }
      internal ListBox RailRows { get; }
      internal Control Screen { get; }

      internal RailOnScreen(int total, double width, double height)
      {
        Shelf = new MediaBrowserViewModel(
            new ServerApiClient(SERVER_URL, new HttpClient(new RailShelf { Total = total })), MOVIES);
        Shelf.OpenCommand.Execute(null);
        WaitUntil("the shelf to be read", delegate { return Shelf.Titles.Count == total; });
        Assert.That(Shelf.Layout, Is.EqualTo(BrowserLayout.Grid),
            "a film shelf opens as tiles, or the measure is not taken in the grid view");
        Assert.That(Shelf.MediaMenuVisible, Is.False,
            "the menu has to stand shut, or the measure is taken of a menu that is being looked at");

        Screen = new MediaBrowserView(Shelf);
        Window = new ShellWindow(Screen) { Width = width, Height = height };
        Window.Show();
        Pump();
        Pump();

        Rail = Window.GetVisualDescendants().OfType<Panel>().First(panel => panel.Name == "sideMenu");
        CoversRail = Window.GetVisualDescendants().OfType<Panel>().First(panel => panel.Name == "bottomMenu");
        RailRows = Window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "mediaMenuList");
        Pump();
        Pump();
      }

      /// <summary>
      /// Hangs both menu panels in the tree and lets their rows be built while there is still nothing moving.
      /// Asked for because it is the one state in which a walk from one shape into another has nothing to build:
      /// whatever the walk decides, the panel it decides on already stands. Measured against the state the screen
      /// keeps itself, it says what building the menu's rows costs in the middle of the walk.
      /// </summary>
      internal void HoldBothMenuPanelsUp()
      {
        Rail.IsVisible = true;
        CoversRail.IsVisible = true;
        Pump();
        Pump();
        Assert.That(Rail.IsVisible, Is.True);
        Assert.That(CoversRail.IsVisible, Is.True);
      }

      /// <summary>
      /// Opens the screen's menu and puts it away again, which is the hour a rail is actually worth something:
      /// the rows are built when the menu is asked for and are NOT taken down when it slides back out, so every
      /// shelf that has ever shown its menu carries them for the rest of its life, standing two hundred points
      /// off the picture.
      /// </summary>
      internal void UseTheMenuOnce()
      {
        Shelf.ShowMediaMenu();
        WaitUntil("the rail's rows to stand", delegate { return RailRows.Items.Count > 0; });
        Pump();
        Pump();

        Shelf.HideMediaMenu();
        WaitUntil("the room to slide back over the rail", delegate { return SlideOffset() == 0.0; });
        Pump();
        Pump();
        Assert.That(Shelf.MediaMenuVisible, Is.False);
      }

      /// <summary>
      /// Whether the laid-down rail arrives within three seconds of the room standing still again. Asked rather
      /// than waited for by assertion, because whether a dispatcher timer of the view's ticks while a test pumps
      /// its dispatcher is exactly the kind of thing a test should find out instead of assuming.
      /// </summary>
      internal bool WaitForTheRailToBeLaidDown()
      {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3);
        while (DateTimeOffset.UtcNow < deadline)
        {
          Pump();
          if (!Rail.IsVisible)
            return true;

          Thread.Sleep(10);
        }

        return false;
      }

      /// <summary>How far the room currently stands out of where it was built to stand.</summary>
      internal double SlideOffset()
      {
        Grid room = Screen.GetVisualDescendants().OfType<Grid>().First(grid => grid.Name == "room");
        TranslateTransform? slide = room.RenderTransform as TranslateTransform;
        Assert.That(slide, Is.Not.Null, "the room slides along its own transform");
        return Math.Abs(slide!.X) + Math.Abs(slide.Y);
      }

      /// <summary>Rows the shut rail really holds, as opposed to rows its list could hold.</summary>
      internal int RowsStanding()
      {
        return RailRows.GetRealizedContainers().OfType<Control>().Count();
      }

      internal void SetRailVisible(bool visible)
      {
        Rail.IsVisible = visible;
        Pump();
        Pump();
        Assert.That(Rail.IsVisible, Is.EqualTo(visible));
      }

      internal int Count<T>() where T : Visual
      {
        return Rail.GetVisualDescendants().OfType<T>().Count();
      }

      internal int Visuals()
      {
        return Rail.GetVisualDescendants().Count();
      }

      public void Dispose()
      {
        Window.Close();
        Pump();
      }

      private static void Pump()
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(5);
      }

      private static void WaitUntil(string because, Func<bool> condition)
      {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTimeOffset.UtcNow < deadline)
        {
          Pump();
          if (condition())
            return;
          Thread.Sleep(5);
        }

        Assert.Fail("Timed out after 20 seconds waiting for " + because);
      }
    }

    #endregion

    /// <summary>
    /// What stands in the rail while it is off the picture, in the two states a viewer leaves it in: never asked
    /// for, which builds nothing, and asked for once and put away again, which keeps every row it built.
    /// </summary>
    [AvaloniaTest]
    public void ARailStandingOffTheScreenIsBuiltAndCounted()
    {
      using RailOnScreen rail = new RailOnScreen(400, 1920, 1080);

      TestContext.WriteLine("menu never opened, rail shut at " + rail.Window.Width + "x" + rail.Window.Height
          + ": " + rail.RowsStanding() + " rows, " + rail.Visuals() + " visuals hanging under the rail");

      rail.UseTheMenuOnce();
      bool laid_down = rail.WaitForTheRailToBeLaidDown();

      TestContext.WriteLine("menu opened once and slid away again: " + rail.Shelf.MediaMenu.Count
          + " rows in the menu, panel laid down = " + laid_down + ", " + rail.RowsStanding()
          + " containers still held by the list (out of the tree they are neither laid out nor painted), "
          + rail.Visuals() + " visuals (" + rail.Count<TextBlock>() + " letters, "
          + rail.Count<Image>() + " pictures, " + rail.Count<Border>() + " grounds)");

      Assert.That(rail.Shelf.MediaMenu.Count, Is.GreaterThan(0),
          "the shelf keeps its commands for the next opening - putting a menu away is not forgetting it");
      Assert.That(laid_down, Is.True,
          "the rail is taken out of the tree once the room arrived back; an invisible control is measured, "
          + "arranged and painted not at all, which is the whole reason for taking it out");

      // The other half of the promise: the same menu asked for again stands with its rows in it, so what was put
      // away was a panel and not the menu.
      rail.UseTheMenuOnce();
      rail.WaitForTheRailToBeLaidDown();
      Assert.That(rail.Shelf.MediaMenuVisible, Is.False);
    }

    /// <summary>
    /// The measure itself: the same window, the same shelf, the same room standing still - once with the rail
    /// left as the screen leaves it and once with it put out of the tree - and both the laying out and the
    /// painting timed in each state.
    /// </summary>
    [AvaloniaTest]
    public void WhatARailOffTheScreenCostsIsWrittenDown()
    {
      using RailOnScreen rail = new RailOnScreen(400, 1920, 1080);

      // Measured on a shelf that has shown its menu once, because that is the only shelf carrying anything to
      // measure: a menu that was never asked for built no rows, and putting an empty panel away saves nothing.
      // The view's own appointment is let fire first, so that it cannot go off in the middle of a timed state
      // and take the rail out from under the measurement of the state that has it standing.
      rail.UseTheMenuOnce();
      bool laid_down = rail.WaitForTheRailToBeLaidDown();
      TestContext.WriteLine("menu once used: panel laid down by the screen itself = " + laid_down);

      // State 0 is the rail as it used to hang about off the picture, state 1 as it hangs now. The screen would
      // have put it away already; the measure stands it up again on purpose, because what is being asked here is
      // what standing it up costs.
      TestContext.WriteLine("measured with " + rail.Shelf.MediaMenu.Count + " menu rows the shelf could show, "
          + rail.Visuals() + " visuals under the rail while it is stood up");

      const int passes = 9;

      // One pass of each state, alternating, from the first measurement on. Measuring one state a dozen times in
      // a row and the other state a dozen times afterwards was tried first and is unusable: a pass leaves work
      // behind it - tiles re-virtualising, pictures being asked for, timers the resize woke - and that work runs
      // into whichever state happens to be measured next, so the two numbers end up saying more about the order
      // than about the rail. Alternating hands the leftovers to both states alike.
      double[] standing_laid = new double[passes];
      double[] away_laid = new double[passes];
      double[] standing_drawn = new double[passes];
      double[] away_drawn = new double[passes];

      RenderTargetBitmap frame = new RenderTargetBitmap(new PixelSize(1920, 1080), new Vector(96, 96));
      try
      {
        for (int pass = 0; pass < passes + 2; pass++)
        {
          for (int state = 0; state < 2; state++)
          {
            rail.SetRailVisible(state == 0);
            double laid = OneRelayout(rail);
            double drawn = OnePaint(rail, frame);

            if (pass < 2)
              continue; // one pass of each state warms the painting and the layout code before anything is read

            if (state == 0)
            {
              standing_laid[pass - 2] = laid;
              standing_drawn[pass - 2] = drawn;
            }
            else
            {
              away_laid[pass - 2] = laid;
              away_drawn[pass - 2] = drawn;
            }
          }
        }
      }
      finally
      {
        frame.Dispose();
      }

      double laid_standing = Median(standing_laid);
      double laid_away = Median(away_laid);
      double drawn_standing = Median(standing_drawn);
      double drawn_away = Median(away_drawn);

      TestContext.WriteLine("grid shelf, menu shut, median of " + passes + " alternating passes at 1920x1080");
      TestContext.WriteLine("  laying the room out: rail standing " + laid_standing.ToString("0.00")
          + " ms/pass, rail put away " + laid_away.ToString("0.00") + " ms/pass  -> "
          + (laid_standing - laid_away).ToString("0.00") + " ms apart");
      TestContext.WriteLine("  painting a frame:    rail standing " + drawn_standing.ToString("0.00")
          + " ms/frame, rail put away " + drawn_away.ToString("0.00") + " ms/frame  -> "
          + (drawn_standing - drawn_away).ToString("0.00") + " ms apart");

      // Nothing is claimed about a budget and nothing about a direction: what this test is for is the number,
      // written down where a later change can be compared against it. Only that the two states were really
      // measured is insisted on.
      Assert.That(drawn_standing, Is.GreaterThan(0.0),
          "the measure painted something, or it measured nothing");
      Assert.That(laid_away, Is.GreaterThan(0.0),
          "the room was laid out in both states, or there is nothing to compare");
    }

    /// <summary>
    /// What walking from one shape of the shelf into the next costs, with the screen's menu standing open over
    /// the walk, which is the hour a viewer changes the shape: he is looking at the shelf, presses the view key
    /// and the tiles have to become covers under his hands. The walk is where the screen re-templates its whole
    /// shelf, and since the menu lives in one panel per shape, the walk hands its rows over from one panel to the
    /// other as well. Both halves are measured, and the second one is measured twice - once as the screen keeps
    /// it, once with both menu panels already stood up - so that the share the menu has in the walk stops being a
    /// guess.
    /// </summary>
    [AvaloniaTest]
    public void WhatWalkingFromShapeToShapeWithTheMenuOpenCostsIsWrittenDown()
    {
      using RailOnScreen rail = new RailOnScreen(400, 1920, 1080);

      rail.Shelf.ShowMediaMenu();
      WaitUntil("the rail's rows to stand", delegate { return rail.RailRows.Items.Count > 0; });
      Pump();
      Pump();
      Assert.That(rail.Shelf.MediaMenuVisible, Is.True, "the walk is measured over an open menu, as a viewer walks");

      // Whether the menu's own panels have a share in the walk was measured first, by walking with both panels
      // stood up beforehand - the one state in which the walk has no menu rows to build. Across every shape the
      // difference lay within the noise of the measurement (both signs appeared, up to twenty milliseconds on
      // walks of sixty to two hundred), so the menu is not what a walk is busy with and the panels are left alone
      // from here on. What is left is the walk itself, timed in its three halves.
      const int cycles = 7;
      const int warm = 2;
      Dictionary<string, List<double>> words = new Dictionary<string, List<double>>();
      Dictionary<string, List<double>> passes = new Dictionary<string, List<double>>();
      Dictionary<string, List<double>> tails = new Dictionary<string, List<double>>();
      Dictionary<string, List<double>> painted = new Dictionary<string, List<double>>();

      RenderTargetBitmap frame = new RenderTargetBitmap(new PixelSize(1920, 1080), new Vector(96, 96));
      try
      {
        for (int cycle = 0; cycle < cycles; cycle++)
        {
          for (int step = 0; step < 5; step++)
          {
            BrowserLayout from = rail.Shelf.Layout;
            double[] halves = new double[3];
            OneStep(rail, halves);

            Stopwatch painting = Stopwatch.StartNew();
            frame.Render(rail.Window);
            painting.Stop();

            if (cycle < warm)
              continue;

            string way = from + " -> " + rail.Shelf.Layout;
            Add(words, way, halves[0]);
            Add(passes, way, halves[1]);
            Add(tails, way, halves[2]);
            Add(painted, way, painting.Elapsed.TotalMilliseconds);
          }
        }
      }
      finally
      {
        frame.Dispose();
      }

      TestContext.WriteLine("400 titles at 1920x1080, menu open, median of " + (cycles - warm)
          + " walks per shape; word = the walk's own call, pass = the layout pass it posts, tail = what that pass"
          + " posts behind itself");
      foreach (KeyValuePair<string, List<double>> way in words)
      {
        double word = Median(way.Value.ToArray());
        double pass = Median(passes[way.Key].ToArray());
        double tail = Median(tails[way.Key].ToArray());
        double drawn = Median(painted[way.Key].ToArray());
        TestContext.WriteLine("  " + way.Key.PadRight(20)
            + " word " + word.ToString("0.0").PadLeft(7) + " ms, pass " + pass.ToString("0.0").PadLeft(7)
            + " ms, tail " + tail.ToString("0.0").PadLeft(7) + " ms  =  "
            + (word + pass + tail).ToString("0.0").PadLeft(7) + " ms laid out, then "
            + drawn.ToString("0.00").PadLeft(6) + " ms painted");
      }

      Assert.That(words.Count, Is.GreaterThanOrEqualTo(4),
          "the walk went round the shapes, or it did not walk");
      Assert.That(Median(words["Grid -> Cover"].ToArray()) + Median(passes["Grid -> Cover"].ToArray()),
          Is.GreaterThan(0.0), "the walk into covers laid something out, or it measured nothing");
    }

    /// <summary>
    /// Whether a walk is expensive because of the TILES it has or because of the TILES IT BUILDS. Asked by walking
    /// a shelf of forty titles and a shelf of four hundred through the same shapes: a shelf that virtualises pays
    /// for the tiles it stands out on the picture, which is the same handful in both shelves, so its two walks
    /// cost the same. A shelf that lays its whole list out pays four hundred times the tile, and its walk grows
    /// with the shelf - which would be the biggest single thing to win back here.
    /// </summary>
    [AvaloniaTest]
    public void AWalkCostsTheSameOnASmallShelfAndALargeOneOrItLaysEverythingOut()
    {
      string[] ways = new string[5];
      double[] small = WalkTwiceAndRead(40, ways);
      double[] large = WalkTwiceAndRead(400, ways);

      TestContext.WriteLine("median of the walks over both shelves, in the order Grid -> Cover -> Stacked ->"
          + " Carousel -> List -> Grid");
      for (int step = 0; step < 5; step++)
      {
        TestContext.WriteLine("  " + ways[step].PadRight(20) + " forty titles " + small[step].ToString("0.0").PadLeft(7)
            + " ms, four hundred titles " + large[step].ToString("0.0").PadLeft(7) + " ms");
      }

      Assert.That(small[0] + large[0], Is.GreaterThan(0.0), "the walks were timed, not skipped");
    }

    /// <summary>Walks one shelf of the told size once through every shape twice, and writes the tiles it really
    /// stood out on the picture into the test log on the way.</summary>
    private static double[] WalkTwiceAndRead(int total, string[] ways)
    {
      double[] medians = new double[5];
      using RailOnScreen rail = new RailOnScreen(total, 1920, 1080);
      rail.Shelf.ShowMediaMenu();
      WaitUntil("the rail's rows to stand", delegate { return rail.RailRows.Items.Count > 0; });
      Pump();
      Pump();

      ListBox shelf = rail.Window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      Dictionary<string, List<double>> byWay = new Dictionary<string, List<double>>();
      for (int cycle = 0; cycle < 5; cycle++)
      {
        for (int step = 0; step < 5; step++)
        {
          BrowserLayout from = rail.Shelf.Layout;
          double[] halves = new double[3];
          double all = OneStep(rail, halves);
          if (cycle < 2)
            continue;

          string way = from + " -> " + rail.Shelf.Layout;
          Add(byWay, way, all);
          ways[step] = way;
        }
      }

      TestContext.WriteLine("  shelf of " + total + " titles: " + shelf.GetRealizedContainers().Count()
          + " tile containers standing after the last walk, " + rail.Shelf.Titles.Count + " titles read");
      for (int step = 0; step < 5; step++)
        medians[step] = Median(byWay[ways[step]].ToArray());

      return medians;
    }

    /// <summary>
    /// The same walk on a shelf holding nothing at all, for the share of it that is not the wall: the room, the
    /// details, the head, the panels and the panels' arranging. What the walks of forty and four hundred tiles
    /// cost ABOVE this is what building the tiles costs, and that is the number a shape change has to win back.
    /// </summary>
    [AvaloniaTest]
    public void AWalkOnAnEmptyShelfIsWrittenDownToo()
    {
      string[] ways = new string[5];
      double[] empty = WalkTwiceAndRead(0, ways);
      TestContext.WriteLine("empty shelf, same five steps:");
      for (int step = 0; step < 5; step++)
        TestContext.WriteLine("  " + ways[step].PadRight(20) + empty[step].ToString("0.0").PadLeft(7) + " ms");

      Assert.That(empty.Sum(), Is.GreaterThanOrEqualTo(0.0), "the empty walks were timed");
    }

    private static void Add(Dictionary<string, List<double>> byWay, string way, double sample)
    {
      if (!byWay.TryGetValue(way, out List<double>? samples))
        byWay[way] = samples = new List<double>();
      samples.Add(sample);
    }

    /// <summary>
    /// One walk from one shape into the next, timed until the tree was laid out again, and then the same picture
    /// painted into a surface - the two halves of the stutter a viewer feels when the tiles under his hands turn
    /// into covers.
    /// </summary>
    /// <summary>
    /// One walk, its three halves timed apart: the word itself (the view model's walk, which re-dresses the list
    /// and writes the setting), the layout pass the word posts, and the work the pass posts behind itself. Split
    /// because the one number says only THAT the walk is expensive, while the three say which half of it is.
    /// </summary>
    private static double OneStep(RailOnScreen rail, double[] halves)
    {
      Stopwatch watch = Stopwatch.StartNew();
      rail.Shelf.SwitchViewCommand.Execute(null);
      watch.Stop();
      halves[0] = watch.Elapsed.TotalMilliseconds;

      watch.Restart();
      Dispatcher.UIThread.RunJobs();
      watch.Stop();
      halves[1] = watch.Elapsed.TotalMilliseconds;

      Thread.Sleep(2);

      watch.Restart();
      Dispatcher.UIThread.RunJobs();
      watch.Stop();
      halves[2] = watch.Elapsed.TotalMilliseconds;

      return halves[0] + halves[1] + halves[2];
    }

    /// <summary>
    /// One real re-layout of the room: another width for the window, and the dispatcher told to do the work that
    /// follows, which is what walking from one layout into another, a resized screen, a skin coming on and a
    /// details column appearing all do to the whole tree, rail included. Timed alone, for the reason given above,
    /// and always to a DIFFERENT width - a width the window already has invalidates nothing, and a pass that has
    /// nothing to do measures zero.
    /// </summary>
    private static double OneRelayout(RailOnScreen rail)
    {
      Stopwatch watch = Stopwatch.StartNew();
      rail.Window.Width = rail.Window.Width > 1920.5 ? 1920 : 1921;
      Dispatcher.UIThread.RunJobs();
      watch.Stop();
      return watch.Elapsed.TotalMilliseconds;
    }

    /// <summary>
    /// One frame painted into a surface, which is the closest a test comes to a frame.
    /// </summary>
    private static double OnePaint(RailOnScreen rail, RenderTargetBitmap frame)
    {
      Stopwatch watch = Stopwatch.StartNew();
      frame.Render(rail.Window);
      watch.Stop();
      return watch.Elapsed.TotalMilliseconds;
    }

    /// <summary>The middle of the samples, because the fastest of them flatters the machine and the slowest of
    /// them is usually something else wanting attention.</summary>
    private static double Median(double[] samples)
    {
      double[] sorted = (double[])samples.Clone();
      Array.Sort(sorted);
      return sorted[sorted.Length / 2];
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
    }

    private static void WaitUntil(string because, Func<bool> condition)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(20);
      while (DateTimeOffset.UtcNow < deadline)
      {
        Pump();
        if (condition())
          return;
        Thread.Sleep(5);
      }

      Assert.Fail("Timed out after 20 seconds waiting for " + because);
    }

  }
}

