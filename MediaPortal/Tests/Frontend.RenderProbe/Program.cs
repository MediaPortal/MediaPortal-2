/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MediaPortal.Frontend;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace FrontendProbe
{
  /// <summary>
  /// Renders the real home screen on a host without a display and writes the frame out as an image.
  /// </summary>
  /// <remarks>
  /// What the test suite already proves, and what only this proves: <c>Tests.Frontend</c> runs on Avalonia's
  /// pure-managed headless rasterizer (<c>UseHeadlessDrawing = true</c>), which is fast and needs nothing from
  /// the platform - and therefore never executes the Skia path that Windows, Linux and Android draw with, never
  /// shapes glyphs through the font manager, and never encodes an image. "The tests pass" is not "it draws".
  /// This probe switches the headless platform's fake rendering off, shows the same <see cref="HomeWindow"/> the
  /// application shows, captures the frame, prints which render path actually ran, and fails if the frame is
  /// flat. The PNG is for a person to look at; the printed numbers are what a gate judges, because a gate cannot
  /// open an image and know whether it is right.
  /// </remarks>
  public static class RenderProbe
  {
    private const string USAGE =
        "Frontend.RenderProbe --out <file.png> [--screen <home|nowplaying|connect|browser|details|search|seen>] [--server <url>] [--width <n>] [--height <n>]\n" +
        "  --out <file>     where to write the rendered frame (PNG); its directory is created\n" +
        "  --screen <name>  which screen to render (default home)\n" +
        "  --server <url>   render the populated screen by asking this server (otherwise MP2_SERVER,\n" +
        "                   otherwise the screen's own 'no server' state is rendered - a real state too)\n" +
        "  --width/--height window size in device-independent pixels (default 1280x720, a 720p TV)";

    /// <summary>
    /// Runs the probe. Exit codes: <c>0</c> a frame with real content was rendered and written, <c>1</c> the
    /// render produced nothing worth looking at, <c>2</c> the arguments were wrong, <c>3</c> the image could not
    /// be written or is not a PNG, <c>4</c> a populated screen was asked for and the server answered no aspects.
    /// </summary>
    public static int Main(string[] args)
    {
      string? outPath = null;
      string? serverUrl = null;
      string screenName = "home";
      int width = 1280;
      int height = 720;

      for (int i = 0; i < args.Length; i++)
      {
        switch (args[i])
        {
          case "--out":
            if (i + 1 >= args.Length)
              return Usage("--out needs a file name.");
            outPath = args[++i];
            break;
          case "--screen":
            if (i + 1 >= args.Length)
              return Usage("--screen needs a name.");
            screenName = args[++i].Trim().ToLowerInvariant();
            break;
          case "--server":
            if (i + 1 >= args.Length)
              return Usage("--server needs a URL.");
            serverUrl = args[++i];
            break;
          case "--width":
            if (i + 1 >= args.Length || !int.TryParse(args[++i], out width))
              return Usage("--width needs a number.");
            break;
          case "--height":
            if (i + 1 >= args.Length || !int.TryParse(args[++i], out height))
              return Usage("--height needs a number.");
            break;
          case "--help":
          case "-h":
            Console.WriteLine(USAGE);
            return 0;
          default:
            return Usage("unknown argument '" + args[i] + "'.");
        }
      }

      if (outPath is null)
        return Usage("--out is required.");
      if (width < 100 || height < 100)
        return Usage("a window below 100x100 would not prove anything about rendering.");

      Console.WriteLine("host:     " + RuntimeInformation.FrameworkDescription + ", "
          + RuntimeInformation.OSDescription + ", " + RuntimeInformation.OSArchitecture);

      // The one line that separates this from the test suite: the headless platform without its stub render
      // interface, so drawing goes through the real one and the frame is real. `UseSkia()` is not decoration -
      // with headless drawing switched off the platform no longer supplies any rendering system at all, and
      // Setup() answers "No rendering system configured. Consider calling UseSkia()." (measured).
      try
      {
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseSkia()
            .WithInterFont()
            .SetupWithoutStarting();
      }
      catch (Exception ex)
      {
        // The whole chain: "TypeInitializationException for SkiaSharp.SKImageInfo" tells you nothing, while its
        // inner DllNotFoundException names the system library the image is missing (measured on Debian: the
        // native libSkiaSharp.so needs fontconfig and freetype, and without them it never loads).
        Console.WriteLine("FAILED:   the application could not be set up: " + ex);
        foreach (string native in Directory.GetFiles(AppContext.BaseDirectory)
            .Where(f => f.EndsWith(".so", StringComparison.OrdinalIgnoreCase)
                     || f.Contains("Skia", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.Ordinal))
        {
          Console.WriteLine("          layout contains " + Path.GetFileName(native)
              + " (" + new FileInfo(native).Length + " bytes)");
        }
        return 1;
      }

      Application? application = Application.Current;
      if (application is null)
      {
        Console.WriteLine("FAILED:   no Application after SetupWithoutStarting().");
        return 1;
      }

      string skia = DescribeSkia();
      Console.WriteLine("skia:     " + skia);
      if (skia.StartsWith("the Avalonia.Skia", StringComparison.Ordinal))
      {
        Console.WriteLine("FAILED:   this probe exists to measure the Skia path; without it the run says nothing.");
        return 1;
      }
      Console.WriteLine("theme:    " + application.Styles.Count + " style set(s), requested variant "
          + application.ActualThemeVariant);

      // A URL from the command line, else MP2_SERVER, else nothing - and "nothing" is a mode, not a failure: it
      // renders the screen a TV shows when the server is down. A URL that does not parse stops the run instead
      // of quietly rendering a different picture than the one that was asked for.
      string? url = string.IsNullOrWhiteSpace(serverUrl)
          ? Environment.GetEnvironmentVariable("MP2_SERVER")
          : serverUrl;
      bool online = !string.IsNullOrWhiteSpace(url);

      Window window;

      // Set when the frame to render is the list with its details panel open. The key is pressed once the window
      // is on screen, because that is when a viewer would press it - and the panel has to survive the layout the
      // window does when it opens.
      bool detailsAsked = false;

      // The same for a list that has been searched: the words are typed into the box on screen and Enter is sent
      // to the box, so the frame covers the key handling, the round trip and the list coming back smaller.
      bool searchAsked = false;

      // And the same for a list that has been filtered by watched state: the key is pressed on the window, twice,
      // so that the frame is the third of the three states with its rows carrying the mark the server gave them.
      bool seenAsked = false;
      HomeViewModel? home = null;
      ConnectionViewModel? connection = null;
      string playbackStatus = "";
      VideoFrameSink? frames = null;
      Func<long>? pictureFrames = null;

      if (screenName.Equals("nowplaying", StringComparison.Ordinal))
      {
        // The playback screen in the state a viewer spends almost all their time in: something playing, halfway
        // through. The player here is scripted rather than libVLC, on purpose - the frame has to be the same on
        // Windows and in a container for the two pictures to mean the same thing, and which frame a real decoder
        // has drawn when the probe captures is nobody's promise. What the screen does with a position, a state
        // and a name is what is being measured.
        if (online)
          Console.WriteLine("note:     --server belongs to the home screen; the playback screen is scripted instead");

        ScriptedPlayer player = new ScriptedPlayer();
        NowPlayingViewModel playback =
            new NowPlayingViewModel(player, action => Dispatcher.UIThread.Post(action));
        playback.Play(MediaLocation.FromFile(
            Path.Combine(Path.GetTempPath(), "mp2-render", "the-big-film.mkv")));

        // Halfway through a minute and a half, because a bar that is half filled is also the proof that the
        // binding is a fraction of the length and not a count of milliseconds.
        player.RaisePosition(45_000, 90_000);
        Pump(TimeSpan.FromMilliseconds(250));

        // A picture, and deliberately not one from a decoder: the frame sink is filled by hand so that the same
        // bytes arrive at the screen on every host, while still going through the whole road a real film takes -
        // sink, copy, bitmap, compositor, file. A black rectangle would have proved that the screen draws; this
        // proves that a film would be visible on it.
        frames = new VideoFrameSink();
        if (!TestPattern.Write(frames, 640, 360))
        {
          Console.WriteLine("FAILED:   the frame sink refused the test picture, so there is nothing to draw.");
          return 1;
        }

        playbackStatus = playback.StateText + ", " + playback.PositionText;
        window = new NowPlayingWindow(playback, frames);
        pictureFrames = () => ((NowPlayingWindow)window).FramesDrawn;
      }
      else if (screenName.Equals("home", StringComparison.Ordinal))
      {
        try
        {
          home = online
              ? new HomeViewModel(new ServerApiClient(ServerApiClient.ResolveBaseUrl(url!)))
              : new HomeViewModel();
        }
        catch (ArgumentException ex)
        {
          Console.WriteLine("FAILED:   " + ex.Message);
          return 2;
        }

        Console.WriteLine("content:  " + (online
            ? "populated from " + home.ServerUrl
            : "offline - the screen's own 'no server' state is what gets rendered"));

        window = new HomeWindow(home);
      }
      else if (screenName.Equals("connect", StringComparison.Ordinal))
      {
        if (online)
          Console.WriteLine("note:     --server belongs to the home screen; the server screen starts from the address"
              + " this machine saved instead");

        // The screen exactly as a first start shows it: whatever address this machine happens to have saved, which
        // on a machine that was never configured is none. Filling the field in by hand would render a picture that
        // nobody ever sees.
        ServerSettings saved = ServerSettings.Load(null, out string? readProblem);
        connection = new ConnectionViewModel(saved, null, readProblem);

      // The frame has to be the same picture every time this runs, so the search the screen starts by itself is
      // switched off: what is drawn is the screen as it looks in the moment before it asks the network anything.
      // Whether the search works is not a question about pixels; the discovery tests and the SSDP gate answer it.
      connection.AutoScan = false;
        Console.WriteLine("content:  " + (saved.HasUsableUrl
            ? "the address saved in " + connection.SettingsFile
            : "no address saved - the empty field, the explanation and the two buttons that are not offered yet"));

        window = new ConnectionWindow(connection);
      }
      else if (screenName.Equals("browser", StringComparison.Ordinal)
          || screenName.Equals("details", StringComparison.Ordinal)
          || screenName.Equals("search", StringComparison.Ordinal)
          || screenName.Equals("seen", StringComparison.Ordinal))
      {
        if (online)
          Console.WriteLine("note:     --server belongs to the home screen; the title list is scripted instead");

        // The library list with something in it. Scripted rather than asked of the gate's server, whose database
        // is empty on purpose: whether a row of titles is laid out and legible from a sofa cannot depend on
        // whether whoever runs the gate happens to have imported anything. The titles arrive in the server's own
        // answer shape (see <see cref="ScriptedTitles"/>), so the frame covers the parsing and the bindings too.
        HomeSection films = new HomeSection("Films",
            new AspectViewModel(new MediaAspect(Guid.Empty, "MovieItem", true, false, null,
                Array.Empty<AspectAttribute>())));

        MediaBrowserViewModel browser = new MediaBrowserViewModel(
            new ServerApiClient(new Uri("http://127.0.0.1:1/MediaPortal/Server/"),
                new HttpClient(new ScriptedTitles())), films);
        browser.OpenCommand.Execute(null);
        Pump(TimeSpan.FromMilliseconds(500));

        Console.WriteLine("content:  " + browser.Titles.Count + " title(s) under \"" + browser.Heading
            + "\", status: " + browser.Status);
        if (browser.Titles.Count == 0)
        {
          Console.WriteLine("FAILED:   the list came up empty, so the frame would show an empty screen and prove nothing.");
          return 1;
        }

        detailsAsked = screenName.Equals("details", StringComparison.Ordinal);
        searchAsked = screenName.Equals("search", StringComparison.Ordinal);
        seenAsked = screenName.Equals("seen", StringComparison.Ordinal);
        window = new MediaBrowserWindow(browser, null);
      }
      else
      {
        return Usage("unknown screen '" + screenName + "'; this probe renders home, nowplaying, connect, browser,"
            + " details, search or seen.");
      }

      window.Width = width;
      window.Height = height;
      window.Show();

      // Layout and rendering are asynchronous even with no display: a render timer and a dispatcher queue do the
      // work. Pump until the window has real bounds, and - if a server was given - until the list has content,
      // always with a deadline, so that a regression fails instead of hanging the gate.
      if (!PumpUntil(() => window.Bounds.Width >= 50 && window.Bounds.Height >= 50, TimeSpan.FromSeconds(10)))
      {
        Console.WriteLine("FAILED:   the window never got a size (bounds " + window.Bounds + ").");
        return 1;
      }

      if (pictureFrames != null)
      {
        // The surface pulls the newest picture on a timer, so the picture only reaches the bitmap while the
        // dispatcher is running. Asking for it before that is what made this check fail on its first run, for no
        // reason but timing - and it is the reason the pump comes first and the number is read afterwards.
        PumpUntil(() => pictureFrames() > 0, TimeSpan.FromSeconds(5));
        long drawn = pictureFrames();
        Console.WriteLine("picture:  " + drawn + " frame(s) drawn on the screen"
            + (drawn > 0 ? "" : " - the video surface stayed empty"));
        if (drawn == 0)
        {
          // A playback screen rendered without its picture is a black rectangle, and a gate that called that
          // green would be measuring the wrong thing entirely.
          Console.WriteLine("FAILED:   nothing reached the video surface, so this frame has no film in it.");
          return 1;
        }
      }

      if (online && home != null)
      {
        // The window reads the server when it opens - the same command the button runs - so the probe does not
        // start a second read of its own. Firing one here is what crashed this probe's first run: two runs were
        // then in flight deliberately, and surviving that is the view model's job, not something to work around
        // here. Pumping is what a running application does while it waits.
        //
        // Asking for a populated screen and getting none is a failure, not a note. A frame of an error screen
        // drawn against a URL is worthless as evidence, and a gate that received exit 0 from it would have no way
        // of knowing - which is why this returns 1 rather than rendering on.
        if (!PumpUntil(() => home.Aspects.Count > 0, TimeSpan.FromSeconds(30)))
        {
          Console.WriteLine("FAILED:   a populated screen was asked for ('" + url + "'), but the server answered no"
              + " aspects. Status says: '" + home.Status + "'.");
          return 4;
        }
      }

      if (detailsAsked && window is MediaBrowserWindow detailsScreen)
      {
        // The key a viewer presses, raised the way the remote raises it, rather than the view model's command
        // called from here: the frame is then the proof that the key, the handler on the window and the panel
        // hang together, and not merely that the panel can be made to show.
        detailsScreen.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.I,
          Source = detailsScreen
        });
        Pump(TimeSpan.FromMilliseconds(500));

        MediaBrowserViewModel list = (MediaBrowserViewModel) detailsScreen.DataContext!;
        Console.WriteLine("details:  " + (list.DetailsVisible
            ? "panel open on \"" + list.DetailsHeading + "\" - \"" + list.DetailsLine + "\""
            : "the panel stayed closed; status says '" + list.Status + "'"));
        if (!list.DetailsVisible)
        {
          Console.WriteLine("FAILED:   the info key did not open the panel, so the frame would be the plain list twice.");
          return 1;
        }
      }

      if (searchAsked && window is MediaBrowserWindow searchScreen)
      {
        // Typed into the box that is really on the screen and entered there, rather than handed to the view model:
        // the frame is then the proof that the box, the Enter key, the question sent to the server and the shorter
        // list are one road, which is the road a viewer takes.
        MediaBrowserViewModel shelf = (MediaBrowserViewModel) searchScreen.DataContext!;
        int before = shelf.Titles.Count;

        TextBox box = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(searchScreen)
            .OfType<TextBox>().First(field => field.Name == "searchBox");
        box.Focus();
        box.Text = "alien";
        searchScreen.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.Enter,
          Source = box
        });
        Pump(TimeSpan.FromMilliseconds(500));

        MediaBrowserViewModel list = (MediaBrowserViewModel) searchScreen.DataContext!;
        Console.WriteLine("search:   \"" + list.ActiveSearch + "\" left " + list.Titles.Count + " of " + before
            + " scripted title(s) - " + list.Status);
        if (!list.HasSearch || list.Titles.Count == 0 || list.Titles.Count >= before)
        {
          Console.WriteLine("FAILED:   the list did not get shorter, so the frame would only be the whole shelf again.");
          return 1;
        }
      }

      if (seenAsked && window is MediaBrowserWindow seenScreen)
      {
        // Two presses, because the frame wanted is the state that shows both halves of the feature at once: the
        // list filtered to what was watched, and the rows of that list carrying the mark that says so. The key is
        // raised on the window in the same way a remote sends it, so the frame is evidence about the key, the
        // question the screen asked with it, and the answer it drew.
        MediaBrowserViewModel shelf = (MediaBrowserViewModel) seenScreen.DataContext!;
        int before = shelf.Titles.Count;

        ListBox rows = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(seenScreen)
            .OfType<ListBox>().First(list => list.Name == "titlesList");
        rows.Focus();
        for (int press = 0; press < 2; press++)
        {
          seenScreen.RaiseEvent(new KeyEventArgs
          {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.W,
            Source = rows
          });
          Pump(TimeSpan.FromMilliseconds(250));
        }
        Pump(TimeSpan.FromMilliseconds(250));

        MediaBrowserViewModel list = (MediaBrowserViewModel) seenScreen.DataContext!;
        int marked = list.Titles.Count(title => title.Seen);
        Console.WriteLine("seen:     filter '" + list.ActiveSeen + "' left " + list.Titles.Count + " of " + before
            + " scripted title(s), " + marked + " of them marked - " + list.Status);

        if (!list.HasSeenFilter || list.ActiveSeen != "watched")
        {
          Console.WriteLine("FAILED:   the watched key did not reach the third state, so the frame is not the filtered list.");
          return 1;
        }
        if (list.Titles.Count == 0 || list.Titles.Count >= before || marked != list.Titles.Count)
        {
          Console.WriteLine("FAILED:   the filtered rows are not the watched ones, so the frame proves nothing about the mark.");
          return 1;
        }
      }

      Console.WriteLine("screen:   " + screenName);
      Console.WriteLine("status:   " + (home != null
          ? home.Status + ", " + home.Aspects.Count + " aspect row(s)"
          : (connection != null
              ? connection.Status + ", " + connection.Servers.Count + " server(s) listed, saving "
                + (connection.SaveCommand.CanExecute(null) ? "is offered" : "is not offered yet")
              : playbackStatus)));

      window.UpdateLayout();
      Pump(TimeSpan.FromMilliseconds(500));

      WriteableBitmap? frame = window.CaptureRenderedFrame();
      if (frame is null)
      {
        Console.WriteLine("FAILED:   CaptureRenderedFrame() returned null - nothing was drawn.");
        return 1;
      }

      Console.WriteLine("frame:    " + frame.PixelSize + " at " + frame.Dpi + ", implementation "
          + frame.GetType().FullName);

      FrameStatistics statistics = Measure(frame);
      Console.WriteLine("pixels:   " + statistics.PixelCount + " (" + statistics.Format + "), "
          + statistics.DistinctColors + " distinct colours, " + statistics.Contrast
          + " differing from the background, " + statistics.Edges + " high-contrast edges, mean luminance "
          + statistics.MeanLuminance);

      // The claims this probe exists for. `CaptureRenderedFrame()` hands back the public wrapper, so the
      // implementation type says nothing; what says something is Skia being loaded (checked above), the
      // framebuffer being readable, and the counts below.
      if (statistics.PixelCount < 100 * 100)
      {
        Console.WriteLine("FAILED:   the frame is " + statistics.PixelCount + " pixels, too small to be a screen.");
        return 1;
      }
      if (statistics.DistinctColors < 16)
      {
        Console.WriteLine("FAILED:   only " + statistics.DistinctColors
            + " distinct colours; a themed screen with anti-aliased text has hundreds.");
        return 1;
      }
      if (statistics.Contrast * 100 < statistics.PixelCount)
      {
        Console.WriteLine("FAILED:   under 1% of the frame differs from its background colour - it is flat.");
        return 1;
      }
      if (statistics.Edges < 500)
      {
        Console.WriteLine("FAILED:   only " + statistics.Edges
            + " high-contrast edges; text and borders produce thousands, a blank panel produces none.");
        return 1;
      }

      if (pictureFrames != null)
      {
        // The frame counter says a picture arrived, and the colour count says it is not flat. Neither says the
        // colours are the ones that went in: a film drawn with its red and blue traded is colourful, varied and
        // wrong, and that is a mistake a count cannot see. So the bars are looked for by name, in order, where
        // they should be.
        string? bars = CheckBars(frame);
        if (bars != null)
        {
          Console.WriteLine("bars:     " + bars);
          Console.WriteLine("FAILED:   the picture on screen is not the picture that was pushed into the sink.");
          return 1;
        }
        Console.WriteLine("bars:     all eight, in the order they were pushed, in the colours they were pushed in");
      }

      try
      {
        string full = Path.GetFullPath(outPath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        if (File.Exists(full))
          File.Delete(full);
        frame.Save(full, PngBitmapEncoderOptions.Default);
        outPath = full;
      }
      catch (Exception ex)
      {
        Console.WriteLine("FAILED:   could not write '" + outPath + "': " + ex.Message);
        return 3;
      }

      byte[] header = new byte[8];
      using (FileStream file = File.OpenRead(outPath))
      {
        if (file.Read(header, 0, 8) != 8)
        {
          Console.WriteLine("FAILED:   '" + outPath + "' is shorter than a PNG header.");
          return 3;
        }
      }

      bool isPng = header[0] == 0x89 && header[1] == (byte)'P' && header[2] == (byte)'N' && header[3] == (byte)'G';
      Console.WriteLine("written:  " + outPath + " (" + new FileInfo(outPath).Length + " bytes, "
          + (isPng ? "PNG signature ok" : "not a PNG: " + BitConverter.ToString(header)) + ")");
      if (!isPng)
        return 3;

      Console.WriteLine("PROBE OK: the front end drew " + statistics.PixelCount + " pixels of real content on "
          + RuntimeInformation.OSDescription.Trim());
      return 0;
    }

    /// <summary>
    /// Says whether the Skia assembly is loaded and from where. Printed because "it rendered with Skia" is a
    /// statement about the running process, not about a property in a project file - and the frame's own
    /// implementation type (printed with the frame) is the other half of that evidence.
    /// </summary>
    private static string DescribeSkia()
    {
      Assembly? skia = AppDomain.CurrentDomain.GetAssemblies()
          .FirstOrDefault(a => string.Equals(a.GetName().Name, "Avalonia.Skia", StringComparison.Ordinal));
      if (skia is null)
        return "the Avalonia.Skia assembly is not loaded - whatever drew, it was not Skia";
      return "Avalonia.Skia " + skia.GetName().Version;
    }

    /// <summary>
    /// Twelve films, in the exact shape <c>GET api/media/titles</c> answers in, served without a network.
    /// </summary>
    /// <remarks>
    /// The answer is written as the wire format rather than handed to the screen as view models, so that the
    /// frame proves the parsing and the bindings as well as the drawing - the same reason the playback frame goes
    /// through the frame sink instead of painting a bitmap by hand. The paths point into this machine's temp
    /// directory because that is a place this machine can open, which is what makes a row say its length instead
    /// of "not playable here".
    /// </remarks>
    private sealed class ScriptedTitles : HttpMessageHandler
    {
      private static readonly string[] NAMES =
      {
          "Nimmerland", "Alien Earth", "The Long Shore", "Winter in the Mountains", "A Quiet Harbour",
          "Paper Cities", "The Sixth Continent", "Between Two Rivers", "Northlight", "The Cartographer",
          "Salt and Static", "A Room Facing East"
      };

      /// <summary>
      /// Which of the twelve the library was told had been watched to the end: the index into <see cref="NAMES"/>,
      /// how often, and when last. Three of twelve is what a shelf looks like after a weekend of television, and it
      /// leaves nine rows to draw the other state.
      /// </summary>
      private static readonly Dictionary<int, (int PlayCount, DateTime LastPlayed)> FINISHED =
          new Dictionary<int, (int PlayCount, DateTime LastPlayed)>
          {
              { 0, (2, new DateTime(2025, 1, 12, 21, 30, 0)) },
              { 2, (1, new DateTime(2024, 11, 3, 20, 0, 0)) },
              { 6, (1, new DateTime(2025, 2, 8, 19, 15, 0)) }
          };

      /// <summary>
      /// One title somebody stopped halfway: a date in the library and no finished viewing behind it. It belongs in
      /// the script because "watched" and "was opened once" are different statements, and a row that shows a tick on
      /// the second one is showing the viewer a film they still have to watch.
      /// </summary>
      private static readonly Dictionary<int, DateTime> STOPPED_HALFWAY =
          new Dictionary<int, DateTime> { { 4, new DateTime(2025, 3, 1, 22, 5, 0) } };

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string folder = Path.Combine(Path.GetTempPath(), "mp2-render");

        // One film read up close, which is what no row of a list has room for. Answered in the shape of
        // GET api/media/items/{id} for whichever id is asked, because the frame is about the panel rather than
        // about a database: the title it opens on is the first of the twelve below.
        if (request.RequestUri!.AbsolutePath.Contains("api/media/items/", StringComparison.Ordinal))
        {
          MediaItemDetails one = new MediaItemDetails(
              new Guid("00000000-0000-4000-8000-000000000001"), NAMES[0], NAMES[0], 2011, 88,
              "Two sisters inherit a house at the end of a road that stops at the sea, and spend the winter"
              + " finding out what the last owner left inside the walls. A quiet film about a house, the tide, and"
              + " the letters nobody ever posted.",
              null, "Some doors are worth leaving shut.", "FSK 12", null, 8, null, null, null, null, 1,
              new DateTime(2025, 1, 12),
              "{D3525B2F-7322-430C-87E7-CDA8EBD6EDD2}://" + Path.Combine(folder, NAMES[0] + ".mkv"),
              false, null);

          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          {
            RequestMessage = request,
            Content = new StringContent(JsonSerializer.Serialize(one), Encoding.UTF8, "application/json")
          });
        }

        List<MediaTitle> titles = new List<MediaTitle>();

        // A searched and/or filtered shelf, if the request asked for one. Answered here in the same shape a real
        // library answers in - matching titles, echoing the words and the filter back - because the frame is about
        // what the screen does with a filtered answer, and the answer it is given has to be the answer a filtered
        // question produces.
        string finding = ParameterIn(request.RequestUri.Query, "search");
        string seeing = ParameterIn(request.RequestUri.Query, "seen");
        List<string> matching = new List<string>();
        foreach (string name in NAMES)
        {
          if (finding != null && name.IndexOf(finding, StringComparison.OrdinalIgnoreCase) < 0)
            continue;

          bool finished = FINISHED.ContainsKey(Array.IndexOf(NAMES, name));
          if (seeing == "watched" && !finished)
            continue;
          if (seeing == "unwatched" && finished)
            continue;

          matching.Add(name);
        }

        for (int i = 0; i < matching.Count; i++)
        {
          int scripted = Array.IndexOf(NAMES, matching[i]);
          FINISHED.TryGetValue(scripted, out (int PlayCount, DateTime LastPlayed) viewing);
          DateTime? lastPlayed = viewing.PlayCount > 0
              ? viewing.LastPlayed
              : STOPPED_HALFWAY.TryGetValue(scripted, out DateTime stopped) ? stopped : (DateTime?) null;

          titles.Add(new MediaTitle(
              new Guid(string.Format("00000000-0000-4000-8000-{0:D12}", scripted + 1)),
              matching[i], matching[i], 2011 + scripted, 88 + scripted * 3,
              "{D3525B2F-7322-430C-87E7-CDA8EBD6EDD2}://" + Path.Combine(folder, matching[i] + ".mkv"),
              false, viewing.PlayCount, lastPlayed));
        }

        MediaTitles page = new MediaTitles("MovieItem", null, matching.Count, 0, matching.Count, matching.Count,
            "title", titles, null, finding, seeing);

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(JsonSerializer.Serialize(page), Encoding.UTF8, "application/json")
        });
      }

      /// <summary>
      /// The value of one query parameter, or <c>null</c> when the request did not carry it. Written against the
      /// wire format rather than against <see cref="Uri.ParseQuery"/>, because what matters here is that the
      /// screen's own URL survives the trip.
      /// </summary>
      private static string ParameterIn(string? query, string name)
      {
        if (string.IsNullOrEmpty(query))
          return null;

        foreach (string pair in query!.TrimStart('?').Split('&'))
        {
          int equals = pair.IndexOf('=');
          if (equals > 0 && pair.Substring(0, equals) == name)
            return Uri.UnescapeDataString(pair.Substring(equals + 1));
        }
        return null;
      }
    }

    /// <summary>
    /// A player that does what it is told and nothing else, for rendering the playback screen in a state a
    /// viewer recognises.
    /// </summary>
    /// <remarks>
    /// The point is that the frame is the same on every host: a real decoder would have drawn a different moment
    /// (or no moment at all, with no media to decode) on Windows than in a container, and then the two pictures
    /// could not be compared. Note that this is not the probe claiming a player works - the real libVLC has its
    /// own tests on both platforms - it is the probe needing a screen to draw.
    /// </remarks>
    private sealed class ScriptedPlayer : IVideoPlayer
    {
      private long _time;
      private long _length = 90_000;

      public PlayerState State { get; private set; } = PlayerState.Idle;
      public MediaLocation? Current { get; private set; }
      public string? LastFailure { get; private set; }

      public long LengthInMs { get { return _length; } }
      public long TimeInMs { get { return _time; } set { _time = value; } }
      public int Volume { get; set; } = 80;
      public bool Muted { get; set; }

      public event EventHandler<PlayerStateChangedEventArgs>? StateChanged;

      // Accessor-only, because a player scripted to be halfway through a film neither fails nor finishes. A
      // field-like event that is never raised is a compiler warning, and the warning is right to ask.
      public event EventHandler<PlayerFailedEventArgs>? Failed { add { } remove { } }
      public event EventHandler? EndOfMedia { add { } remove { } }
      public event EventHandler<PlaybackPositionEventArgs>? PositionChanged;

      public bool Open(MediaLocation location)
      {
        Current = location;
        _time = 0;
        Announce(PlayerState.Opening);
        Announce(PlayerState.Playing);
        return true;
      }

      public bool Play()
      {
        Announce(PlayerState.Playing);
        return true;
      }

      public bool Pause()
      {
        Announce(PlayerState.Paused);
        return true;
      }

      public bool Stop()
      {
        Announce(PlayerState.Stopped);
        return true;
      }

      public IReadOnlyList<PlaybackTrack> Tracks(TrackKind kind)
      {
        return kind == TrackKind.Audio ? _audio : _subtitles;
      }

      public bool SelectTrack(PlaybackTrack track)
      {
        if (track.Kind == TrackKind.Audio)
          _audioCurrent = track.Id;
        else
          _subtitleCurrent = track.Id;
        return true;
      }

      public int CurrentTrackId(TrackKind kind)
      {
        return kind == TrackKind.Audio ? _audioCurrent : _subtitleCurrent;
      }

      /// <summary>
      /// Three sound tracks and two subtitles, one of them with no language on it, which is what a real file
      /// usually looks like. The playback screen's frame is meant to prove the picker draws what an engine says,
      /// including the case where an engine says nothing but a name - an empty list would prove only that an
      /// empty list draws nothing.
      /// </summary>
      private readonly List<PlaybackTrack> _audio = new List<PlaybackTrack>
      {
          new PlaybackTrack(TrackKind.Audio, 1, "Language (eng)", "eng"),
          new PlaybackTrack(TrackKind.Audio, 2, "Language (ger)", "ger"),
          new PlaybackTrack(TrackKind.Audio, 3, "Director's commentary")
      };

      private readonly List<PlaybackTrack> _subtitles = new List<PlaybackTrack>
      {
          new PlaybackTrack(TrackKind.Subtitle, 1, "Language (eng)", "eng"),
          new PlaybackTrack(TrackKind.Subtitle, 2, "German (forced)")
      };

      private int _audioCurrent = 1;
      private int _subtitleCurrent = -1;

      public bool AttachTo(nint videoTarget)
      {
        return videoTarget != 0;
      }

      /// <summary>
      /// Refused, and the probe does not need it: the pictures for a rendered frame are pushed into the sink by
      /// the probe itself and handed to the window directly, so no player has to be involved in them. Answering
      /// true here would suggest this player produced them, which it did not.
      /// </summary>
      public bool AttachTo(VideoFrameSink frameSink)
      {
        return false;
      }

      /// <summary>
      /// Nothing counted. This player exists so that the playback screen's frame means the same thing on every
      /// host; a frame that carried decoder counters would not mean the same thing twice anywhere, and no screen
      /// asks for them yet.
      /// </summary>
      public PlayerStatistics Statistics
      {
        get { return PlayerStatistics.None; }
      }

      public void Dispose()
      {
      }

      /// <summary>Puts the picture where a viewer who has been watching for three quarters of a minute is.</summary>
      public void RaisePosition(long timeInMs, long lengthInMs)
      {
        _time = timeInMs;
        _length = lengthInMs;
        PositionChanged?.Invoke(this, new PlaybackPositionEventArgs(timeInMs, lengthInMs));
      }

      private void Announce(PlayerState newState)
      {
        PlayerState old = State;
        State = newState;
        StateChanged?.Invoke(this, new PlayerStateChangedEventArgs(old, newState));
      }
    }

    private readonly struct FrameStatistics
    {
      public FrameStatistics(long pixelCount, int distinctColors, long contrast, long edges, int meanLuminance,
          string format)
      {
        PixelCount = pixelCount;
        DistinctColors = distinctColors;
        Contrast = contrast;
        Edges = edges;
        MeanLuminance = meanLuminance;
        Format = format;
      }

      public long PixelCount { get; }
      public int DistinctColors { get; }
      public long Contrast { get; }
      public long Edges { get; }
      public int MeanLuminance { get; }
      public string Format { get; }
    }

    /// <summary>
    /// Reads the captured frame and describes it in counts, so that a gate can judge it without a reference
    /// image: a themed screen has hundreds of colours (anti-aliasing), a few percent of pixels that are not the
    /// background, and thousands of edges where text and borders are.
    /// </summary>
    /// <summary>
    /// The test picture's bars as red-green-blue, in the order they were pushed into the sink.
    /// </summary>
    private static readonly int[][] BarColors =
    {
      new[] { 200, 200, 200 }, new[] { 255, 255, 0 }, new[] { 0, 255, 255 }, new[] { 0, 255, 0 },
      new[] { 255, 0, 255 }, new[] { 255, 0, 0 }, new[] { 0, 0, 255 }, new[] { 0, 0, 0 }
    };

    /// <summary>
    /// Look for the test picture's eight bars on the rendered frame, in the order and the colours they were
    /// pushed in.
    /// </summary>
    /// <returns>
    /// null when they are all where they should be; a sentence about what was found instead when they are not.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A frame counter says a picture arrived and a colour count says it is not flat. Neither of them says the
    /// colours are the ones that went in, and the likeliest way for a video path to be wrong while both of those
    /// counts stay green is a picture drawn with red and blue traded - a forest gone to rust, which is as
    /// colourful as it is wrong. So the bars are looked for by name, along a line through the middle of the
    /// screen, between the picture's own white borders.
    /// </para>
    /// <para>
    /// Nothing here knows the layout or the scale factor: the compositor may fit the picture wherever it likes.
    /// What it may not do is change what the picture is.
    /// </para>
    /// </remarks>
    private static string? CheckBars(WriteableBitmap frame)
    {
      using ILockedFramebuffer surface = frame.Lock();

      int redOffset;
      int blueOffset;
      if (surface.Format == PixelFormat.Rgba8888)
      {
        redOffset = 0;
        blueOffset = 2;
      }
      else if (surface.Format == PixelFormat.Bgra8888)
      {
        redOffset = 2;
        blueOffset = 0;
      }
      else
      {
        return "the frame is " + surface.Format + ", which this check cannot read";
      }

      int width = surface.Size.Width;
      int height = surface.Size.Height;
      int rowBytes = surface.RowBytes;
      byte[] pixels = new byte[rowBytes * height];
      Marshal.Copy(surface.Address, pixels, 0, pixels.Length);

      // The middle of the screen first, then a little above and below it: whether a line crosses the picture
      // depends on the letterboxing, and the letterboxing belongs to the layout, not to this check.
      string? firstFailure = null;
      foreach (int y in new[] { height / 2, height * 2 / 5, height * 3 / 5 })
      {
        string? failure = CheckOneRow(pixels, rowBytes, width, y, redOffset, blueOffset);
        if (failure == null)
          return null;
        if (firstFailure == null)
          firstFailure = failure;
      }
      return firstFailure ?? "no line to look at";
    }

    /// <summary>
    /// One line of the frame: find the picture by its white borders, then ask the eight places between them for
    /// the colours that should be there.
    /// </summary>
    private static string? CheckOneRow(byte[] pixels, int rowBytes, int width, int y, int redOffset,
                                       int blueOffset)
    {
      List<int[]> whiteRuns = new List<int[]>();
      int runStart = -1;
      for (int x = 0; x <= width; x++)
      {
        bool white = false;
        if (x < width)
        {
          int at = y * rowBytes + x * 4;
          white = pixels[at + redOffset] >= 240 && pixels[at + 1] >= 240 && pixels[at + blueOffset] >= 240;
        }

        if (white && runStart < 0)
          runStart = x;
        else if (!white && runStart >= 0)
        {
          if (x - runStart >= 3)
            whiteRuns.Add(new[] { runStart, x });
          runStart = -1;
        }
      }

      if (whiteRuns.Count < 2)
        return "line " + y + " holds no pair of white borders to find the picture by";

      int left = whiteRuns[0][1];
      int right = whiteRuns[whiteRuns.Count - 1][0];
      if (right - left < width / 4)
      {
        return "the outermost white marks on line " + y + " are " + (right - left)
            + " pixels apart out of " + width + ", which is a caption, not a picture";
      }

      int barWidth = (right - left) / BarColors.Length;
      if (barWidth < 4)
        return "the bars between the borders on line " + y + " would be " + barWidth + " pixels wide";

      for (int i = 0; i < BarColors.Length; i++)
      {
        int x = left + (2 * i + 1) * barWidth / 2;
        int at = y * rowBytes + x * 4;
        int red = pixels[at + redOffset];
        int green = pixels[at + 1];
        int blue = pixels[at + blueOffset];
        int[] wanted = BarColors[i];
        if (Math.Abs(red - wanted[0]) > 24 || Math.Abs(green - wanted[1]) > 24 || Math.Abs(blue - wanted[2]) > 24)
        {
          return "bar " + (i + 1) + " of " + BarColors.Length + " on line " + y + " at x=" + x + " is rgb("
              + red + "," + green + "," + blue + ") instead of rgb(" + wanted[0] + "," + wanted[1] + ","
              + wanted[2] + ") - the picture between its white borders is not the picture that was pushed";
        }
      }
      return null;
    }

    private static FrameStatistics Measure(WriteableBitmap frame)
    {
      using ILockedFramebuffer surface = frame.Lock();

      // Measured, not assumed: with Skia doing the drawing this framebuffer is Rgba8888, not the Bgra8888 the
      // managed headless stub uses. Anything else must be looked at, not decoded by guess.
      bool redFirst;
      if (surface.Format == PixelFormat.Rgba8888)
        redFirst = true;
      else if (surface.Format == PixelFormat.Bgra8888)
        redFirst = false;
      else
      {
        throw new InvalidOperationException("the frame is " + surface.Format
            + ", and this probe only reads Rgba8888 and Bgra8888 - update it rather than guess at a stride.");
      }

      int height = surface.Size.Height;
      int width = surface.Size.Width;
      int rowBytes = surface.RowBytes;
      byte[] pixels = new byte[rowBytes * height];
      Marshal.Copy(surface.Address, pixels, 0, pixels.Length);

      int blueOffset = redFirst ? 2 : 0;
      int greenOffset = 1;
      int redOffset = redFirst ? 0 : 2;

      int backgroundRed = pixels[redOffset];
      int backgroundGreen = pixels[greenOffset];
      int backgroundBlue = pixels[blueOffset];

      HashSet<uint> colors = new HashSet<uint>();
      long contrast = 0;
      long edges = 0;
      long luminance = 0;

      for (int y = 0; y < height; y++)
      {
        int row = y * rowBytes;
        for (int x = 0; x < width; x++)
        {
          int offset = row + (x * 4);
          byte blue = pixels[offset + blueOffset];
          byte green = pixels[offset + greenOffset];
          byte red = pixels[offset + redOffset];
          byte alpha = pixels[offset + 3];

          colors.Add(((uint)alpha << 24) | ((uint)red << 16) | ((uint)green << 8) | blue);
          luminance += (red * 299 + green * 587 + blue * 114) / 1000;

          int difference = Math.Max(Math.Abs(red - backgroundRed),
              Math.Max(Math.Abs(green - backgroundGreen), Math.Abs(blue - backgroundBlue)));
          if (difference > 8)
            contrast++;
          if (difference > 96)
            edges++;
        }
      }

      long count = (long)width * height;
      return new FrameStatistics(count, colors.Count, contrast, edges, count == 0 ? 0 : (int)(luminance / count),
          surface.Format.ToString());
    }

    /// <summary>
    /// Runs the dispatcher until the condition holds or the deadline passes, and says which happened. Without
    /// the deadline a broken render would turn the probe into a hung gate.
    /// </summary>
    private static bool PumpUntil(Func<bool> condition, TimeSpan timeout)
    {
      Stopwatch watch = Stopwatch.StartNew();
      while (watch.Elapsed < timeout)
      {
        Dispatcher.UIThread.RunJobs();
        if (condition())
          return true;
        Thread.Sleep(5);
      }
      return condition();
    }

    private static void Pump(TimeSpan forHowLong)
    {
      Stopwatch watch = Stopwatch.StartNew();
      while (watch.Elapsed < forHowLong)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(5);
      }
    }

    private static int Usage(string problem)
    {
      Console.WriteLine("FAILED:   " + problem);
      Console.WriteLine(USAGE);
      return 2;
    }
  }
}
