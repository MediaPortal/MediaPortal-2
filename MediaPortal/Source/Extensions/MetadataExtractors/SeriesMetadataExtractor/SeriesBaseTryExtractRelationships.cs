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
using System.Drawing;
using System.Globalization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeriesBaseTryExtractRelationships
  {
    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects, bool forceQuickMode)
    {
      extractedLinkedAspects = null;

      string id;
      if (!MediaItemAspect.TryGetExternalAttribute(aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
        return false;

      int tvdbId;
      if (!Int32.TryParse(id, NumberStyles.None, null, out tvdbId))
        return false;

      TvdbSeries seriesDetail;

      if (!SeriesTvDbMatcher.Instance.TryGetSeries(tvdbId, out seriesDetail))
        return false;

      extractedLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();

      // Build the series MI

      IDictionary<Guid, IList<MediaItemAspect>> seriesAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      extractedLinkedAspects.Add(seriesAspects);

      MediaItemAspect.SetAttribute(seriesAspects, SeriesAspect.ATTR_SERIESNAME, seriesDetail.SeriesName);
      MediaItemAspect.SetAttribute(seriesAspects, SeriesAspect.ATTR_DESCRIPTION, seriesDetail.Overview);
      MediaItemAspect.SetAttribute(seriesAspects, MediaAspect.ATTR_TITLE, seriesDetail.SeriesName);
      MediaItemAspect.AddOrUpdateExternalIdentifier(seriesAspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, id);
      MediaItemAspect.AddOrUpdateExternalIdentifier(seriesAspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, seriesDetail.ImdbId);

      foreach (TvdbBanner banner in seriesDetail.Banners)
      {
        if (banner.LoadBanner())
        {
          ImageConverter converter = new ImageConverter();
          MediaItemAspect.SetAttribute(seriesAspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, converter.ConvertTo(banner.BannerImage, typeof(byte[])));
          banner.UnloadBanner();
          break;
        }
      }

      // TODO: Build the other series MIs?

      return true;
    }
  }
}
