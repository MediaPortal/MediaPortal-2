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
using System.Collections;
using System.Drawing;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class EpisodeSeasonRelationshipExtractor : IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { EpisodeAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { SeasonAspect.ASPECT_ID };

    public Guid Role
    {
      get { return EpisodeAspect.ROLE_EPISODE; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return SeasonAspect.ROLE_SEASON; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects, bool forceQuickMode)
    {
      extractedLinkedAspects = null;

      int seasonNum;
      if (!MediaItemAspect.TryGetAttribute(aspects, EpisodeAspect.ATTR_SEASON, out seasonNum))
        return false;

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

      // Build the season MI

      IDictionary<Guid, IList<MediaItemAspect>> seasonAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      extractedLinkedAspects.Add(seasonAspects);

      MediaItemAspect.SetAttribute(seasonAspects, MediaAspect.ATTR_TITLE, string.Format("{0} S{1}", seriesDetail.SeriesName, seasonNum));
      MediaItemAspect.SetAttribute(seasonAspects, SeasonAspect.ATTR_SERIESNAME, seriesDetail.SeriesName);
      MediaItemAspect.SetAttribute(seasonAspects, SeasonAspect.ATTR_SERIES_SEASON, string.Format("{0} S{1}", seriesDetail.SeriesName, seasonNum));
      MediaItemAspect.SetAttribute(seasonAspects, SeasonAspect.ATTR_SEASON, seasonNum);
      MediaItemAspect.AddOrUpdateExternalIdentifier(seasonAspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, id);
      MediaItemAspect.AddOrUpdateExternalIdentifier(seasonAspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, seriesDetail.ImdbId);

      foreach (TvdbSeasonBanner banner in seriesDetail.SeasonBanners)
      {
        if (banner.Season == seasonNum)
        {
          if (banner.LoadBanner())
          {
            ImageConverter converter = new ImageConverter();
            MediaItemAspect.SetAttribute(seasonAspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, converter.ConvertTo(banner.BannerImage, typeof(byte[])));
            banner.UnloadBanner();
            break;
          }
        }
      }

      // TODO: Build the episode MIs?
      seriesDetail.GetEpisodes(seasonNum);

      return true;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      int linkedSeasonNum;
      if (!MediaItemAspect.TryGetAttribute(linkedAspects, EpisodeAspect.ATTR_SEASON, out linkedSeasonNum))
        return false;

      int existingSeasonNum;
      if (!MediaItemAspect.TryGetAttribute(existingAspects, SeasonAspect.ATTR_SEASON, out existingSeasonNum))
        return false;

      return linkedSeasonNum == existingSeasonNum;
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect aspect;
      if (!MediaItemAspect.TryGetAspect(aspects, EpisodeAspect.Metadata, out aspect))
        return false;

      IEnumerable<object> indexes = aspect.GetCollectionAttribute<object>(EpisodeAspect.ATTR_EPISODE);
      if (indexes == null)
        return false;

      IList<object> episodeNums = indexes.ToList();
      Logger.Info("Getting first index from [{0}]", string.Join(",", episodeNums));
      if (episodeNums.Count == 0)
        return false;

      index = Int32.Parse(episodeNums.First().ToString());
      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
