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
  internal class SeriesShowInfo : IAdditionalMediaInfo
  {
    public string MediaType => "series";
    public string Id => SeriesId.ToString();
    public int MpMediaType => (int)MpMediaTypes.TVShow;
    public int MpProviderId => (int)MpProviders.MPSeries;

    /// <summary>
    /// ID of the series in TVSeries' DB
    /// </summary>
    public Guid SeriesId { get; set; }
    /// <summary>
    /// Plot summary
    /// </summary>
    public string Summary { get; set; }
    /// <summary>
    /// Series title
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// Actors in this series
    /// </summary>
    public string Actors { get; set; }
    /// <summary>
    /// Online series rating
    /// </summary>
    private string rating;
    public string Rating
    {
      get { return rating; }
      set
      {
        // Shorten to 3 chars, ie
        // 5.67676767 to 5.6
        if (value.Length > 3)
        {
          value = value.Remove(3);
        }
        rating = value;
      }
    }
    /// <summary>
    /// Number of online votes
    /// </summary>
    public string RatingCount { get; set; }
    /// <summary>
    /// Series air date
    /// </summary>
    public string AirDate { get; set; }
    /// <summary>
    /// Status of the series
    /// </summary>
    public string Status { get; set; }
    /// <summary>
    /// Genre of the series
    /// </summary>
    public string Genre { get; set; }
    /// <summary>
    /// Certification of the movie
    /// </summary>
    public string Certification { get; set; }
    /// <summary>
    /// Series poster filepath
    /// </summary>
    public string ImageName { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public SeriesShowInfo(MediaItem mediaItem)
    {
      try
      {
        SeriesId = mediaItem.MediaItemId;

        SeriesInfo series = new SeriesInfo();
        series.FromMetadata(mediaItem.Aspects);

        Summary = series.Description.Text;
        Title = series.SeriesName.Text;
        Actors = String.Join(", ", series.Actors);
        Genre = String.Join(", ", series.Genres.Select(g => g.Name));
        AirDate = series.FirstAired.HasValue ? series.FirstAired.Value.ToLongDateString() : "";
        Rating = Convert.ToString(series.Rating.RatingValue ?? 0);
        RatingCount = Convert.ToString(series.Rating.VoteCount ?? 0);
        Status = series.IsEnded ? "Ended" : "Running";
        Certification = series.Certification;
        ImageName = Helper.GetImageBaseURL(mediaItem, FanArtMediaTypes.Series, FanArtTypes.Poster);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WifiRemote: Error getting series info", e);
      }
    }
  }
}
