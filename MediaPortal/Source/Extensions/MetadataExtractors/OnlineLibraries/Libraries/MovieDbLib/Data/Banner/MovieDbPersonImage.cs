using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Cache;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner
{
  public class MovieDbPersonImage : MovieDbBanner
  {

    #region private/protected properties
    private const string IMAGE_TYPE = "Persons";
    private BannerSize _profile;

    #endregion

    public MovieDbPersonImage()
    {
      PersonId = ObjectId;
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
        Profile.CacheProvider = value;
      }
    }

    public int PersonId { get; set; }

    public BannerSize Profile
    {
      get { return _profile; }
      set
      {
        _profile = value;
        AddBannerSize(BannerSizes.Profile, value);
      }
    }

    public override string ImageType
    {
      get { return IMAGE_TYPE; }
    }
  }
}
