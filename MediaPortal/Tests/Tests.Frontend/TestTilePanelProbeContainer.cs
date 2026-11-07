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
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using NUnit.Framework;
using System;
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
  /// Counts the children the tiles' panel actually holds after it has been laid out many times.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A probe, not a claim. The panel measures ONE container to learn the cell's size (VirtualizingTilePanel.cs,
  /// TheCell), and it asks for that container through the same door a standing tile comes in by (TakeOrMake),
  /// which puts it ON the tree. Nothing in the panel hands it back afterwards: it is not among the standing rows,
  /// not in the pool and not in one of the two holds. So either the framework takes it back somewhere away from
  /// this file, or every layout pass leaves one more tile hanging on the shelf - never arranged, so nothing of it
  /// is painted, but wearing its title and therefore its picture, and walked by every pass over the visual tree.
  /// </para>
  /// <para>
  /// This is the one measurement the panel's own counters cannot make: ContainersBuilt does not climb while the
  /// pool is deep enough to serve the probe, and GetRealizedContainers answers with the standing rows only. Both
  /// stay pretty while the tree fills.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTilePanelProbeContainer
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
      _ownDir = Path.Combine(Path.GetTempPath(), "mp2-probecount-" + Guid.NewGuid().ToString("N"));
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

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
    }

    /// <summary>The middle of the samples between the two told bounds.</summary>
    private static double Median(double[] samples, int from, int to)
    {
      double[] window = samples.Skip(from).Take(to - from).ToArray();
      Array.Sort(window);
      return window[window.Length / 2];
    }

    /// <summary>How deep the tree of one tile goes, counted from the tile itself.</summary>
    private static int Deepest(Visual visual, int depth)
    {
      int deepest = depth;
      foreach (Visual child in visual.GetVisualChildren())
        deepest = Math.Max(deepest, Deepest(child, depth + 1));
      return deepest;
    }

    private static void WaitUntil(string because, Func<bool> condition)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
      while (DateTimeOffset.UtcNow < deadline)
      {
        Pump();
        if (condition())
          return;
        Thread.Sleep(5);
      }

      Assert.Fail("Timed out after 30 seconds waiting for " + because);
    }

    /// <summary>
    /// Lays a shelf of six hundred titles out a hundred and twenty times over - which is what a scrolled shelf
    /// does - and writes down how many children the panel holds against how many tiles really stand.
    /// </summary>
    [AvaloniaTest]
    public void AHundredLayoutPassesLeaveNoTilesBehindThatDoNotStand()
    {
      MediaBrowserViewModel shelf = new MediaBrowserViewModel(
          new ServerApiClient(SERVER_URL, new HttpClient(new CountedShelf { Total = 600 })), MOVIES);
      shelf.OpenCommand.Execute(null);
      WaitUntil("the shelf to be read", delegate { return shelf.Titles.Count == 600; });

      MediaBrowserView view = new MediaBrowserView(shelf);
      ShellWindow window = new ShellWindow(view) { Width = 1920, Height = 1080 };
      window.Show();
      Pump();
      Pump();

      VirtualizingTilePanel? panel = window.GetVisualDescendants().OfType<VirtualizingTilePanel>()
          .FirstOrDefault(wall => wall.StandingCount > 0);
      Assert.That(panel, Is.Not.Null, "the wall of tiles is laid by the tile panel, or there is nothing to count");

      int children_first = panel!.GetVisualChildren().Count();
      int standing_first = panel.StandingCount;
      int built_first = panel.ContainersBuilt;
      int reused_first = panel.ContainersReused;
      TestContext.WriteLine("after the first layout: " + children_first + " children, " + standing_first
          + " standing, " + panel.ContainersBuilt + " ever built, " + panel.ContainersReused + " reused");

      // What one tile is made of, since the whole price of a wall is this number times the tiles it stands.
      Visual tile = panel.GetVisualChildren().First();
      Visual[] inside = tile.GetVisualDescendants().ToArray();
      TestContext.WriteLine("one tile container holds " + inside.Length + " visuals: "
          + inside.Count(v => v is TextBlock) + " texts, "
          + inside.Count(v => v is Avalonia.Controls.Shapes.Shape) + " shapes, "
          + inside.Count(v => v is Image) + " images, "
          + inside.OfType<Decorator>().Count() + " decorators, "
          + inside.OfType<Panel>().Count() + " panels; deepest nesting " + Deepest(tile, 0)
          + ", effects on " + inside.OfType<Visual>().Count(v => v is Visual { Effect: not null })
          + " of them, opacity under one on "
          + inside.OfType<Visual>().Count(v => v.Opacity < 1.0) + ", clipped "
          + inside.OfType<Visual>().Count(v => v.ClipToBounds) + " (the tile itself is "
          + tile.GetType().Name + ")");

      // The list carries its caret into sight on its own, which is right for a viewer and would have a test's
      // scroll snap back to the standing tile every pass; the measurement below therefore asks for the passes
      // themselves rather than moving a scroller.
      int stood_at_first = panel.FirstStandingIndex;
      double[] pass_ms = new double[120];
      for (int pass = 0; pass < 120; pass++)
      {
        // Every pass is one the front end really asks for: a scroll step, a tile dressed, a picture arriving and
        // a key pressed all end here. The cell is measured from one container per pass, whichever asks.
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        panel.InvalidateMeasure();
        Pump();
        watch.Stop();
        pass_ms[pass] = watch.Elapsed.TotalMilliseconds;
      }

      int children_end = panel.GetVisualChildren().Count();
      TestContext.WriteLine("after 120 layout passes: " + children_end + " children, " + panel.StandingCount
          + " standing, " + panel.ContainersBuilt + " ever built, " + panel.ContainersReused + " reused");
      TestContext.WriteLine("=> the band stands where it stood (" + stood_at_first + " -> "
          + panel.FirstStandingIndex + "); " + (children_end - children_first)
          + " children came onto the panel, of which " + (panel.StandingCount - standing_first)
          + " are standing tiles; " + (panel.ContainersBuilt - built_first) + " containers were BUILT along the"
          + " way and the pool handed back " + (panel.ContainersReused - reused_first) + " of them");
      TestContext.WriteLine("   one pass cost " + Median(pass_ms, 0, 40).ToString("0.00") + " ms at the start and "
          + Median(pass_ms, 80, 120).ToString("0.00") + " ms after eighty more tiles hung on the tree");

      Assert.That(children_end - children_first, Is.LessThan(40),
          "a scrolled panel does not keep one tile for every layout pass");
      window.Close();
      Pump();
    }

    /// <summary>A shelf of titles that answers in json and says nothing about pictures.</summary>
    private class CountedShelf : HttpMessageHandler
    {
      internal int Total { get; set; } = 600;

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
  }
}
