using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MediaPortal.Frontend.Services
{
  /// <summary>
  /// Turns the page of a video site into the address a player can actually open.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A trailer tagged by a metadata tool names a <i>page</i> - <c>https://www.youtube.com/watch?v=bwzLiQZDw2I</c> -
  /// and a page is not a film. libVLC used to know that page: VLC 3.0 shipped a <c>youtube.lua</c> playlist script
  /// that read it, and the installed 3.0.20 still carries one, from October 2023, which answers
  /// "Couldn't extract youtube video URL, please check for updates to this script" and then nothing at all. VLC
  /// 3.0.23 removed that script, and VLC 4 stopped resolving sites itself as well - it asks an external
  /// <c>yt-dlp</c> for the film behind the page and opens what comes back. This class is that asking, kept out of
  /// the engine so that the engine stays the 3.0.20 this front end's picture path was measured against.
  /// </para>
  /// <para>
  /// What comes back is the film's own address, and it is <b>short lived</b> - measured: the address a resolution
  /// hands over carries an <c>expire</c> of a few hours and is bound to the machine that asked for it. Nothing here
  /// keeps one, and nothing writes one into the library: it is a lookup for the next open, not metadata.
  /// </para>
  /// <para>
  /// What the viewer is offered is a table of sizes, not one guess: see <see cref="VideoSiteStream.Qualities"/>.
  /// Every size names the player client it was measured with and the shape asked of the tool, because the two
  /// belong together - the <c>tv</c> client's address answered HTTP 403 to this machine's VLC 3.0.20 while
  /// <c>android</c>'s answered HTTP 206 and played, and only <c>web_embedded</c> is offered the big shapes at all.
  /// </para>
  /// </remarks>
  /// <summary>
  /// A picture size a viewer can stand at the trailer's <i>which size</i> line, and what standing there asks of
  /// the tool. Its own type of its own because it travels: the line carries it, the ask carries it, and the
  /// resolver reads it back - see <see cref="VideoSiteStream.Qualities"/> for the measured table.
  /// </summary>
  /// <param name="Word">What the viewer reads, before the waiting is said.</param>
  /// <param name="PlayerClient">Which player the resolver has to ask to be offered this size at all.</param>
  /// <param name="Format">The shape asked of the tool, which falls back to the biggest single file.</param>
  /// <param name="Muxed">
  /// Whether the size has to be brought to this machine and put together first, because no single address carries
  /// picture and sound at it. False means the answer is an address the player is handed as it stands.
  /// </param>
  /// <param name="PatienceSeconds">
  /// How long the tool may take for this size before it is called off, or nought where the size needs no more than
  /// a plain lookup. Never shown to anybody.
  /// </param>
  /// <remarks>
  /// One address for the smallest and two for the rest is not a shortcut - it is what the site answers. The table
  /// is the measured one, from the format list the installed tool prints for a two-minute trailer: the
  /// <c>android</c> client answers <b>one</b> playable shape (<c>18</c>, 640x266, picture and sound together,
  /// 4.77 MiB) and nothing else but storyboards, <c>web</c>, <c>tv</c>, <c>ios</c> and <c>web_safari</c> answer
  /// nothing playable at all, and only <c>web_embedded</c> and <c>tv_embedded</c> answer more - where every
  /// shape above 360p is either <c>video only</c> plus its own sound track (so two addresses, which nobody can
  /// hand a media player as one) or an <c>m3u8</c> manifest, which this engine cannot open. So a bigger picture
  /// has to be <b>brought here first</b> and put together, which costs time: the
  /// 720p pair of that trailer is 10 MiB and took 21 s from this machine, and 1080p stood at 82 MiB in the list.
  /// </remarks>
  public sealed record TrailerQuality(string Word, string PlayerClient, string Format, bool Muxed, int PatienceSeconds)
  {
    /// <summary>
    /// What the viewer reads on the line: the size, and nothing else. Whether the film has to be brought over
    /// first is the machine's business and is said by the dialog that waits, not by a word on a choice.
    /// </summary>
    public string Display => Word;
  }

  internal static class VideoSiteStream
  {
    /// <summary>
    /// How long the resolver is allowed to think, in milliseconds. Measured with the installed yt-dlp against a
    /// YouTube page: 1.2 to 2.4 s from a warm machine, and a page that will not answer is not worth holding a
    /// viewer for longer than this.
    /// </summary>
    internal const int ResolveTimeoutMs = 20000;

    /// <summary>
    /// The format asked for: one stream holding picture and sound, <b>over plain http(s)</b>.
    /// </summary>
    /// <remarks>
    /// <c>proto=https</c> is no decoration. Measured against the same trailer: the clients this front end falls
    /// back to (<c>web_embedded</c>, <c>tv_embedded</c>) also offer the picture-and-sound shapes 91 to 96 as
    /// <c>m3u8</c> - and the 1080p one of those was played through this front end's own engine in a headless run
    /// and answered <c>adaptive demux error: Failed to create demuxer</c>, standing at 0 ms. A manifest is
    /// several addresses wearing one name, and the player is handed one address.
    /// </remarks>
    internal const string WantedFormat = "b[protocol=https][acodec!=none][vcodec!=none]";

    /// <summary>
    /// The qualities the <i>which quality</i> question offers, small first, each named by nothing but its size.
    /// Nothing here promises a height the site has not got: every selector falls back to the biggest single file
    /// when the shape above it is missing, so a line can name a size it cannot exceed without claiming to reach it.
    /// </summary>
    /// <remarks>
    /// The player client belongs to the quality, not to the lookup: measured on one trailer, <c>android</c>
    /// answers only the 360p single file, while the shapes above it only appear at all when <c>web_embedded</c>
    /// is asked - and that one is also the client whose answer this engine can actually open.
    /// </remarks>
    internal static readonly TrailerQuality[] Qualities =
    {
      new TrailerQuality("360p", "android", WantedFormat, false, 0),
      new TrailerQuality("720p", "web_embedded",
          "bv*[protocol=https][height<=720][acodec=none]+ba[protocol=https]/" + WantedFormat, true, 90),
      new TrailerQuality("1080p", "web_embedded",
          "bv*[protocol=https][height<=1080][acodec=none]+ba[protocol=https]/" + WantedFormat, true, 300)
    };

    /// <summary>
    /// The tool's own command line for one quality of one page, either the address of a film or the film itself.
    /// </summary>
    /// <param name="page">The page as the library holds it.</param>
    /// <param name="quality">The quality that was stood on.</param>
    /// <param name="file">Where the film is to be written, when the quality is one that has to be brought here.</param>
    internal static string[] BuildArguments(string page, TrailerQuality quality, string? file)
    {
      List<string> arguments = new List<string>(BuildArguments(page, quality.PlayerClient));
      // The quality's own shape, in the place the shared command line has for it.
      arguments[arguments.IndexOf("-f") + 1] = quality.Format;
      if (file != null)
      {
        // The shared line only asks for the address, and a tool that answers an address goes home without ever
        // fetching the film: measured with the installed yt-dlp on the exact line this class builds, it printed
        // the googlevideo address, answered exit code nought and wrote no file - which is what the viewer met as
        // "the resolver said it was done, but trailer-<film>.mp4 is not there". So the question is taken out of
        // the line before the answer is asked of it.
        arguments.RemoveAt(arguments.IndexOf("--get-url"));

        arguments.Add("-o");
        arguments.Add(file);
        arguments.Add("--no-part");

        // The file the caller looks for is the one it named, mp4 in it, so the container is said rather than
        // left to the pieces: measured on a 720p trailer, the web_embedded client hands the picture as mp4 (f398)
        // but the sound as opus in webm (f251), and opus does not live in an mp4 - left to itself the tool moves
        // such a pair to another container and so to another name. Asked plainly, the merge wrote
        // "trailer-<film>.mp4", 7 905 417 bytes, at the name this class had given it.
        arguments.Add("--merge-output-format");
        arguments.Add("mp4");
      }

      return arguments.ToArray();
    }

    /// <summary>
    /// The address of the film behind a site's page, in the quality that was stood on - or the one sentence
    /// saying why there is none. Only for a quality that lives in one address.
    /// </summary>
    public static bool TryResolve(string page, TrailerQuality quality, out string stream, out string reason,
        string? tool = null)
    {
      stream = string.Empty;
      reason = string.Empty;

      string? binary = tool ?? FindTool();
      if (binary == null)
        return NoTool(page, out reason);

      if (!Ask(binary, BuildArguments(page, quality, null), ResolveTimeoutMs, out string address,
          out string complaint))
      {
        reason = "nothing could be found behind " + Name(page)
            + (complaint.Length == 0 ? " - the resolver had no answer." : ": " + complaint);
        return false;
      }

      stream = address;
      return true;
    }

    /// <summary>
    /// Brings the film behind a site's page to this machine and puts its picture and sound into one file, for a
    /// quality that no single address carries. This is the slow road and it says so: the wait is the download.
    /// </summary>
    /// <param name="page">The page as the library holds it.</param>
    /// <param name="quality">The quality that was stood on; one of the muxed ones.</param>
    /// <param name="file">The file to write, which the caller owns and deletes.</param>
    /// <param name="reason">Why nothing came.</param>
    /// <param name="tool">The resolver to ask; <c>null</c> looks it up.</param>
    /// <returns>True with the file written.</returns>
    public static bool TryFetch(string page, TrailerQuality quality, string file, out string reason,
        string? tool = null)
    {
      reason = string.Empty;

      string? binary = tool ?? FindTool();
      if (binary == null)
        return NoTool(page, out reason);

      if (QualityTool("ffmpeg") == null)
      {
        reason = "this machine has no ffmpeg, so a trailer in " + quality.Word
            + " cannot be put together from its pieces.";
        return false;
      }

      if (!Ask(binary, BuildArguments(page, quality, file), quality.PatienceSeconds * 1000, out string answer,
          out string complaint))
      {
        reason = quality.Word + " could not be brought over from " + Name(page)
            + (complaint.Length == 0 ? "." : ": " + complaint);
        return false;
      }

      if (!File.Exists(file) || new FileInfo(file).Length == 0)
      {
        reason = "the resolver said it was done, but " + Path.GetFileName(file) + " is not there.";
        return false;
      }

      return true;
    }

    /// <summary>
    /// The film's address in the quality that was stood on, whichever road that quality takes.
    /// </summary>
    /// <param name="page">The page as the library holds it.</param>
    /// <param name="quality">The quality that was stood on; <c>null</c> asks for the smallest single file.</param>
    /// <param name="file">The file the film was brought to, when the quality needed that; else an empty string.</param>
    /// <param name="reason">Why there is nothing.</param>
    /// <param name="tool">The resolver to ask; <c>null</c> looks it up.</param>
    /// <returns>The address to hand the player - a site's address or a local file.</returns>
    public static bool TryPlayAddress(string page, TrailerQuality? quality, out string address, out string file,
        out string reason, string? tool = null)
    {
      file = string.Empty;
      address = string.Empty;

      TrailerQuality asked = quality ?? Qualities[0];
      if (!asked.Muxed)
      {
        if (!TryResolve(page, asked, out address, out reason, tool))
          return false;
        return true;
      }

      string to = TrailerFileOf(page);
      if (!TryFetch(page, asked, to, out reason, tool))
        return false;

      file = to;
      address = to;
      return true;
    }

    /// <summary>
    /// The file a brought-over trailer is written to: one name per film, under this machine's temporary folder.
    /// The caller deletes it when the trailer is through; a machine that was pulled out of a trailer mid-flight
    /// is cleaned by <see cref="ForgetOldTrailers"/> on the next start.
    /// </summary>
    public static string TrailerFileOf(string page)
    {
      string id = VideoIdOf(page) ?? Math.Abs(page.GetHashCode()).ToString("x");
      foreach (char c in Path.GetInvalidFileNameChars())
        id = id.Replace(c.ToString(), "");

      return Path.Combine(Path.GetTempPath(), "MediaPortal", "trailer-" + id + ".mp4");
    }

    /// <summary>
    /// Throws away the films a previous run brought over and never needed again - older than a day, in this
    /// front end's own corner of the temporary folder, and nothing else.
    /// </summary>
    /// <returns>How many files went.</returns>
    public static int ForgetOldTrailers()
    {
      string folder = Path.Combine(Path.GetTempPath(), "MediaPortal");
      if (!Directory.Exists(folder))
        return 0;

      int gone = 0;
      foreach (string file in Directory.GetFiles(folder, "trailer-*.mp4"))
      {
        try
        {
          if (DateTime.UtcNow - File.GetLastWriteTimeUtc(file) > TimeSpan.FromDays(1))
          {
            File.Delete(file);
            gone++;
          }
        }
        catch (IOException)
        {
          // Still playing somewhere, or gone already. Neither is a reason to stop.
        }
      }

      return gone;
    }

    private static bool NoTool(string page, out string reason)
    {
      reason = "this machine has no yt-dlp, so the film behind " + Name(page) + " cannot be looked up.";
      return false;
    }

    /// <summary>
    /// Where a tool the resolver needs besides itself stands - today only ffmpeg, which puts the two pieces of a
    /// big picture back together. The folder yt-dlp was installed into is looked at first, because that is the
    /// folder its own installer puts ffmpeg in on this machine.
    /// </summary>
    internal static string? QualityTool(string name)
    {
      foreach (string candidate in new[]
      {
        Path.Combine(Path.GetDirectoryName(FindTool() ?? "") ?? "", name + ".exe"),
        name + ".exe"
      })
      {
        if (candidate.Length > 0 && File.Exists(candidate))
          return candidate;
      }

      return null;
    }



    /// <summary>
    /// The tool's own command line for one page and one player client.
    /// </summary>
    /// <remarks>
    /// <c>--no-warnings</c> and <c>--no-progress</c> because the answer is read from standard output, where a
    /// warning would be read as an address. <c>--no-playlist</c> because a trailer page can be the first entry of a
    /// playlist, and a playlist is several answers where one was asked for.
    /// </remarks>
    internal static string[] BuildArguments(string page, string playerClient)
    {
      return new[]
      {
        "--no-warnings",
        "--no-progress",
        "--no-playlist",
        "--extractor-args", "youtube:player_client=" + playerClient,
        "-f", WantedFormat,
        "--get-url",
        page
      };
    }

    /// <summary>
    /// Whether this address names a page at a site whose films this resolver can look up.
    /// </summary>
    /// <remarks>
    /// A file, a server's own address, or a direct film address is not a page and is answered <c>false</c> -
    /// nothing here is asked of it, and a trailer at a site this front end does not resolve is handed to the player
    /// as it stands so that the engine says in its own words what it made of it.
    /// </remarks>
    internal static bool IsSitePage(string? address)
    {
      if (string.IsNullOrWhiteSpace(address) || address.StartsWith("plugin://", StringComparison.Ordinal))
        return false;
      if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? page))
        return false;
      if (!page.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
          && !page.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        return false;

      string host = page.Host.ToLowerInvariant();
      if (host.StartsWith("www.", StringComparison.Ordinal))
        host = host.Substring(4);
      if (host.StartsWith("m.", StringComparison.Ordinal))
        host = host.Substring(2);

      // A channel, a search page or an uploads page names no film, and a resolver would answer with whatever the
      // page happens to lead to. Only an address that carries a video id names one.
      if (host.Equals("youtu.be", StringComparison.Ordinal))
        return page.AbsolutePath.Trim('/').Length > 0;

      if (!host.Equals("youtube.com", StringComparison.Ordinal))
        return false;

      return VideoIdOf(address) != null
          || page.AbsolutePath.StartsWith("/shorts/", StringComparison.Ordinal);
    }

    /// <summary>
    /// The film an address of that site names, read off <c>?v=</c>, <c>/shorts/</c> or <c>youtu.be/</c>, or
    /// <c>null</c> when it names none.
    /// </summary>
    /// <remarks>
    /// Read by hand rather than with a query parser: the id is the one thing a site's address always spells the
    /// same way, and this way nothing outside the base framework is needed to read it.
    /// </remarks>
    internal static string? VideoIdOf(string? address)
    {
      if (string.IsNullOrWhiteSpace(address) || !Uri.TryCreate(address, UriKind.Absolute, out Uri? page))
        return null;

      string host = page.Host.ToLowerInvariant();
      if (host.StartsWith("www.", StringComparison.Ordinal))
        host = host.Substring(4);
      if (host.StartsWith("m.", StringComparison.Ordinal))
        host = host.Substring(2);

      if (host.Equals("youtu.be", StringComparison.Ordinal))
      {
        string shortId = page.AbsolutePath.Trim('/').Split('/')[0];
        return shortId.Length > 0 ? shortId : null;
      }

      if (!host.Equals("youtube.com", StringComparison.Ordinal))
        return null;

      string shorts = "/shorts/";
      int at = page.AbsolutePath.IndexOf(shorts, StringComparison.Ordinal);
      if (at >= 0)
      {
        string tail = page.AbsolutePath.Substring(at + shorts.Length).Split('/')[0];
        return tail.Length > 0 ? tail : null;
      }

      foreach (string part in page.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
      {
        string[] pair = part.Split('=', 2);
        if (pair.Length == 2 && pair[0].Equals("v", StringComparison.OrdinalIgnoreCase) && pair[1].Length > 0)
          return Uri.UnescapeDataString(pair[1]);
      }

      return null;
    }

    /// <summary>
    /// Where the tool stands, or <c>null</c> when this machine has none.
    /// </summary>
    /// <remarks>
    /// <c>MP2_YTDLP</c> first - a machine that keeps the tool somewhere of its own should not have to change its
    /// PATH. Then the names the installers leave on the PATH, then the folders the installers write the tool into
    /// without promising anything about the PATH: winget's link folder and pip's user script folder. Nothing is
    /// downloaded and nothing is installed by this front end.
    /// </remarks>
    internal static string? FindTool()
    {
      string fromEnvironment = (Environment.GetEnvironmentVariable("MP2_YTDLP") ?? string.Empty).Trim().Trim('"');
      if (fromEnvironment.Length > 0 && File.Exists(fromEnvironment))
        return fromEnvironment;

      string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      foreach (string candidate in new[]
      {
        Path.Combine(local, "Microsoft", "WinGet", "Links", "yt-dlp.exe"),
        Path.Combine(roaming, "Python", "Scripts", "yt-dlp.exe"),
        Path.Combine(local, "Programs", "Python", "Scripts", "yt-dlp.exe")
      })
      {
        if (File.Exists(candidate))
          return candidate;
      }

      // On the PATH itself, which is where winget and pip leave it more often than not. Checked the way the
      // operating system checks it, so that a tool in the current folder is not picked up as a side effect.
      foreach (string name in OperatingSystem.IsWindows()
                   ? new[] { "yt-dlp.exe", "yt-dlp.cmd" }
                   : new[] { "yt-dlp" })
      {
        foreach (string folder in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
          try
          {
            string full = Path.Combine(folder.Trim(), name);
            if (File.Exists(full))
              return full;
          }
          catch (ArgumentException)
          {
            // A PATH entry that is no path at all, which a machine that has been edited by hand often has.
          }
        }
      }

      return null;
    }

    /// <summary>
    /// One question to the tool, with the arguments this class built for it.
    /// </summary>
    /// <param name="patienceMs">
    /// How long the tool is given. A lookup is done in a couple of seconds, while a size that has to be brought
    /// over is a download and is given the minutes its quality asks for.
    /// </param>
    /// <remarks>
    /// Both answers are read <b>as they arrive</b> rather than by emptying standard output first: a download writes
    /// nothing until it is finished, so reading it to the end would sit there for as long as the tool likes and
    /// leave no room for the patience above to be kept.
    /// </remarks>
    private static bool Ask(string binary, string[] arguments, int patienceMs, out string address,
        out string complaint)
    {
      address = string.Empty;
      complaint = string.Empty;

      try
      {
        using Process process = new Process();
        process.StartInfo = new ProcessStartInfo(binary, QuoteAll(arguments))
        {
          UseShellExecute = false,
          CreateNoWindow = true,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          StandardOutputEncoding = Encoding.UTF8,
          StandardErrorEncoding = Encoding.UTF8
        };

        StringBuilder errors = new StringBuilder();
        process.ErrorDataReceived += (s, e) =>
        {
          if (e.Data != null && errors.Length < 400)
            errors.AppendLine(e.Data);
        };

        StringBuilder answers = new StringBuilder();
        process.OutputDataReceived += (s, e) =>
        {
          if (e.Data != null && answers.Length < 65536)
            answers.AppendLine(e.Data);
        };

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        if (!process.WaitForExit(patienceMs))
        {
          try
          {
            process.Kill(entireProcessTree: true);
          }
          catch (Exception)
          {
            // It was already leaving.
          }

          complaint = "the resolver took longer than " + (patienceMs / 1000) + " s.";
          return false;
        }

        // Waits for the two readers above to have seen everything the tool wrote.
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
          complaint = ShortComplaint(errors.ToString(), process.ExitCode);
          return false;
        }

        // A command that was asked to bring a film here answers with the film, not with a sentence, and it is
        // asked to be quiet while it works: the caller looks for the file it named. Only the lookup road asks for
        // an address, so only there is a line reading as one owed. Measured on the fetch road, once the film was
        // really being fetched: it stood at the named file (4 074 767 bytes in 6 s) while this method went on
        // hunting an address in the quiet output and answered "the resolver answered nothing that reads as an
        // address", which the viewer then read as a failure.
        if (Array.IndexOf(arguments, "--get-url") < 0)
          return true;

        // The address is the first line that reads as one. The tool is asked to be quiet, but a version that
        // decides to say something first must not turn that sentence into a media address.
        foreach (string line in answers.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
          string candidate = line.Trim();
          if (candidate.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
              || candidate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
          {
            address = candidate;
            return true;
          }
        }

        complaint = "the resolver answered nothing that reads as an address.";
        return false;
      }
      catch (Exception ex)
      {
        complaint = "the resolver could not be run: " + ex.Message;
        return false;
      }
    }

    /// <summary>
    /// The tool's own words, shortened to what a screen can hold.
    /// </summary>
    /// <summary>
    /// The tool's own words, shortened to what a screen can hold.
    /// </summary>
    /// <remarks>
    /// Asked case-insensitively on purpose: the tool writes its complaints as <c>ERROR: â€¦</c> but its command line
    /// errors as <c>yt-dlp.exe: error: no such option: â€¦</c>, and both are worth showing. Measured: an option the
    /// tool does not know ends with exit code 2 and no capital <c>ERROR</c> anywhere, so a case-sensitive search
    /// left the viewer with "exit code 2" where the tool had said exactly what was wrong.
    /// </remarks>
    internal static string ShortComplaint(string errors, int exitCode)
    {
      foreach (string line in errors.Split('\n', StringSplitOptions.RemoveEmptyEntries))
      {
        string candidate = line.Trim();
        if (candidate.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
        {
          // Its own program name in front of a usage error tells the viewer nothing and eats half a line.
          int at = candidate.IndexOf("error:", StringComparison.OrdinalIgnoreCase);
          if (at > 0)
            candidate = candidate.Substring(at);
          return candidate.Length > 200 ? candidate.Substring(0, 200) + "â€¦" : candidate;
        }
      }

      return "the resolver answered with exit code " + exitCode + ".";
    }

    /// <summary>
    /// The page as a viewer would name it: the video id rather than a two hundred character address.
    /// </summary>
    internal static string Name(string page)
    {
      string? id = VideoIdOf(page);
      return id != null ? "the film " + id : "that address";
    }

    /// <summary>
    /// One command line out of the argument list, quoting what needs it.
    /// </summary>
    private static string QuoteAll(string[] arguments)
    {
      StringBuilder line = new StringBuilder();
      foreach (string argument in arguments)
      {
        if (line.Length > 0)
          line.Append(' ');
        if (argument.Contains(' ') || argument.Contains('"'))
          line.Append('"').Append(argument.Replace("\"", "\\\"")).Append('"');
        else
          line.Append(argument);
      }

      return line.ToString();
    }
  }
}
