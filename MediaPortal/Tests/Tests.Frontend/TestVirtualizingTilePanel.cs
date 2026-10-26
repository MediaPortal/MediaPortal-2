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
using Avalonia.Input;
using Avalonia.Interactivity;
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
  /// The tiles' panel: that a shelf of a thousand titles only builds the rows the screen could be looking at,
  /// that it keeps the wrap panel's grid, and that a container leaving the top comes back in at the bottom
  /// instead of being made again.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This exists because a wrap panel lays out a whole film library at once - eleven hundred of the skin's tile
  /// frames measured 1.7 seconds before the first picture reached the screen, while the row view, which is
  /// virtualised, needed a hundredth of that. The reference never pays the price, and neither may this one.
  /// What is proven here is therefore counting, geometry and identity: how many containers stand, where they
  /// stand, and whether they are the same ones they were before the shelf was rolled.
  /// </para>
  /// <para>
  /// Every test names its own settings folder through MP2_SKIN_SETTINGS_DIR and MP2_SCREEN_VIEWS_FILE, so none
  /// of them reads or writes what a real viewer on this machine set, and all of them take the worn skin off
  /// again afterwards.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestVirtualizingTilePanel
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
      _ownDir = Path.Combine(Path.GetTempPath(), "mp2-tilepanel-" + Guid.NewGuid().ToString("N"));
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

    #region The shelf on the screen

    /// <summary>
    /// A shelf of as many titles as it is told, asked for in the sizes the screen asks in, and silent about
    /// pictures - a picture the tests were measuring would only say how fast the test's own server is.
    /// </summary>
    private class TiledShelf : HttpMessageHandler
    {
      internal int Total { get; set; } = 1200;

      internal int PicturesAsked { get; private set; }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;

        if (path.Contains("/fanart", StringComparison.Ordinal))
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });

        if (path.EndsWith("/thumbnail", StringComparison.Ordinal))
        {
          PicturesAsked++;
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

    /// <summary>A shelf's titles on the screen as a viewer would find them: in the skin, in a window.</summary>
    private class ShelfOnScreen : IDisposable
    {
      internal ShellWindow Window { get; }
      internal ListBox Titles { get; }
      internal VirtualizingTilePanel Panel { get; }
      internal MediaBrowserViewModel Shelf { get; }
      internal ScrollViewer Scroller { get; }

      internal ShelfOnScreen(TiledShelf server, int total, double width, double height)
      {
        Shelf = new MediaBrowserViewModel(
            new ServerApiClient(SERVER_URL, new HttpClient(server)), MOVIES);
        Shelf.OpenCommand.Execute(null);
        WaitUntil("the shelf to be read", delegate { return Shelf.Titles.Count == total; });

        Window = new ShellWindow(new MediaBrowserView(Shelf)) { Width = width, Height = height };
        Window.Show();
        Pump();
        Pump();

        Titles = Window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "titlesList");
        Scroller = Titles.GetVisualDescendants().OfType<ScrollViewer>().First();
        WaitUntil("a first row to stand", delegate
        {
          return Standing().Count > 0 && Scroller.Extent.Height > Scroller.Viewport.Height;
        });

        Panel = Window.GetVisualDescendants().OfType<VirtualizingTilePanel>().FirstOrDefault()
            ?? throw new Exception("a tiled shelf must stand on a VirtualizingTilePanel; the list is drawn by: "
                + string.Join(", ", Titles.GetVisualDescendants().OfType<Panel>().Select(p => p.GetType().Name)
                                  .Distinct()));
      }

      internal void Roll(int toTitle)
      {
        Titles.ScrollIntoView(toTitle);
        WaitUntil("the shelf to roll", delegate
        {
          return Panel.FirstStandingIndex <= toTitle && Panel.FirstStandingIndex + Panel.StandingCount > toTitle;
        });
        Pump();
      }

      internal void PumpForLayout()
      {
        Pump();
        Pump();
      }

      internal IList<Control> Standing()
      {
        return Titles.GetRealizedContainers().OfType<Control>().ToList();
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
        }

        Assert.Fail("Timed out after 20 seconds waiting for " + because);
      }
    }

    private static ShelfOnScreen TiledShelfOnScreen(int total = 1200, double width = 1920, double height = 1080)
    {
      ShelfOnScreen shown = new ShelfOnScreen(new TiledShelf { Total = total }, total, width, height);
      Assert.That(shown.Shelf.Layout, Is.EqualTo(BrowserLayout.Grid),
          "a film shelf opens as tiles, or nothing here is testing tiles");
      return shown;
    }

    #endregion

    [AvaloniaTest]
    public void AThousandTitlesStandOnlyWhereTheScreenCouldBeLooking()
    {
      using ShelfOnScreen shelf = TiledShelfOnScreen();
      int standing = shelf.Panel.StandingCount;
      int children = shelf.Panel.Children.Count;

      TestContext.WriteLine("tile panel: " + shelf.Shelf.Titles.Count + " titles, " + standing
          + " containers standing, " + children + " children on the panel, " + shelf.Panel.Columns + " columns");

      Assert.That(standing, Is.GreaterThan(0), "the tiles the viewer is looking at are there");
      Assert.That(standing, Is.LessThan(shelf.Shelf.Titles.Count / 4),
          "and the shelf is not holding a container for every title it has read - which is the whole reason "
          + "this panel exists instead of a wrap panel");
      Assert.That(standing, Is.LessThan(220),
          "a screenful with a screen's worth above and below is a few dozen tiles, however large the shelf is");
      Assert.That(children, Is.LessThan(260),
          "and the pooled containers are not standing on the tree while nobody uses them");
    }

    [AvaloniaTest]
    public void TheShelfStretchesToEveryTitleItHasReadEvenThoughOnlyRowsStand()
    {
      using ShelfOnScreen shelf = TiledShelfOnScreen();
      int columns = shelf.Panel.Columns;
      int count = shelf.Shelf.Titles.Count;
      int rows = (int)Math.Ceiling(count / (double)columns);
      double cell = shelf.Standing()[0].Bounds.Height;

      Assert.That(columns, Is.GreaterThan(2), "a thousand titles are laid in columns, not in one long line");
      Assert.That(shelf.Scroller.Extent.Height, Is.EqualTo(rows * cell).Within(2),
          "the scrollbar knows the whole shelf, although only the rows in sight are built");
      Assert.That(shelf.Scroller.Offset.Y, Is.EqualTo(0));
    }

    [AvaloniaTest]
    public void TitlesStandInTheirColumnAndRowTheWayAWrapPanelStandsThem()
    {
      using ShelfOnScreen shelf = TiledShelfOnScreen();
      int columns = shelf.Panel.Columns;
      shelf.Roll(columns + 1);

      Control[] standing = shelf.Standing().ToArray();
      Control first = standing[0];
      Control second = standing[1];
      Control underTheFirst = standing[columns];

      Assert.That(second.Bounds.X, Is.EqualTo(first.Bounds.Right).Within(1),
          "the next title is next door, not under it");
      Assert.That(second.Bounds.Y, Is.EqualTo(first.Bounds.Y).Within(1), "and it is in the same row");
      Assert.That(underTheFirst.Bounds.X, Is.EqualTo(first.Bounds.X).Within(1),
          "the title a row down is in the same column");
      Assert.That(underTheFirst.Bounds.Y, Is.EqualTo(first.Bounds.Bottom).Within(1),
          "and rows sit one under the other, margins and all - the tile frame carries the spacing, so a row is "
          + "as high as the tile is tall plus its margin");
      Assert.That(first.Bounds.Height, Is.GreaterThan(200), "a poster tile is as high as the skin says");
    }

    [AvaloniaTest]
    public void AContainerThatLeftTheTopComesBackInAtTheBottom()
    {
      using ShelfOnScreen shelf = TiledShelfOnScreen();
      int count = shelf.Shelf.Titles.Count;
      IList<Control> atTheStart = shelf.Standing();
      int standingAtTheStart = atTheStart.Count;

      shelf.Roll(count - 1);
      IList<Control> atTheEnd = shelf.Standing();
      EveryStandingContainerWearsItsOwnTitle(shelf, atTheEnd, "after rolling to the far end of the shelf");

      Assert.That(shelf.Panel.FirstStandingIndex, Is.GreaterThan(900),
          "the shelf rolled to the titles it had read but never built");
      Assert.That(atTheEnd.Any(row => ReferenceEquals(row.DataContext, shelf.Shelf.Titles[count - 1])),
          Is.True,
          "the last title of the shelf stands, although it was never built until it was asked for");
      Assert.That(atTheEnd.Count, Is.LessThan(shelf.Shelf.Titles.Count / 4));

      shelf.Roll(0);
      IList<Control> backAtTheStart = shelf.Standing();
      EveryStandingContainerWearsItsOwnTitle(shelf, backAtTheStart, "after rolling back to where the shelf began");

      Assert.That(backAtTheStart.Intersect(atTheEnd), Is.Not.Empty,
          "the containers the bottom row handed back are the ones the top row got again - the pool is what "
          + "keeps a long shelf from building the same tile frame a thousand times");
      Assert.That(backAtTheStart.Count, Is.EqualTo(standingAtTheStart),
          "and rolling back to where the shelf started leaves exactly as much standing as stood there");

      Console.WriteLine($"tile panel over a roll of {count} titles: built={shelf.Panel.ContainersBuilt} "
          + $"reused={shelf.Panel.ContainersReused} standing={standingAtTheStart}");
      Assert.That(shelf.Panel.ContainersBuilt, Is.LessThan(count / 2),
          "a shelf rolled to its far end must not build a container for every title it passes on the way");
    }

    /// <summary>
    /// The one thing a pooling shelf must never get wrong: a container that stands between two titles wears the
    /// title of the row it stands in, never the one it was first built for.
    /// </summary>
    private static void EveryStandingContainerWearsItsOwnTitle(ShelfOnScreen shelf, IList<Control> standing,
        string when)
    {
      Assert.That(
          standing.Select(container => shelf.Shelf.Titles.IndexOf((TitleViewModel)container.DataContext!)),
          Is.EquivalentTo(Enumerable.Range(shelf.Panel.FirstStandingIndex, shelf.Panel.StandingCount)),
          when + ": every container that stands wears the title of the row it stands in");
    }

    [AvaloniaTest]
    public void AnArrowDownWalksOneRowDownAndBringsItInSight()
    {
      using ShelfOnScreen shelf = TiledShelfOnScreen();
      int columns = shelf.Panel.Columns;
      shelf.Titles.Focus();
      shelf.Shelf.Selected = shelf.Shelf.Titles[0];
      shelf.PumpForLayout();

      shelf.Titles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Down,
        Source = shelf.Titles
      });
      shelf.PumpForLayout();

      Assert.That(shelf.Shelf.Titles.IndexOf(shelf.Shelf.Selected), Is.EqualTo(columns),
          "down is the tile under this one - the same place in the row below, not the next title in the list, "
          + "which would be the tile beside it");

      shelf.Titles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Right,
        Source = shelf.Titles
      });
      shelf.PumpForLayout();
      Assert.That(shelf.Shelf.Titles.IndexOf(shelf.Shelf.Selected), Is.EqualTo(columns + 1),
          "and right is one tile across");

      shelf.Titles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Up,
        Source = shelf.Titles
      });
      shelf.PumpForLayout();
      Assert.That(shelf.Shelf.Titles.IndexOf(shelf.Shelf.Selected), Is.EqualTo(1),
          "and up walks back the row");
    }

    [AvaloniaTest]
    public void GrowingTheShelfBehindLeavesTheStandingRowsAlone()
    {
      // A shelf of 130 titles is read in two goes: the first screenful, then the rest behind it. The rows the
      // viewer is looking at must not notice that second go in any way at all.
      TiledShelf server = new TiledShelf { Total = 130 };
      MediaBrowserViewModel growing = new MediaBrowserViewModel(
          new ServerApiClient(SERVER_URL, new HttpClient(server)), MOVIES);
      ShellWindow window = new ShellWindow(new MediaBrowserView(growing))
      {
        Width = 1920,
        Height = 1080
      };
      window.Show();

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "titlesList");
      VirtualizingTilePanel panel = window.GetVisualDescendants().OfType<VirtualizingTilePanel>().First();

      WaitUntil("the first screenful to stand", delegate
      {
        return growing.Titles.Count >= 120 && panel.StandingCount > 0;
      });
      Control[] before = titles.GetRealizedContainers().OfType<Control>().ToArray();

      WaitUntil("the rest of the shelf to arrive", delegate { return growing.Titles.Count == 130; });
      for (int i = 0; i < 5; i++)
        Pump();

      Control[] after = titles.GetRealizedContainers().OfType<Control>().ToArray();
      Assert.That(after, Is.EquivalentTo(before),
          "a shelf that grows at the back leaves every standing container where it was - the rows would "
          + "otherwise blink while the viewer is reading them");

      window.Close();
      Pump();
    }

    [AvaloniaTest]
    public void LayingAThousandTilesOutIsTheWorkOfAFewContainers()
    {
      // The number this whole change was made for: the time between the shelf arriving and the tiles standing.
      // Measured here rather than once by hand, so that a later change paying the wrap panel's price again is
      // caught by the suite and not by a viewer.
      using ShelfOnScreen shelf = TiledShelfOnScreen();

      Stopwatch watch = Stopwatch.StartNew();
      shelf.Roll(shelf.Shelf.Titles.Count - 1);
      watch.Stop();

      TestContext.WriteLine("rolling to the last of " + shelf.Shelf.Titles.Count + " titles: "
          + watch.ElapsedMilliseconds + " ms with " + shelf.Panel.StandingCount + " containers standing");

      Assert.That(watch.ElapsedMilliseconds, Is.LessThan(2500),
          "rolling to the end of a film library is a matter of seconds at most; the wrap panel needed 1.7 "
          + "seconds before the shelf had even been laid out once");
      Assert.That(shelf.Panel.StandingCount, Is.LessThan(220));
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
      }

      Assert.Fail("Timed out after 20 seconds waiting for " + because);
    }
  }
}
