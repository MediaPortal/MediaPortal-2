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
using System.Globalization;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="ImageCollectionInfo"/> contains metadata information about a image collection item.
  /// </summary>
  public class ImageCollectionInfo : BaseInfo
  {
    /// <summary>
    /// Gets or sets the collection name.
    /// </summary>
    public string Name = null;
    public string CollectionType = null;
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

      if (CollectionType == ImageCollectionAspect.TYPE_YEAR)
        MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_YEAR, ExternalIdentifierAspect.TYPE_COLLECTION, CollectionDate.Value.Year.ToString(CultureInfo.InvariantCulture));
      else if (CollectionType == ImageCollectionAspect.TYPE_DATE)
        MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_DATE, ExternalIdentifierAspect.TYPE_COLLECTION, CollectionDate.Value.Date.ToString(CultureInfo.InvariantCulture));
      else if (CollectionType == ImageCollectionAspect.TYPE_CITY)
        MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_COLLECTION, Name);

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!aspectData.ContainsKey(ImageCollectionAspect.ASPECT_ID))
        return false;

      MediaItemAspect.TryGetAttribute(aspectData, ImageCollectionAspect.ATTR_COLLECTION_NAME, out Name);
      MediaItemAspect.TryGetAttribute(aspectData, ImageCollectionAspect.ATTR_COLLECTION_DATE, out CollectionDate);
      MediaItemAspect.TryGetAttribute(aspectData, ImageCollectionAspect.ATTR_LATITUDE, out Latitude);
      MediaItemAspect.TryGetAttribute(aspectData, ImageCollectionAspect.ATTR_LONGITUDE, out Longitude);
      MediaItemAspect.TryGetAttribute(aspectData, ImageCollectionAspect.ATTR_COLLECTION_DATE, out CollectionDate);

      byte[] data;
      if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
        Thumbnail = data;

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
