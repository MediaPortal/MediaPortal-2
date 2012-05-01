using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using MovieDbLib.Cache;
using System.Net;
using System.IO;
using MovieDb;

namespace MovieDbLib.Data.Banner
{
  public abstract class MovieDbBanner
  {
    #region private/protected fields
    private string m_id;
    private MovieDbLanguage m_language;
    private int m_objectId;
    private ICacheProvider m_cacheProvider;
    private BannerSize m_originalSize;
    private BannerSize m_thumbSize;
    private Dictionary<BannerSizes, BannerSize> m_imageSizes;
    #endregion


    #region factory methods

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_objectId">id of the object of this banner (person, movie,...)</param>
    /// <param name="_bannerId">id of banner</param>
    /// <param name="_images">list of image sizes available for this banner</param>
    /// <returns></returns>
    internal static MovieDbBanner CreateBanner(int _objectId, string _bannerId, List<string[]> _images)
    {
      //get type of banner (poster or backdrop for now)
      switch (_images[0][0])
      {
        case "poster":
          return CreatePoster(_objectId, _bannerId, _images);
        case "backdrop":
          return CreateBackdrop(_objectId, _bannerId, _images);
        case "profile":
          return CreateProfile(_objectId, _bannerId, _images);
      }
      return null;
    }

    private static MovieDbBanner CreateProfile(int _personId, string _bannerId, List<string[]> _images)
    {
      MovieDbPersonImage image = new MovieDbPersonImage();
      image.Id = _bannerId;
      image.PersonId = _personId;
      foreach (string[] i in _images)
      {
        switch (i[1])
        {
          case "original":
            image.Original = new BannerSize(null, _personId, _bannerId, BannerTypes.person, BannerSizes.original, i[2]);
            break;
          case "thumb":
            image.Thumbnail = new BannerSize(null, _personId, _bannerId, BannerTypes.person, BannerSizes.thumb, i[2]);
            break;
          case "profile":
            image.Profile = new BannerSize(null, _personId, _bannerId, BannerTypes.person, BannerSizes.profile, i[2]);
            break;
          default:
            Log.Warn("Parsing the unknown image size \"" + i[1] + "\"");
            break;
        }
      }

      return image;
    }


    private static MovieDbBanner CreatePoster(int _movieId, string _bannerId, List<string[]> _images)
    {
      MovieDbPoster banner = new MovieDbPoster();
      banner.Id = _bannerId;
      banner.ObjectId = _movieId;
      foreach (string[] i in _images)
      {
        switch (i[1])
        {
          case "original":
            banner.Original = new BannerSize(null, _movieId, _bannerId, BannerTypes.poster, BannerSizes.original, i[2]);
            break;
          case "thumb":
            banner.Thumbnail = new BannerSize(null, _movieId, _bannerId, BannerTypes.poster, BannerSizes.thumb, i[2]);
            break;
          case "mid":
            banner.Mid = new BannerSize(null, _movieId, _bannerId, BannerTypes.poster, BannerSizes.mid, i[2]);
            break;
          case "cover":
            banner.Cover = new BannerSize(null, _movieId, _bannerId, BannerTypes.poster, BannerSizes.cover, i[2]);
            break;
          default:
            Log.Warn("Parsing the unknown image size \"" + i[1] + "\"");
            break;
        }
      }

      return banner;
    }

    private static MovieDbBanner CreateBackdrop(int _movieId, string _bannerId, List<string[]> _images)
    {
      MovieDbBackdrop banner = new MovieDbBackdrop();
      banner.Id = _bannerId;
      banner.ObjectId = _movieId;
      foreach (string[] i in _images)
      {
        switch (i[1])
        {
          case "original":
            banner.Original = new BannerSize(null, _movieId, _bannerId, BannerTypes.backdrop, BannerSizes.original, i[2]);
            break;
          case "thumb":
            banner.Thumbnail = new BannerSize(null, _movieId, _bannerId, BannerTypes.backdrop, BannerSizes.thumb, i[2]);
            break;
          case "poster":
            banner.Poster = new BannerSize(null, _movieId, _bannerId, BannerTypes.backdrop, BannerSizes.poster, i[2]);
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
      m_imageSizes = new Dictionary<BannerSizes, BannerSize>();
    }

    public enum BannerTypes
    {
      poster = 0,
      backdrop = 1,
      person = 2
    };

    public enum BannerSizes
    {
      original = 0,
      thumb = 1,
      mid = 2,
      poster = 3,
      cover = 4,
      profile = 5
    };

    public override string ToString()
    {
      return "Movie Banner (" + this.Id + ")";
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
      get { return m_cacheProvider; }
      set
      {
        m_cacheProvider = value;
        this.Original.CacheProvider = value;
        this.Thumbnail.CacheProvider = value;
      }
    }

    public Dictionary<BannerSizes, BannerSize> ImageSizes
    {
      get
      {
        return m_imageSizes;
      }
    }

    public abstract String ImageType
    {
      get;
    }

    /// <summary>
    /// Language of the banner
    /// </summary>
    public MovieDbLanguage Language
    {
      get { return m_language; }
      set { m_language = value; }
    }

    /// <summary>
    /// Id of the banner
    /// </summary>
    public string Id
    {
      get { return m_id; }
      set { m_id = value; }
    }

    /// <summary>
    /// Id of the person/movie/... this banner belongs to
    /// </summary>
    public int ObjectId
    {
      get { return m_objectId; }
      set { m_objectId = value; }
    }

    /// <summary>
    /// The original sized banner
    /// </summary>
    public BannerSize Original
    {
      get { return m_originalSize; }
      set
      {
        m_originalSize = value;
        AddBannerSize(BannerSizes.original, value);
      }
    }

    /// <summary>
    /// The thumbnail of the banner
    /// </summary>
    public BannerSize Thumbnail
    {
      get { return m_thumbSize; }
      set { 
        m_thumbSize = value;
        AddBannerSize(BannerSizes.thumb, value);
      }
    }

    protected void AddBannerSize(BannerSizes _key, BannerSize _value)
    {
      if (m_imageSizes.ContainsKey(_key))
      {
        m_imageSizes.Remove(_key);
      }
      m_imageSizes.Add(_key, _value);
    }
  }
}
