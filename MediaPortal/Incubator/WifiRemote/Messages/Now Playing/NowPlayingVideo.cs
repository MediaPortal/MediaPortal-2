using System;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;

namespace WifiRemote
{
  public class NowPlayingVideo : IAdditionalNowPlayingInfo
  {
    private string mediaType = "video";

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
      get { return (int)MpExtendedProviders.MPVideo; }
    }

    private Guid itemId;

    /// <summary>
    /// ID of the movie in MyMovie's DB
    /// </summary>
    public Guid ItemId
    {
      get { return itemId; }
      set { itemId = value; }
    }

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

    private string imageUrl;

    /// <summary>
    /// Movie poster
    /// </summary>
    public string ImageUrl
    {
      get { return imageUrl; }
      set { imageUrl = value; }
    }


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="aMovie">The currently playing movie</param>
    public NowPlayingVideo(MediaItem aMovie)
    {
      var movieAspect = aMovie[VideoAspect.Metadata];
      ItemId = aMovie.MediaItemId;
      Summary = (string)movieAspect[VideoAspect.ATTR_STORYPLOT];
      Title = (string)aMovie[MediaAspect.Metadata][MediaAspect.ATTR_TITLE];
      Tagline = String.Empty;
      Directors = (string)movieAspect[VideoAspect.ATTR_DIRECTORS];
      Writers = (string)movieAspect[VideoAspect.ATTR_WRITERS];
      Actors = (string)movieAspect[VideoAspect.ATTR_ACTORS];
      Rating = String.Empty;
      Year = 0;
      Genres = (string)movieAspect[VideoAspect.ATTR_GENRES];
      Certification = String.Empty;

      //ImageUrl = aMovie.ThumbURL;
    }
  }
}