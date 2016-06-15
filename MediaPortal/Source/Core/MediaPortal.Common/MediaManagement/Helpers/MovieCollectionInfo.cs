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
  /// <see cref="MovieCollectionInfo"/> contains metadata information about a movie collection item.
  /// </summary>
  public class MovieCollectionInfo : BaseInfo
  {
    /// <summary>
    /// Gets or sets the character TheMovieDb id.
    /// </summary>
    public int MovieDbId = 0;
    /// <summary>
    /// Gets or sets the collection name.
    /// </summary>
    public LanguageText CollectionName = null;
    public List<MovieInfo> Movies = new List<MovieInfo>();

    #region Members

    /// <summary>
    /// Copies the contained character information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (CollectionName.IsEmpty) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, MovieCollectionAspect.ATTR_COLLECTION_NAME, CollectionName.Text);

      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COLLECTION, MovieDbId.ToString());

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (aspectData.ContainsKey(MovieCollectionAspect.ASPECT_ID))
      {
        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, MovieCollectionAspect.ATTR_COLLECTION_NAME, out tempString);
        CollectionName = new LanguageText(tempString, false);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COLLECTION, out id))
          MovieDbId = Convert.ToInt32(id);

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          Thumbnail = data;

        return true;
      }
      else if (aspectData.ContainsKey(MovieAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_COLLECTION_NAME, out CollectionName);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COLLECTION, out id))
          MovieDbId = Convert.ToInt32(id);

        return true;
      }
      return false;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return CollectionName.Text;
    }

    #endregion
  }
}
