namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner
{
  public class MovieDbPoster : MovieDbBanner
  {
    #region private/protected properties
    private const string IMAGE_TYPE = "Posters";
    private BannerSize _mid;
    private BannerSize _cover;

    #endregion

    public MovieDbPoster()
    {
      MovieId = ObjectId;
    }

    public override string ToString()
    {
      return "Movie Poster (" + Id + ")";
    }

    public int MovieId { get; set; }

    public BannerSize Mid
    {
      get { return _mid; }
      set
      {
        _mid = value;
        AddBannerSize(BannerSizes.Mid, value);
      }
    }


    public BannerSize Cover
    {
      get { return _cover; }
      set
      {
        _cover = value;
        AddBannerSize(BannerSizes.Cover, value);
      }
    }

    public override string ImageType
    {
      get { return IMAGE_TYPE; }
    }
  }
}
