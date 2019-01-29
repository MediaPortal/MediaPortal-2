#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using System;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{
  class BaseEpisodeDetailed : BaseEpisodeBasic
  {
    internal static ISet<Guid> DetailedNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID,
      EpisodeAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID
    };

    internal static ISet<Guid> DetailedOptionalMIATypeIds = new HashSet<Guid>
    {
      RelationshipAspect.ASPECT_ID,
      ExternalIdentifierAspect.ASPECT_ID
    };

    internal static WebTVEpisodeDetailed EpisodeDetailed(MediaItem item, Guid? showId = null, Guid? seasonId = null)
    {
      WebTVEpisodeBasic episodeBasic = EpisodeBasic(item, showId, seasonId);
      MediaItemAspect episodeAspect = MediaItemAspect.GetAspect(item.Aspects, EpisodeAspect.Metadata);
      MediaItemAspect videoAspect = MediaItemAspect.GetAspect(item.Aspects, VideoAspect.Metadata);

      var writers = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_WRITERS)?.Distinct().ToList() ?? new List<string>();
      var directors = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_DIRECTORS)?.Distinct().ToList() ?? new List<string>();

      WebTVEpisodeDetailed webTvEpisodeDetailed = new WebTVEpisodeDetailed
      {
        EpisodeNumber = episodeBasic.EpisodeNumber,
        ExternalId = episodeBasic.ExternalId,
        FirstAired = episodeBasic.FirstAired,
        IsProtected = episodeBasic.IsProtected,
        Rating = episodeBasic.Rating,
        SeasonNumber = episodeBasic.SeasonNumber,
        ShowId = episodeBasic.ShowId,
        SeasonId = episodeBasic.SeasonId,
        Type = episodeBasic.Type,
        Watched = episodeBasic.Watched,
        Path = episodeBasic.Path,
        DateAdded = episodeBasic.DateAdded,
        Id = episodeBasic.Id,
        PID = episodeBasic.PID,
        Title = episodeBasic.Title,
        Artwork = episodeBasic.Artwork,
        Show = episodeAspect.GetAttributeValue<string>(EpisodeAspect.ATTR_SERIES_NAME),
        Summary = videoAspect.GetAttributeValue<string>(VideoAspect.ATTR_STORYPLOT),
        Writers = writers,
        Directors = directors
      };

      return webTvEpisodeDetailed;
    }
  }
}
