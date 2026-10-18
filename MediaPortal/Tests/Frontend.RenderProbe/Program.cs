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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
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
        "Frontend.RenderProbe --out <file.png> [--screen <home|nowplaying|connect>] [--server <url>] [--width <n>] [--height <n>]\n" +
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
      HomeViewModel? home = null;
      ConnectionViewModel? connection = null;
      string playbackStatus = "";

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

        playbackStatus = playback.StateText + ", " + playback.PositionText;
        window = new NowPlayingWindow(playback);
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
      else
      {
        return Usage("unknown screen '" + screenName + "'; this probe renders home, nowplaying or connect.");
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
        return Array.Empty<PlaybackTrack>();
      }

      public bool SelectTrack(PlaybackTrack track)
      {
        return false;
      }

      public bool AttachTo(nint videoTarget)
      {
        return videoTarget != 0;
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
