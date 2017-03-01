#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.MLQueries;
using System.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  class TrackAlbumRelationshipExtractor : IAudioRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { AudioAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { AudioAlbumAspect.ASPECT_ID };

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
      return GetAlbumSearchFilter(extractedAspects);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, bool importOnly, out IList<RelationshipItem> extractedLinkedAspects)
    {
      extractedLinkedAspects = null;

      TrackInfo trackInfo = new TrackInfo();
      if (!trackInfo.FromMetadata(aspects))
        return false;

      if (CheckCacheContains(trackInfo))
        return false;
      
      IList<Guid> linkedIds;
      Guid albumId = BaseInfo.TryGetLinkedIds(aspects, LinkedRole, out linkedIds) ? linkedIds[0] : Guid.Empty;

      AlbumInfo cachedAlbum;
      Guid cachedId;
      AlbumInfo albumInfo = trackInfo.CloneBasicInstance<AlbumInfo>();
      UpdateAlbum(aspects, albumInfo);
      UpdatePersons(aspects, albumInfo.Artists, true);
      if (TryGetInfoFromCache(albumInfo, out cachedAlbum, out cachedId))
      {
        albumInfo = cachedAlbum;
        if (albumId == Guid.Empty)
          albumId = cachedId;
      }
      else if (!AudioMetadataExtractor.SkipOnlineSearches)
      {
        OnlineMatcherService.Instance.UpdateAlbum(albumInfo, false, importOnly);
      }

      if (!BaseInfo.HasRelationship(aspects, LinkedRole))
        albumInfo.HasChanged = true; //Force save if no relationship exists

      if (!albumInfo.HasChanged && !importOnly)
        return false;

      AddToCheckCache(trackInfo);

      extractedLinkedAspects = new List<RelationshipItem>();
      IDictionary<Guid, IList<MediaItemAspect>> albumAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      albumInfo.SetMetadata(albumAspects);

      bool trackVirtual = true;
      if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out trackVirtual))
      {
        MediaItemAspect.SetAttribute(albumAspects, MediaAspect.ATTR_ISVIRTUAL, trackVirtual);
      }

      byte[] data;
      if (MediaItemAspect.TryGetAttribute(aspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
      {
        //Use image from track as image
        MediaItemAspect.SetAttribute(albumAspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, data);
      }

      if (importOnly)
        StorePersons(albumAspects, albumInfo.Artists, true);

      if (!albumAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        return false;

      if (albumId != Guid.Empty)
        extractedLinkedAspects.Add(new RelationshipItem(albumAspects, albumId, albumInfo.HasChanged));
      else
        extractedLinkedAspects.Add(new RelationshipItem(albumAspects, Guid.Empty));
      return true;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      return existingAspects.ContainsKey(AudioAlbumAspect.ASPECT_ID);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect aspect;
      if (!MediaItemAspect.TryGetAspect(aspects, AudioAspect.Metadata, out aspect))
        return false;

      int disc = aspect.GetAttributeValue<int>(AudioAspect.ATTR_DISCID);
      int track = aspect.GetAttributeValue<int>(AudioAspect.ATTR_TRACK);
      if (disc <= 0)
        disc = 1;

      index = disc * 1000 + track;
      return true;
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      AlbumInfo album = new AlbumInfo();
      album.FromMetadata(extractedAspects);
      AddToCache(extractedItemId, album, false);
    }
  }
}
