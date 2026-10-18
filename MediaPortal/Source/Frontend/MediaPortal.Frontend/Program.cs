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
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
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
        "                   (otherwise MP2_SERVER, otherwise " + ServerApiClient.DefaultUrl + ")\n" +
        "  --check          read the server and the player, print what was found, exit - without a window";

    [STAThread]
    public static int Main(string[] args)
    {
      string? serverUrl = null;
      bool check = false;

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
          case "--help":
          case "-h":
            Console.WriteLine(USAGE);
            return 0;
          default:
            break; // Avalonia has its own arguments; anything unknown is its business.
        }
      }

      if (check)
        return RunCheckAsync(serverUrl).GetAwaiter().GetResult();

      Uri baseUrl;
      try
      {
        baseUrl = ServerApiClient.ResolveBaseUrl(serverUrl);
      }
      catch (ArgumentException ex)
      {
        Console.WriteLine(ex.Message);
        return 2;
      }

      // The client lives as long as the application; it is not disposed on purpose, the process outlives its
      // sockets and there is nothing to flush.
      ServerApiClient client = new ServerApiClient(baseUrl);

      // One player for the whole application, built here so that the reason for its absence is printed where a
      // human reads it. A machine without libVLC still gets a front end - the library browses, the playback
      // screen is simply not offered - because not being able to play a film is not a reason to refuse to show
      // one's music collection.
      LibVlcPlayer? player;
      string playerReason;
      bool havePlayer = LibVlcPlayer.TryCreate(out player, out playerReason);
      Console.WriteLine(havePlayer ? "player:   " + player!.EngineDescription : "player:   none - " + playerReason);

      AppBuilder app = BuildAvaloniaApp(() => new App(new HomeViewModel(client), player));
      return app.StartWithClassicDesktopLifetime(args);
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
          .LogToTrace();
    }

    /// <summary>
    /// Asks the server the same questions the home screen asks and prints the answers. No window, no display,
    /// no platform-specific code - which is what makes it usable as a gate: if this prints a library, the API,
    /// the HTTP layer, the JSON and the front end's reading of it all work on this machine.
    /// </summary>
    private static async Task<int> RunCheckAsync(string? serverUrl)
    {
      Uri baseUrl;
      try
      {
        baseUrl = ServerApiClient.ResolveBaseUrl(serverUrl);
      }
      catch (ArgumentException ex)
      {
        Console.WriteLine(ex.Message);
        return 2;
      }

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
