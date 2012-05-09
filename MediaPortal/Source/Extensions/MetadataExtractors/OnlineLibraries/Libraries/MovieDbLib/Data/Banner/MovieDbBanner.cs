using System;
using System.Collections.Generic;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Cache;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner
{
  public abstract class MovieDbBanner
  {
    #region private/protected fields

    private ICacheProvider _cacheProvider;
    private BannerSize _originalSize;
    private BannerSize _thumbSize;

    #endregion


    #region factory methods

    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectId">id of the object of this banner (Person, movie,...)</param>
    /// <param name="bannerId">id of banner</param>
    /// <param name="images">list of image sizes available for this banner</param>
    /// <returns></returns>
    internal static MovieDbBanner CreateBanner(int objectId, string bannerId, List<string[]> images)
    {
      //get type of banner (Poster or Backdrop for now)
      switch (images[0][0])
      {
        case "Poster":
          return CreatePoster(objectId, bannerId, images);
        case "Backdrop":
          return CreateBackdrop(objectId, bannerId, images);
        case "profile":
          return CreateProfile(objectId, bannerId, images);
      }
      return null;
    }

    private static MovieDbBanner CreateProfile(int personId, string bannerId, List<string[]> images)
    {
      MovieDbPersonImage image = new MovieDbPersonImage();
      image.Id = bannerId;
      image.PersonId = personId;
      foreach (string[] i in images)
      {
        switch (i[1].ToLowerInvariant())
        {
          case "original":
            image.Original = new BannerSize(null, personId, bannerId, BannerTypes.Person, BannerSizes.Original, i[2]);
            break;
          case "thumb":
            image.Thumbnail = new BannerSize(null, personId, bannerId, BannerTypes.Person, BannerSizes.Thumb, i[2]);
            break;
          case "profile":
            image.Profile = new BannerSize(null, personId, bannerId, BannerTypes.Person, BannerSizes.Profile, i[2]);
            break;
          default:
            Log.Warn("Parsing the unknown image size \"" + i[1] + "\"");
            break;
        }
      }

      return image;
    }


    private static MovieDbBanner CreatePoster(int movieId, string bannerId, List<string[]> images)
    {
      MovieDbPoster banner = new MovieDbPoster();
      banner.Id = bannerId;
      banner.ObjectId = movieId;
      foreach (string[] i in images)
      {
        switch (i[1].ToLowerInvariant())
        {
          case "original":
            banner.Original = new BannerSize(null, movieId, bannerId, BannerTypes.Poster, BannerSizes.Original, i[2]);
            break;
          case "thumb":
            banner.Thumbnail = new BannerSize(null, movieId, bannerId, BannerTypes.Poster, BannerSizes.Thumb, i[2]);
            break;
          case "mid":
            banner.Mid = new BannerSize(null, movieId, bannerId, BannerTypes.Poster, BannerSizes.Mid, i[2]);
            break;
          case "cover":
            banner.Cover = new BannerSize(null, movieId, bannerId, BannerTypes.Poster, BannerSizes.Cover, i[2]);
            break;
          default:
            Log.Warn("Parsing the unknown image size \"" + i[1] + "\"");
            break;
        }
      }

      return banner;
    }

    private static MovieDbBanner CreateBackdrop(int movieId, string bannerId, List<string[]> images)
    {
      MovieDbBackdrop banner = new MovieDbBackdrop();
      banner.Id = bannerId;
      banner.ObjectId = movieId;
      foreach (string[] i in images)
      {
        switch (i[1].ToLowerInvariant())
        {
          case "original":
            banner.Original = new BannerSize(null, movieId, bannerId, BannerTypes.Backdrop, BannerSizes.Original, i[2]);
            break;
          case "thumb":
            banner.Thumbnail = new BannerSize(null, movieId, bannerId, BannerTypes.Backdrop, BannerSizes.Thumb, i[2]);
            break;
          case "poster":
            banner.Poster = new BannerSize(null, movieId, bannerId, BannerTypes.Backdrop, BannerSizes.Poster, i[2]);
            break;
          default:
            Log.Warn("Parsing the unknown image size \"" + i[1] + "\"");
            break;
        }
      }
      return banner;
    }
    #endregion

    public MovieDbBanner()
    {
      ImageSizes = new Dictionary<BannerSizes, BannerSize>();
    }

    public enum BannerTypes
    {
      Poster = 0,
      Backdrop = 1,
      Person = 2
    };

    public enum BannerSizes
    {
      Original = 0,
      Thumb = 1,
      Mid = 2,
      Poster = 3,
      Cover = 4,
      Profile = 5
    };

    public override string ToString()
    {
      return "Movie Banner (" + Id + ")";
    }

    /// <summary>
    /// Used to load/save images persistent if we're using a cache provider 
    /// (should keep memory usage much lower)
    /// 
    /// on the other hand we have a back-ref to tvdb (from a data class), which sucks
    /// 
    /// todo: think of a better way to handle this
    /// </summary>
    public virtual ICacheProvider CacheProvider
    {
      get { return _cacheProvider; }
      set
      {
        _cacheProvider = value;
        Original.CacheProvider = value;
        Thumbnail.CacheProvider = value;
      }
    }

    public Dictionary<BannerSizes, BannerSize> ImageSizes { get; private set; }

    public abstract String ImageType
    {
      get;
    }

    /// <summary>
    /// Language of the banner
    /// </summary>
    public MovieDbLanguage Language { get; set; }

    /// <summary>
    /// Id of the banner
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Id of the Person/movie/... this banner belongs to
    /// </summary>
    public int ObjectId { get; set; }

    /// <summary>
    /// The Original sized banner
    /// </summary>
    public BannerSize Original
    {
      get { return _originalSize; }
      set
      {
        _originalSize = value;
        AddBannerSize(BannerSizes.Original, value);
      }
    }

    /// <summary>
    /// The thumbnail of the banner
    /// </summary>
    public BannerSize Thumbnail
    {
      get { return _thumbSize; }
      set
      {
        _thumbSize = value;
        AddBannerSize(BannerSizes.Thumb, value);
      }
    }

    protected void AddBannerSize(BannerSizes key, BannerSize value)
    {
      if (ImageSizes.ContainsKey(key))
        ImageSizes.Remove(key);
      ImageSizes.Add(key, value);
    }
  }
}
