using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UI.ServerCommunication;

namespace WifiRemote
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

    private string seasonId;

    /// <summary>
    /// ID of the season in TVSeries' DB
    /// </summary>
    public string SeasonId
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

        ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
        necessaryMIATypes.Add(MediaAspect.ASPECT_ID);

        // show
        IFilter searchFilter = new RelationalFilter(MediaAspect.ATTR_TITLE, RelationalOperator.EQ, (string)mediaItem.Aspects[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_SERIESNAME]);
        MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, null, searchFilter);

        IList<MediaItem> show = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory.Search(searchQuery, false);
        Guid showId = Guid.Empty;
        if (show.Count > 0)
          showId = show[0].MediaItemId;

        SeriesId = showId;
        SeasonId = String.Format("{0}:{1}", showId, (int)mediaItem.Aspects[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_SEASON]);
        EpisodeId = mediaItem.MediaItemId;
        //CompositeId = episodes[0].fullItem[DBEpisode.cCompositeID];

        var episodeNumber = (List<int>)mediaItem[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_EPISODE];
        Episode = episodeNumber[0];
        Season = (int)mediaItem.Aspects[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_SEASON];
        Plot = (string)mediaItem.Aspects[VideoAspect.ASPECT_ID][VideoAspect.ATTR_STORYPLOT];
        Title = (string)mediaItem.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];
        var videoDirectors = (List<string>)mediaItem[VideoAspect.ASPECT_ID][VideoAspect.ATTR_DIRECTORS];
        if (videoDirectors != null)
          Director = String.Join(", ", videoDirectors.Cast<string>().ToArray());

        var videoWriters = (List<string>)mediaItem[VideoAspect.ASPECT_ID][VideoAspect.ATTR_WRITERS];
        if (videoWriters != null)
          Writer = String.Join(", ", videoWriters.Cast<string>().ToArray());

        var videoGenres = (List<string>)mediaItem[VideoAspect.ASPECT_ID][VideoAspect.ATTR_GENRES];
        if (videoGenres != null)
          Genre = String.Join(", ", videoGenres.Cast<string>().ToArray());

        var firstAired = mediaItem.Aspects[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_FIRSTAIRED];
        if (firstAired != null)
          AirDate = ((DateTime)mediaItem.Aspects[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_FIRSTAIRED]).ToLongDateString();

        MyRating = Convert.ToString((double)mediaItem.Aspects[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_TOTAL_RATING]);

        //DBSeries s = Helper.getCorrespondingSeries(episodes[0].onlineEpisode[DBOnlineEpisode.cSeriesID]);
        Series = (string)mediaItem.Aspects[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_SERIESNAME];
        //Status = s[DBOnlineSeries.cStatus];

        // Get season poster path
        //DBSeason season = DBSeason.getRaw(SeriesId, episodes[0].onlineEpisode[DBOnlineEpisode.cSeasonIndex]);
        //ImageName = ImageAllocator.GetSeasonBannerAsFilename(season);

        // Fall back to series poster if no season poster is available
        if (String.IsNullOrEmpty(ImageName))
        {
          //ImageName = ImageAllocator.GetSeriesPosterAsFilename(s);
        }
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