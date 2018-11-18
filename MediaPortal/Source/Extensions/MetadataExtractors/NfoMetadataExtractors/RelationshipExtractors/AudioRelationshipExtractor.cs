#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  class AudioRelationshipExtractor : IRelationshipExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the audio relationship metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "6DD95D70-7827-403E-AA47-C858C8EE4B38";

    /// <summary>
    /// Audio relationship metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    protected RelationshipExtractorMetadata _metadata;
    private IList<IRelationshipRoleExtractor> _extractors;

    public AudioRelationshipExtractor()
    {
      _metadata = new RelationshipExtractorMetadata(METADATAEXTRACTOR_ID, "NFO Audio relationship extractor", MetadataExtractorPriority.Extended);
      RegisterRelationships();
      InitExtractors();
    }

    protected void InitExtractors()
    {
      _extractors = new List<IRelationshipRoleExtractor>();
      _extractors.Add(new TrackAlbumRelationshipExtractor());
      _extractors.Add(new TrackAlbumArtistRelationshipExtractor());
      _extractors.Add(new AlbumArtistRelationshipExtractor());
    }

    /// <summary>
    /// Registers all relationships that are extracted by this relationship extractor.
    /// </summary>
    protected void RegisterRelationships()
    {
      IRelationshipTypeRegistration relationshipRegistration = ServiceRegistration.Get<IRelationshipTypeRegistration>();

      //Relationships must be registered in order from tracks up to all parent relationships

      //Hierarchical relationships
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Album", true,
        AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, AudioAspect.ASPECT_ID, AudioAlbumAspect.ASPECT_ID,
        AudioAspect.ATTR_TRACK, AudioAlbumAspect.ATTR_AVAILABLE_TRACKS, true), true);

      //Simple (non hierarchical) relationships
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Album Artist", AudioAspect.ROLE_TRACK, PersonAspect.ROLE_ALBUMARTIST), true);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Album->Artist", AudioAlbumAspect.ROLE_ALBUM, PersonAspect.ROLE_ALBUMARTIST), false);
    }

    public RelationshipExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public IList<IRelationshipRoleExtractor> RoleExtractors
    {
      get { return _extractors; }
    }

    public IDictionary<IFilter, uint> GetLastChangedItemsFilters()
    {
      return null;
    }

    public void ResetLastChangedItems()
    {
    }

    public IDictionary<Guid, IList<MediaItemAspect>> GetBaseChildAspectsFromExistingAspects(IDictionary<Guid, IList<MediaItemAspect>> existingChildAspects, IDictionary<Guid, IList<MediaItemAspect>> existingParentAspects)
    {
      return null;
    }
  }
}
