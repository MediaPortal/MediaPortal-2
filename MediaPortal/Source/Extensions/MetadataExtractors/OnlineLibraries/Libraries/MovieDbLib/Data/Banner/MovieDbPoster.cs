using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MovieDbLib.Cache;

namespace MovieDbLib.Data.Banner
{
  public class MovieDbPoster : MovieDbBanner
  {
    #region private/protected properties
    private const string IMAGE_TYPE = "Posters";
    private BannerSize m_mid;
    private BannerSize m_cover;
    private int m_movieId;
    #endregion

    public MovieDbPoster()
      : base()
    {
      this.MovieId = this.ObjectId;
    }

    public override string ToString()
    {
      return "Movie Poster (" + this.Id + ")";
    }

    public int MovieId
    {
      get { return m_movieId; }
      set { m_movieId = value; }
    }

    public BannerSize Mid
    {
      get { return m_mid; }
      set
      {
        m_mid = value;
        AddBannerSize(BannerSizes.mid, value);
      }
    }


    public BannerSize Cover
    {
      get { return m_cover; }
      set
      {
        m_cover = value;
        AddBannerSize(BannerSizes.cover, value);
      }
    }

    public override string ImageType
    {
      get { throw new NotImplementedException(); }
    }
  }
}
