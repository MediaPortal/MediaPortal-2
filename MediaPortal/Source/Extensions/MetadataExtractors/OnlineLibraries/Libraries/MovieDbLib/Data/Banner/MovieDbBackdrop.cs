using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MovieDbLib.Cache;

namespace MovieDbLib.Data.Banner
{
  public class MovieDbBackdrop : MovieDbBanner
  {
    #region private/protected properties
    private const string IMAGE_TYPE = "Backdrops";
    private BannerSize m_poster;
    private int m_movieId;
    #endregion

    public MovieDbBackdrop()
      : base()
    {
      this.MovieId = this.ObjectId;
    }


    public override string ToString()
    {
      return "Movie Backdrop (" + this.Id + ")";
    }

    /// <summary>
    /// Used to load/save images persistent if we're using a cache provider 
    /// (should keep memory usage much lower)
    /// 
    /// on the other hand we have a back-ref to tvdb (from a data class), which sucks
    /// 
    /// todo: think of a better way to handle this
    /// </summary>
    public override ICacheProvider CacheProvider
    {
      set
      {
        base.CacheProvider = value;
        this.Poster.CacheProvider = value;
      }
    }

    public int MovieId
    {
      get { return m_movieId; }
      set { m_movieId = value; }
    }

    public BannerSize Poster
    {
      get { return m_poster; }
      set 
      { 
        m_poster = value;
        AddBannerSize(BannerSizes.poster, value);
      }
    }

    public override string ImageType
    {
      get { return IMAGE_TYPE; }
    }
  }
}
