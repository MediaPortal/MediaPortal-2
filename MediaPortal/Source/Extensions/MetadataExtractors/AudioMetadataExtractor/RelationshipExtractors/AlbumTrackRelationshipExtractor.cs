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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  class AlbumTrackRelationshipExtractor : IAudioRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { AudioAlbumAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { AudioAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      //Album -> track relationship already exists
      get { return false; }
    }

    public Guid Role
    {
      get { return AudioAlbumAspect.ROLE_ALBUM; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return AudioAspect.ROLE_TRACK; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public Guid[] MatchAspects
    {
      get { return TrackInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      return GetTrackSearchFilter(extractedAspects);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, bool importOnly, out IList<RelationshipItem> extractedLinkedAspects)
    {
      extractedLinkedAspects = null;

      if (importOnly)
        return false;

      if (AudioMetadataExtractor.OnlyLocalMedia)
        return false;

      AlbumInfo albumInfo = new AlbumInfo();
      if (!albumInfo.FromMetadata(aspects))
        return false;

      if (CheckCacheContains(albumInfo))
        return false;

      if (!AudioMetadataExtractor.SkipOnlineSearches)
        OnlineMatcherService.Instance.UpdateAlbum(albumInfo, true, importOnly);

      if (albumInfo.Tracks.Count == 0)
        return false;

      if (BaseInfo.CountRelationships(aspects, LinkedRole) < albumInfo.Tracks.Count)
        albumInfo.HasChanged = true; //Force save if no relationship exists
      else
        return false;

      if (!albumInfo.HasChanged)
        return false;

      AddToCheckCache(albumInfo);

      extractedLinkedAspects = new List<RelationshipItem>();
      for (int i = 0; i < albumInfo.Tracks.Count; i++)
      {
        TrackInfo trackInfo = albumInfo.Tracks[i];
        trackInfo.AlbumNameId = albumInfo.NameId;

        IDictionary<Guid, IList<MediaItemAspect>> trackAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        trackInfo.SetMetadata(trackAspects);
        MediaItemAspect.SetAttribute(trackAspects, MediaAspect.ATTR_ISVIRTUAL, true);

        if (trackAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(new RelationshipItem(trackAspects, Guid.Empty));
      }
      return extractedLinkedAspects.Count > 0;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(AudioAspect.ASPECT_ID))
        return false;

      TrackInfo linkedTrack = new TrackInfo();
      if (!linkedTrack.FromMetadata(extractedAspects))
        return false;

      TrackInfo existingTrack = new TrackInfo();
      if (!existingTrack.FromMetadata(existingAspects))
        return false;

      return linkedTrack.Equals(existingTrack);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect linkedAspect;
      if (!MediaItemAspect.TryGetAspect(linkedAspects, AudioAspect.Metadata, out linkedAspect))
        return false;

      int disc = linkedAspect.GetAttributeValue<int>(AudioAspect.ATTR_DISCID);
      int track = linkedAspect.GetAttributeValue<int>(AudioAspect.ATTR_TRACK);
      if (disc <= 0)
        disc = 1;

      index = disc * 1000 + track;
      return true;
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
