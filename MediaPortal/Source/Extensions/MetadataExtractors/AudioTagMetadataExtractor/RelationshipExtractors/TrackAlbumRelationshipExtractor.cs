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
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor
{
  /// <summary>
  /// Builds the album a track belongs to out of the track's own tag.
  /// </summary>
  /// <remarks>
  /// <para>
  /// An album in the library is an item of its own, with a name, an artist and a release date, and the
  /// tracks on it are linked to it. Everything an album needs is in the tags of the files on it, so
  /// the album is derived from the track that is being imported: the library recognises two files as
  /// being on the same album because both describe the same album, which is what
  /// <see cref="MatchAspects"/> and <see cref="TryMatch"/> are for.
  /// </para>
  /// <para>
  /// This is how albums arrive in a library without an album.nfo file next to the music - which is
  /// nearly every music folder. The NFO plugin's own album extractor stays registered next to this
  /// one and wins where an album.nfo exists, because both describe the same album and the library
  /// merges what the extractors of one import round say about it.
  /// </para>
  /// </remarks>
  internal class TrackAlbumRelationshipExtractor : IRelationshipRoleExtractor
  {
    #region Static fields

    private static readonly Guid[] ROLE_ASPECTS = { AudioAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { AudioAlbumAspect.ASPECT_ID };

    #endregion

    #region IRelationshipRoleExtractor implementation

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return AudioAspect.ROLE_TRACK; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return AudioAlbumAspect.ROLE_ALBUM; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public Guid[] MatchAspects
    {
      get { return AlbumInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
        return null;
      return RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_ALBUM);
    }

    public ICollection<string> GetExternalIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
        return new List<string>();
      return RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_ALBUM);
    }

    public Task<bool> TryExtractRelationshipsAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> aspects, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects)
    {
      TrackInfo trackInfo = new TrackInfo();
      if (!trackInfo.FromMetadata(aspects))
        return Task.FromResult(false);

      // Everything an album knows about itself, the track's tag already said: name, release date,
      // genres, ids, and the artists under their album artist names.
      AlbumInfo albumInfo = trackInfo.CloneBasicInstance<AlbumInfo>();
      if (albumInfo == null || string.IsNullOrEmpty(albumInfo.Album))
        return Task.FromResult(false);

      // The album's own sort title and release ids are in the file's tag but not in the track's
      // aspects, so they are read from the file here.
      AudioTags.TryUpdateAlbum(mediaItemAccessor, albumInfo);

      IDictionary<Guid, IList<MediaItemAspect>> albumAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      if (!albumInfo.SetMetadata(albumAspects))
        return Task.FromResult(false);

      bool trackVirtual = true;
      if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out trackVirtual))
        MediaItemAspect.SetAttribute(albumAspects, MediaAspect.ATTR_ISVIRTUAL, trackVirtual);

      // Without an id of its own the library could not tell this album from any other album of the
      // same name, so it is not linked at all rather than linked to a guess.
      if (!albumAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        return Task.FromResult(false);

      extractedLinkedAspects.Add(albumAspects);
      return Task.FromResult(true);
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (existingAspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
      {
        AlbumInfo extracted = new AlbumInfo();
        extracted.FromMetadata(extractedAspects);
        AlbumInfo existing = new AlbumInfo();
        existing.FromMetadata(existingAspects);

        return existing.Equals(extracted);
      }
      return false;
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      if (!MediaItemAspect.TryGetAspect(aspects, AudioAspect.Metadata, out SingleMediaItemAspect aspect))
        return false;

      int disc = aspect.GetAttributeValue<int>(AudioAspect.ATTR_DISCID);
      int track = aspect.GetAttributeValue<int>(AudioAspect.ATTR_TRACK);
      if (disc <= 0)
        disc = 1;

      // Keeps the tracks of an album in disc-then-track order, which is the order a record has.
      index = disc * 1000 + track;
      return true;
    }

    #endregion
  }
}
