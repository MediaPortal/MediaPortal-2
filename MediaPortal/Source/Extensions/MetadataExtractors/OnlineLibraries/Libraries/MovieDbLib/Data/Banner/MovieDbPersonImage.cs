using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MovieDbLib.Cache;

namespace MovieDbLib.Data.Banner
{
  public class MovieDbPersonImage : MovieDbBanner
  {

    #region private/protected properties
    private const string IMAGE_TYPE = "Persons";
    private BannerSize m_profile;
    private int m_personId;
    #endregion

    public MovieDbPersonImage()
      : base()
    {
      this.PersonId = this.ObjectId;
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
        this.Profile.CacheProvider = value;
      }
    }

    public int PersonId
    {
      get { return m_personId; }
      set { m_personId = value; }
    }

    public BannerSize Profile
    {
      get { return m_profile; }
      set
      {
        m_profile = value;
        AddBannerSize(BannerSizes.profile, value);
      }
    }

    public override string ImageType
    {
      get { return IMAGE_TYPE; }
    }
  }
}
