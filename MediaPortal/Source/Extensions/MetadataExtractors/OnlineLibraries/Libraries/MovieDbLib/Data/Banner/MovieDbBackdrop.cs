using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Cache;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner
{
  public class MovieDbBackdrop : MovieDbBanner
  {
    #region private/protected properties
    private const string IMAGE_TYPE = "Backdrops";
    private BannerSize _poster;

    #endregion

    public MovieDbBackdrop()
    {
      MovieId = ObjectId;
    }


    public override string ToString()
    {
      return "Movie Backdrop (" + Id + ")";
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
        Poster.CacheProvider = value;
      }
    }

    public int MovieId { get; set; }

    public BannerSize Poster
    {
      get { return _poster; }
      set 
      { 
        _poster = value;
        AddBannerSize(BannerSizes.Poster, value);
      }
    }

    public override string ImageType
    {
      get { return IMAGE_TYPE; }
    }
  }
}
