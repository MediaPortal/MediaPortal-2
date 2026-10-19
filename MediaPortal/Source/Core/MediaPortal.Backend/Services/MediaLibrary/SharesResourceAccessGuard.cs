#region Copyright

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

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  /// <summary>
  /// The rule for <c>GetResource</c>: a resource may leave the server over HTTP if it is inside a share somebody
  /// registered, and not otherwise.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the answer <see cref="MediaPortal.Common.Services.ResourceAccess.ResourceAccessModule"/> has wanted
  /// since it was written and could not find, because the shares belong to the media library. It is also a
  /// deliberately narrow answer: <i>somebody registered this folder as media</i> is the strongest claim a server
  /// can make about a file without knowing who is asking, and this endpoint has no idea who is asking - a UPnP
  /// renderer is handed these URLs to fetch by itself and cannot present a ticket. Everything else - the database,
  /// the configuration, the logs, the operating system - is not media, and is not served.
  /// </para>
  /// <para>
  /// <b>Why the comparison is done here rather than with <see cref="ResourcePath.IsSameOrParentOf"/>.</b> For the
  /// providers that matter the whole file path lives inside one provider segment - the local filesystem provider
  /// keeps <c>/D:/films/sub/film.mkv</c> as a single segment - so comparing segment by segment says nothing about
  /// whether a file is under a folder. And <c>IsSameOrParentOf</c> compares with
  /// <see cref="StringComparer.InvariantCulture"/>, which on Windows would refuse a share whose letters happen to
  /// be written in a different case than the file the importer found; the same file on the same disk is the same
  /// media. Case is ignored where the operating system ignores it, and nowhere else.
  /// </para>
  /// <para>
  /// <b>Traversal.</b> A prefix test on a path that may contain <c>..</c> is not a boundary: the local provider's
  /// path becomes a DOS path by swapping separators and nothing else, so
  /// <c>{local}:///D:/films/../windows/win.ini</c> starts with <c>/D:/films/</c> and a naive guard would serve it.
  /// A path containing <c>..</c> or <c>.</c> is refused outright - which is also what a path the importer wrote
  /// never contains.
  /// </para>
  /// <para>
  /// <b>The provider has to match, not only the path.</b> A provider id is part of a resource path because two
  /// providers can name the same string and mean different things; matching on the text alone would let a caller
  /// put a path under a share that was registered on some other provider.
  /// </para>
  /// <para>
  /// <b>Chained resources are refused.</b> A path of more than one segment names something stacked on something
  /// else - an entry in an archive, a stream a virtual provider transforms - and no registered folder contains it.
  /// Those titles are streamed by id (<c>GET api/media/items/{id}/stream</c>), which asks the library instead of
  /// the filesystem.
  /// </para>
  /// <para>
  /// <b>The shares are read rarely and kept briefly.</b> One film is hundreds of range requests and each arrives
  /// here; asking the database for the share table on every one would make the guard the most expensive part of
  /// watching something. A snapshot is kept for <see cref="SHARE_CACHE_TIME"/> and refreshed on the first request
  /// after that, so a share added in the front end is served within the window without a restart. If the library
  /// cannot be reached, the last snapshot that was read stays in force - interrupting a film because the database
  /// hiccuped would be its own bug - and if none ever was, nothing is served.
  /// </para>
  /// </remarks>
  public class SharesResourceAccessGuard : IResourceAccessGuard
  {
    /// <summary>How long a successfully read list of shares is used before it is asked again.</summary>
    public static readonly TimeSpan SHARE_CACHE_TIME = TimeSpan.FromSeconds(30);

    private readonly IMediaLibrary _mediaLibrary;
    private readonly object _sync = new object();

    private List<ServedRoot> _roots = new List<ServedRoot>();
    private DateTime _readAt = DateTime.MinValue;
    private int _reads;

    /// <summary>
    /// The guard over the library that owns the shares.
    /// </summary>
    /// <param name="mediaLibrary">The library to ask. A guard without a library would guard nothing.</param>
    public SharesResourceAccessGuard(IMediaLibrary mediaLibrary)
    {
      if (mediaLibrary == null)
        throw new ArgumentNullException(nameof(mediaLibrary));
      _mediaLibrary = mediaLibrary;
    }

    /// <summary>
    /// How often the share list was actually read. Exists so a test can prove the cache is doing its job: a guard
    /// that asked the database once per range request would behave correctly and cost the viewer their film.
    /// </summary>
    public int SharesReadCount
    {
      get
      {
        lock (_sync)
          return _reads;
      }
    }

    /// <inheritdoc/>
    public bool IsAllowed(ResourcePath resourcePath)
    {
      bool isShareRoot;
      return Matches(resourcePath, out isShareRoot) && !isShareRoot;
    }

    /// <inheritdoc/>
    public bool IsInsideAShare(ResourcePath resourcePath)
    {
      bool isShareRoot;
      return Matches(resourcePath, out isShareRoot);
    }

    /// <summary>
    /// Whether the path belongs to a registered share, and whether it <i>is</i> one rather than something inside it.
    /// </summary>
    private bool Matches(ResourcePath resourcePath, out bool isShareRoot)
    {
      isShareRoot = false;

      Guid provider;
      string requested;
      if (!TryServeable(resourcePath, out provider, out requested))
        return false;

      foreach (ServedRoot root in CurrentRoots())
      {
        if (root.ProviderId != provider)
          continue;

        if (IsInside(requested, root.Path))
          return true;

        if (string.Equals(requested, root.Path, PathComparison))
        {
          isShareRoot = true;
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// How two paths on this machine are compared: case-insensitively where the operating system does.
    /// </summary>
    private static StringComparison PathComparison
    {
      get
      {
        return OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
      }
    }

    /// <summary>
    /// The comparable form of a request: one provider, one path that does not walk out of itself.
    /// </summary>
    private static bool TryServeable(ResourcePath resourcePath, out Guid providerId, out string path)
    {
      providerId = Guid.Empty;
      path = null;
      if (resourcePath == null)
        return false;

      List<ProviderPathSegment> segments = resourcePath.ToList();
      if (segments.Count != 1)
      {
        // One segment, because a stacked path names something inside something else, and no share is a folder that
        // contains it. Also one segment, because matching only the first of several would let a caller name any
        // base provider they liked and any transform on top of it.
        return false;
      }

      providerId = segments[0].ProviderId;
      path = Normalize(segments[0].Path);
      return path != null;
    }

    /// <summary>
    /// The comparable form of a provider path: forward slashes, one leading slash, no trailing slash.
    /// </summary>
    /// <returns>The path, or <c>null</c> when it is empty or walks out of itself.</returns>
    /// <remarks>
    /// Nothing is "cleaned up" beyond separators: a path containing <c>..</c> is refused rather than resolved,
    /// because resolving it would need this machine's current directory and a resource path has none. Refusing is
    /// also the answer a library never notices - an imported file's path carries no <c>..</c>.
    /// </remarks>
    private static string Normalize(string providerPath)
    {
      if (string.IsNullOrWhiteSpace(providerPath))
        return null;

      string text = providerPath.Replace('\\', '/');
      if (!text.StartsWith("/", StringComparison.Ordinal))
        text = "/" + text;
      text = text.TrimEnd('/');
      if (text.Length == 0)
        return null;

      foreach (string part in text.Split('/'))
      {
        if (part == ".." || part == ".")
          return null;
      }
      return text;
    }

    /// <summary>
    /// Whether <paramref name="path"/> is below - not equal to - <paramref name="share"/>.
    /// </summary>
    private static bool IsInside(string path, string share)
    {
      if (path.Length <= share.Length || !path.StartsWith(share, PathComparison))
        return false;

      // "/D:/films" must not serve "/D:/films-x/film.mkv": the character after the share has to be a separator.
      return path[share.Length] == '/';
    }

    /// <summary>
    /// The share roots as they were last read, re-read when that read has gone stale.
    /// </summary>
    private List<ServedRoot> CurrentRoots()
    {
      lock (_sync)
      {
        if (_reads > 0 && DateTime.UtcNow - _readAt <= SHARE_CACHE_TIME)
          return _roots;

        try
        {
          IDictionary<Guid, Share> shares = _mediaLibrary.GetShares(null);
          var roots = new List<ServedRoot>();
          if (shares != null)
          {
            foreach (Share share in shares.Values)
            {
              ServedRoot root = RootOf(share);
              if (root != null)
                roots.Add(root);
            }
          }

          _roots = roots;
        }
        catch (Exception ex)
        {
          // The list that was read stays in force. Nobody watching a film should lose it because the database was
          // busy, and dropping it on a hiccup would be a self-inflicted outage. When nothing was ever read, the
          // empty list stands, which serves nothing - the safe answer when the truth is unknown.
          ILogger logger = ServiceRegistration.Get<ILogger>(false);
          if (logger != null)
            logger.Warn("SharesResourceAccessGuard: could not read the shares, keeping the previous list: {0}",
                ex.Message);
        }

        _reads++;
        _readAt = DateTime.UtcNow;
        return _roots;
      }
    }

    /// <summary>
    /// One share's root as a provider and a path, or <c>null</c> when the share is not a place files live under.
    /// </summary>
    private static ServedRoot RootOf(Share share)
    {
      if (share == null || share.BaseResourcePath == null)
        return null;

      List<ProviderPathSegment> segments = share.BaseResourcePath.ToList();
      if (segments.Count != 1)
        return null;

      string path = Normalize(segments[0].Path);
      if (path == null)
        return null;

      return new ServedRoot(segments[0].ProviderId, path);
    }

    /// <summary>
    /// One registered share, in the form the comparison needs.
    /// </summary>
    private sealed class ServedRoot
    {
      public ServedRoot(Guid providerId, string path)
      {
        ProviderId = providerId;
        Path = path;
      }

      public Guid ProviderId { get; }

      public string Path { get; }
    }
  }
}
