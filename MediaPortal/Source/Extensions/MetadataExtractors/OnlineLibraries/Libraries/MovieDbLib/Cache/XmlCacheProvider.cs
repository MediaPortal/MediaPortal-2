using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MovieDbLib.Xml;
using MovieDbLib.Data;
using System.IO;
using System.Drawing;
using MovieDbLib.Data.Banner;
using System.Text.RegularExpressions;

namespace MovieDbLib.Cache
{
  public class XmlCacheProvider : ICacheProvider
  {
    #region private fields
    MovieDbXmlWriter m_xmlWriter;
    MovieDbXmlReader m_xmlReader;
    String m_rootFolder;
    private bool m_initialised = false;
    #endregion

    /// <summary>
    /// Constructor for XmlCacheProvider
    /// </summary>
    /// <param name="_rootFolder">This is the folder on the disk where all the information are stored</param>
    public XmlCacheProvider(String _rootFolder)
    {
      m_xmlWriter = new MovieDbXmlWriter();
      m_xmlReader = new MovieDbXmlReader();
      m_rootFolder = _rootFolder;
    }

    #region ICacheProvider Members

    /// <summary>
    /// Is the cache provider initialised
    /// </summary>
    public bool Initialised
    {
      get { return m_initialised; }
    }

    /// <summary>
    /// Initialises the cache, should do the following things
    /// - initialise connections used for this cache provider (db connections, network shares,...)
    /// - create folder structure / db tables / ...  if they are not created already
    /// - if this is the first time the cache has been initialised (built), mark last_updated with the
    ///   current date
    /// </summary>
    /// <returns>true if successful, false otherwise</returns>
    public bool InitCache()
    {
      try
      {
        if (!Directory.Exists(m_rootFolder))
        {
          Directory.CreateDirectory(m_rootFolder);
        }

        m_initialised = true;
        return true;
      }
      catch (Exception ex)
      {
        Log.Error("Couldn't initialise cache: " + ex.ToString());
        return false;
      }
    }


    public bool CloseCache()
    {
      return true;
    }

    public bool ClearCache()
    {
      throw new NotImplementedException();
    }

    public bool RemoveFromCache(int _seriesId)
    {
      throw new NotImplementedException();
    }

    public List<MovieDbLanguage> LoadLanguageListFromCache()
    {
      throw new NotImplementedException();
    }

    public List<MovieDbMovie> LoadAllMoviesFromCache()
    {
      throw new NotImplementedException();
    }

    public MovieDbMovie LoadMovieFromCache(int _movieId)
    {
      String movieRoot = m_rootFolder + Path.DirectorySeparatorChar + "movies" + Path.DirectorySeparatorChar + _movieId;
      if (!Directory.Exists(movieRoot)) return null;
      MovieDbMovie movie = new MovieDbMovie();

      #region load series in all available languages
      String[] movieLanguages = Directory.GetFiles(movieRoot, "*.xml");
      foreach (String l in movieLanguages)
      {
        String content = File.ReadAllText(l);
        List<MovieFields> movieList = m_xmlReader.ExtractMovieFields(content);
        if (movieList != null && movieList.Count == 1)
        {
          MovieFields s = movieList[0];
          movie.AddLanguage(s);
        }
      }



      if (movie.SeriesTranslations != null && movie.SeriesTranslations.Count > 0)
      {//change language of the series to the default language
        movie.SetLanguage(movie.SeriesTranslations.Keys.First());
      }
      else
      {//no series info could be loaded
        return null;
      }

      #endregion
      /*
      #region Banner loading
      String bannerFile = movieRoot + Path.DirectorySeparatorChar + "banners.xml";
      //load cached banners
      if (File.Exists(bannerFile))
      {//banners have been already loaded
        List<TvdbBanner> bannerList = m_xmlReader.ExtractBanners(File.ReadAllText(bannerFile));

        String[] banners = Directory.GetFiles(movieRoot, "banner*.jpg");
        foreach (String b in banners)
        {
          try
          {
            int bannerId = Int32.Parse(b.Remove(b.IndexOf(".")).Remove(0, b.LastIndexOf("_") + 1));
            foreach (TvdbBanner banner in bannerList)
            {
              if (banner.Id == bannerId)
              {
                if (b.Contains("thumb") && banner.GetType().BaseType == typeof(TvdbBannerWithThumb))
                {
                  ((TvdbBannerWithThumb)banner).LoadThumb(Image.FromFile(b));
                }
                else if (b.Contains("vignette") && banner.GetType() == typeof(TvdbFanartBanner))
                {
                  ((TvdbFanartBanner)banner).LoadVignette(Image.FromFile(b));
                }
                else
                {
                  banner.LoadBanner(Image.FromFile(b));
                }
              }
            }

          }
          catch (Exception)
          {
            Log.Warn("Couldn't load image file " + b);
          }
        }
        movie.Banners = bannerList;
      }
      #endregion
      
      #region actor loading
      //load actor info
      String actorFile = movieRoot + Path.DirectorySeparatorChar + "actors.xml";
      if (File.Exists(actorFile))
      {
        List<TvdbActor> actorList = m_xmlReader.ExtractActors(File.ReadAllText(actorFile));

        String[] banners = Directory.GetFiles(movieRoot, "actor_*.jpg");
        foreach (String b in banners)
        {
          try
          {
            int actorId = Int32.Parse(b.Remove(b.IndexOf(".")).Remove(0, b.LastIndexOf("_") + 1));
            foreach (TvdbActor actor in actorList)
            {
              if (actor.Id == actorId)
              {
                actor.ActorImage.LoadBanner(Image.FromFile(b));
              }
            }

          }
          catch (Exception)
          {
            Log.Warn("Couldn't load image file " + b);
          }
        }
        movie.TvdbActors = actorList;
      }
      #endregion
      */
      return movie;
    }

    /// <summary>
    /// Load the given person from cache
    /// </summary>
    /// <param name="_movieId">Id of the person to load</param>
    /// <returns>The MovieDbPerson object from cache or null</returns>
    public MovieDbPerson LoadPersonFromCache(int _personId)
    {
      String personFile = m_rootFolder + Path.DirectorySeparatorChar + "persons" +
                          Path.DirectorySeparatorChar + _personId + Path.DirectorySeparatorChar
                          + _personId + ".xml";
      if (!File.Exists(personFile)) return null;//person not cached

      try
      {
        String content = File.ReadAllText(personFile);

        List<MovieDbPerson> persons = m_xmlReader.ExtractPersons(content);
        if (persons != null && persons.Count == 1)
        {
          return persons[0];
        }
        else
        {
          Log.Warn("Couldn't extract person " + _personId + " from xml content");
        }
      }
      catch (Exception ex)
      {
        Log.Warn("Couldn't load person " + _personId + " from cache: ", ex);
      }
      return null;
    }

    public void SaveToCache(List<MovieDbLanguage> _languageList)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Saves the movie to cache
    /// </summary>
    /// <param name="_movie">MovieDbMovie object</param>
    public void SaveToCache(MovieDbMovie _movie)
    {
      String root = m_rootFolder + Path.DirectorySeparatorChar + "movies" + Path.DirectorySeparatorChar + _movie.Id;
      if (!Directory.Exists(root)) Directory.CreateDirectory(root);
      try
      {//delete old cached content
        String[] files = Directory.GetFiles(root, "*.xml");
        foreach (String f in files)
        {
          File.Delete(f);
        }
      }
      catch (Exception ex)
      {
        Log.Warn("Couldn't delete old cache files", ex);
      }

      foreach (MovieDbLanguage l in _movie.GetAvailableLanguages())
      {//write all languages to file
        String fName = root + Path.DirectorySeparatorChar + l.Abbriviation + ".xml";
        _movie.SetLanguage(l);
        m_xmlWriter.WriteMovieContent(_movie, fName);
      }
    }

    public void SaveToCache(MovieDbPerson _person)
    {
      String root = m_rootFolder + Path.DirectorySeparatorChar + "persons" + 
                    Path.DirectorySeparatorChar + _person.Id;
      if (!Directory.Exists(root)) Directory.CreateDirectory(root);
      try
      {//delete old cached content
        String[] files = Directory.GetFiles(root, "*.xml");
        foreach (String f in files)
        {
          File.Delete(f);
        }
      }
      catch (Exception ex)
      {
        Log.Warn("Couldn't delete old cache files", ex);
      }

      //write person to file
      String fName = root + Path.DirectorySeparatorChar + _person.Id + ".xml";
      m_xmlWriter.WritePersonContent(_person, fName);
    }

    /// <summary>
    /// Save the given image to cache
    /// </summary>
    /// <param name="_image">banner to save</param>
    /// <param name="_objectId">id of movie/person/...</param>
    /// <param name="_bannerId">id of banner</param>
    /// <param name="_type">type of the banner</param>
    /// <param name="_size">size of the banner</param>
    public void SaveToCache(Image _image, int _objectId, string _bannerId, MovieDbBanner.BannerTypes _type, MovieDbBanner.BannerSizes _size)
    {
      if (_image != null)
      {
        FileInfo cacheName = new FileInfo(CreateBannerCacheName(m_rootFolder, _objectId, _bannerId, _type, _size));
        try
        {
          if (cacheName.Directory.Exists)
          {
            cacheName.Directory.Create();
          }
          _image.Save(cacheName.FullName);
        }
        catch (Exception ex)
        {
          Log.Error("Error while storing banner " + _bannerId + " as " + cacheName);
        }
      }

      else
      {
        Log.Warn("Couldn't save image " + _bannerId + ", no image given (null)");
      }
    }

    private String CreateBannerCacheName(String _root, int _objectId, string _bannerId, MovieDbBanner.BannerTypes _type, MovieDbBanner.BannerSizes _size)
    {
      StringBuilder builder = new StringBuilder(_root);

      switch (_type)
      {
        case MovieDbBanner.BannerTypes.backdrop:
          builder.Append(Path.DirectorySeparatorChar);
          builder.Append("movies");
          builder.Append(Path.DirectorySeparatorChar);
          builder.Append(_objectId);
          builder.Append(Path.DirectorySeparatorChar);
          builder.Append("backdrop_");
          builder.Append(_objectId);
          builder.Append("_");
          builder.Append(_size);
          builder.Append("_");
          builder.Append(_bannerId);
          builder.Append(".jpg");
          break;
        case MovieDbBanner.BannerTypes.poster:
          builder.Append(Path.DirectorySeparatorChar);
          builder.Append("movies");
          builder.Append(Path.DirectorySeparatorChar);
          builder.Append(_objectId);
          builder.Append(Path.DirectorySeparatorChar);
          builder.Append("poster_");
          builder.Append(_objectId);
          builder.Append("_");
          builder.Append(_size);
          builder.Append("_");
          builder.Append(_bannerId);
          builder.Append(".jpg");
          break;
        case MovieDbBanner.BannerTypes.person:
          builder.Append(Path.DirectorySeparatorChar);
          builder.Append("persons");
          builder.Append(Path.DirectorySeparatorChar);
          builder.Append(_objectId);
          builder.Append(Path.DirectorySeparatorChar);
          builder.Append("person_");
          builder.Append(_objectId);
          builder.Append("_");
          builder.Append(_size);
          builder.Append("_");
          builder.Append(_bannerId);
          builder.Append(".jpg");
          break;
      }
      return builder.ToString();
    }

    public Image LoadImageFromCache(int _objectId, string _bannerId, MovieDbBanner.BannerTypes _type, MovieDbBanner.BannerSizes _size)
    {

      String fName = CreateBannerCacheName(m_rootFolder, _objectId, _bannerId, _type, _size);
      if (File.Exists(fName))
      {
        try
        {
          return Image.FromFile(fName);
        }
        catch (Exception ex)
        {
          Log.Warn("Couldn't load image " + fName + " for banner " + _bannerId, ex);
        }
      }

      return null;
    }

    public bool RemoveImageFromCache(int _movieId, string _fileName)
    {
      throw new NotImplementedException();
    }

    public List<int> GetCachedMovies()
    {
      return new List<int>();
    }

    public List<int> GetCachedPersons()
    {
      return new List<int>();
    }

    public bool IsMovieCached(int _movieId)
    {
      try
      {
        if (Directory.Exists(m_rootFolder + Path.DirectorySeparatorChar +
                             "movies" + Path.DirectorySeparatorChar + _movieId))
        {
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    public bool IsPersonCached(int _personId)
    {
      try
      {
        if (Directory.Exists(m_rootFolder + Path.DirectorySeparatorChar +
                             "persons" + Path.DirectorySeparatorChar + _personId))
        {
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    #endregion

  }
}
