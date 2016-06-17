#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Extensions.OnlineLibraries.Matchers;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  class TrackAlbumRelationshipExtractor : IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { AudioAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { AudioAlbumAspect.ASPECT_ID };

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

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects, bool forceQuickMode)
    {
      extractedLinkedAspects = null;

      // Build the album MI

      AlbumInfo albumInfo = new AlbumInfo();
      if (!albumInfo.FromMetadata(aspects))
        return false;

      MusicTheAudioDbMatcher.Instance.UpdateAlbum(albumInfo, forceQuickMode);
      MusicBrainzMatcher.Instance.UpdateAlbum(albumInfo, forceQuickMode);
      MusicFanArtTvMatcher.Instance.UpdateAlbum(albumInfo, forceQuickMode);

      extractedLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      IDictionary<Guid, IList<MediaItemAspect>> albumAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      extractedLinkedAspects.Add(albumAspects);

      return albumInfo.SetMetadata(albumAspects);
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

      index = disc * 100 + track;
      return true;
    }
  }
}
