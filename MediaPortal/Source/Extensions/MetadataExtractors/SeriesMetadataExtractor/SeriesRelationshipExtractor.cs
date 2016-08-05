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
using MediaPortal.Common.Settings;
using MediaPortal.Common;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeriesRelationshipExtractor : IRelationshipExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the series relationship metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "FE565C89-AFC6-4036-977D-255A630BD868";

    /// <summary>
    /// Series relationship metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    protected RelationshipExtractorMetadata _metadata;
    private IList<IRelationshipRoleExtractor> _extractors;

    public SeriesRelationshipExtractor()
    {
      bool onlyLocalMedia = ServiceRegistration.Get<ISettingsManager>().Load<SeriesMetadataExtractorSettings>().OnlyLocalMedia;

      _metadata = new RelationshipExtractorMetadata(METADATAEXTRACTOR_ID, "Series relationship extractor");

      _extractors = new List<IRelationshipRoleExtractor>();

      _extractors.Add(new EpisodeSeriesRelationshipExtractor());
      _extractors.Add(new EpisodeSeasonRelationshipExtractor());
      _extractors.Add(new EpisodeActorRelationshipExtractor());
      _extractors.Add(new EpisodeDirectorRelationshipExtractor());
      _extractors.Add(new EpisodeWriterRelationshipExtractor());
      _extractors.Add(new EpisodeCharacterRelationshipExtractor());

      _extractors.Add(new SeasonSeriesRelationshipExtractor());

      _extractors.Add(new SeriesActorRelationshipExtractor());
      _extractors.Add(new SeriesCharacterRelationshipExtractor());
      _extractors.Add(new SeriesNetworkRelationshipExtractor());
      _extractors.Add(new SeriesProductionRelationshipExtractor());
      if(!onlyLocalMedia)
        _extractors.Add(new SeriesEpisodeRelationshipExtractor());
    }

    public RelationshipExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public IList<IRelationshipRoleExtractor> RoleExtractors
    {
      get { return _extractors; }
    }
  }
}
