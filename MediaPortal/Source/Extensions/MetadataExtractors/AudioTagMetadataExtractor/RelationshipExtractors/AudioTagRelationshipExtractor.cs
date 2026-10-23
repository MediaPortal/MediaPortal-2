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
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor
{
  /// <summary>
  /// Builds the album and the people a track's tags name, and the relationships to them.
  /// </summary>
  /// <remarks>
  /// Without this extractor a library has tracks and nothing else: the tracks name their album and
  /// their artists, but albums and artists are items of their own, and only a relationship extractor
  /// turns a name in a track's aspect into such an item.
  /// </remarks>
  public class AudioTagRelationshipExtractor : IRelationshipExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the audio tag relationship extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "77B18100-83B1-4E46-9B10-23052BF486CA";

    /// <summary>
    /// Audio tag relationship extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    #region Private fields

    private RelationshipExtractorMetadata _metadata;
    private IList<IRelationshipRoleExtractor> _extractors;

    #endregion

    #region Ctor

    public AudioTagRelationshipExtractor()
    {
      _metadata = new RelationshipExtractorMetadata(METADATAEXTRACTOR_ID, "Audio tag relationship extractor", MetadataExtractorPriority.Core);
      RegisterRelationships();
      InitExtractors();
    }

    #endregion

    #region Private methods

    private void InitExtractors()
    {
      _extractors = new List<IRelationshipRoleExtractor>();
      _extractors.Add(new TrackAlbumRelationshipExtractor());
      _extractors.Add(new TrackAlbumArtistRelationshipExtractor());
      _extractors.Add(new TrackArtistRelationshipExtractor());
      _extractors.Add(new TrackComposerRelationshipExtractor());
      _extractors.Add(new TrackConductorRelationshipExtractor());
      _extractors.Add(new AlbumArtistRelationshipExtractor());
    }

    /// <summary>
    /// Registers all relationships that are extracted by this relationship extractor.
    /// </summary>
    /// <remarks>
    /// A relationship may only start from a role that another relationship has already reached (or
    /// from a primary resource), so the order here is load-bearing: "Album->Artist" comes after
    /// "Track->Album", which is what makes albums reachable at all.
    /// </remarks>
    private void RegisterRelationships()
    {
      IRelationshipTypeRegistration relationshipRegistration = ServiceRegistration.Get<IRelationshipTypeRegistration>();

      // Hierarchical relationships
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Album", true,
        AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, AudioAspect.ASPECT_ID, AudioAlbumAspect.ASPECT_ID,
        AudioAspect.ATTR_TRACK, AudioAlbumAspect.ATTR_AVAILABLE_TRACKS, true), true);

      // Simple (non hierarchical) relationships
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Artist", AudioAspect.ROLE_TRACK, PersonAspect.ROLE_ARTIST), true);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Album Artist", AudioAspect.ROLE_TRACK, PersonAspect.ROLE_ALBUMARTIST), true);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Composer", AudioAspect.ROLE_TRACK, PersonAspect.ROLE_COMPOSER), true);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Conductor", AudioAspect.ROLE_TRACK, PersonAspect.ROLE_CONDUCTOR), true);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Album->Artist", AudioAlbumAspect.ROLE_ALBUM, PersonAspect.ROLE_ALBUMARTIST), false);
    }

    #endregion

    #region IRelationshipExtractor implementation

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
      // Nothing changed somewhere else that we would reimport for; that is what an online library
      // would answer, and there is none in a portable server.
      return null;
    }

    public void ResetLastChangedItems()
    {
    }

    public IDictionary<Guid, IList<MediaItemAspect>> GetBaseChildAspectsFromExistingAspects(IDictionary<Guid, IList<MediaItemAspect>> existingChildAspects, IDictionary<Guid, IList<MediaItemAspect>> existingParentAspects)
    {
      // Only needed to rebuild a hierarchical relationship's index when the parent already exists but
      // the link was lost; the track's own aspects always carry its album name and track number, so
      // there is nothing to rebuild here.
      return null;
    }

    #endregion
  }
}
