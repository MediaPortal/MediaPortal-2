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
  class SeriesRelationshipExtractor : IRelationshipExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the series relationship metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "71F98A77-08E2-45DF-98DD-E013779914D5";

    /// <summary>
    /// Series relationship metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    protected RelationshipExtractorMetadata _metadata;
    private IList<IRelationshipRoleExtractor> _extractors;

    public SeriesRelationshipExtractor()
    {
      _metadata = new RelationshipExtractorMetadata(METADATAEXTRACTOR_ID, "NFO Series relationship extractor", MetadataExtractorPriority.Extended);
      RegisterRelationships();
      InitExtractors();
    }

    protected void InitExtractors()
    {
      _extractors = new List<IRelationshipRoleExtractor>();

      _extractors.Add(new EpisodeSeriesRelationshipExtractor());

      _extractors.Add(new EpisodeActorRelationshipExtractor());
      _extractors.Add(new EpisodeCharacterRelationshipExtractor());

      _extractors.Add(new SeriesActorRelationshipExtractor());
      _extractors.Add(new SeriesCharacterRelationshipExtractor());
    }

    /// <summary>
    /// Registers all relationships that are extracted by this relationship extractor.
    /// </summary>
    protected void RegisterRelationships()
    {
      IRelationshipTypeRegistration relationshipRegistration = ServiceRegistration.Get<IRelationshipTypeRegistration>();

      //Relationships must be registered in order from episodes up to all parent relationships

      //Hierarchical relationships
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Episode->Series", true,
        EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, EpisodeAspect.ASPECT_ID, SeriesAspect.ASPECT_ID,
        EpisodeAspect.ATTR_EPISODE, SeriesAspect.ATTR_AVAILABLE_EPISODES, true), true);

      //Simple (non hierarchical) relationships
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Episode->Actor", EpisodeAspect.ROLE_EPISODE, PersonAspect.ROLE_ACTOR), true);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Episode->Character", EpisodeAspect.ROLE_EPISODE, CharacterAspect.ROLE_CHARACTER), true);

      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Series->Actor", SeriesAspect.ROLE_SERIES, PersonAspect.ROLE_ACTOR), false);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Series->Character", SeriesAspect.ROLE_SERIES, CharacterAspect.ROLE_CHARACTER), false);
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
