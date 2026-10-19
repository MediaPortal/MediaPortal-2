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
using MediaPortal.Frontend.Theming;
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
        "Frontend.RenderProbe --out <file.png> [--screen <home|continue|nowplaying|connect|browser|details|search|seen|folders|pictures|browse|news|weather|party|settings>] [--server <url>] [--aspect <name>] [--theme <name>] [--width <n>] [--height <n>]\n" +
        "  --out <file>     where to write the rendered frame (PNG); its directory is created\n" +
        "  --screen <name>  which screen to render (default home)\n" +
        "  --server <url>   render the populated screen by asking this server (otherwise MP2_SERVER,\n" +
        "                   otherwise the screen's own 'no server' state is rendered - a real state too)\n" +
        "  --aspect <name>  which aspect the library screen lists when a server was given (default MovieItem)\n" +
        "  --theme <name>   draw the frame with the colours of one of the old skins: default, nereus and its\n" +
        "                   six nereus-<family> tints, wmc, wmc-dark or wmc-turquoise. A name that is not one\n" +
        "                   of those stops the run instead of quietly drawing the colours you always had,\n" +
        "                   because a gate that asked for one theme and was given another would report a pass\n" +
        "                   for a picture it never looked at\n" +
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
      string aspectName = "MovieItem";
      string? theme = null;
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
          case "--aspect":
            if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
              return Usage("--aspect needs a name.");
            aspectName = args[++i].Trim();
            break;
          case "--theme":
            if (i + 1 >= args.Length)
              return Usage("--theme needs a palette name: default, nereus, nereus-blue, ..., wmc, wmc-dark, "
                  + "wmc-turquoise.");
            theme = args[++i];
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
      Console.WriteLine("styles:   " + application.Styles.Count + " style set(s), requested variant "
          + application.ActualThemeVariant);

      // The palette this frame is drawn with. A name that is not a palette stops the run instead of falling back
      // to the default colours: a gate that asked for the Nereus look, was quietly given the default one, and
      // found the pixels it expected would report a pass for a picture it never looked at.
      string appliedTheme = FrontendTheme.Apply(application, FrontendTheme.SelectedName(theme),
          out string? themeProblem);
      if (themeProblem != null)
      {
        Console.WriteLine("FAILED:   " + themeProblem);
        return 2;
      }

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
      FoldersViewModel? foldersScreen = null;

      // Set when the frame to render is the news screen. The read it starts when the window opens is waited for
      // before the picture is taken, and its scripted internet lives in this file rather than out there.
      NewsViewModel? newsScreen = null;

      // The same for the weather screen: one saved place in a temporary settings file, and a scripted
      // OpenWeatherMap answering the current sky and the days ahead.
      WeatherViewModel? weatherScreen = null;

      // And for the party screen: the scripted music shelf below answers the eight songs, and the shuffle runs
      // from a fixed seed so that the queue in the frame is the same queue on every machine that draws it.
      PartyViewModel? partyScreen = null;

      // The settings screen needs no script - its whole content is the palette table, which is in the binary -
      // but it needs a name to exist for the pixel check further down to know the frame it is looking at.
      ThemeViewModel? settingsScreen = null;

      // Set when the library screen lists what a running server really holds. The frame is then somebody's own
      // shelf with the pictures the importer found on it, which is the one frame that says the whole road works.
      MediaBrowserViewModel? browserScreen = null;
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
      else if (screenName.Equals("continue", StringComparison.Ordinal))
      {
        if (online)
          Console.WriteLine("note:     --server belongs to the home screen; the library behind this one is scripted"
              + " instead, because the gate's server holds no titles on purpose");

        // The home screen of a household that has been watching things: MP2's main menu, and under it the titles
        // that were started and not finished. Unlike the shelf frames this one reads its content through the whole
        // path - the screen asks the server for its parts, counts them, and then asks each part what it holds
        // that was left half way - because the strip is not one answer but a merge of several.
        home = new HomeViewModel(new ServerApiClient(new Uri("http://127.0.0.1:1/MediaPortal/Server/"),
            new HttpClient(new ScriptedTitles())));
        home.RefreshCommand.Execute(null);

        if (!PumpUntil(delegate { return home.Unfinished.Count > 0; }, TimeSpan.FromSeconds(10)))
        {
          Console.WriteLine("FAILED:   the strip never filled, so the frame would show an ordinary home screen and"
              + " prove nothing about the titles left half way.");
          return 1;
        }
        Pump(TimeSpan.FromMilliseconds(300));

        // The frame is refused unless the strip is the answer the script describes. Two of the four rows came from
        // the other part of the library, and they sit in the middle of the list rather than at its end: that is
        // the whole point of asking every tile rather than only the first one.
        string[] expected = { "Salt and Static", "S01E02 - Low Tide", "Northlight", "A Quiet Harbour" };
        string[] shown = home.Unfinished.Select(row => row.Name).ToArray();
        if (!shown.SequenceEqual(expected))
        {
          Console.WriteLine("FAILED:   the strip holds \"" + string.Join("\", \"", shown)
              + "\"; expected the four stopped titles newest first, \"" + string.Join("\", \"", expected) + "\".");
          return 1;
        }
        if (home.Unfinished.Any(row => row.Seen))
        {
          Console.WriteLine("FAILED:   a row of the strip carries a watched mark; a title that was finished does"
              + " not belong on a list of things still to watch.");
          return 1;
        }
        // Where the viewer left each of them, in the order the strip shows them. The newest three carry a figure
        // and the oldest does not, which is the pair a frame has to hold: a percentage where the server had one,
        // and an empty space where it had none rather than a nought invented for it.
        string[] expectedPositions = { "12% watched", "74% watched", "61% watched", "" };
        string[] shownPositions = home.Unfinished.Select(row => row.ResumeText).ToArray();
        if (!shownPositions.SequenceEqual(expectedPositions))
        {
          Console.WriteLine("FAILED:   the strip's positions read \"" + string.Join("\", \"", shownPositions)
              + "\"; expected \"" + string.Join("\", \"", expectedPositions)
              + "\" - the three newest titles carry the position their viewer left, the oldest carries none.");
          return 1;
        }
        if (home.Unfinished[0].ResumePercent != 12 || home.Unfinished[3].ResumePercent != null)
        {
          Console.WriteLine("FAILED:   the figure behind the words is not the one the script sent ("
              + home.Unfinished[0].ResumePercent + ", " + home.Unfinished[3].ResumePercent + "), so the row would"
              + " be saying one thing to the viewer and carrying another to the player.");
          return 1;
        }
        if (home.Sections.Count != 2)
        {
          Console.WriteLine("FAILED:   " + home.Sections.Count
              + " tile(s), but the script offers two parts of the library a viewer can watch.");
          return 1;
        }
        if (home.Menu.Count(entry => entry.Openable) != 9)
        {
          // The screen does not draw `Sections` any more, it draws the menu built from them, so this is the check
          // that belongs to the picture: the two parts the script offers have to be the two entries a viewer can
          // open, and the entries the script cannot fill have to be saying so rather than being missing. The other
          // seven openable entries are the folders screen, the pictures screen, the browse screen, the news,
          // the weather, the party and the settings, all of which are openable whatever the server holds - one
          // reads the
          // folders the library was built from, one walks the shares, the third asks the server to group whatever
          // it turns out to hold, two ask the internet instead, the party simply queues nothing until a
          // shelf answers, and the settings repaint the box without asking anybody; none needs a title to have
          // been imported.
          Console.WriteLine("FAILED:   " + home.Menu.Count(entry => entry.Openable)
              + " openable menu entr(ies) of " + home.Menu.Count
              + "; expected two plus the folders, pictures, browse, news, weather, party and settings screens,"
              + " because the script offers two parts of the library a viewer can watch.");
          return 1;
        }
        if ((home.ServerDescription ?? "").IndexOf("MediaPortal 2 Server", StringComparison.Ordinal) < 0)
        {
          // Cheap and specific: every question the home screen asks goes to a different endpoint, and a script
          // that answers the wrong path still lets the tiles fill up - the screen just quietly has no server to
          // name at the top. Checked here because that is the one mistake a frame of this screen would not show.
          Console.WriteLine("FAILED:   the screen never showed the scripted server as \""
              + home.ServerDescription + "\" - the script answers a path the screen does not ask.");
          return 1;
        }

        Console.WriteLine("content:  " + home.UnfinishedStatus + " - tiles: "
            + string.Join(", ", home.Sections.Select(section => section.Label + " (" + section.CountText + ")")));
        Console.WriteLine("menu:     " + string.Join(" | ",
            home.Menu.Select(entry => entry.Label + " " + entry.Detail)));
        Console.WriteLine("strip:    " + string.Join(" | ",
            home.Unfinished.Select(row => row.Name + " - " + row.LastPlayedText)));
        Console.WriteLine("resume:   " + string.Join(" | ",
            home.Unfinished.Select(row => row.Name + " "
                + (row.ResumePercent.HasValue ? row.ResumeText : "nothing stored"))));

        // The playback screen is handed over although the frame never opens it: the strip hides itself on a
        // machine that cannot start a film, and a picture of a list the viewer cannot act on would be the frame
        // that lies.
        window = new HomeWindow(home, delegate
        {
          return new NowPlayingWindow(new NowPlayingViewModel(new ScriptedPlayer(),
              action => Dispatcher.UIThread.Post(action)));
        });
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
        // Only the plain library screen is asked of a running server. The three screens that press something - the
        // info key, the search box, the watched key - stay scripted, because which title their frame would then
        // depend on is whatever that shelf happens to hold first, and a frame like that cannot be compared with
        // anything from one run to the next.
        bool live = online && screenName.Equals("browser", StringComparison.Ordinal);
        if (online && !live)
          Console.WriteLine("note:     --server is taken at face value by the home screen, the folders screen and"
              + " the library screen; the title list of this one is scripted instead");

        ServerApiClient client;
        HomeSection section;
        if (live)
        {
          // The aspect is asked of the server rather than assumed, so that the shelf is listed under the id the
          // server itself gave it. Whoever runs the probe names which shelf; films are the default because that
          // is the one a MediaPortal installation is usually opened for.
          client = new ServerApiClient(new Uri(url!));
          LibraryAspects? answered = null;
          string? refusal = null;
          client.GetLibraryAspectsAsync(CancellationToken.None).ContinueWith(
              delegate (Task<LibraryAspects> task)
              {
                if (task.IsFaulted) refusal = task.Exception != null ? task.Exception.GetBaseException().Message : null;
                else answered = task.Result;
              }, TaskScheduler.Default);
          if (!PumpUntil(delegate { return answered != null || refusal != null; }, TimeSpan.FromSeconds(15)))
          {
            Console.WriteLine("FAILED:   '" + url + "' did not name its aspects"
                + (refusal == null ? " within 15 seconds." : ": " + refusal));
            return 4;
          }

          MediaAspect? chosen = answered!.Aspects.FirstOrDefault(aspect =>
              aspect.Name.Equals(aspectName, StringComparison.OrdinalIgnoreCase));
          if (chosen == null)
          {
            Console.WriteLine("FAILED:   '" + url + "' has no aspect named '" + aspectName + "'; it offered '"
                + string.Join("', '", answered.Aspects.Select(aspect => aspect.Name)) + "'.");
            return 4;
          }
          // The word a viewer reads above the shelf is the catalogue's own, not the aspect id the server stores
          // titles under - "Movies" rather than "MovieItem", which is what the home tile says too.
          section = new HomeSection(HomeSection.LabelFor(chosen.Name), new AspectViewModel(chosen));
        }
        else
        {
          // The library list with something in it. Scripted rather than asked of the gate's server, whose database
          // is empty on purpose: whether a row of titles is laid out and legible from a sofa cannot depend on
          // whether whoever runs the gate happens to have imported anything. The titles arrive in the server's own
          // answer shape (see <see cref="ScriptedTitles"/>), so the frame covers the parsing and the bindings too.
          client = new ServerApiClient(new Uri("http://127.0.0.1:1/MediaPortal/Server/"),
              new HttpClient(new ScriptedTitles()));
          section = new HomeSection("Films",
              new AspectViewModel(new MediaAspect(Guid.Empty, "MovieItem", true, false, null,
                  Array.Empty<AspectAttribute>())));
        }

        MediaBrowserViewModel browser = new MediaBrowserViewModel(client, section);
        browser.OpenCommand.Execute(null);
        Pump(TimeSpan.FromMilliseconds(500));
        if (live)
          browserScreen = browser;

        Console.WriteLine("content:  " + browser.Titles.Count + " title(s) under \"" + browser.Heading
            + "\", status: " + browser.Status);
        if (browser.Titles.Count == 0 && !live)
        {
          Console.WriteLine("FAILED:   the list came up empty, so the frame would show an empty screen and prove nothing.");
          return 1;
        }

        detailsAsked = screenName.Equals("details", StringComparison.Ordinal);
        searchAsked = screenName.Equals("search", StringComparison.Ordinal);
        seenAsked = screenName.Equals("seen", StringComparison.Ordinal);
        window = new MediaBrowserWindow(browser, null);
      }
      else if (screenName.Equals("folders", StringComparison.Ordinal))
      {
        // Unlike the scripted screens this one is only worth a frame against a server that is really running: what
        // it shows is the folders somebody's own library was built from, and inventing three of them would picture
        // a screen that has never answered a question. Without `--server` it renders the state a television falls
        // back to when nothing answered, which is a picture of its own worth having.
        FoldersViewModel folders = new FoldersViewModel(
            online ? new ServerApiClient(new Uri(url!)) : null);
        Console.WriteLine("content:  " + (online
            ? "the folders " + url + " says its library was built from"
            : "offline - the screen's own 'no server' state is what gets rendered"));
        foldersScreen = folders;
        window = new FoldersWindow(folders);
      }
      else if (screenName.Equals("pictures", StringComparison.Ordinal))
      {
        // The pictures screen drawn from a scripted server rather than from a disk: whose photographs and whose
        // folders the gate's machine happens to have is nobody's business, and a frame that changed with them could
        // not be compared with the run before it. The photographs are flat colours, so the question asked of the
        // frame afterwards is whether a preview arrived, and not whether a photograph is nice.
        PicturesViewModel pictures = new PicturesViewModel(new ServerApiClient(
            new Uri("http://127.0.0.1:1/MediaPortal/Server/"),
            new HttpClient(new ScriptedFoldersAndPictures())));
        pictures.RefreshCommand.Execute(null);
        if (!PumpUntil(delegate { return pictures.Tiles.Count > 0; }, TimeSpan.FromSeconds(10)))
        {
          Console.WriteLine("FAILED:   the pictures screen never listed the shares it was served. Status: "
              + pictures.Status);
          return 1;
        }

        // Walking into the folder is the frame worth having: the list of shares is one row of names, while a
        // folder is where tiles, captions and previews are all on screen together.
        _ = pictures.OpenAsync(pictures.Tiles[0]);
        PumpUntil(delegate
        {
          return pictures.Tiles.Count(tile => tile.Picture != null) >= 2;
        }, TimeSpan.FromSeconds(10));

        Console.WriteLine("content:  " + pictures.Tiles.Count + " tile(s) in \"" + pictures.Heading + "\", "
            + pictures.Tiles.Count(tile => tile.Picture != null) + " of them with a picture fetched, status: "
            + pictures.Status);
        if (pictures.Tiles.Count < 6)
        {
          Console.WriteLine("FAILED:   only " + pictures.Tiles.Count
              + " tile(s); the script serves two folders and four photographs, so the frame would prove nothing.");
          return 1;
        }

        window = new PicturesWindow(pictures);
      }
      else if (screenName.Equals("browse", StringComparison.Ordinal))
      {
        // The browse screen drawn from a scripted count, for the same reason the title list is scripted: the
        // gate's database is empty, and a frame of an empty third column would picture a screen that never got
        // an answer rather than the screen itself. The numbers are the script's; everything between the answer
        // and the pixels - JSON, the view model, the three columns - is the machinery a viewer actually uses.
        HomeSection browsed = new HomeSection("Movies", new AspectViewModel(
            new MediaAspect(new Guid("11111111-1111-1111-1111-111111111111"), "MovieItem", false, false,
                null, Array.Empty<AspectAttribute>())));
        BrowseViewModel browse = new BrowseViewModel(
            new ServerApiClient(new Uri("http://127.0.0.1:1/MediaPortal/Server/"),
                new HttpClient(new ScriptedBrowse())),
            new List<HomeSection> { browsed });
        browse.RefreshCommand.Execute(null);
        if (!PumpUntil(delegate { return browse.Values.Count > 0; }, TimeSpan.FromSeconds(10)))
        {
          Console.WriteLine("FAILED:   the browse screen never listed the groups it was served. Status: "
              + browse.Status);
          return 1;
        }

        Console.WriteLine("content:  " + browse.Values.Count + " group(s) for " + browsed.Label + " by "
            + browse.SelectedCategory!.Word + ": "
            + string.Join(", ", browse.Values.Select(value => value.Value + " (" + value.Count + ")"))
            + ", status: " + browse.Status);
        if (!browse.Values.Select(value => value.Value).SequenceEqual(new[] { "Drama", "Action", "Shorts" }))
        {
          Console.WriteLine("FAILED:   the groups arrived as \""
              + string.Join("\", \"", browse.Values.Select(value => value.Value))
              + "\"; the script serves them biggest first with an empty hole among them, so the screen must show"
              + " Drama, Action, Shorts - the order is the server's and the hole is not a row.");
          return 1;
        }
        if (browse.SelectedValue == null || !browse.SelectedValue.Value.Equals("Drama", StringComparison.Ordinal))
        {
          Console.WriteLine("FAILED:   the first row of the third column is not already picked; a viewer who "
              + "presses OK without reading should land on the biggest group, not on nothing.");
          return 1;
        }

        window = new BrowseWindow(browse);
      }
      else if (screenName.Equals("news", StringComparison.Ordinal))
      {
        // The news screen, with the internet scripted. Not because fetching is expensive but because a frame and
        // a gate must say the same thing on every machine that runs them: a gate that drew whatever
        // install.team-mediaportal.com happened to serve today would fail on a Tuesday when the foundation
        // repapered its feed list, and a frame of a screen with no internet is a frame of an empty list. The
        // script serves the real XML shape, one RSS feed, one Atom feed and one refusal, so the frame shows the
        // columns full, the merged order, and a feed row carrying a 500 - which is the screen's best day and its
        // worst one at the same time.
        newsScreen = new NewsViewModel(
            new NewsApiClient(new HttpClient(new ScriptedNews())), "en");
        window = new NewsWindow(newsScreen);
      }
      else if (screenName.Equals("weather", StringComparison.Ordinal))
      {
        // The weather screen, with OpenWeatherMap scripted. The frame has to mean the same thing on every
        // machine that draws it and on every afternoon, and a real forecast changes by the hour - so the script
        // answers with one fixed sky: eight degrees and few clouds now, two days coming, one of them rainy by
        // morning and clear by its own noon, which is the midday-entry rule made visible. The place itself comes
        // from a temporary settings file, because where the weather is watched is the viewer's saved decision
        // and the screen's own first frame is the one it draws from saved places.
        string weatherSettingsFile = Path.Combine(Path.GetTempPath(), "mp2-render-weather.json");
        File.WriteAllText(weatherSettingsFile,
            "{\"places\":[{\"name\":\"Berlin\",\"detail\":\"DE\",\"lat\":52.517,\"lon\":13.3889}]}");
        weatherScreen = new WeatherViewModel(
            new WeatherApiClient(new HttpClient(new ScriptedWeather())), weatherSettingsFile, "en-GB");
        window = new WeatherWindow(weatherScreen);
      }
      else if (screenName.Equals("party", StringComparison.Ordinal))
      {
        // The party screen, over the scripted music shelf. The shuffle is the screen, and a shuffle that is
        // random would make the frame mean something different on every afternoon - so the seed is fixed here
        // and only here: the application passes none, and every real party dances in a different order. The
        // probe presses start the way the focused button's click does, because the frame wanted is the party
        // going, not the screen waiting for somebody to press something.
        partyScreen = new PartyViewModel(
            new ServerApiClient(new Uri("http://127.0.0.1:9/MediaPortal/Server"),
                new HttpClient(new ScriptedTitles())), 20260205);
        window = new PartyWindow(partyScreen);
      }
      else if (screenName.Equals("settings", StringComparison.Ordinal))
      {
        // The front end's own settings screen: every palette there is, each row wearing its own swatches. This
        // gate must not save a theme into the real viewer's file, so the screen points at a temporary file of
        // the same fixed-neighbourhood kind the weather frame uses - and the frame is about picking, so nothing
        // is picked; it is drawn with whatever --theme asked for.
        string themeSettingsFile = Path.Combine(Path.GetTempPath(), "mp2-render-theme.json");
        settingsScreen = new ThemeViewModel(application, themeSettingsFile);
        window = new ThemeWindow(settingsScreen);
      }
      else
      {
        return Usage("unknown screen '" + screenName + "'; this probe renders home, continue, nowplaying, connect,"
            + " browser, details, search, seen, folders, pictures, browse, news, weather, party or settings.");
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

      if (foldersScreen != null)
      {
        // The screen reads the server as it appears, so the probe waits for that read rather than starting one.
        if (!PumpUntil(delegate { return !foldersScreen.IsBusy; }, TimeSpan.FromSeconds(30)))
        {
          Console.WriteLine("FAILED:   the folders screen never finished reading '" + url + "'.");
          return 4;
        }

        Console.WriteLine("folders:  " + foldersScreen.Shares.Count + " folder(s), "
            + foldersScreen.Categories.Count + " media type(s) on offer, queue: " + foldersScreen.Queue
            + ", status: " + foldersScreen.Status);

        // The sentence the view model holds is only worth anything if the line on the screen carries it, so the
        // control is looked for by the name the screen gives it - the way the other screens' probes look for the
        // box or the row they are about to press.
        if (window is FoldersWindow foldersScreenWindow)
        {
          TextBlock? line = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(foldersScreenWindow)
              .OfType<TextBlock>().FirstOrDefault(text => text.Name == "statusLine");
          Console.WriteLine("line:     " + (line == null
              ? "the screen has no text block named \"statusLine\""
              : "\"" + line.Text + "\" at " + line.Bounds + ", visible " + line.IsVisible
                + ", drawn " + line.IsEffectivelyVisible));
          if (line == null || string.IsNullOrWhiteSpace(line.Text))
          {
            Console.WriteLine("FAILED:   the line on the screen is empty while the view model holds a sentence.");
            return 4;
          }
        }

        // A frame of an empty folder list drawn against a server that was said to have folders is not evidence of
        // anything, so it is refused rather than printed.
        if (online && foldersScreen.Shares.Count == 0)
        {
          Console.WriteLine("FAILED:   '" + url + "' answered with no folders, so the frame would be the screen's"
              + " empty state rather than the library behind it. Status says: '" + foldersScreen.Status + "'.");
          return 4;
        }
      }

      if (browserScreen != null)
      {
        // The screen reads the server as it opens, so the probe waits for that read rather than starting one of
        // its own. A shelf of nothing drawn against a server that was said to hold titles is not evidence.
        if (!PumpUntil(delegate { return browserScreen.Titles.Count > 0; }, TimeSpan.FromSeconds(30)))
        {
          Console.WriteLine("FAILED:   '" + url + "' answered no titles under '" + aspectName
              + "'; status says: '" + browserScreen.Status + "'.");
          return 4;
        }

        // The pictures follow the rows - they are read a page at a time, four at a time, and never block the list
        // - so the frame waits for them as well. A row of titles not one of which has a poster is the artwork
        // getting lost between the server and the screen, which is exactly what this frame is here to catch.
        PumpUntil(delegate { return browserScreen.Titles.Any(row => row.Artwork != null); },
            TimeSpan.FromSeconds(30));
        int withPictures = browserScreen.Titles.Count(row => row.Artwork != null);
        Console.WriteLine("posters:  " + withPictures + " of " + browserScreen.Titles.Count
            + " title(s) on the frame carry a picture");
        if (withPictures == 0)
        {
          Console.WriteLine("FAILED:   not one row was given a picture, so the frame would be a list of words.");
          return 4;
        }

        // What the top of the screen really reads, read off the control rather than from the view model, so that
        // the heading of a shelf somebody's library was built from is evidence about the screen and not about a
        // property that was handed to it.
        if (window is MediaBrowserWindow browserWindow)
        {
          TextBlock? headline = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(browserWindow)
              .OfType<TextBlock>().FirstOrDefault(text => text.Name == "shelfHeading");
          Console.WriteLine("heading:  " + (headline == null
              ? "the screen has no text block named \"shelfHeading\""
              : "\"" + headline.Text + "\" on the screen, \"" + browserScreen.Heading + "\" in the view model"));
          if (headline == null || string.IsNullOrWhiteSpace(headline.Text))
          {
            Console.WriteLine("FAILED:   the shelf has no heading on the screen.");
            return 4;
          }
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
      if (weatherScreen != null)
      {
        // The saved place is selected by the view model itself, and the selection starts the read - this wait
        // says both halves of that happened, and says it before the picture is taken.
        if (!PumpUntil(delegate { return weatherScreen.Days.Count >= 2 && !weatherScreen.IsBusy; },
            TimeSpan.FromSeconds(10)))
        {
          Console.WriteLine("FAILED:   the weather screen never showed the days it was served. Status: "
              + weatherScreen.Status);
          return 1;
        }

        Console.WriteLine("content:  " + weatherScreen.CurrentLine + " | " + weatherScreen.CurrentDetail + " | "
            + string.Join(", ", weatherScreen.Days.Select(day => day.DateText + " " + day.Overview + " "
                + day.Range + " " + day.Rain)) + ", status: " + weatherScreen.Status);
        if (!weatherScreen.CurrentLine.Contains("8°C", StringComparison.Ordinal)
            || !weatherScreen.CurrentDetail.Contains("NW", StringComparison.Ordinal)
            && !weatherScreen.CurrentDetail.Contains("NE", StringComparison.Ordinal))
        {
          Console.WriteLine("FAILED:   the current panel does not carry the scripted sky - the script answers "
              + "eight degrees and few clouds with a NE wind, and the frame has to say what the API said.");
          return 1;
        }

        WeatherDayRow first = weatherScreen.Days[0];
        WeatherDayRow second = weatherScreen.Days[1];
        if (first.Overview != "broken clouds" || second.Overview != "light snow")
        {
          Console.WriteLine("FAILED:   a day's words are not its midday entry's; the script makes the first day "
              + "rainy at ten and broken-cloud at two, and the day must read broken clouds.");
          return 1;
        }
        if (first.Range != "6°C / 1°C" || second.Range != "4°C / -1°C")
        {
          Console.WriteLine("FAILED:   the days carry " + first.Range + " and " + second.Range
              + "; the script's three-hour entries span 6.1 over 1.2 and 3.9 over -1.4 in the city's own "
              + "local day - the aggregation is the frame's whole claim.");
          return 1;
        }
      }
      if (partyScreen != null)
      {
        // The party does not start itself - that is the viewer's press - so the probe presses the same command
        // the button is wired to, and waits for what a start owes: the whole scripted shelf queued, and the
        // first song of the shuffle asked for and printed big. A frame of a party that never started would be a
        // frame of an empty list, and this screen's empty list is its offline state, not its subject.
        partyScreen.StartCommand.Execute(null);
        if (!PumpUntil(delegate { return partyScreen.Upcoming.Count == 8 && partyScreen.CurrentLine.Length > 0; },
            TimeSpan.FromSeconds(10)))
        {
          Console.WriteLine("FAILED:   the party never started. Status: " + partyScreen.Status);
          return 1;
        }

        Console.WriteLine("content:  " + partyScreen.Upcoming.Count + " song(s) queued, first "
            + partyScreen.Upcoming[0].Name + ", now: " + partyScreen.CurrentLine
            + ", rest: " + string.Join(", ", partyScreen.Upcoming.Skip(1).Select(song => song.Title.Title))
            + ", status: " + partyScreen.Status);
        if (!partyScreen.CurrentLine.Equals(partyScreen.Upcoming[0].Name, StringComparison.Ordinal))
        {
          Console.WriteLine("FAILED:   the big line and the head of the queue name different songs; the queue "
              + "on the screen would then be scenery rather than a statement about this evening.");
          return 1;
        }

        if (!partyScreen.Upcoming.Select(song => song.Title.Title)
                .OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(Enumerable.Range(0, 8).Select(at => ScriptedTitles.TRACKS[at, 0])
                    .OrderBy(name => name, StringComparer.Ordinal)))
        {
          Console.WriteLine("FAILED:   the queue is not the whole scripted shelf in some order; the shuffle "
              + "reorders the music, it does not edit it, and eight songs went in.");
          return 1;
        }

        if (!partyScreen.Status.Contains("The party is on", StringComparison.Ordinal))
        {
          Console.WriteLine("FAILED:   the status line does not say the party is on after start; it says: '"
              + partyScreen.Status + "'.");
          return 1;
        }
      }
      if (newsScreen != null)
      {
        // The window started reading when it opened - that is the screen's own doing, not the probe's, and this
        // wait is the proof. The picture is then taken with the columns full and the refusal in place.
        if (!PumpUntil(delegate { return newsScreen.Feeds.Count == 4 && !newsScreen.IsBusy; },
            TimeSpan.FromSeconds(10)))
        {
          Console.WriteLine("FAILED:   the news screen never listed the feeds it was served. Status: "
              + newsScreen.Status);
          return 1;
        }

        Console.WriteLine("content:  " + newsScreen.Headlines.Count + " headline(s), feeds: "
            + string.Join(", ", newsScreen.Feeds.Select(feed => feed.Name + (feed.State.Length > 0
                ? " [" + feed.State + "]" : ""))) + ", status: " + newsScreen.Status);
        if (newsScreen.Headlines.Count != 3)
        {
          Console.WriteLine("FAILED:   the merged column shows " + newsScreen.Headlines.Count
              + " headline(s); the script serves three across the two feeds that answer, and the third feed's"
              + " refusal must cost nothing of them.");
          return 1;
        }
        if (!newsScreen.Status.Contains("Dead Paper", StringComparison.Ordinal))
        {
          Console.WriteLine("FAILED:   the status line does not name the refusing feed; a screen that swallows"
              + " a refusal swallows the difference between one dead paper and no papers at all.");
          return 1;
        }
      }
      if (home != null)
      {
        // The picture has to be taken after the screen has finished reading, not while it is reading: caught here
        // by the line below printing `Movies ?` for a server that answers `0`, because the counts are one request
        // per aspect and the capture happened between the aspect list and the last of them.
        for (int tick = 0; tick < 20 && !home.IsBusy; tick++)
          Pump(TimeSpan.FromMilliseconds(50));
        for (int tick = 0; tick < 200 && home.IsBusy; tick++)
          Pump(TimeSpan.FromMilliseconds(50));
      }

      Console.WriteLine("status:   " + (home != null
          ? home.Status + ", " + home.Aspects.Count + " aspect row(s)"
          : (connection != null
              ? connection.Status + ", " + connection.Servers.Count + " server(s) listed, saving "
                + (connection.SaveCommand.CanExecute(null) ? "is offered" : "is not offered yet")
              : playbackStatus)));

      if (home != null && screenName.Equals("home", StringComparison.Ordinal))
      {
        // Said here, after the screen has read the server and just before the picture is taken, because this is the
        // one view of the menu against a real MP2 server: what the gate prints is then something the run claims and
        // can be wrong about, rather than a sentence about a picture that nobody can check.
        Console.WriteLine("menu:     " + string.Join(" | ",
            home.Menu.Select(entry => entry.Label + " " + entry.Detail)));
      }

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

      // Which palette the frame was painted with is a claim about pixels, not about a dictionary: the colour behind
      // the whole screen has to be the one the theme says it is, or the theme never reached the television. After
      // the frame was written, on purpose - the thing needed to judge a complaint about colour is the frame.
      string paletteReport;
      string? paletteWrong = CheckBackgroundColour(frame, appliedTheme, out paletteReport);
      Console.WriteLine("palette:  " + paletteReport);
      if (paletteWrong != null)
      {
        Console.WriteLine("FAILED:   " + paletteWrong);
        return 1;
      }

      if (settingsScreen != null)
      {
        // The settings screen's claim is that every palette is on the television, not just in the table - and
        // the screen itself proves it, because each row paints its own accent as a swatch. The frame is asked
        // for all eleven accents by name; one that paints no pixels is a row that did not make it to the
        // screen, which no count of distinct colours would ever notice.
        string swatchReport;
        string? swatchesMissing = CheckThemeSwatches(frame, out swatchReport);
        Console.WriteLine("swatches: " + swatchReport);
        if (swatchesMissing != null)
        {
          Console.WriteLine("FAILED:   " + swatchesMissing);
          return 1;
        }
      }

      if (screenName == "pictures")
      {
        // The scripted photographs are flat colours for exactly this question, which is the one the pictures
        // screen actually has: did the bytes that came over HTTP for a tile become pixels inside that tile? A
        // person looking at the frame can see that; a gate cannot, so the frame is asked for two colours and for
        // the two places they turned up in.
        string pictureReport;
        string? picturesMissing = CheckPictureTiles(frame, out pictureReport);
        Console.WriteLine("pictures: " + pictureReport);
        if (picturesMissing != null)
        {
          Console.WriteLine("FAILED:   " + picturesMissing);
          return 1;
        }

        // The other question a tile's picture raises is how big the decoded thing is, and that is a question only
        // a real decoder can answer - see the reason on the check itself.
        string decodeReport;
        string? decodeMissing = CheckShrunkDecode(out decodeReport);
        Console.WriteLine("decode:   " + decodeReport);
        if (decodeMissing != null)
        {
          Console.WriteLine("FAILED:   " + decodeMissing);
          return 1;
        }
      }

      if (screenName == "browser" || screenName == "details")
      {
        // Only a cover of a colour this probe knows can be looked for in the pixels; a shelf of episode stills is
        // full of photographs, and the most the probe can say about those by itself is how many rows were given
        // bytes, which it printed before rendering. Whoever wants to see those pictures looks at the frame.
        bool knownCover = aspectName.Equals("MovieItem", StringComparison.OrdinalIgnoreCase);
        if (browserScreen != null && !knownCover)
        {
          Console.WriteLine("pictures: the shelf was asked for '" + aspectName + "', whose pictures are photographs"
              + " - how many rows carry one is the line above, and the frame is for a person to look at");
        }
        else
        {
          // The scripted database stores a flat poster for two of the twelve films, nothing for nine, and bytes
          // that are not a picture for one. Whether any of that reached the shelf is a question about the colour of
          // particular pixels, and only a frame with the whole twelve rows on it can answer it in order - which is
          // why the filtered frames (search results, the watched half) are not asked: their rows are wherever the
          // filter left them.
          //
          // This runs after the frame was written on purpose. A poster the check cannot find is a complaint about
          // colour, and the thing a person needs in order to judge it is the frame itself.
          string? posters;
          string posterReport;
          if (browserScreen != null)
            posters = CheckLivePoster(frame, out posterReport);
          else
            posters = CheckPosters(frame, screenName == "details", out posterReport);
          Console.WriteLine("pictures: " + posterReport);
          if (posters != null)
          {
            Console.WriteLine("FAILED:   " + posters);
            return 1;
          }
        }
      }

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
    /// <summary>
    /// Answers a request with one object in the server's own JSON shape, so that a scripted frame goes through the
    /// client's parsing rather than skipping it.
    /// </summary>
    private static Task<HttpResponseMessage> Answer<T>(HttpRequestMessage request, T body)
    {
      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
      {
        RequestMessage = request,
        Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
      });
    }

    /// <summary>
    /// A flat colour as the importer stores one: PNG bytes of a 64 by 64 picture, which is MP2's thumbnail size.
    /// </summary>
    /// <remarks>
    /// Flat on purpose. A check that looks for a colour in a rendered frame can then be a statement about whether
    /// the bytes that came over HTTP became the pixels a viewer would have seen, and not about how a particular
    /// photograph happened to be drawn.
    /// </remarks>
    private static byte[] SolidPicture(int[] rgb)
    {
      WriteableBitmap picture = new WriteableBitmap(new PixelSize(64, 64), new Vector(96, 96),
          PixelFormats.Bgra8888, AlphaFormat.Opaque);
      using (ILockedFramebuffer surface = picture.Lock())
      {
        int redOffset = surface.Format == PixelFormat.Rgba8888 ? 0 : 2;
        int blueOffset = surface.Format == PixelFormat.Rgba8888 ? 2 : 0;
        for (int y = 0; y < 64; y++)
        {
          for (int x = 0; x < 64; x++)
          {
            IntPtr at = surface.Address + y * surface.RowBytes + x * 4;
            System.Runtime.InteropServices.Marshal.WriteByte(at + redOffset, (byte) rgb[0]);
            System.Runtime.InteropServices.Marshal.WriteByte(at + 1, (byte) rgb[1]);
            System.Runtime.InteropServices.Marshal.WriteByte(at + blueOffset, (byte) rgb[2]);
            System.Runtime.InteropServices.Marshal.WriteByte(at + 3, 255);
          }
        }
      }

      using MemoryStream png = new MemoryStream();
      picture.Save(png);
      return png.ToArray();
    }

    /// <summary>
    /// Whether the converter really hands back a smaller picture when a template asks for one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This can only be measured on a platform with a real decoder. The unit-test fixture writes down that
    /// Avalonia's pure-managed headless rasterizer answers every array of bytes with a one-pixel picture, so a
    /// shrink asked for there is a shrink that cannot be seen; here Skia is switched on, which is the same code
    /// path Windows, Linux and Android decode their photographs with.
    /// </para>
    /// <para>
    /// It is worth this much trouble because the whole point of the smaller decode is invisible otherwise: a tile
    /// looks the same whether it holds a 100-pixel picture or a 12-megapixel one, while the second of the two
    /// pushes every other tile out of the converter's budget.
    /// </para>
    /// </remarks>
    /// <summary>
    /// Whether the frame was really painted with the palette it says it was.
    /// </summary>
    /// <remarks>
    /// Two things have to hold. The pixel in the corner - where nothing but the window's own background can be -
    /// has to be the palette's background colour, and that colour has to be what the frame is standing on rather
    /// than a patch somewhere. A palette applied to the application but never asked for by a screen fails the
    /// second half, which is the failure a green build would otherwise hide.
    /// </remarks>
    private static string? CheckBackgroundColour(WriteableBitmap frame, string theme, out string report)
    {
      Color wanted = FrontendTheme.ColorOf(theme, "ScreenBackgroundBrush");

      string? unreadable = ReadFrame(frame, out byte[] pixels, out int rowBytes, out int width, out int height,
          out int redOffset, out int blueOffset);
      if (unreadable != null)
      {
        report = "the frame could not be read (" + unreadable + ")";
        return "the frame could not be read to check its palette: " + unreadable;
      }

      int[] rgb = { wanted.R, wanted.G, wanted.B };
      long painted = 0;
      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          if (IsColour(pixels, rowBytes, redOffset, blueOffset, x, y, rgb, 4))
            painted++;
        }
      }

      long total = (long) width * height;
      double percent = total == 0 ? 0 : painted * 100.0 / total;
      string share = percent.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
      bool cornerPainted = IsColour(pixels, rowBytes, redOffset, blueOffset, 0, 0, rgb, 4);

      report = "'" + theme + "' draws " + Hex(wanted) + " behind the screen; the corner is that colour and "
          + share + "% of the " + total + " pixels are";

      if (!cornerPainted)
        return "the top-left pixel is not " + Hex(wanted) + ", so the screen was not drawn with the theme '"
            + theme + "'";

      // A palette that coloured one corner and nothing else would satisfy the check above while never having
      // reached a single screen, which is the thing this probe exists to notice.
      if (percent < 2.0)
        return "only " + share + "% of the frame carries the theme's background colour, so the palette did not "
            + "reach the screens";

      return null;
    }

    /// <summary>
    /// Whether every palette's accent turned into pixels on the settings screen. Each row of that screen paints
    /// its own accent as a swatch, so a palette that exists in the table but not on the television is visible
    /// here in a way no count of distinct colours could show.
    /// </summary>
    private static string? CheckThemeSwatches(WriteableBitmap frame, out string report)
    {
      string? unreadable = ReadFrame(frame, out byte[] pixels, out int rowBytes, out int width, out int height,
          out int redOffset, out int blueOffset);
      if (unreadable != null)
      {
        report = "the frame could not be read (" + unreadable + ")";
        return "the frame could not be read to check the swatches: " + unreadable;
      }

      long least = long.MaxValue;
      string thinnest = "";

      foreach (string name in FrontendTheme.Names)
      {
        Color accent = FrontendTheme.ColorOf(name, "AccentBrush");
        int[] rgb = { accent.R, accent.G, accent.B };
        long painted = 0;
        for (int y = 0; y < height; y++)
        {
          for (int x = 0; x < width; x++)
          {
            if (IsColour(pixels, rowBytes, redOffset, blueOffset, x, y, rgb, 4))
              painted++;
          }
        }

        if (painted < least)
        {
          least = painted;
          thinnest = name;
        }
      }

      // A miniature swatch is 64 by 20 pixels, so a row that was drawn carries about that many of its accent's
      // pixels; three hundred is the difference between "that row is on the screen" and "one anti-aliased edge
      // somewhere happened to hit this colour".
      report = "all " + FrontendTheme.Names.Length + " palettes painted their accent; the thinnest (" + thinnest
          + ") has " + least + " pixels";

      return least < 300
          ? "only " + least + " pixels of the '" + thinnest + "' accent are on the frame, so that row was not "
            + "drawn - a swatch is 64x20"
          : null;
    }

    /// <summary>
    /// A colour the way a palette writes one. <c>Color.ToString()</c> answers a well-known colour with its name
    /// ("BLACK"), which in a log a gate reads is worse than useless beside the hex a palette is written in.
    /// </summary>
    private static string Hex(Color colour)
    {
      return string.Format(System.Globalization.CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}",
          colour.A, colour.R, colour.G, colour.B);
    }

    private static string? CheckShrunkDecode(out string report)
    {
      const int SOURCE_WIDTH = 300;
      const int SOURCE_HEIGHT = 150;
      const int ASKED_WIDTH = 100;

      ArtworkConverter converter = new ArtworkConverter();
      byte[] bytes = SolidPictureOfSize(POSTER_PINK, SOURCE_WIDTH, SOURCE_HEIGHT);

      object? whole = converter.Convert(bytes, typeof(Bitmap), null!,
          System.Globalization.CultureInfo.InvariantCulture);
      object? shrunk = converter.Convert(bytes, typeof(Bitmap), ASKED_WIDTH,
          System.Globalization.CultureInfo.InvariantCulture);

      if (whole is not Bitmap wholePicture || shrunk is not Bitmap shrunkPicture)
      {
        report = "the converter gave no picture for bytes that hold one";
        return "the converter answered a picture with nothing on a platform that has a decoder: "
            + (whole == null ? "the whole picture" : "the shrunk one") + " is missing";
      }

      // No string.Format with a culture here: this file spells its numbers by hand because it does not carry
      // System.Globalization, and a report line has no business changing the usings of a probe.
      report = "a " + SOURCE_WIDTH + "x" + SOURCE_HEIGHT + " picture decodes at "
          + wholePicture.PixelSize.Width + "x" + wholePicture.PixelSize.Height + " asked for as a whole and at "
          + shrunkPicture.PixelSize.Width + "x" + shrunkPicture.PixelSize.Height + " asked for at " + ASKED_WIDTH
          + " across";

      if (wholePicture.PixelSize.Width != SOURCE_WIDTH || wholePicture.PixelSize.Height != SOURCE_HEIGHT)
        return "a picture nobody asked to shrink came back " + wholePicture.PixelSize.Width + "x"
            + wholePicture.PixelSize.Height + " instead of " + SOURCE_WIDTH + "x" + SOURCE_HEIGHT
            + " - a screen that raises a photograph is being handed a thumbnail of it";

      if (shrunkPicture.PixelSize.Width != ASKED_WIDTH)
        return "a picture asked for at " + ASKED_WIDTH + " across was decoded " + shrunkPicture.PixelSize.Width
            + " across - the tile is being handed the whole photograph, which is the memory the smaller decode "
            + "exists to avoid";

      if (shrunkPicture.PixelSize.Height != SOURCE_HEIGHT * ASKED_WIDTH / SOURCE_WIDTH)
        return "the shrunk picture is " + shrunkPicture.PixelSize.Height + " lines tall, which is not the same "
            + "shape as the original - a tile that stretches its picture draws the photograph out of proportion";

      if (converter.Held != 2)
        return "the same bytes at two sizes were remembered as " + converter.Held
            + " picture(s); a raised photograph and its tile need their own, or one evicts the other";

      return null;
    }

    /// <summary>
    /// A flat PNG of the given size, for the questions where the size of the file is the subject.
    /// </summary>
    private static byte[] SolidPictureOfSize(int[] rgb, int width, int height)
    {
      WriteableBitmap picture = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96),
          PixelFormats.Bgra8888, AlphaFormat.Opaque);
      using (ILockedFramebuffer surface = picture.Lock())
      {
        int redOffset = surface.Format == PixelFormat.Rgba8888 ? 0 : 2;
        int blueOffset = surface.Format == PixelFormat.Rgba8888 ? 2 : 0;
        for (int y = 0; y < height; y++)
        {
          for (int x = 0; x < width; x++)
          {
            IntPtr at = surface.Address + y * surface.RowBytes + x * 4;
            System.Runtime.InteropServices.Marshal.WriteByte(at + redOffset, (byte) rgb[0]);
            System.Runtime.InteropServices.Marshal.WriteByte(at + 1, (byte) rgb[1]);
            System.Runtime.InteropServices.Marshal.WriteByte(at + blueOffset, (byte) rgb[2]);
            System.Runtime.InteropServices.Marshal.WriteByte(at + 3, 255);
          }
        }
      }

      using MemoryStream png = new MemoryStream();
      picture.Save(png);
      return png.ToArray();
    }

    /// <summary>
    /// Whether one pixel is the colour this probe knows, allowing for the blending a rounded corner or a scaling
    /// step puts on an otherwise flat picture.
    /// </summary>
    private static bool IsColour(int red, int green, int blue, int[] wanted)
    {
      return Math.Abs(red - wanted[0]) <= 24 && Math.Abs(green - wanted[1]) <= 24
          && Math.Abs(blue - wanted[2]) <= 24;
    }

    private sealed class ScriptedTitles : HttpMessageHandler
    {
      private static readonly string[] NAMES =
      {
          "Nimmerland", "Alien Earth", "The Long Shore", "Winter in the Mountains", "A Quiet Harbour",
          "Paper Cities", "The Sixth Continent", "Between Two Rivers", "Northlight", "The Cartographer",
          "Salt and Static", "A Room Facing East"
      };

      /// <summary>
      /// Which of the films the library was told had been watched to the end: the index into <see cref="NAMES"/>,
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
      /// <remarks>
      /// Four of them, on three different weeks, because the home screen's strip promises the most recently stopped
      /// title first - an order that cannot be seen in a frame holding one row.
      /// </remarks>
      private static readonly Dictionary<int, DateTime> STOPPED_HALFWAY =
          new Dictionary<int, DateTime>
          {
              { 4, new DateTime(2025, 3, 1, 22, 5, 0) },
              { 8, new DateTime(2025, 4, 18, 20, 10, 0) },
              { 10, new DateTime(2025, 4, 22, 18, 45, 0) }
          };

      /// <summary>
      /// The series side of the same household. The strip asks every part of the library it offers a tile for, so
      /// a second aspect is the only way a frame can show what the screen does with answers from two of them: it
      /// puts them in one list by the date they were stopped, not in two lists by where they came from.
      /// </summary>
      private static readonly string[] EPISODES =
      {
          "S01E01 - The House at the End", "S01E02 - Low Tide", "S01E03 - The Letters"
      };

      private static readonly Dictionary<int, (int PlayCount, DateTime LastPlayed)> EPISODES_FINISHED =
          new Dictionary<int, (int PlayCount, DateTime LastPlayed)>
          {
              { 0, (1, new DateTime(2025, 2, 1, 20, 30, 0)) }
          };

      private static readonly Dictionary<int, DateTime> EPISODES_STOPPED_HALFWAY =
          new Dictionary<int, DateTime> { { 1, new DateTime(2025, 4, 20, 21, 5, 0) } };

      /// <summary>
      /// How far the person using the front end got in the titles they stopped: the library's per-person figure,
      /// which is what turns "you looked at this in April" into "eleven minutes of this are gone".
      /// </summary>
      /// <remarks>
      /// Deliberately not every stopped title - <c>A Quiet Harbour</c> is left without a figure so that the frame
      /// shows both states the column has: a percentage where the server had one, and nothing at all where it had
      /// none. A screen that invented "0%" for the missing ones would look like a fact.
      /// </remarks>
      private static readonly Dictionary<int, int> LEFT_HALF_WAY =
          new Dictionary<int, int> { { 8, 61 }, { 10, 12 } };

      private static readonly Dictionary<int, int> EPISODES_LEFT_HALF_WAY =
          new Dictionary<int, int> { { 1, 74 } };

      /// <summary>How many titles the library says each aspect holds, for the tiles on the home screen.</summary>
      private static readonly Dictionary<string, int> COUNTS =
          new Dictionary<string, int> { { "MovieItem", 12 }, { "EpisodeItem", 3 }, { "PersonItem", 40 } };

      /// <summary>
      /// The id of one of the twelve films, in the same shape the title list below gives it.
      /// </summary>
      private static Guid FilmId(int index)
      {
        return new Guid(string.Format("00000000-0000-4000-8000-{0:D12}", index + 1));
      }

      /// <summary>
      /// What the database holds for the three titles whose pictures get asked for.
      /// </summary>
      /// <remarks>
      /// <para>
      /// Two real pictures, each one flat colour, because the check on the other side of this asks the rendered
      /// frame for a colour and a square: anything with detail in it would make the check depend on how a picture
      /// was drawn rather than on whether it arrived at all. Two colours, not one, because one poster on screen
      /// could be any row's poster - two, in two rows and two colours, is what says each row drew its own.
      /// </para>
      /// <para>
      /// The third entry is not a picture. A library can hold bytes that are not an image - a download cut short,
      /// a file somebody renamed - and the shelf has to survive it, which is only observable with a real decoder
      /// and therefore belongs here rather than in the headless unit tests.
      /// </para>
      /// </remarks>
      private static readonly Dictionary<Guid, byte[]> StoredPictures = new Dictionary<Guid, byte[]>
      {
          { FilmId(0), SolidPicture(POSTER_PINK) },
          { FilmId(2), SolidPicture(POSTER_TEAL) },
          { FilmId(4), System.Text.Encoding.ASCII.GetBytes("this used to be a poster") }
      };

      /// <summary>
      /// The music shelf: eight songs, each a name and the artist the shelf carries with it. Songs are the one
      /// shelf no other frame touches, and the party screen is the one screen that plays without a picture -
      /// so the shelf lives here, in the same shape the titles handler gives films.
      /// </summary>
      internal static readonly string[,] TRACKS =
      {
          { "Harbour Lights", "The Long Shore" },
          { "Paper Radio", "Sixth Continent" },
          { "Northlight", "The Cartographer" },
          { "Salt and Static", "The Long Shore" },
          { "A Room Facing East", "Paper Cities" },
          { "Winter's End", "Nimmerland" },
          { "Low Tide Again", "Between Two Rivers" },
          { "The Last Ferry", "Sixth Continent" }
      };

      /// <summary>Whether this is the party's question - the shelf of songs rather than of films.</summary>
      private static bool askedAspectMusic(HttpRequestMessage request)
      {
        return string.Equals(ParameterIn(request.RequestUri!.Query, "aspect"), "AudioItem",
            StringComparison.OrdinalIgnoreCase);
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string folder = Path.Combine(Path.GetTempPath(), "mp2-render");
        string path = request.RequestUri!.AbsolutePath;

        // The three questions a home screen asks before it can show a tile: who is there, what parts of a library
        // is it made of, and how many titles each holds. Answered here for the same reason the titles are: the
        // gate's server has an empty database on purpose, and a strip of titles left half way cannot be drawn
        // from one, so the whole screen - tiles, counts and strip - is scripted in the server's own shapes.
        if (path.Contains("api/serverinfo", StringComparison.Ordinal))
        {
          ServerInfo info = new ServerInfo("MediaPortal 2 Server", "2.0.0-probe", ".NET 10.0",
              "Windows", "x64", "x64", 4711, "2025-04-26T19:00:00Z", "/MediaPortal/Server",
              new List<string> { "127.0.0.1:1234" }, true, false);
          return Answer(request, info);
        }

        if (path.Contains("api/library/aspects", StringComparison.Ordinal))
        {
          LibraryAspects aspects = new LibraryAspects(3, new List<MediaAspect>
          {
              new MediaAspect(new Guid("00000000-0000-4000-8000-0000000000a1"), "MovieItem", false, false, null,
                  Array.Empty<AspectAttribute>()),
              new MediaAspect(new Guid("00000000-0000-4000-8000-0000000000a2"), "EpisodeItem", false, false, null,
                  Array.Empty<AspectAttribute>()),
              new MediaAspect(new Guid("00000000-0000-4000-8000-0000000000a3"), "PersonItem", true, false, null,
                  Array.Empty<AspectAttribute>())
          });
          return Answer(request, aspects);
        }

        if (path.Contains("api/library/count", StringComparison.Ordinal))
        {
          string counted = ParameterIn(request.RequestUri.Query, "aspect") ?? "MovieItem";
          COUNTS.TryGetValue(counted, out int howMany);
          return Answer(request, new MediaCount(counted, null, howMany));
        }

        // The pictures of the shelf. Three of the twelve titles answer, because a real library's shelf is exactly
        // this mixture: two films whose poster the importer stored in the database, one whose stored picture is
        // not a picture, and nine with nothing stored at all. The gate then asks the rendered frame for the two
        // posters by colour and for the box behind the corrupt one, which is the only way to find out whether the
        // bytes that came over HTTP were the pixels a viewer would have seen.
        if (path.EndsWith("/thumbnail", StringComparison.Ordinal))
        {
          Guid asked = Guid.Parse(path.Substring(path.LastIndexOf('/') - 36, 36));
          if (StoredPictures.TryGetValue(asked, out byte[]? stored))
          {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
              RequestMessage = request,
              Content = new ByteArrayContent(stored)
            });
          }

          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }

        // One film read up close, which is what no row of a list has room for. Answered in the shape of
        // GET api/media/items/{id} for whichever id is asked, because the frame is about the panel rather than
        // about a database: the title it opens on is the first of the twelve below.
        if (path.Contains("api/media/items/", StringComparison.Ordinal))
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

          return Answer(request, one);
        }

        // The music shelf, for the party screen. Songs rather than films because the party asks the server for
        // the AudioItem aspect and the frame has to be what that question gets: short names with an artist and a
        // track number behind them, files that end in .mp3, and no watching states - a song is not watched.
        if (askedAspectMusic(request))
        {
          List<MediaTitle> songs = new List<MediaTitle>();
          for (int at = 0; at < TRACKS.GetLength(0); at++)
          {
            songs.Add(new MediaTitle(
                new Guid(string.Format("00000000-0000-5000-8000-{0:D12}", at + 1)),
                TRACKS[at, 0], TRACKS[at, 0], 2014 + at, null,
                "{D3525B2F-7322-430C-87E7-CDA8EBD6EDD2}://" + Path.Combine(folder,
                    TRACKS[at, 0].Replace(' ', '_') + ".mp3"),
                false, null, null, null, null, null, at + 1, TRACKS[at, 1]));
          }

          MediaTitles wholeShelf = new MediaTitles("AudioItem", null, songs.Count, 0, songs.Count, songs.Count,
              "title", songs, null);
          return Answer(request, wholeShelf);
        }

        List<MediaTitle> titles = new List<MediaTitle>();

        // A searched and/or filtered shelf, if the request asked for one. Answered here in the same shape a real
        // library answers in - matching titles, echoing the words and the filter back - because the frame is about
        // what the screen does with a filtered answer, and the answer it is given has to be the answer a filtered
        // question produces.
        string finding = ParameterIn(request.RequestUri.Query, "search");
        string seeing = ParameterIn(request.RequestUri.Query, "seen");
        string askedAspect = ParameterIn(request.RequestUri.Query, "aspect") ?? "MovieItem";
        bool episodes = string.Equals(askedAspect, "EpisodeItem", StringComparison.OrdinalIgnoreCase);
        string[] shelf = episodes ? EPISODES : NAMES;
        Dictionary<int, (int PlayCount, DateTime LastPlayed)> finishedOn =
            episodes ? EPISODES_FINISHED : FINISHED;
        Dictionary<int, DateTime> stoppedOn = episodes ? EPISODES_STOPPED_HALFWAY : STOPPED_HALFWAY;
        Dictionary<int, int> leftAt = episodes ? EPISODES_LEFT_HALF_WAY : LEFT_HALF_WAY;

        List<string> matching = new List<string>();
        foreach (string name in shelf)
        {
          if (finding != null && name.IndexOf(finding, StringComparison.OrdinalIgnoreCase) < 0)
            continue;

          int scripted = Array.IndexOf(shelf, name);
          bool finished = finishedOn.ContainsKey(scripted);
          bool opened = stoppedOn.ContainsKey(scripted) || finished;
          if (seeing == "watched" && !finished)
            continue;
          if (seeing == "unwatched" && finished)
            continue;
          if (seeing == "started" && (finished || !opened))
            continue;

          matching.Add(name);
        }

        for (int i = 0; i < matching.Count; i++)
        {
          int scripted = Array.IndexOf(shelf, matching[i]);
          finishedOn.TryGetValue(scripted, out (int PlayCount, DateTime LastPlayed) viewing);
          DateTime? lastPlayed = viewing.PlayCount > 0
              ? viewing.LastPlayed
              : stoppedOn.TryGetValue(scripted, out DateTime stopped) ? stopped : (DateTime?) null;

          // The position is only ever sent for titles somebody stopped, and never as a nought: the server's own
          // rule, kept here so that the frame is drawn from the same kind of answer a real server gives.
          leftAt.TryGetValue(scripted, out int howFar);

          titles.Add(new MediaTitle(
              new Guid(string.Format("00000000-0000-4000-8000-{0:D12}", scripted + 1 + (episodes ? 100 : 0))),
              matching[i], matching[i], 2011 + scripted, 88 + scripted * 3,
              "{D3525B2F-7322-430C-87E7-CDA8EBD6EDD2}://" + Path.Combine(folder, matching[i] + ".mkv"),
              false, viewing.PlayCount, lastPlayed, howFar > 0 ? howFar : (int?) null));
        }

        MediaTitles page = new MediaTitles(askedAspect, null, matching.Count, 0, matching.Count, matching.Count,
            "title", titles, null, finding, seeing);

        return Answer(request, page);
      }

      /// <summary>One scripted answer, in the server's own JSON shape.</summary>
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
    /// One share, what is inside it, and two flat photographs to fetch, served without a network.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The paths are built by <c>LocalFsResourceProviderBase</c> rather than typed out: the provider's spelling is
    /// a GUID followed by a <c>://</c>, a hand-written shorthand parses as nothing, and a frame whose paths the
    /// screen could not have asked for would be a frame of a screen that never runs.
    /// </para>
    /// <para>
    /// The answers are written as the wire format rather than as objects, so that the client's parsing and the
    /// bindings are inside the frame as well as the drawing - the same reason the scripted titles are written that
    /// way.
    /// </para>
    /// </remarks>
    /// <summary>
    /// A server that answers the one question the browse screen asks. The counts are the point of the frame: they
    /// are served biggest first with a hole - the empty value a database answers with for "nobody wrote a genre" -
    /// because whether the screen keeps the server's order and drops the hole is exactly what a picture can show
    /// and a unit test can only assert.
    /// </summary>
    private sealed class ScriptedBrowse : HttpMessageHandler
    {
      private const string GROUPS = "{\"aspect\":\"MovieItem\",\"by\":\"genre\",\"groups\":["
          + "{\"value\":\"Drama\",\"count\":7},{\"value\":\"\",\"count\":3},"
          + "{\"value\":\"Action\",\"count\":5},{\"value\":\"Shorts\",\"count\":1}]}";

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string body = request.RequestUri!.AbsolutePath.EndsWith("/groups", StringComparison.Ordinal)
            ? GROUPS
            : "{\"error\":\"this probe answers only the groups question\"}";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
      }
    }

    /// <summary>
    /// The internet as the news frame needs it: MediaPortal's feed list in the shape the real one has, one RSS
    /// feed, one Atom feed, and one address that refuses with a 500 so that the frame pictures the screen's
    /// ugliest honest state next to its best one.
    /// </summary>
    private sealed class ScriptedNews : HttpMessageHandler
    {
      private const string FEEDS_XML = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RegionalFeeds>
  <Region Code=""en""><Feeds>
    <Feed Name=""TV Weekly"">http://127.0.0.1:1/feeds/tv.rss</Feed>
    <Feed Name=""Tech Wire"">http://127.0.0.1:1/feeds/tech.atom</Feed>
    <Feed Name=""Dead Paper"">http://127.0.0.1:1/feeds/dead</Feed>
  </Feeds></Region>
</RegionalFeeds>";

      private const string TV_RSS = @"<?xml version=""1.0"" encoding=""utf-8""?>
<rss version=""2.0""><channel>
<title>TV Weekly</title><link>http://127.0.0.1:1/</link><description>What is on tonight</description>
<item><title>Series returns for a final season</title><pubDate>Fri, 02 Jan 2026 08:00:00 GMT</pubDate>
<description><![CDATA[<p>The cast returned <b>in full</b> &amp; filmed the whole season</p>]]></description></item>
<item><title>Notes without a date</title><description>An item the feed forgot to date</description></item>
</channel></rss>";

      private const string TECH_ATOM = @"<?xml version=""1.0"" encoding=""utf-8""?>
<feed xmlns=""http://www.w3.org/2005/Atom"">
<title>Tech Wire</title><updated>2026-01-03T10:00:00Z</updated>
<entry>
  <title>New player ships with hardware decoding</title>
  <id>urn:uuid:6d67e0f1-3a1d-4c2e-9a11-0f9e8d7c6b5a</id>
  <published>2026-01-03T10:00:00Z</published>
  <updated>2026-01-03T11:00:00Z</updated>
  <summary>Just the summary</summary>
  <content type=""text"">The player decodes the big files on its own chip now</content>
</entry>
</feed>";

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/dead", StringComparison.Ordinal))
        {
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
          {
            RequestMessage = request,
            Content = new StringContent("gone", Encoding.UTF8, "text/plain")
          });
        }

        string body = path.EndsWith("/DefaultFeeds.xml", StringComparison.Ordinal) ? FEEDS_XML
            : path.EndsWith("/tv.rss", StringComparison.Ordinal) ? TV_RSS
            : path.EndsWith("/tech.atom", StringComparison.Ordinal) ? TECH_ATOM
            : null!;
        if (body == null)
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/rss+xml")
        });
      }
    }

    /// <summary>
    /// OpenWeatherMap as the weather frame needs it: one fixed sky now and four three-hourly entries spanning
    /// two of the city's own local days - rainy at ten, broken clouds at two - so the frame pictures the
    /// aggregation rule (a day is its midday's weather and its own extremes) rather than a live forecast that
    /// would mean a different picture every afternoon.
    /// </summary>
    private sealed class ScriptedWeather : HttpMessageHandler
    {
      private const string CURRENT_JSON = "{\"coord\":{\"lon\":13.39,\"lat\":52.52},"
          + "\"weather\":[{\"id\":801,\"main\":\"Clouds\",\"description\":\"few clouds\",\"icon\":\"02d\"}],"
          + "\"main\":{\"temp\":8.04,\"feels_like\":6.1,\"temp_min\":6.5,\"temp_max\":8.98,\"pressure\":1032,"
          + "\"humidity\":78},\"wind\":{\"speed\":3.09,\"deg\":60},\"name\":\"Berlin\"}";

      private const string FORECAST_JSON = "{\"cod\":\"200\",\"cnt\":4,\"city\":{\"id\":2950159,"
          + "\"name\":\"Berlin\",\"timezone\":7200},\"list\":["
          + "{\"dt\":1767427200,\"main\":{\"temp\":4.5,\"feels_like\":2.0,\"temp_min\":1.2,\"temp_max\":5.4,"
          + "\"pressure\":1030,\"humidity\":88},\"weather\":[{\"id\":500,\"main\":\"Rain\","
          + "\"description\":\"light rain\",\"icon\":\"10d\"}],\"pop\":0.2,\"wind\":{\"speed\":2.0,\"deg\":200}},"
          + "{\"dt\":1767434400,\"main\":{\"temp\":6.0,\"feels_like\":4.0,\"temp_min\":3.0,\"temp_max\":6.1,"
          + "\"pressure\":1031,\"humidity\":70},\"weather\":[{\"id\":803,\"main\":\"Clouds\","
          + "\"description\":\"broken clouds\",\"icon\":\"04d\"}],\"pop\":0.1,\"wind\":{\"speed\":2.5,\"deg\":240}},"
          + "{\"dt\":1767513600,\"main\":{\"temp\":2.0,\"feels_like\":-1.0,\"temp_min\":-1.4,\"temp_max\":3.3,"
          + "\"pressure\":1028,\"humidity\":92},\"weather\":[{\"id\":600,\"main\":\"Snow\","
          + "\"description\":\"snow\",\"icon\":\"13d\"}],\"pop\":0.6,\"wind\":{\"speed\":4.0,\"deg\":320}},"
          + "{\"dt\":1767520800,\"main\":{\"temp\":3.5,\"feels_like\":1.0,\"temp_min\":0.5,\"temp_max\":3.9,"
          + "\"pressure\":1029,\"humidity\":80},\"weather\":[{\"id\":600,\"main\":\"Snow\","
          + "\"description\":\"light snow\",\"icon\":\"13d\"}],\"pop\":0.4,\"wind\":{\"speed\":3.5,\"deg\":340}}"
          + "]}";

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        string? body = path.EndsWith("/weather", StringComparison.Ordinal) ? CURRENT_JSON
            : path.EndsWith("/forecast", StringComparison.Ordinal) ? FORECAST_JSON
            : null;
        if (body == null)
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
      }
    }

    private sealed class ScriptedFoldersAndPictures : HttpMessageHandler
    {
      private static readonly string SHARE_DIR = Path.Combine(Path.GetTempPath(), "mp2-render-pictures");
      private static readonly string TRIP_DIR = Path.Combine(SHARE_DIR, "2019");
      private static readonly string SHARE_PATH = AsResourcePath(SHARE_DIR);
      private static readonly string TRIP_PATH = AsResourcePath(TRIP_DIR);

      /// <summary>
      /// The photographs whose bytes this script hands back, in the two colours the frame then looks for.
      /// </summary>
      /// <remarks>
      /// Two of four on purpose: a folder with more photographs than the screen fetches previews for is the
      /// ordinary case, and the tiles that got nothing are part of the picture being judged.
      /// </remarks>
      private static readonly Dictionary<string, byte[]> PHOTOGRAPHS = new Dictionary<string, byte[]>
      {
          { "beach", SolidPicture(POSTER_PINK) },
          { "harbour", SolidPicture(POSTER_TEAL) }
      };

      private static string AsResourcePath(string dosPath)
      {
        // The way a folder of this machine arrives from a server: the local provider's id in braces, "://", and the
        // provider's own path, which starts with a slash even in front of a drive letter. Spelled by hand because
        // the probe talks to the front end over HTTP and carries no server assembly with it.
        return "{e88e64a8-0233-4fdf-ba27-0b44c6a39ae9}" + "://" + "/" + dosPath.Replace('\\', '/');
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;

        if (path.EndsWith("/api/media/shares", StringComparison.Ordinal))
          return Text(request, "{\"count\":1,\"shares\":[{"
              + "\"id\":\"00000000-0000-4000-8000-0000000000b1\",\"name\":\"Photographs\","
              + "\"path\":\"" + SHARE_DIR.Replace("\\", "\\\\") + "\","
              + "\"resourcePath\":\"" + SHARE_PATH + "\","
              + "\"systemId\":\"host\",\"local\":true,\"mediaCategories\":[\"Image\"],"
              + "\"useShareWatcher\":true}],\"mediaCategories\":[\"Image\"]}");

        if (path.EndsWith("/api/media/browse", StringComparison.Ordinal))
        {
          string asked = Query(request, "path");
          if (asked == TRIP_PATH)
            return Text(request, Listing(TRIP_PATH, "2019", new string[0], new string[0], 0));
          return Text(request, Listing(SHARE_PATH, "Photographs",
              new[] { "2019", "2020" }, new[] { "beach", "harbour", "dunes", "lighthouse" }, 3));
        }

        if (path.EndsWith("/GetResource", StringComparison.Ordinal))
        {
          string asked = Query(request, "ResourcePath");
          string name = asked.Substring(asked.LastIndexOf('/') + 1);
          if (name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            name = name.Substring(0, name.Length - 4);

          byte[]? bytes;
          if (PHOTOGRAPHS.TryGetValue(name, out bytes))
          {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
              RequestMessage = request,
              Content = new ByteArrayContent(bytes)
            });
          }

          // The same answer the real file server gives for a file it will not or cannot hand over, which is what
          // makes the tile say "no picture" rather than "the server is broken".
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
      }

      /// <summary>
      /// One folder as <c>GET api/media/browse</c> answers it.
      /// </summary>
      private static string Listing(string path, string name, string[] folders, string[] pictures, int others)
      {
        List<string> folderEntries = new List<string>();
        foreach (string folder in folders)
          folderEntries.Add("{\"name\":\"" + folder + "\",\"path\":\"" + path + "/" + folder + "\"}");

        List<string> pictureEntries = new List<string>();
        foreach (string picture in pictures)
        {
          pictureEntries.Add("{\"name\":\"" + picture + "\",\"path\":\"" + path + "/" + picture + ".jpg\","
              + "\"size\":180000,\"lastChanged\":\"2019-06-01T10:00:00\"}");
        }

        return "{\"path\":\"" + path + "\",\"name\":\"" + name + "\",\"folders\":["
            + string.Join(",", folderEntries.ToArray()) + "],\"pictures\":["
            + string.Join(",", pictureEntries.ToArray()) + "],\"foldersTruncated\":false,"
            + "\"picturesTruncated\":false,\"otherFileCount\":" + others + "}";
      }

      private static Task<HttpResponseMessage> Text(HttpRequestMessage request, string json)
      {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
      }

      /// <summary>
      /// One query parameter of a request, unescaped. Small and deliberate: the screen's own escaping is part of
      /// what the frame is testing, so this reads what arrived rather than what was meant.
      /// </summary>
      private static string Query(HttpRequestMessage request, string name)
      {
        string? query = request.RequestUri!.Query;
        if (string.IsNullOrEmpty(query))
          return string.Empty;

        foreach (string pair in query!.TrimStart('?').Split('&'))
        {
          int equals = pair.IndexOf('=');
          if (equals > 0 && pair.Substring(0, equals) == name)
            return Uri.UnescapeDataString(pair.Substring(equals + 1));
        }
        return string.Empty;
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
      string? unreadable = ReadFrame(frame, out byte[] pixels, out int rowBytes, out int width, out int height,
          out int redOffset, out int blueOffset);
      if (unreadable != null)
        return unreadable;

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

    /// <summary>
    /// The frame's pixels, and the two offsets anyone reading a colour from them needs.
    /// </summary>
    /// <returns>null when the pixels were read; a sentence about why they could not be when they were not.</returns>
    private static string? ReadFrame(WriteableBitmap frame, out byte[] pixels, out int rowBytes, out int width,
        out int height, out int redOffset, out int blueOffset)
    {
      using ILockedFramebuffer surface = frame.Lock();

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
        pixels = Array.Empty<byte>();
        rowBytes = width = height = 0;
        redOffset = blueOffset = 0;
        return "the frame is " + surface.Format + ", which this check cannot read";
      }

      width = surface.Size.Width;
      height = surface.Size.Height;
      rowBytes = surface.RowBytes;
      pixels = new byte[rowBytes * height];
      Marshal.Copy(surface.Address, pixels, 0, pixels.Length);
      return null;
    }

    /// <summary>The first scripted film's poster, as the scripted database stores it.</summary>
    private static readonly int[] POSTER_PINK = { 212, 64, 168 };

    /// <summary>The third scripted film's poster.</summary>
    private static readonly int[] POSTER_TEAL = { 32, 176, 152 };

    /// <summary>The box the shelf draws behind a picture, which is what a title without one shows.</summary>
    private static readonly int[] PLACEHOLDER_BOX = { 23, 32, 42 };

    /// <summary>
    /// How many pixels of one poster colour the details panel has to show. A shelf thumbnail is 64 by 64 = 4096,
    /// the panel's own picture measured 240 by 240 = 57600, so anything between the two says something is wrong.
    /// </summary>
    private const int PANEL_POSTER_MINIMUM = 30000;

    /// <summary>
    /// One flat-coloured patch of the frame: how big it is and where it lies.
    /// </summary>
    private sealed class Patch
    {
      public Patch(int left, int top, int right, int bottom, int pixels)
      {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Pixels = pixels;
      }

      public int Left { get; }

      public int Top { get; }

      public int Right { get; }

      public int Bottom { get; }

      public int Pixels { get; }

      public int CentreX
      {
        get { return (Left + Right) / 2; }
      }

      public int CentreY
      {
        get { return (Top + Bottom) / 2; }
      }

      public override string ToString()
      {
        return Pixels + " px at x " + Left + ".." + Right + ", y " + Top + ".." + Bottom;
      }
    }

    /// <summary>
    /// Whether the pixel at one place in the frame is this colour, give or take the tolerance a rendered frame
    /// leaves for a blit and for the few pixels an edge is made of.
    /// </summary>
    private static bool IsColour(byte[] pixels, int rowBytes, int redOffset, int blueOffset, int x, int y,
        int[] rgb, int tolerance)
    {
      int at = y * rowBytes + x * 4;
      return Math.Abs(pixels[at + redOffset] - rgb[0]) <= tolerance
          && Math.Abs(pixels[at + 1] - rgb[1]) <= tolerance
          && Math.Abs(pixels[at + blueOffset] - rgb[2]) <= tolerance;
    }

    /// <summary>
    /// Every connected patch of one colour in the frame, largest first.
    /// </summary>
    private static List<Patch> FindPatches(byte[] pixels, int rowBytes, int width, int height, int redOffset,
        int blueOffset, int[] rgb)
    {
      bool[] seen = new bool[width * height];
      Stack<long> todo = new Stack<long>();
      List<Patch> patches = new List<Patch>();

      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          int start = y * width + x;
          if (seen[start] || !IsColour(pixels, rowBytes, redOffset, blueOffset, x, y, rgb, 24))
            continue;

          seen[start] = true;
          todo.Push(start);

          int left = x, top = y, right = x, bottom = y, count = 0;
          while (todo.Count > 0)
          {
            long here = todo.Pop();
            int atX = (int) (here % width);
            int atY = (int) (here / width);
            count++;
            if (atX < left) left = atX;
            if (atX > right) right = atX;
            if (atY < top) top = atY;
            if (atY > bottom) bottom = atY;

            for (int dy = -1; dy <= 1; dy++)
            {
              for (int dx = -1; dx <= 1; dx++)
              {
                int nextX = atX + dx;
                int nextY = atY + dy;
                if (nextX < 0 || nextY < 0 || nextX >= width || nextY >= height)
                  continue;

                int next = nextY * width + nextX;
                if (seen[next] || !IsColour(pixels, rowBytes, redOffset, blueOffset, nextX, nextY, rgb, 24))
                  continue;

                seen[next] = true;
                todo.Push(next);
              }
            }
          }

          patches.Add(new Patch(left, top, right, bottom, count));
        }
      }

      patches.Sort(delegate (Patch a, Patch b) { return b.Pixels.CompareTo(a.Pixels); });
      return patches;
    }

    /// <summary>
    /// Whether the pictures the scripted library stores reached the screen.
    /// </summary>
    /// <param name="frame">The rendered screen.</param>
    /// <param name="detailsOpen">Whether the frame shows the details panel, which hides the shelf.</param>
    /// <param name="report">What was found, in the words the gate prints.</param>
    /// <returns>null when the frame is as it should be; a sentence about what it is not when it is not.</returns>
    /// <remarks>
    /// The counts this probe otherwise prints - colours, edges, contrast - cannot tell a poster from a poster of
    /// the wrong film, nor a decoded picture from the box that is drawn where there is none. So the posters are
    /// looked for by the colour they were stored in. With the panel open the shelf is asked about as well: the
    /// panel sits beside the list rather than over it, and a frame where opening a panel emptied the list behind it
    /// is exactly the mistake this check used to be blind to.
    /// </remarks>
    /// <summary>
    /// Whether the shelf of a running server drew the picture that was stored for one of its titles, in the colour
    /// that picture was made of.
    /// </summary>
    /// <remarks>
    /// The scripted frame is judged by two posters and by the row in between whose stored bytes are not a picture.
    /// A real library has whatever it happens to hold, so this asks the one question that survives: did the bytes
    /// the server sent become a picture on a row? The cover the scratch driver puts into the film's folder is a
    /// lossless PNG of flat rgb(212,64,168) - the colour the scripted poster uses for the same reason - so a patch
    /// of that colour the size of a row's picture square can only be the decoded file.
    /// </remarks>
    /// <summary>
    /// Looks for the two scripted photographs in the rendered pictures screen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both colours have to be on screen, each with at least half a thumbnail's worth of pixels, and they have to
    /// be in two different places. One tile painted twice would say as little as none painted at all; the claim
    /// being checked is that every tile drew the picture it fetched for itself, in its own square.
    /// </para>
    /// <para>
    /// Runs after the frame was written for the same reason the shelf's poster check does: a colour this probe
    /// cannot find is a complaint worth judging with the picture in front of you.
    /// </para>
    /// </remarks>
    private static string? CheckPictureTiles(WriteableBitmap frame, out string report)
    {
      byte[] pixels;
      int rowBytes;
      int width;
      int height;
      int redOffset;
      int blueOffset;
      string? unreadable = ReadFrame(frame, out pixels, out rowBytes, out width, out height, out redOffset,
          out blueOffset);
      if (unreadable != null)
      {
        report = unreadable;
        return "the frame could not be read: " + unreadable;
      }

      int pink = 0;
      int teal = 0;
      int pinkLeft = width;
      int pinkTop = height;
      int tealLeft = width;
      int tealTop = height;

      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          int at = y * rowBytes + x * 4;
          if (IsColour(pixels[at + redOffset], pixels[at + 1], pixels[at + blueOffset], POSTER_PINK))
          {
            pink++;
            if (x < pinkLeft)
              pinkLeft = x;
            if (y < pinkTop)
              pinkTop = y;
          }
          else if (IsColour(pixels[at + redOffset], pixels[at + 1], pixels[at + blueOffset], POSTER_TEAL))
          {
            teal++;
            if (x < tealLeft)
              tealLeft = x;
            if (y < tealTop)
              tealTop = y;
          }
        }
      }

      // A thumbnail is 64 by 64 = 4096 pixels; half of that is the line between "a picture is in this tile" and
      // "an antialiased edge happened to be this colour".
      const int ENOUGH_FOR_A_TILE = 2048;

      report = "the scripted photographs cover " + pink + " and " + teal
          + " pixels, starting at " + pinkLeft + "," + pinkTop + " and " + tealLeft + "," + tealTop;

      if (pink < ENOUGH_FOR_A_TILE)
        return "the first photograph covers only " + pink + " pixels, so no tile drew the bytes it fetched for it.";
      if (teal < ENOUGH_FOR_A_TILE)
        return "the second photograph covers only " + teal + " pixels, so one of the two previews never reached"
            + " the screen.";
      if (pinkLeft == tealLeft && pinkTop == tealTop)
        return "both photographs were found in the same square, so one tile drew twice rather than two tiles each"
            + " drawing the picture it was given.";
      return null;
    }

    private static string? CheckLivePoster(WriteableBitmap frame, out string report)
    {
      string? unreadable = ReadFrame(frame, out byte[] pixels, out int rowBytes, out int width, out int height,
          out int redOffset, out int blueOffset);
      if (unreadable != null)
      {
        report = "the frame could not be read";
        return unreadable;
      }

      List<Patch> patches = FindPatches(pixels, rowBytes, width, height, redOffset, blueOffset, POSTER_PINK);
      Patch? largest = null;
      foreach (Patch candidate in patches)
        if (largest == null || candidate.Pixels > largest.Pixels)
          largest = candidate;

      if (largest == null)
      {
        report = "no poster";
        return "nothing on the frame is rgb(" + string.Join(",", POSTER_PINK)
            + "), the colour of the cover lying in the film's folder; the bytes the server sent never became a"
            + " picture on the screen";
      }
      if (largest.Pixels < 3000)
      {
        report = largest.ToString();
        return "the patch of that colour on the frame is " + largest.Pixels
            + " pixels, which is smaller than the picture square a row draws";
      }

      report = "the bytes the server sent are a picture on the screen (" + largest.Pixels + " px of rgb("
          + string.Join(",", POSTER_PINK) + ") at x " + largest.Left + ".." + largest.Right + ", y "
          + largest.Top + ".." + largest.Bottom + ")";
      return null;
    }

    private static string? CheckPosters(WriteableBitmap frame, bool detailsOpen, out string report)
    {
      string? shelf = CheckShelfPosters(frame, out string shelfReport);
      if (shelf != null || !detailsOpen)
      {
        report = shelfReport;
        return shelf;
      }

      string? panel = CheckPanelPoster(frame, out string panelReport);
      report = shelfReport + ", " + panelReport;
      return panel;
    }

    /// <summary>
    /// Whether the details panel drew the poster of the title it was opened on, and not another title's.
    /// </summary>
    /// <remarks>
    /// The panel opens on the first film, whose stored poster is pink; the teal one belongs to the third film, and
    /// a teal panel would mean the panel read a different id than the row it was opened from. The size is asked
    /// for too: the panel draws its poster larger than a row does, so a row-sized patch on the right would say the
    /// panel is showing a picture that was read for the shelf. Measured here, the panel's poster is 240 by 240.
    /// </remarks>
    private static string? CheckPanelPoster(WriteableBitmap frame, out string report)
    {
      string? unreadable = ReadFrame(frame, out byte[] pixels, out int rowBytes, out int width, out int height,
          out int redOffset, out int blueOffset);
      if (unreadable != null)
      {
        report = "the frame could not be read";
        return unreadable;
      }

      List<Patch> pink = FindPatches(pixels, rowBytes, width, height, redOffset, blueOffset, POSTER_PINK);
      List<Patch> teal = FindPatches(pixels, rowBytes, width, height, redOffset, blueOffset, POSTER_TEAL);

      Patch? panel = null;
      foreach (Patch candidate in pink)
      {
        if (panel == null && candidate.Pixels >= PANEL_POSTER_MINIMUM && candidate.CentreX > width / 2)
          panel = candidate;
      }

      foreach (Patch candidate in teal)
      {
        if (candidate.Pixels >= PANEL_POSTER_MINIMUM && candidate.CentreX > width / 2)
        {
          report = candidate.ToString();
          return "the details panel is drawn in the third film's colour, so it shows a picture that was stored "
              + "for another title";
        }
      }

      if (panel == null)
      {
        report = pink.Count == 0 ? "no poster" : pink[0].ToString();
        return "no poster of at least " + PANEL_POSTER_MINIMUM + " pixels of rgb("
            + string.Join(",", POSTER_PINK) + ") was found on the right half of the frame, where the panel that is "
            + "open on the first film draws its picture"
            + (pink.Count == 0 ? "" : "; the pink on the frame is " + pink[0]);
      }

      report = "the panel draws the poster of the title it is open on (" + panel.Pixels + " px at x "
          + panel.Left + ".." + panel.Right + ", y " + panel.Top + ".." + panel.Bottom + "), and not the other "
          + "film's";
      return null;
    }

    /// <summary>
    /// Whether the shelf drew the pictures the server stored for it, and the box where it had none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two posters are looked for by colour, and the row whose stored bytes are not a picture is looked for by
    /// its geometry: two rows below the first poster, in the same column, where the shelf's own row pitch says it
    /// must be. Nothing here knows the layout, the font or the scale; the pitch is measured out of the frame.
    /// </para>
    /// </remarks>
    private static string? CheckShelfPosters(WriteableBitmap frame, out string report)
    {
      string? unreadable = ReadFrame(frame, out byte[] pixels, out int rowBytes, out int width, out int height,
          out int redOffset, out int blueOffset);
      if (unreadable != null)
      {
        report = "the frame could not be read";
        return unreadable;
      }

      List<Patch> pink = FindPatches(pixels, rowBytes, width, height, redOffset, blueOffset, POSTER_PINK);
      List<Patch> teal = FindPatches(pixels, rowBytes, width, height, redOffset, blueOffset, POSTER_TEAL);

      if (pink.Count == 0)
      {
        report = "no poster";
        return "nothing on the frame is rgb(" + string.Join(",", POSTER_PINK)
            + "), the colour of the first film's poster; the bytes the server sent never became a picture";
      }
      if (teal.Count == 0)
      {
        report = "one poster";
        return "nothing on the frame is rgb(" + string.Join(",", POSTER_TEAL)
            + "), the colour of the third film's poster; a shelf that draws one picture out of two is a shelf "
            + "whose rows are interchangeable";
      }

      Patch firstRow = pink[0];
      foreach (Patch candidate in pink)
        if (candidate.Left < firstRow.Left && candidate.Pixels >= 3000)
          firstRow = candidate;

      Patch secondRow = teal[0];
      foreach (Patch candidate in teal)
        if (candidate.Left < secondRow.Left && candidate.Pixels >= 3000)
          secondRow = candidate;

      if (firstRow.Pixels < 3000)
      {
        report = firstRow.ToString();
        return "the first film's poster is a patch of " + firstRow.Pixels
            + " pixels, which is not the 64 by 64 picture that was stored";
      }
      if (secondRow.Pixels < 3000)
      {
        report = secondRow.ToString();
        return "the third film's poster is a patch of " + secondRow.Pixels
            + " pixels, which is not the 64 by 64 picture that was stored";
      }
      if (Math.Abs(firstRow.CentreX - secondRow.CentreX) > 6)
      {
        report = firstRow + " / " + secondRow;
        return "the two posters are " + Math.Abs(firstRow.CentreX - secondRow.CentreX)
            + " pixels apart horizontally; one shelf has one column of pictures";
      }
      if (secondRow.CentreY <= firstRow.CentreY)
      {
        report = firstRow + " / " + secondRow;
        return "the third film's poster is not below the first film's, so the rows are not the rows the server sent";
      }

      int pitch = secondRow.CentreY - firstRow.CentreY;
      int brokenRow = firstRow.CentreY + 2 * pitch;
      if (brokenRow >= height)
      {
        report = firstRow + " / " + secondRow;
        return "the row whose stored bytes are not a picture is below the frame at y " + brokenRow;
      }

      bool boxShows = IsColour(pixels, rowBytes, redOffset, blueOffset, firstRow.CentreX, brokenRow,
          PLACEHOLDER_BOX, 10);
      bool posterThere = IsColour(pixels, rowBytes, redOffset, blueOffset, firstRow.CentreX, brokenRow,
          POSTER_PINK, 24)
          || IsColour(pixels, rowBytes, redOffset, blueOffset, firstRow.CentreX, brokenRow, POSTER_TEAL, 24);
      if (posterThere)
      {
        report = firstRow + " / " + secondRow;
        return "a poster was drawn on the row whose stored bytes are not a picture, which means the shelf put "
            + "another title's picture there";
      }
      if (!boxShows)
      {
        int at = brokenRow * rowBytes + firstRow.CentreX * 4;
        report = firstRow + " / " + secondRow;
        return "the picture square of the row with the unreadable bytes is rgb(" + pixels[at + redOffset] + ","
            + pixels[at + 1] + "," + pixels[at + blueOffset] + ") rather than the box behind it, rgb("
            + string.Join(",", PLACEHOLDER_BOX) + ")";
      }

      report = "row at y " + firstRow.Top + " draws the poster of the first film (" + firstRow.Pixels
          + " px), the row at y " + secondRow.Top + " the third film's own (" + secondRow.Pixels
          + " px), the row at y " + brokenRow + " shows the box because its stored bytes are not a picture";
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
