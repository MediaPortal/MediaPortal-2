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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using Fluid.Avalonia.Acrylic;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Caching;
using MediaPortal.Frontend.Discovery;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend
{
  /// <summary>
  /// Entry point.
  /// </summary>
  public static class Program
  {
    private const string USAGE =
        "MediaPortal.Frontend [--server <url>] [--check]\n" +
        "  --server <url>   the server to talk to, e.g. http://192.168.1.5:55555/MediaPortal/Server\n" +
        "                   (otherwise MP2_SERVER, otherwise the address saved on the \"which server?\" screen,\n" +
        "                   otherwise " + ServerApiClient.DefaultUrl + ")\n" +
        "  --check          read the server and the player, print what was found, exit - without a window\n" +
        "  --discover [sec] ask the network which MediaPortal servers are there, print them, exit.\n" +
        "                   Listens for <sec> seconds (default 3); the exit code is 0 only if a server answered.\n" +
        "  --play <file> [sec]\n" +
        "                   open the playback screen and play that file - the same screen and the same transport\n" +
        "                   the library opens, so a film can be watched before any library exists.\n" +
        "                   With <sec>, leave after that many seconds and print how many frames the screen drew;\n" +
        "                   the exit code is 0 only if at least one picture arrived.\n" +
        "  --start <time>   with --play, begin at that point of the film: seconds, mm:ss or hh:mm:ss. Worth\n" +
        "                   having because films open dark - a black title card says nothing about whether the\n" +
        "                   picture works, and asking for a scene that has one is the only honest test.\n" +
        "  --shot <file>  with --play, save a photograph of what the screen itself drew to that PNG before\n" +
        "                   leaving. Counters can be wrong about a picture; this is the picture.\n" +
        "  --theme <name> wear one of the three skins the front end knows: " + FrontendTheme.DEFAULT_NAME
            + ", nereus or wmc\n" +
        "                   (otherwise MP2_THEME, otherwise "
            + FrontendTheme.DEFAULT_NAME + ").\n" +
        "                   An unknown name is said out loud and the colours you always had are used.\n" +
        "\n" +
        "Which skin files are worn: the build lays its skins beside the executable and the front end wears that\n" +
        "folder, so what is on disk beside it is always the skin this build was made from and an edit that was\n" +
        "built is an edit that is seen. A machine that keeps its skins elsewhere says so with MP2_SKINS_DIR=\n" +
        "<folder>; those files are then worn instead of the build's, and the start-up log says which of the two\n" +
        "copies is on the screen. A named folder that is not there is said out loud rather than quietly skipped -\n" +
        "'nobody named a folder' and 'the folder you named is missing' are different answers, and only one of\n" +
        "them explains why an edit cannot be seen.";


    [STAThread]
    public static int Main(string[] args)
    {
      string? serverUrl = null;
      bool check = false;
      bool discover = false;
      int discoverSeconds = 3;
      string? playFile = null;
      int playSeconds = 0;
      string? shotPath = null;
      string? startAt = null;
      string? theme = null;

      for (int i = 0; i < args.Length; i++)
      {
        switch (args[i])
        {
          case "--server":
            if (i + 1 >= args.Length)
            {
              Console.WriteLine("--server needs a URL.\n" + USAGE);
              return 2;
            }
            serverUrl = args[++i];
            break;
          case "--check":
            check = true;
            break;
          case "--discover":
            discover = true;
            // An optional number of seconds, because a gate wants to say how long it is willing to wait and a
            // wireless network is slower to answer than a cable.
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out int seconds) && seconds > 0)
              discoverSeconds = Math.Min(seconds, 60);
            break;
          case "--play":
            if (i + 1 >= args.Length)
            {
              Console.WriteLine("--play needs a file to play.\n" + USAGE);
              return 2;
            }
            playFile = args[++i];
            // The same optional number of seconds --discover takes, for the same reason: the person who started
            // it may be a script with a deadline.
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out int playFor) && playFor > 0)
              playSeconds = Math.Min(playFor, 3600);
            break;
          case "--start":
            if (i + 1 >= args.Length)
            {
              Console.WriteLine("--start needs a point in the film, like 240, 4:00 or 0:04:00.\n" + USAGE);
              return 2;
            }
            startAt = args[++i];
            break;
          case "--shot":
            if (i + 1 >= args.Length)
            {
              Console.WriteLine("--shot needs a file to write the photograph to.\n" + USAGE);
              return 2;
            }
            shotPath = args[++i];
            break;
          case "--theme":
            if (i + 1 >= args.Length)
            {
              Console.WriteLine("--theme needs a name: " + string.Join(", ", FrontendTheme.Names) + ".\n" + USAGE);
              return 2;
            }
            theme = args[++i];
            break;
          case "--help":
          case "-h":
            Console.WriteLine(USAGE);
            return 0;
          default:
            break; // Avalonia has its own arguments; anything unknown is its business.
        }
      }

      // Read once, for the whole run: what the saved address is, and - if it could not be read - the sentence that
      // says why. A front end that started silently on a stale address after a broken file would be investigated for
      // an hour by somebody who could have been told in one line.
      ServerSettings settings = ServerSettings.Load(null, out string? settingsProblem);

      if (discover)
        return RunDiscoverAsync(discoverSeconds).GetAwaiter().GetResult();

      if (check)
        return RunCheckAsync(serverUrl, settings, settingsProblem).GetAwaiter().GetResult();

      Uri baseUrl;
      try
      {
        baseUrl = ServerApiClient.ResolveBaseUrl(serverUrl, settings.HasUsableUrl ? settings.ServerUrl : null);
      }
      catch (ArgumentException ex)
      {
        Console.WriteLine(ex.Message);
        return 2;
      }

      ReportSettings(settings, settingsProblem);

      // The client lives as long as the application; it is not disposed on purpose, the process outlives its
      // sockets and there is nothing to flush.
      ServerApiClient client = new ServerApiClient(baseUrl);

      // Whoever started the front end with an address meant that address, so the screen that chooses one is not
      // offered: a button whose choice is then ignored is worse than no button.
      bool serverMayBeChanged = string.IsNullOrWhiteSpace(serverUrl)
          && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ServerApiClient.UrlEnvironmentVariable));

      // One player for the whole application, built here so that the reason for its absence is printed where a
      // human reads it. A machine without libVLC still gets a front end - the library browses, the playback
      // screen is simply not offered - because not being able to play a film is not a reason to refuse to show
      // one's music collection.
      LibVlcPlayer? player;
      string playerReason;
      bool havePlayer = LibVlcPlayer.TryCreate(out player, out playerReason);
      Console.WriteLine(havePlayer ? "player:   " + player!.EngineDescription : "player:   none - " + playerReason);

      // --play is checked here, where an answer can still be a sentence on a terminal, rather than in a window
      // that opens to say the same thing. A file that is not there and a machine with no engine are the two ways
      // this request fails, and both are the caller's mistake to fix. A stream address skips the file questions,
      // because the only thing that can be said about it from a terminal is that it is an address.
      if (playFile != null)
      {
        bool isStream = Uri.TryCreate(playFile, UriKind.Absolute, out Uri? playUri)
            && (playUri.Scheme == Uri.UriSchemeHttp || playUri.Scheme == Uri.UriSchemeHttps
                || playUri.Scheme == "rtsp" || playUri.Scheme == "udp");
        if (!isStream)
        {
          string fullPath;
          try
          {
            fullPath = System.IO.Path.GetFullPath(playFile);
          }
          catch (Exception ex)
          {
            Console.WriteLine("play:     that is not a usable path - " + ex.Message);
            return 2;
          }

          if (!System.IO.File.Exists(fullPath))
          {
            Console.WriteLine("play:     there is no file at " + fullPath);
            return 2;
          }
          playFile = fullPath;
        }
        if (!havePlayer)
        {
          Console.WriteLine("play:     this machine has no player to play it with - " + playerReason);
          return 2;
        }
      }

      // --start is turned into a position here, where a bad one can still be a sentence on a terminal rather
      // than a window that silently plays the wrong part of the film. Plain seconds, mm:ss and hh:mm:ss all
      // work, because the three ways a person writes a point in a film are the three they think in.
      TimeSpan playStart = TimeSpan.Zero;
      if (startAt != null && !TryParseStart(startAt, out playStart))
      {
        Console.WriteLine("--start does not look like a point in a film: '" + startAt
            + "'. Use seconds (240), mm:ss (4:00) or hh:mm:ss (0:04:00).\n" + USAGE);
        return 2;
      }

      // The pictures of the library are the heaviest thing this front end moves over the network - a backdrop is
      // the picture lying next to the media, not the 256 pixel thumbnail the database holds - so what a machine
      // keeps of them is answered on the media settings screen. It is read here because this is where an answer
      // can still be printed for a box nobody is looking at.
      PictureCache pictures = new PictureCache();
      Console.WriteLine("pictures: shelf=" + pictures.Folder + " kept=" + (pictures.Enabled ? "on" : "off")
          + " room=" + pictures.Settings.CacheSizeMb + " MB in memory=" + pictures.RememberedPictures
          + " at once=" + pictures.PicturesAtOnce);

      AppBuilder app = BuildAvaloniaApp(() => new App(new HomeViewModel(client, pictures), player, settings, null,
          serverMayBeChanged, serverMayBeChanged && !settings.HasUsableUrl, playFile, playSeconds, shotPath,
          playStart, theme));
      return app.StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Reads a point in a film written by hand: 240, 4:00, 1:02:03.
    /// </summary>
    private static bool TryParseStart(string text, out TimeSpan start)
    {
      start = TimeSpan.Zero;
      if (string.IsNullOrWhiteSpace(text))
        return false;

      if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
          && seconds >= 0)
      {
        start = TimeSpan.FromSeconds(seconds);
        return true;
      }

      if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out TimeSpan parsed) && parsed >= TimeSpan.Zero)
      {
        start = parsed;
        return true;
      }

      return false;
    }

    /// <summary>
    /// Builds the application. Separate from <see cref="Main"/> and taking the app as a factory because the
    /// headless tests configure the same application onto a display-free platform.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp(Func<App> appFactory)
    {
      return AppBuilder.Configure(appFactory)
          .UsePlatformDetect()
          .WithInterFont()
          // The shortcut tiles frost the screen behind them through the acrylic shader (see HomeView); this puts
          // the compositor and Skia settings the glass pipeline is tuned for in place before the first frame.
          .UseAcrylicPerformanceDefaults()
          .LogToTrace();
    }

    /// <summary>
    /// Asks the network which MediaPortal 2 servers are there, and prints what answered. No window, no display.
    /// </summary>
    /// <remarks>
    /// This is the same search the "which server?" screen runs, in front of a person who wants to know why the list
    /// is empty - and in front of a gate, which is the only way to prove that a server running on another machine
    /// really is found rather than merely hoped for. The exit code is about the answer, not about the search: a
    /// network with no server on it is a working search that found nothing, and the caller can read that difference
    /// in the lines above it.
    /// </remarks>
    private static async Task<int> RunDiscoverAsync(int seconds)
    {
      SsdpSearchOptions options = SsdpSearchOptions.ForNetworkSearch(TimeSpan.FromSeconds(seconds));
      Console.WriteLine("search:   " + options.SearchTarget);
      Console.WriteLine("listening: " + options.ListenWindow.TotalSeconds + "s on udp/" + SsdpAnnouncement.SsdpPort);

      SsdpSearchResult result;
      try
      {
        result = await new SsdpSearchClient().SearchAsync(options, CancellationToken.None);
      }
      catch (Exception ex)
      {
        // Not a "nothing found": the search itself could not be run, which is the one case worth an exception on
        // the console, because there is nobody to ask and the sentence on the screen is not being drawn either.
        Console.WriteLine("search:   failed - " + ex.GetType().Name + ": " + ex.Message);
        return 2;
      }

      foreach (string failure in result.SendFailures) Console.WriteLine("send:     " + failure);
      foreach (SsdpAnnouncement announcement in result.Announcements)
      {
        Console.WriteLine("device:   " + announcement.Location
            + (announcement.IsMediaPortalServer ? string.Empty : " (announced something else)"));
      }

      IReadOnlyList<DetectedServer> servers =
          await new ServerLocator().LocateAsync(result.Announcements, CancellationToken.None);
      foreach (DetectedServer server in servers)
        Console.WriteLine("server:   " + server.Name + " " + server.Version + " at " + server.BaseUrl);

      Console.WriteLine("answered: " + result.MessagesReceived + " message(s), " + result.Announcements.Count
          + " device(s), " + servers.Count + " MediaPortal server(s)");
      return servers.Count > 0 ? 0 : 1;
    }

    /// <summary>
    /// Asks the server the same questions the home screen asks and prints the answers. No window, no display,
    /// no platform-specific code - which is what makes it usable as a gate: if this prints a library, the API,
    /// the HTTP layer, the JSON and the front end's reading of it all work on this machine.
    /// </summary>
    private static async Task<int> RunCheckAsync(string? serverUrl, ServerSettings settings, string? settingsProblem)
    {
      Uri baseUrl;
      try
      {
        baseUrl = ServerApiClient.ResolveBaseUrl(serverUrl, settings.HasUsableUrl ? settings.ServerUrl : null);
      }
      catch (ArgumentException ex)
      {
        Console.WriteLine(ex.Message);
        return 2;
      }

      ReportSettings(settings, settingsProblem);
      Console.WriteLine("server:   " + baseUrl);
      ReportPlayer();
      using (ServerApiClient client = new ServerApiClient(baseUrl))
      using (CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
      {
        try
        {
          HealthInfo health = await client.GetHealthAsync(cancellation.Token).ConfigureAwait(false);
          Console.WriteLine("health:   {0} {1} (pathBase={2})", health.Name, health.Version, health.PathBase);

          ServerInfo info = await client.GetServerInfoAsync(cancellation.Token).ConfigureAwait(false);
          Console.WriteLine("server:   " + info.Describe());

          LibraryAspects aspects = await client.GetLibraryAspectsAsync(cancellation.Token).ConfigureAwait(false);
          Console.WriteLine("library:  {0} aspects", aspects.Count);
          if (aspects.Aspects.Count > 0)
            Console.WriteLine("first:    {0} with {1} attributes",
                aspects.Aspects[0].Name, aspects.Aspects[0].Attributes.Count);

          MediaCount all = await client.GetMediaCountAsync(null, cancellation.Token).ConfigureAwait(false);
          Console.WriteLine("items:    {0} media items", all.Count);
        }
        catch (ServerApiException ex)
        {
          Console.WriteLine("FAILED:   " + ex.Message);
          return ex.NotReady ? 3 : 1;
        }
        catch (Exception ex)
        {
          Console.WriteLine("FAILED:   {0}: {1}", ex.GetType().Name, ex.Message);
          return 1;
        }
      }

      Console.WriteLine("the front end read a running server.");
      return 0;
    }

    /// <summary>
    /// Prints where the saved server address lives and what is in it, before anything is asked of a server: when the
    /// front end talks to a machine nobody meant, the first question is always which address it picked and where it
    /// picked it up.
    /// </summary>
    private static void ReportSettings(ServerSettings settings, string? problem)
    {
      string saved = string.IsNullOrWhiteSpace(settings.ServerUrl)
          ? "nothing saved"
          : (settings.HasUsableUrl ? settings.ServerUrl : "'" + settings.ServerUrl + "' is not a usable address");
      Console.WriteLine("settings: {0} ({1})", ServerSettings.DefaultFilePath, saved);
      if (problem != null)
        Console.WriteLine("settings: " + problem);
    }

    /// <summary>
    /// Builds a player the way the application builds it and prints what came of it, then throws it away again.
    /// </summary>
    /// <remarks>
    /// Headless on purpose: <c>--check</c> is meant to work over a plain ssh session with no display, and the
    /// answer to "can this machine play at all" does not depend on a window. A missing player is printed and
    /// changes nothing about the exit code - a media centre without libVLC still browses its library, and the
    /// gate is about the front end working, not about one optional library being installed.
    /// </remarks>
    private static void ReportPlayer()
    {
      LibVlcPlayer? player;
      string reason;
      if (!LibVlcPlayer.TryCreate(out player, out reason, new PlayerOptions { Headless = true }))
      {
        Console.WriteLine("player:   none - " + reason);
        return;
      }

      using (player!)
      {
        Console.WriteLine("player:   " + player!.EngineDescription);
      }
    }
  }
}
