#region Copyright (C) 2007-2025 Team MediaPortal

/*
    Copyright (C) 2007-2025 Team MediaPortal
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
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities;
using File = TagLib.File;
using TagLib;

namespace MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor
{
  /// <summary>
  /// Opens audio files with TagLibSharp and reads the values that more than one extractor in this
  /// plugin needs.
  /// </summary>
  /// <remarks>
  /// <para>
  /// TagLibSharp wants a <see cref="File.IFileAbstraction"/>, not a path, because a MediaPortal share
  /// is not necessarily on the local disk - it can be a WebDAV mount or a subservice's stream. Handing
  /// it the resource accessor keeps every share type readable and never writes to the user's files
  /// (the tag is only ever read back, never saved).
  /// </para>
  /// <para>
  /// The relationship extractors also use <see cref="TryUpdateAlbum"/> and <see cref="TryUpdateArtists"/>
  /// after they have built an album or a person from a track's tag, because those carry details that
  /// the track's own aspects do not keep: the sort title of an album and the MusicBrainz ids of the
  /// artists. Upstream the same three methods exist on the Windows audio extractor for the same reason.
  /// </para>
  /// </remarks>
  internal static class AudioTags
  {
    #region Constants

    /// <summary>
    /// File extensions whose tags this plugin reads.
    /// </summary>
    /// <remarks>
    /// Same list as the Windows audio extractor's settings, minus the ones its BASS.NET stream probe
    /// needed for playback. A file whose extension is not in here is left to the other extractors, so
    /// an image or a subtitle in a music folder is never mistaken for a track.
    /// </remarks>
    internal static readonly ICollection<string> AUDIO_EXTENSIONS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
        ".ape", ".flac", ".mp3", ".ogg", ".wv", ".wav", ".wma", ".mp4", ".m4a", ".m4p",
        ".mpc", ".mp+", ".mpp", ".dsf", ".dff", ".oga"
      };

    #endregion

    #region Public methods

    /// <summary>
    /// Checks whether the given file name has an extension whose tags this plugin can read.
    /// </summary>
    /// <param name="fileName">Relative or absolute file path to check.</param>
    /// <returns><c>true</c> if the extension is an audio extension.</returns>
    public static bool HasAudioExtension(string fileName)
    {
      if (string.IsNullOrEmpty(fileName))
        return false;
      return AUDIO_EXTENSIONS.Contains(DosPathHelper.GetExtension(fileName));
    }

    /// <summary>
    /// Opens the given file read-only and returns the tags TagLibSharp found in it.
    /// </summary>
    /// <param name="fsra">The audio file to open.</param>
    /// <returns>An opened <see cref="File"/>, or <c>null</c> if the file has nothing to read.</returns>
    /// <remarks>
    /// The caller owns the returned file and must dispose it. A file whose tag blocks are damaged is
    /// not an error - it is a file without tags, which happens in real music folders more often than
    /// the tests suggest - so it is reported at debug level and answered with <c>null</c>.
    /// </remarks>
    public static File LoadTag(IFileSystemResourceAccessor fsra)
    {
      try
      {
        // Many tags in the wild hold non-latin1 bytes in frames that claim latin1. Without this
        // TagLibSharp throws instead of returning the name with a wrong character in it.
        ByteVector.UseBrokenLatin1Behavior = true;
        // Created without a write mode and never saved, so a user's file is never rewritten by an import.
        return File.Create(new ResourceProviderFileAbstraction(fsra));
      }
      catch (CorruptFileException e)
      {
        ServiceRegistration.Get<ILogger>().Debug("AudioTagMetadataExtractor: Cannot read the tags of '{0}': {1}",
          fsra.ResourceName, e.Message);
        return null;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("AudioTagMetadataExtractor: Cannot open '{0}'", fsra.ResourceName, e);
        return null;
      }
    }

    /// <summary>
    /// Adds the album details that only the file's tags know to the given album.
    /// </summary>
    /// <param name="mediaItemAccessor">Points to an audio file of the album.</param>
    /// <param name="album">Album to update.</param>
    /// <returns><c>true</c> if the album was updated.</returns>
    public static bool TryUpdateAlbum(IResourceAccessor mediaItemAccessor, AlbumInfo album)
    {
      if (album == null)
        return false;
      var fsra = mediaItemAccessor as IFileSystemResourceAccessor;
      if (fsra == null || !HasAudioExtension(mediaItemAccessor.ResourceName))
        return false;

      File tag = LoadTag(fsra);
      if (tag == null)
        return false;

      bool updated = false;
      using (tag)
      {
        string albumSort = StringUtils.TrimToNull(tag.Tag.AlbumSort);
        if (albumSort != null)
        {
          album.AlbumSort = albumSort;
          updated = true;
        }
        if (string.IsNullOrEmpty(album.MusicBrainzId) && !string.IsNullOrEmpty(tag.Tag.MusicBrainzReleaseId))
        {
          album.MusicBrainzId = tag.Tag.MusicBrainzReleaseId.Trim();
          updated = true;
        }
        if (string.IsNullOrEmpty(album.MusicBrainzGroupId) && !string.IsNullOrEmpty(tag.Tag.MusicBrainzReleaseGroupId))
        {
          album.MusicBrainzGroupId = tag.Tag.MusicBrainzReleaseGroupId.Trim();
          updated = true;
        }
        if (string.IsNullOrEmpty(album.AmazonId) && !string.IsNullOrEmpty(tag.Tag.AmazonId))
        {
          album.AmazonId = tag.Tag.AmazonId.Trim();
          updated = true;
        }
      }
      return updated;
    }

    /// <summary>
    /// Adds the MusicBrainz ids that the file's tags hold for its artists to the given persons.
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the audio file the persons came from.</param>
    /// <param name="persons">Persons to update.</param>
    /// <param name="forAlbum">If <c>true</c>, read the album artist frames instead of the track artist ones.</param>
    /// <returns><c>true</c> if at least one person was updated.</returns>
    /// <remarks>
    /// A tag can name several artists in one frame and hold several ids in another, in the same order.
    /// Only pairs that line up are used; a person that the tag does not name is left untouched, so a
    /// wrong guess here can never attach an id to the wrong artist.
    /// </remarks>
    public static bool TryUpdateArtists(IResourceAccessor mediaItemAccessor, IList<PersonInfo> persons, bool forAlbum)
    {
      if (persons == null || persons.Count == 0)
        return false;
      var fsra = mediaItemAccessor as IFileSystemResourceAccessor;
      if (fsra == null || !HasAudioExtension(mediaItemAccessor.ResourceName))
        return false;

      File tag = LoadTag(fsra);
      if (tag == null)
        return false;

      using (tag)
        return TryUpdateArtists(tag, persons, forAlbum);
    }

    /// <summary>
    /// Adds the MusicBrainz ids of the opened tag to the given persons.
    /// </summary>
    public static bool TryUpdateArtists(File tag, IList<PersonInfo> persons, bool forAlbum)
    {
      IEnumerable<string> names = (forAlbum ? tag.Tag.AlbumArtists : tag.Tag.Performers)
          .Where(name => !string.IsNullOrEmpty(name))
          .Select(name => name.Trim())
          .ToList();
      if (!names.Any())
        return false;

      string ids = forAlbum ? tag.Tag.MusicBrainzReleaseArtistId : tag.Tag.MusicBrainzArtistId;
      if (string.IsNullOrEmpty(ids))
        return false;

      // Several ids can be held in one frame, separated the same way the artist names are separated in
      // theirs; '/' would break names like "AC/DC", so only the ';' the ID3v2.4 multi-value frames use
      // is taken as a separator.
      List<string> idList = ids.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
          .Select(id => id.Trim())
          .Where(id => id.Length > 0)
          .ToList();
      if (idList.Count == 0)
        return false;

      bool success = false;
      List<string> nameList = names.ToList();
      for (int index = 0; index < idList.Count && index < nameList.Count; index++)
      {
        PersonInfo person = persons.FirstOrDefault(p => string.Equals(p.Name, nameList[index], StringComparison.InvariantCultureIgnoreCase));
        if (person == null || !string.IsNullOrEmpty(person.MusicBrainzId))
          continue;
        person.MusicBrainzId = idList[index];
        success = true;
      }
      return success;
    }

    #endregion

    #region Nested types

    /// <summary>
    /// Hands a MediaPortal resource accessor to TagLibSharp as if it were a file on disk.
    /// </summary>
    internal class ResourceProviderFileAbstraction : File.IFileAbstraction
    {
      private readonly IFileSystemResourceAccessor _resourceAccessor;

      public ResourceProviderFileAbstraction(IFileSystemResourceAccessor resourceAccessor)
      {
        _resourceAccessor = resourceAccessor;
      }

      public string Name
      {
        get { return _resourceAccessor.ResourcePathName; }
      }

      public Stream ReadStream
      {
        get { return _resourceAccessor.OpenRead(); }
      }

      /// <summary>
      /// Not supported. This plugin reads tags; it never writes one into a user's file.
      /// </summary>
      public Stream WriteStream
      {
        get { throw new NotSupportedException("The audio tag extractor does not write tags."); }
      }

      public void CloseStream(Stream stream)
      {
        if (stream != null)
          stream.Dispose();
      }
    }

    #endregion
  }
}
