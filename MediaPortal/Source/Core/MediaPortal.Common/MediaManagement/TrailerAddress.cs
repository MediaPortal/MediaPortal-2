#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Turns the address a trailer was tagged with into something the player can open.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Trailer addresses are written by tagging tools for those tools' own use, so the library meets four shapes
  /// where one was expected. A film tagged with the reference tool's own format names its web trailer as
  /// <c>plugin://plugin.video.youtube/play/?video_id=VWqJifMMgZE</c>, an older version of the same tool as
  /// <c>plugin://plugin.video.youtube/?action=play_video&amp;videoid=VWqJifMMgZE</c>, hand written files
  /// sometimes as <c>http://www.youtube.com/watch?v=VWqJifMMgZE&amp;hd=1</c>, and a trailer bought from a
  /// trailer site as <c>plugin://plugin.video.hdtrailers_net/video/apple.com/…/file.mov</c> with the real
  /// address hidden in the last part and percent encoded. Of those, only the plain web addresses and the
  /// trailer site's file are something a player opens as they stand.
  /// </para>
  /// <para>
  /// Everything here is deliberately forgiving: a trailer nobody can name must not stop an import, and a
  /// trailer whose address makes no sense is reported as such and shown as it was written.
  /// </para>
  /// </remarks>
  public static class TrailerAddress
  {
    /// <summary>
    /// Hosts whose films the player reaches through their watch page rather than through a file.
    /// </summary>
    private static readonly string[] VIDEO_SITE_HOSTS = { "youtube.com", "youtu.be" };

    /// <summary>
    /// Sites that used to hand out trailer films and now send everybody off to their streaming front page.
    /// </summary>
    /// <remarks>
    /// Measured on this machine with its own HTTP client: an Apple trailer file address answers
    /// <c>301</c> pointing at <c>https://tv.apple.com/</c> and carries <c>content-type: text/html</c> in the
    /// reply, both for <c>trailers.apple.com</c> and for <c>movietrailers.apple.com</c>. A trailer is asked
    /// for its length before anything is played, so such an address does not just fail to play - nothing
    /// starts at all. That is why these links are not imported and are thrown out of a library that holds
    /// them already.
    /// </remarks>
    private static readonly string[] DEAD_SITE_HOSTS = { "apple.com" };

    /// <summary>
    /// The sites that answer no more, for whoever wants to ask a database about them in one stroke instead of
    /// looking at every address on its own.
    /// </summary>
    public static IEnumerable<string> DeadSiteHosts
    {
      get { return DEAD_SITE_HOSTS; }
    }

    /// <summary>
    /// Tells whether a trailer stands at a site that answers no more.
    /// </summary>
    /// <param name="raw">The address as the metadata file wrote it, or as the library stored it.</param>
    /// <remarks>
    /// All three shapes a dead Apple link is met in are recognised: the plain
    /// <c>https://movietrailers.apple.com/movies/…/movie-hd.mov</c>, the older
    /// <c>http://trailers.apple.com/trailers/…</c>, and a trailer bought from a trailer site as
    /// <c>plugin://plugin.video.hdtrailers_net/video/apple.com/…</c> with the Apple address hidden behind
    /// the plugin. A look-alike host such as <c>pineapple.com</c> is not caught, because the character in
    /// front of the host has to be a separator.
    /// </remarks>
    public static bool IsDeadSite(string raw)
    {
      string text = Clean(raw);
      if (text.Length == 0)
        return false;
      foreach (string host in DEAD_SITE_HOSTS)
        if (CarriesHost(text, host))
          return true;
      return false;
    }

    /// <summary>
    /// Says what sort of address <paramref name="raw"/> is, without deciding whether it can be played.
    /// </summary>
    /// <returns>One of the <c>TrailerAspect.KIND_</c> values.</returns>
    public static int DetectKind(string raw)
    {
      string text = Clean(raw);
      if (text.Length == 0)
        return TrailerAspect.KIND_OTHER;
      string lower = text.ToLowerInvariant();
      if (lower.StartsWith("plugin://", StringComparison.Ordinal))
      {
        // The plugin format names a video site by the plugin's own name, so the name is the only clue there is.
        return lower.Contains("youtube") ? TrailerAspect.KIND_YOUTUBE :
            (HideBehindPlugin(text) != null ? TrailerAspect.KIND_STREAM : TrailerAspect.KIND_OTHER);
      }
      if (IsWebAddress(lower))
        return IsVideoSiteAddress(lower) ? TrailerAspect.KIND_YOUTUBE : TrailerAspect.KIND_STREAM;
      return TrailerAspect.KIND_FILE;
    }

    /// <summary>
    /// Names the address of the trailer for the player.
    /// </summary>
    /// <param name="raw">The address as the metadata file wrote it.</param>
    /// <param name="kind">The <c>TrailerAspect.KIND_</c> value stored with the trailer.</param>
    /// <returns>An address the player understands, or <c>null</c> when nothing can be made of it - which is
    /// reported to the screen rather than dropped, because the trailer does exist and only its address is
    /// beyond this front end.</returns>
    /// <remarks>
    /// A file returns <c>null</c> on purpose: the path means nothing away from the machine holding it, and the
    /// screen reaches such a trailer through the server's own trailer address instead.
    /// </remarks>
    public static string ToPlayable(string raw, int kind)
    {
      string text = Clean(raw);
      if (text.Length == 0)
        return null;
      string lower = text.ToLowerInvariant();
      if (lower.StartsWith("plugin://", StringComparison.Ordinal))
      {
        string videoId = YouTubeId(text);
        if (videoId != null)
          return "https://www.youtube.com/watch?v=" + videoId;
        return HideBehindPlugin(text);
      }
      if (IsWebAddress(lower))
      {
        string videoId = IsVideoSiteAddress(lower) ? YouTubeId(text) : null;
        return videoId != null ? "https://www.youtube.com/watch?v=" + videoId : text;
      }
      return null; // A file. The caller names the server's address for it.
    }

    /// <summary>
    /// Shortens an address to what a screen can show as the trailer's own name, so that a file called
    /// <c>...\Film\Film-trailer.mp4</c> reads as "Film-trailer".
    /// </summary>
    /// <param name="raw">The address as the metadata file wrote it.</param>
    /// <param name="kind">Its kind, which decides whether the address names a film at all.</param>
    /// <remarks>
    /// Only a trailer the library holds as a file gets a name out of its address, because its file name is the
    /// one thing somebody chose: whoever left <c>alien-trailer.mp4</c> in the folder named it. A web address
    /// names nothing a person chose - <c>lincolnlawyer-tlr2_h720p.mov</c> is a folder on a company's server, and
    /// a video site's address does not even name that - so nothing comes back, and a screen calls the trailer by
    /// what it is instead of spelling out an address nobody can read off across a room.
    /// </remarks>
    public static string ToLabel(string raw, int kind)
    {
      if (kind != TrailerAspect.KIND_FILE)
        return null;

      string text = Clean(raw);
      if (text.Length == 0)
        return null;
      string tail = text.TrimEnd('/');
      int slash = tail.LastIndexOfAny(new[] { '/', '\\' });
      if (slash >= 0 && slash < tail.Length - 1)
        tail = tail.Substring(slash + 1);
      int question = tail.IndexOf('?');
      if (question > 0)
        tail = tail.Substring(0, question);
      // A trailer site's file is percent encoded and carries a query of its own after a '/'.
      tail = Uri.UnescapeDataString(tail);
      int dot = tail.LastIndexOf('.');
      if (dot > 0)
        tail = tail.Substring(0, dot);
      return tail.Length == 0 ? null : tail;
    }

    #region Helpers

    /// <summary>
    /// Pulls the film number out of a video site's address, in whichever of the shapes the tagging tools use.
    /// </summary>
    /// <returns>The number of the film, or <c>null</c> when the address names no film.</returns>
    private static string YouTubeId(string raw)
    {
      string text = Clean(raw);
      // The number hides in the query for the plugin shapes and for a watch page, and in the path for the
      // short address of the site.
      string query = null;
      int question = text.IndexOf('?');
      if (question >= 0)
        query = text.Substring(question + 1);
      foreach (KeyValuePair<string, string> pair in ParseQuery(query))
      {
        string key = pair.Key.ToLowerInvariant();
        if (key == "v" || key == "videoid" || key == "video_id")
          if (pair.Value.Length > 0)
            return pair.Value;
      }
      if (query == null)
      {
        string lower = text.ToLowerInvariant();
        int at = lower.IndexOf("youtu.be/", StringComparison.Ordinal);
        if (at >= 0)
        {
          string tail = text.Substring(at + "youtu.be/".Length);
          int slash = tail.IndexOf('/');
          if (slash > 0)
            tail = tail.Substring(0, slash);
          if (tail.Length > 0)
            return tail;
        }
      }
      return null;
    }

    /// <summary>
    /// Reads the address a trailer site's plugin format carries at its end, which is the real file address but
    /// written as part of the plugin's own path and percent encoded.
    /// </summary>
    private static string HideBehindPlugin(string raw)
    {
      string tail = Clean(raw).TrimEnd('/');
      int slash = tail.LastIndexOf('/');
      if (slash < 0 || slash == tail.Length - 1)
        return null;
      string candidate = Uri.UnescapeDataString(tail.Substring(slash + 1));
      return IsWebAddress(candidate.ToLowerInvariant()) ? candidate : null;
    }

    /// <summary>
    /// Splits a query into its key and value pairs. Values keep their percent encoding no longer than
    /// necessary, and a pair without an equals sign is ignored because no trailer address uses one.
    /// </summary>
    private static IDictionary<string, string> ParseQuery(string query)
    {
      var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrEmpty(query))
        return result;
      foreach (string pair in query.Split('&'))
      {
        if (pair.Length == 0)
          continue;
        int equals = pair.IndexOf('=');
        if (equals <= 0)
          continue;
        string value = pair.Substring(equals + 1);
        try
        {
          value = Uri.UnescapeDataString(value);
        }
        catch (UriFormatException)
        {
          // Percent signs the tagging tool did not mean as encoding. Keeping the text as it stands is the
          // friendliest answer, since a film number never contains one.
        }
        result[pair.Substring(0, equals)] = value;
      }
      return result;
    }

    private static bool IsWebAddress(string lower)
    {
      return lower.StartsWith("http://", StringComparison.Ordinal) ||
          lower.StartsWith("https://", StringComparison.Ordinal) ||
          lower.StartsWith("rtmp://", StringComparison.Ordinal) ||
          lower.StartsWith("rtsp://", StringComparison.Ordinal);
    }

    private static bool IsVideoSiteAddress(string lower)
    {
      foreach (string host in VIDEO_SITE_HOSTS)
        if (lower.Contains(host))
          return true;
      return false;
    }

    /// <summary>
    /// Looks for <paramref name="host"/> in <paramref name="text"/> as a host of its own, that is with a
    /// separator in front of it and the end of the address, a port, a question mark or an encoded separator
    /// behind it. The character in front is what keeps <c>pineapple.com</c> from counting as
    /// <c>apple.com</c>: a dot behind a letter means the host is only the tail of a longer name.
    /// </summary>
    private static bool CarriesHost(string text, string host)
    {
      for (int from = 0; ;)
      {
        int at = text.IndexOf(host, from, StringComparison.OrdinalIgnoreCase);
        if (at < 0)
          return false;
        char before = at == 0 ? '/' : text[at - 1];
        int after = at + host.Length;
        char behind = after >= text.Length ? '/' : text[after];
        if ((before == '/' || before == '.') && (behind == '/' || behind == ':' || behind == '?' || behind == '%'))
          return true;
        from = at + 1;
      }
    }

    /// <summary>
    /// Removes what a metadata file leaves around a value: the address of a trailer is sometimes written on
    /// several lines or with a label in front of it.
    /// </summary>
    private static string Clean(string raw)
    {
      if (string.IsNullOrEmpty(raw))
        return string.Empty;
      var builder = new StringBuilder(raw.Length);
      foreach (char c in raw)
        if (!char.IsWhiteSpace(c))
          builder.Append(c);
      return builder.ToString();
    }

    #endregion
  }
}
