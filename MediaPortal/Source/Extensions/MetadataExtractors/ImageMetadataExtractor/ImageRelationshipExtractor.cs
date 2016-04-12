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
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.MetadataExtractors.ImageMetadataExtractor
{
  class ImageRelationshipExtractor : IRelationshipExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the image relationship metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "647B9455-04B5-4B09-8ED8-D59FBF43B063";

    /// <summary>
    /// Series relationship metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    protected RelationshipExtractorMetadata _metadata;
    private IList<IRelationshipRoleExtractor> _extractors;

    public ImageRelationshipExtractor()
    {
      _metadata = new RelationshipExtractorMetadata(METADATAEXTRACTOR_ID, "Image relationship extractor");

      _extractors = new List<IRelationshipRoleExtractor>();

      _extractors.Add(new ImageYearCollectionRelationshipExtractor());
      _extractors.Add(new ImageDateCollectionRelationshipExtractor());
      _extractors.Add(new ImageCityCollectionRelationshipExtractor());
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
