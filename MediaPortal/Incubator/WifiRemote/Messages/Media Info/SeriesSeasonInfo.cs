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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Plugins.WifiRemote;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class SeriesSeasonInfo : IAdditionalMediaInfo
  {
    public string MediaType => "season";
    public string Id => SeasonId.ToString();
    public int MpMediaType => (int)MpMediaTypes.TVSeason;
    public int MpProviderId => (int)MpProviders.MPSeries;

    /// <summary>
    /// ID of the series in TVSeries' DB
    /// </summary>
    public Guid SeriesId { get; set; }
    /// <summary>
    /// ID of the season in TVSeries' DB
    /// </summary>
    public Guid SeasonId { get; set; }
    /// <summary>
    /// Series name
    /// </summary>
    public string Series { get; set; }
    /// <summary>
    /// Season number
    /// </summary>
    public int Season { get; set; }
    /// <summary>
    /// Season title
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// Plot summary
    /// </summary>
    public string Summary { get; set; }
    /// <summary>
    /// Season poster filepath
    /// </summary>
    public string ImageName { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public SeriesSeasonInfo(MediaItem mediaItem)
    {
      try
      {
        SeriesId = Guid.Empty;
        SeasonId = mediaItem.MediaItemId;
        if (mediaItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
        {
          if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, RelationshipAspect.Metadata, out IList<MultipleMediaItemAspect> relationAspects))
          {
            foreach (MultipleMediaItemAspect relation in relationAspects)
            {
              if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == SeriesAspect.ROLE_SERIES)
                SeriesId = (Guid)relation[RelationshipAspect.ATTR_LINKED_ID];
            }
          }
        }

        SeasonInfo season = new SeasonInfo();
        season.FromMetadata(mediaItem.Aspects);

        Season = season.SeasonNumber.Value;
        Summary = season.Description.Text;
        Title = $"Season {season.SeasonNumber}";
        Series = season.SeriesName.Text;
        ImageName = Helper.GetImageBaseURL(mediaItem, FanArtMediaTypes.SeriesSeason, FanArtTypes.Thumbnail);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WifiRemote: Error getting season info", e);
      }
    }
  }
}
