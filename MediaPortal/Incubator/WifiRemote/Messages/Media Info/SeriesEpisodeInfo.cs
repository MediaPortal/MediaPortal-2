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
  internal class SeriesEpisodeInfo : IAdditionalMediaInfo
  {
    public string MediaType => "episode";
    public string MpExtId => EpisodeId.ToString();
    public int MpExtMediaType => (int)MpExtendedMediaTypes.TVEpisode; 
    public int MpExtProviderId => (int)MpExtendedProviders.MPTvSeries;

    private Guid seriesId;

    /// <summary>
    /// ID of the series in TVSeries' DB
    /// </summary>
    public Guid SeriesId { get; set; }
    /// <summary>
    /// ID of the season in TVSeries' DB
    /// </summary>
    public Guid SeasonId { get; set; }
    /// <summary>
    /// ID of the episode in TVSeries' DB
    /// </summary>
    public Guid EpisodeId { get; set; }
    /// <summary>
    /// Series name
    /// </summary>
    public string Series { get; set; }
    /// <summary>
    /// Episode number
    /// </summary>
    public int Episode { get; set; }
    /// <summary>
    /// Season number
    /// </summary>
    public int Season { get; set; }
    /// <summary>
    /// Plot summary
    /// </summary>
    public string Plot { get; set; }
    /// <summary>
    /// Episode title
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// Director of this episode
    /// </summary>
    public string Director { get; set; }
    /// <summary>
    /// Writer of this episode
    /// </summary>
    public string Writer { get; set; }
    /// <summary>
    /// Online episode rating
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
    /// Episode air date
    /// </summary>
    public string AirDate { get; set; }
    /// <summary>
    /// Genre of the series
    /// </summary>
    public string Genre { get; set; }
     /// <summary>
    /// Season poster filepath
    /// </summary>
    public string ImageName { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="filename">Filename of the currently played episode</param>
    public SeriesEpisodeInfo(MediaItem mediaItem)
    {
      try
      {
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
        Rating = Convert.ToString(episode.Rating.RatingValue ?? 0);
        RatingCount = Convert.ToString(episode.Rating.VoteCount ?? 0);
        Series = episode.SeriesName.Text;
        ImageName = Helper.GetImageBaseURL(mediaItem, FanArtMediaTypes.Episode, FanArtTypes.Thumbnail);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WifiRemote: Error getting episode info", e);
      }
    }
  }
}
