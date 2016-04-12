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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="ImageCollectionInfo"/> contains metadata information about a image collection item.
  /// </summary>
  public class ImageCollectionInfo
  {
    private const string COORD_FORMAT = "{0:0.00}x{1:0.00}";

    /// <summary>
    /// Gets or sets the collection name.
    /// </summary>
    public string Name = null;
    public ImageCollectionType CollectionType = ImageCollectionType.Year;
    public DateTime? CollectionDate;
    public double? Longitude;
    public double? Latitude;

    #region Members

    /// <summary>
    /// Copies the contained character information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Name)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, ImageCollectionAspect.ATTR_COLLECTION_NAME, Name);
      if (CollectionDate.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, CollectionDate.Value);
      if (CollectionDate.HasValue) MediaItemAspect.SetAttribute(aspectData, ImageCollectionAspect.ATTR_COLLECTION_DATE, CollectionDate.Value);
      if (Latitude.HasValue) MediaItemAspect.SetAttribute(aspectData, ImageCollectionAspect.ATTR_LATITUDE, Latitude.Value);
      if (Longitude.HasValue) MediaItemAspect.SetAttribute(aspectData, ImageCollectionAspect.ATTR_LONGITUDE, Longitude.Value);

      if (CollectionType == ImageCollectionType.Year)
        MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_YEAR, ExternalIdentifierAspect.TYPE_COLLECTION, CollectionDate.Value.Year.ToString());
      else if (CollectionType == ImageCollectionType.Date)
        MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_DATE, ExternalIdentifierAspect.TYPE_COLLECTION, CollectionDate.Value.Date.ToString());
      else if (CollectionType == ImageCollectionType.City)
        MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_LATITUDE_LONGITUDE, ExternalIdentifierAspect.TYPE_COLLECTION, 
          string.Format(COORD_FORMAT, Latitude.Value, Longitude.Value));

      return true;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return Name;
    }

    #endregion
  }
}
