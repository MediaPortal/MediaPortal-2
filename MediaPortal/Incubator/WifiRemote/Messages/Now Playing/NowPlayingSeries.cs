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
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class NowPlayingSeries : IAdditionalNowPlayingInfo
  {
    private bool episodeFound = false;

    private string mediaType = "series";

    public string MediaType
    {
      get { return mediaType; }
    }

    public string MpExtId
    {
      get { return CompositeId ?? String.Empty; }
    }

    public int MpExtMediaType
    {
      get { return (int)MpExtendedMediaTypes.TVEpisode; }
    }

    public int MpExtProviderId
    {
      get { return (int)MpExtendedProviders.MPTvSeries; }
    }

    private Guid seriesId;

    /// <summary>
    /// ID of the series in TVSeries' DB
    /// </summary>
    public Guid SeriesId
    {
      get { return seriesId; }
      set { seriesId = value; }
    }

    private Guid seasonId;

    /// <summary>
    /// ID of the season in TVSeries' DB
    /// </summary>
    public Guid SeasonId
    {
      get { return seasonId; }
      set { seasonId = value; }
    }

    private Guid episodeId;

    /// <summary>
    /// ID of the episode in TVSeries' DB
    /// </summary>
    public Guid EpisodeId
    {
      get { return episodeId; }
      set { episodeId = value; }
    }

    private string compositeId;

    /// <summary>
    /// Composite ID of the episode in TVSeries' DB
    /// </summary>
    public string CompositeId
    {
      get { return compositeId; }
      set { compositeId = value; }
    }

    private string series;

    /// <summary>
    /// Series name
    /// </summary>
    public string Series
    {
      get { return series; }
      set { series = value; }
    }

    private int episode;

    /// <summary>
    /// Episode number
    /// </summary>
    public int Episode
    {
      get { return episode; }
      set { episode = value; }
    }

    private int season;

    /// <summary>
    /// Season number
    /// </summary>
    public int Season
    {
      get { return season; }
      set { season = value; }
    }

    private string plot;

    /// <summary>
    /// Plot summary
    /// </summary>
    public string Plot
    {
      get { return plot; }
      set { plot = value; }
    }

    private string title;

    /// <summary>
    /// Episode title
    /// </summary>
    public string Title
    {
      get { return title; }
      set { title = value; }
    }

    private string director;

    /// <summary>
    /// Director of this episode
    /// </summary>
    public string Director
    {
      get { return director; }
      set { director = value; }
    }

    private string writer;

    /// <summary>
    /// Writer of this episode
    /// </summary>
    public string Writer
    {
      get { return writer; }
      set { writer = value; }
    }

    private string rating;

    /// <summary>
    /// Online episode rating
    /// </summary>
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

    private string myRating;

    /// <summary>
    /// My episode rating
    /// </summary>
    public string MyRating
    {
      get { return myRating; }
      set { myRating = value; }
    }

    private string ratingCount;

    /// <summary>
    /// Number of online votes
    /// </summary>
    public string RatingCount
    {
      get { return ratingCount; }
      set { ratingCount = value; }
    }

    private string airDate;

    /// <summary>
    /// Episode air date
    /// </summary>
    public string AirDate
    {
      get { return airDate; }
      set { airDate = value; }
    }

    private string status;

    /// <summary>
    /// Status of the series
    /// </summary>
    public string Status
    {
      get { return status; }
      set { status = value; }
    }

    private string genre;

    /// <summary>
    /// Genre of the series
    /// </summary>
    public string Genre
    {
      get { return genre; }
      set { genre = value; }
    }

    private string imageName;

    /// <summary>
    /// Season poster filepath
    /// </summary>
    public string ImageName
    {
      get { return imageName; }
      set { imageName = value; }
    }


    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="filename">Filename of the currently played episode</param>
    public NowPlayingSeries(MediaItem mediaItem)
    {
      try
      {
        episodeFound = true;

        SeriesId = Guid.Empty;
        SeasonId = Guid.Empty;
        if (mediaItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
        {
          if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, RelationshipAspect.Metadata, out IList<MultipleMediaItemAspect> relationAspects))
          {
            foreach (MultipleMediaItemAspect relation in relationAspects)
            {
              if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == SeriesAspect.ROLE_SERIES)
                SeriesId = (Guid)relation[RelationshipAspect.ATTR_LINKED_ID];
              if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == SeasonAspect.ROLE_SEASON)
                SeasonId = (Guid)relation[RelationshipAspect.ATTR_LINKED_ID];
            }
          }
        }

        //CompositeId = episodes[0].fullItem[DBEpisode.cCompositeID];

        EpisodeInfo episode = new EpisodeInfo();
        episode.FromMetadata(mediaItem.Aspects);

        Episode = episode.EpisodeNumbers.First();
        Season = episode.SeasonNumber.Value;
        Plot = episode.Summary.Text;
        Title = episode.EpisodeName.Text;
        Director = String.Join(", ", episode.Directors);
        Writer = String.Join(", ", episode.Writers);
        Genre = String.Join(", ", episode.Genres.Select(g => g.Name));
        AirDate = episode.FirstAired.HasValue ? episode.FirstAired.Value.ToLongDateString() : "";
        MyRating = Convert.ToString(episode.Rating.RatingValue ?? 0);
        Series = episode.SeriesName.Text;
        //Status = s[DBOnlineSeries.cStatus];
        ImageName = Helper.GetImageBaseURL(mediaItem, FanArtMediaTypes.Episode, FanArtTypes.Thumbnail);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error getting now playing tvseries: " + e.Message);
      }
    }

    /// <summary>
    /// Is this file a tv series episode?
    /// </summary>
    /// <returns><code>true</code> if the file is a tv series episode</returns>
    public bool IsEpisode()
    {
      return episodeFound;
    }
  }
}
