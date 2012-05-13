using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Persons;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Xml;
using System.IO;
using System.Drawing;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Cache
{
  public class XmlCacheProvider : ICacheProvider
  {
    #region private fields

    readonly MovieDbXmlWriter _xmlWriter;
    readonly MovieDbXmlReader _xmlReader;
    readonly String _rootFolder;
    private bool _initialised = false;

    #endregion

    /// <summary>
    /// Constructor for XmlCacheProvider
    /// </summary>
    /// <param name="rootFolder">This is the folder on the disk where all the information are stored</param>
    public XmlCacheProvider(String rootFolder)
    {
      _xmlWriter = new MovieDbXmlWriter();
      _xmlReader = new MovieDbXmlReader();
      _rootFolder = rootFolder;
    }

    #region ICacheProvider Members

    /// <summary>
    /// Is the cache provider initialised
    /// </summary>
    public bool Initialised
    {
      get { return _initialised; }
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
        if (!Directory.Exists(_rootFolder))
        {
          Directory.CreateDirectory(_rootFolder);
        }

        _initialised = true;
        return true;
      }
      catch (Exception ex)
      {
        Log.Error("Couldn't initialise cache: " + ex);
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

    public bool RemoveFromCache(int seriesId)
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

    public MovieDbMovie LoadMovieFromCache(int movieId)
    {
      String movieRoot = Path.Combine(_rootFolder, "movies" + Path.DirectorySeparatorChar + movieId);
      if (!Directory.Exists(movieRoot)) return null;
      MovieDbMovie movie = new MovieDbMovie();

      #region load series in all available languages
      String[] movieLanguages = Directory.GetFiles(movieRoot, "*.xml");
      foreach (String l in movieLanguages)
      {
        String content = File.ReadAllText(l);
        List<MovieFields> movieList = _xmlReader.ExtractMovieFields(content);
        if (movieList != null && movieList.Count == 1)
        {
          MovieFields s = movieList[0];
          movie.AddLanguage(s);
        }
      }



      if (movie.MovieTranslations != null && movie.MovieTranslations.Count > 0)
      {
        //change language of the series to the default language
        movie.SetLanguage(movie.MovieTranslations.Keys.First());
      }
      else
      {
        //no series info could be loaded
        return null;
      }

      #endregion
      /*
      #region Banner loading
      String bannerFile = movieRoot + Path.DirectorySeparatorChar + "banners.xml";
      //load cached banners
      if (File.Exists(bannerFile))
      {//banners have been already loaded
        List<TvdbBanner> bannerList = _xmlReader.ExtractBanners(File.ReadAllText(bannerFile));

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
                if (b.Contains("Thumb") && banner.GetType().BaseType == typeof(TvdbBannerWithThumb))
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
        List<TvdbActor> actorList = _xmlReader.ExtractActors(File.ReadAllText(actorFile));

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
        movie.Persons = actorList;
      }
      #endregion
      */
      return movie;
    }

    /// <summary>
    /// Load the given Person from cache
    /// </summary>
    /// <param name="personId">Id of the Person to load</param>
    /// <returns>The MovieDbPerson object from cache or null</returns>
    public MovieDbPerson LoadPersonFromCache(int personId)
    {
      String personFile = Path.Combine(_rootFolder, "persons" + Path.DirectorySeparatorChar + personId + ".xml");
      if (!File.Exists(personFile)) return null;//Person not cached

      try
      {
        String content = File.ReadAllText(personFile);

        List<MovieDbPerson> persons = _xmlReader.ExtractPersons(content);
        if (persons != null && persons.Count == 1)
        {
          return persons[0];
        }
        Log.Warn("Couldn't extract Person " + personId + " from xml content");
      }
      catch (Exception ex)
      {
        Log.Warn("Couldn't load Person " + personId + " from cache: ", ex);
      }
      return null;
    }

    public void SaveToCache(List<MovieDbLanguage> languageList)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Saves the movie to cache
    /// </summary>
    /// <param name="movie">MovieDbMovie object</param>
    public void SaveToCache(MovieDbMovie movie)
    {
      String root = Path.Combine(_rootFolder, "movies" + Path.DirectorySeparatorChar + movie.Id);
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

      foreach (MovieDbLanguage l in movie.GetAvailableLanguages())
      {//write all languages to file
        String fName = root + Path.DirectorySeparatorChar + l.Abbreviation + ".xml";
        movie.SetLanguage(l);
        _xmlWriter.WriteMovieContent(movie, fName);
      }
    }

    public void SaveToCache(MovieDbPerson person)
    {
      String root = Path.Combine(_rootFolder, "persons" + Path.DirectorySeparatorChar + person.Id);
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

      //write Person to file
      String fName = root + Path.DirectorySeparatorChar + person.Id + ".xml";
      _xmlWriter.WritePersonContent(person, fName);
    }

    /// <summary>
    /// Save the given image to cache
    /// </summary>
    /// <param name="image">banner to save</param>
    /// <param name="objectId">id of movie/Person/...</param>
    /// <param name="bannerId">id of banner</param>
    /// <param name="type">type of the banner</param>
    /// <param name="size">size of the banner</param>
    public void SaveToCache(Image image, int objectId, string bannerId, MovieDbBanner.BannerTypes type, MovieDbBanner.BannerSizes size)
    {
      if (image != null)
      {
        FileInfo cacheName = new FileInfo(CreateBannerCacheName(_rootFolder, objectId, bannerId, type, size));
        try
        {
          if (cacheName.Directory.Exists)
          {
            cacheName.Directory.Create();
          }
          image.Save(cacheName.FullName);
        }
        catch (Exception ex)
        {
          Log.Error("Error while storing banner " + bannerId + " as " + cacheName, ex);
        }
      }
      else
      {
        Log.Warn("Couldn't save image " + bannerId + ", no image given (null)");
      }
    }

    private String CreateBannerCacheName(String root, int objectId, string bannerId, MovieDbBanner.BannerTypes type, MovieDbBanner.BannerSizes size)
    {
      StringBuilder builder = new StringBuilder(root);

      switch (type)
      {
        case MovieDbBanner.BannerTypes.Backdrop:
          return string.Format("{0}movies{0}{1}{0}backdrop_{1}_{2}_{3}.jpg", Path.DirectorySeparatorChar, objectId, size, bannerId);
          
        case MovieDbBanner.BannerTypes.Poster:
          return string.Format("{0}movies{0}{1}{0}poster_{1}_{2}_{3}.jpg", Path.DirectorySeparatorChar, objectId, size, bannerId);

        case MovieDbBanner.BannerTypes.Person:
          return string.Format("{0}persons{0}{1}{0}person_{1}_{2}_{3}.jpg", Path.DirectorySeparatorChar, objectId, size, bannerId);
      }
      return builder.ToString();
    }

    public Image LoadImageFromCache(int objectId, string bannerId, MovieDbBanner.BannerTypes type, MovieDbBanner.BannerSizes size)
    {

      String fName = CreateBannerCacheName(_rootFolder, objectId, bannerId, type, size);
      if (File.Exists(fName))
      {
        try
        {
          return Image.FromFile(fName);
        }
        catch (Exception ex)
        {
          Log.Warn("Couldn't load image " + fName + " for banner " + bannerId, ex);
        }
      }

      return null;
    }

    public bool RemoveImageFromCache(int movieId, string fileName)
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

    public bool IsMovieCached(int movieId)
    {
      try
      {
        return Directory.Exists(Path.Combine(_rootFolder, "movies" + Path.DirectorySeparatorChar + movieId));
      }
      catch (Exception)
      {
        return false;
      }
    }

    public bool IsPersonCached(int personId)
    {
      try
      {
        return Directory.Exists(Path.Combine(_rootFolder, "persons" + Path.DirectorySeparatorChar + personId));
      }
      catch (Exception)
      {
        return false;
      }
    }

    #endregion

  }
}
