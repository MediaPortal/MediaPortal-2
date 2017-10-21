using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;

namespace WifiRemote
{
  internal class NowPlayingMovingPictures : IAdditionalNowPlayingInfo
  {
    private bool movieFound = false;

    private string mediaType = "movie";

    public string MediaType
    {
      get { return mediaType; }
    }

    public string MpExtId
    {
      get { return ItemId.ToString(); }
    }

    public int MpExtMediaType
    {
      get { return (int)MpExtendedMediaTypes.Movie; }
    }

    public int MpExtProviderId
    {
      get { return (int)MpExtendedProviders.MovingPictures; }
    }

    /// <summary>
    /// Movie ID in moving pictures database table "movie_info"
    /// </summary>
    public Guid ItemId { get; set; }

    private string summary;

    /// <summary>
    /// Plot summary
    /// </summary>
    public string Summary
    {
      get { return summary; }
      set { summary = value; }
    }

    private string title;

    /// <summary>
    /// Movie title
    /// </summary>
    public string Title
    {
      get { return title; }
      set { title = value; }
    }

    private string alternateTitles;

    /// <summary>
    /// Alternate titles of this movie
    /// </summary>
    public string AlternateTitles
    {
      get { return alternateTitles; }
      set { alternateTitles = value; }
    }

    private string tagline;

    /// <summary>
    /// Tagline of the movie
    /// </summary>
    public string Tagline
    {
      get { return tagline; }
      set { tagline = value; }
    }

    private string directors;

    /// <summary>
    /// Director of this movie
    /// </summary>
    public string Directors
    {
      get { return directors; }
      set { directors = value; }
    }

    private string writers;

    /// <summary>
    /// Writer of this movie
    /// </summary>
    public string Writers
    {
      get { return writers; }
      set { writers = value; }
    }

    private string actors;

    /// <summary>
    /// Actors in this movie
    /// </summary>
    public string Actors
    {
      get { return actors; }
      set { actors = value; }
    }


    private string rating;

    /// <summary>
    /// Online rating
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

    private int year;

    /// <summary>
    /// Movie air date
    /// </summary>
    public int Year
    {
      get { return year; }
      set { year = value; }
    }

    private string genres;

    /// <summary>
    /// Genres of the movie
    /// </summary>
    public string Genres
    {
      get { return genres; }
      set { genres = value; }
    }

    private string certification;

    /// <summary>
    /// Certification of the movie
    /// </summary>
    public string Certification
    {
      get { return certification; }
      set { certification = value; }
    }

    private string detailsUrl;

    /// <summary>
    /// Get more info about the movie at this URL
    /// </summary>
    public string DetailsUrl
    {
      get { return detailsUrl; }
      set { detailsUrl = value; }
    }

    private string imageName;

    /// <summary>
    /// Movie poster filepath
    /// </summary>
    public string ImageName
    {
      get { return imageName; }
      set { imageName = value; }
    }


    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="filename">Filename of the currently played media file</param>
    public NowPlayingMovingPictures(MediaItem mediaItem)
    {
      try
      {
        movieFound = true;

        ItemId = mediaItem.MediaItemId;
        Title = (string)mediaItem[MovieAspect.Metadata][MovieAspect.ATTR_MOVIE_NAME];
        //AlternateTitles = match.AlternateTitles.ToString();
        var videoDirectors = (List<string>)mediaItem[VideoAspect.Metadata][VideoAspect.ATTR_DIRECTORS];
        if (videoDirectors != null)
          Directors = String.Join(", ", videoDirectors.Cast<string>().ToArray());

        var videoWriters = (List<string>)mediaItem[VideoAspect.Metadata][VideoAspect.ATTR_WRITERS];
        if (videoWriters != null)
          Writers = String.Join(", ", videoWriters.Cast<string>().ToArray());

        var videoActors = (List<string>)mediaItem[VideoAspect.Metadata][VideoAspect.ATTR_ACTORS];
        if (videoActors != null)
          Actors = String.Join(", ", videoActors.Cast<string>().ToArray());

        var videoGenres = (List<string>)mediaItem[VideoAspect.Metadata][VideoAspect.ATTR_GENRES];
        if (videoGenres != null)
          Genres = String.Join(", ", videoGenres.Cast<string>().ToArray());

        Rating = Convert.ToString((double)mediaItem[MovieAspect.Metadata][MovieAspect.ATTR_TOTAL_RATING]);

        /*Year = (int)mediaItem[VideoAspect.Metadata][VideoAspect.];
        Certification = match.Certification;
        Tagline = match.Tagline;*/
        Summary = (string)mediaItem[VideoAspect.Metadata][VideoAspect.ATTR_STORYPLOT];
        /*DetailsUrl = match.DetailsURL;
        ImageName = match.CoverFullPath;*/
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error getting now playing moving pictures: " + e.Message);
      }
    }

    /// <summary>
    /// Checks if the supplied filename is a moving pictures movie
    /// </summary>
    /// <returns></returns>
    public bool IsMovingPicturesMovie()
    {
      return movieFound;
    }
  }
}