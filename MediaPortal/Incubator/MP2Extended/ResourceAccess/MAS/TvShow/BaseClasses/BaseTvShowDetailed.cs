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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using Microsoft.Owin;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{

  // TODO: Add more detailes
  class BaseTvShowDetailed : BaseTvShowBasic
  {
    internal WebTVShowDetailed TVShowDetailed(IOwinContext context, MediaItem item, MediaItem showItem = null)
    {
      var seriesAspect = item.GetAspect(SeriesAspect.Metadata);
      var tvShowBasic = TVShowBasic(context, item);

      return new WebTVShowDetailed()
      {
        Summary = seriesAspect.GetAttributeValue<string>(SeriesAspect.ATTR_DESCRIPTION),
        // From TvShowBasic
        Id = tvShowBasic.Id,
        Title = tvShowBasic.Title,
        DateAdded = tvShowBasic.DateAdded,
        EpisodeCount = tvShowBasic.EpisodeCount,
        UnwatchedEpisodeCount = tvShowBasic.UnwatchedEpisodeCount,
        PID = tvShowBasic.PID,
        Genres = tvShowBasic.Genres,
        Actors = tvShowBasic.Actors,
        Artwork = tvShowBasic.Artwork,
        ContentRating = tvShowBasic.ContentRating,
        ExternalId = tvShowBasic.ExternalId,
        IsProtected = tvShowBasic.IsProtected,
        Rating = tvShowBasic.Rating,
        SeasonCount = tvShowBasic.SeasonCount,
        Year = tvShowBasic.Year
      };
    }
  }
}
