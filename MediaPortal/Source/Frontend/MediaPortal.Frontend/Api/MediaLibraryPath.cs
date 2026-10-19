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
using System.IO;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// The bridge between a path as the media database stores it and something a player on this machine can open.
  /// </summary>
  /// <remarks>
  /// <para>
  /// MP2 stores a resource as a chain of provider segments - <c>{provider-id}://path</c>, then
  /// <c>&gt;{provider-id}://path</c> for each virtual provider stacked on top - because a film may be a file, a
  /// share, a zip entry, or a stream, and the database does not care which. A player does care, so somebody has
  /// to translate, and the honest place for that translation is the machine that has to do the opening: the
  /// server cannot know whether the client can reach the share.
  /// </para>
  /// <para>
  /// What is recognized here is deliberately narrow: a single segment whose path is a location this operating
  /// system can name. Everything else - a stacked provider, a database row, an entry inside an archive - comes
  /// back as "not playable from here" with the reason, rather than as a path the player would fail on later.
  /// </para>
  /// <para>
  /// A path this machine cannot open is no longer the end of the story: the server can send the bytes
  /// (<c>GET api/media/items/{id}/stream</c>, built by <see cref="ServerApiClient.GetStreamUri"/>), and that is
  /// what turns "this film is on the server" into a film. <see cref="CanBeStreamed"/> is the other half of the
  /// same question, because the endpoint sends one file and not every title is one file.
  /// </para>
  /// </remarks>
  public static class MediaLibraryPath
  {
    /// <summary>
    /// Turns a serialized MP2 resource path into a file on this machine.
    /// </summary>
    /// <param name="serializedPath">The <c>path</c> field of a title, as it arrived from the server.</param>
    /// <param name="localPath">The path to hand a player, only set when the answer is <c>true</c>.</param>
    /// <param name="reason">Why it cannot be opened here, only set when the answer is <c>false</c>.</param>
    public static bool TryAsLocalFile(string? serializedPath, out string localPath, out string? reason)
    {
      localPath = string.Empty;
      reason = null;

      if (string.IsNullOrWhiteSpace(serializedPath))
      {
        reason = "the server did not name a file for this title";
        return false;
      }

      string text = serializedPath.Trim();

      // A ">" marks a segment stacked on the one before it, i.e. something *inside* something else, or a
      // provider that transforms what it wraps. Only the first segment can be a plain file on disk.
      int second = text.IndexOf('>');
      if (second >= 0)
      {
        reason = "this title is made of more than one resource, which this client cannot open by itself yet";
        return false;
      }

      int separator = text.IndexOf("://", StringComparison.Ordinal);
      string path = separator < 0 ? text : text.Substring(separator + 3);
      if (string.IsNullOrWhiteSpace(path))
      {
        reason = "the server named a provider but no path within it";
        return false;
      }

      // Forward slashes are what MP2 stores; a Windows player takes them too, but the separators are normalized
      // so that what a person sees in a message is what their own shell would show.
      if (Path.DirectorySeparatorChar != '/' && path.Contains('/') && LooksLikeWindowsPath(path))
        path = path.Replace('/', Path.DirectorySeparatorChar);

      if (!Path.IsPathFullyQualified(path))
      {
        reason = "'" + path + "' is not a path this machine can open";
        return false;
      }

      localPath = path;
      return true;
    }

    /// <summary>
    /// Whether the server could send this title's bytes at all, which is not the same question as whether this
    /// machine can open them.
    /// </summary>
    /// <param name="serializedPath">The <c>path</c> field of a title, as it arrived from the server.</param>
    /// <param name="reason">Why no stream would help, only set when the answer is <c>false</c>.</param>
    /// <remarks>
    /// <para>
    /// The stream endpoint sends the title's primary resource as one file, opened by the server's own resource
    /// provider. That covers the case the endpoint exists for - the film is on the server's disk, or on a share
    /// only the server was given - and it does not cover a title whose path is a <i>chain</i>: an entry inside an
    /// archive, or a stream a virtual provider transforms on the way. There is no single file behind such a title
    /// for the endpoint to send, and it says so with a refusal.
    /// </para>
    /// <para>
    /// Asking here rather than letting the player find out is the difference between a row that says "cannot be
    /// watched from here" before anything is pressed and a row that looks watchable, starts the player, and shows
    /// a black screen while the server's refusal sits in a response body nobody reads. A path this machine cannot
    /// open is on the other side of that line: it is exactly what streaming is for.
    /// </para>
    /// </remarks>
    public static bool CanBeStreamed(string? serializedPath, out string? reason)
    {
      reason = null;

      if (string.IsNullOrWhiteSpace(serializedPath))
      {
        reason = "the server did not name a file for this title";
        return false;
      }

      string text = serializedPath.Trim();
      if (text.IndexOf('>') >= 0)
      {
        reason = "this title is made of more than one resource, which no single stream can carry";
        return false;
      }

      int separator = text.IndexOf("://", StringComparison.Ordinal);
      string path = separator < 0 ? text : text.Substring(separator + 3);
      if (string.IsNullOrWhiteSpace(path))
      {
        reason = "the server named a provider but no path within it";
        return false;
      }

      return true;
    }

    /// <summary>
    /// Whether the slashes in this path are separators rather than part of a POSIX name.
    /// </summary>
    /// <remarks>
    /// Only a path that starts with a drive letter is rewritten. On a Windows client a POSIX path stays as it
    /// is, because turning <c>/srv/media/film.mkv</c> into <c>\srv\media\film.mkv</c> would be inventing a
    /// UNC share out of a Linux path.
    /// </remarks>
    private static bool LooksLikeWindowsPath(string path)
    {
      return path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' &&
             (path[2] == '/' || path[2] == '\\');
    }
  }
}
