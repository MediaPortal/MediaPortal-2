using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MovieDbLib.Data;
using System.Diagnostics;
using MovieDbLib.Cache;
using MovieDbLib.Data.Banner;
using MovieDbLib.Data.Persons;

namespace MovieDbLib
{
  public class MovieDbHandler
  {
    private MovieDbDownloader m_movieDownloader;
    private String m_apiKey;
    private ICacheProvider m_cacheProvider;

    public MovieDbHandler(String _apiKey)
    {
      m_apiKey = _apiKey;
      m_movieDownloader = new MovieDbDownloader(_apiKey);
    }

    public MovieDbHandler(String _apiKey, ICacheProvider _cacheProvider)
      : this(_apiKey)
    {
      m_cacheProvider = _cacheProvider;
    }

    public MovieDbMovie GetMovie(String _imdbId, MovieDbLanguage _language)
    {
      Stopwatch watch = new Stopwatch();
      watch.Start();
      MovieDbMovie movie = null;
      //Did I get the movie completely from cache or did I have to make an additional online request
      bool loadedAdditionalInfo = false;

      if (movie == null)
      {
        movie = m_movieDownloader.DownloadMovie(_imdbId, _language);
        loadedAdditionalInfo = true;
      }
      else if (!movie.GetAvailableLanguages().Contains(_language))
      {//add additional movie to already existing movie

      }

      if (movie != null)
      {
        if (m_cacheProvider != null)
        {//we're using a cache provider
          //if we've loaded data from online source -> save to cache
          if (m_cacheProvider.Initialised && loadedAdditionalInfo)
          {
            Log.Info("Store movie " + movie.Id + " with " + m_cacheProvider.ToString());
            m_cacheProvider.SaveToCache(movie);
          }

          AddCacheProviderToBanner(movie, m_cacheProvider);
        }
      }
      return movie;
    }

    private void AddCacheProviderToBanner(MovieDbMovie _movie, ICacheProvider _cacheProvider)
    {
      //Store a ref to the cacheprovider and series id in each banner, so the banners
      //can be stored/loaded to/from cache
      #region add cache provider/series id
      if (_movie.Banners != null)
      {
        _movie.Banners.ForEach(delegate(MovieDbBanner b)
        {
          b.CacheProvider = _cacheProvider;
          b.ObjectId = _movie.Id;
        });
      }

      if (_movie.Cast != null)
      {
        _movie.Cast.ForEach(delegate(MovieDbCast p)
        {
          if (p.Images != null)
          {
            p.Images.ForEach(delegate(MovieDbBanner b)
            {
              b.CacheProvider = _cacheProvider;
              b.ObjectId = p.Id;
            });
          }
        });
      }
      #endregion
    }

    public MovieDbMovie GetMovie(int _movieId, MovieDbLanguage _language)
    {
      Stopwatch watch = new Stopwatch();
      watch.Start();
      MovieDbMovie movie = null;
      //Did I get the movie completely from cache or did I have to make an additional online request
      bool loadedAdditionalInfo = false;

      if (m_cacheProvider != null && m_cacheProvider.Initialised)
      {
        if (m_cacheProvider.IsMovieCached(_movieId))
        {
          movie = m_cacheProvider.LoadMovieFromCache(_movieId);
        }
      }

      if (movie == null)
      {
        movie = m_movieDownloader.DownloadMovie(_movieId, _language);
        loadedAdditionalInfo = true;
      }
      else if (!movie.GetAvailableLanguages().Contains(_language))
      {//add additional movie to already existing movie

      }

      if (m_cacheProvider != null)
      {//we're using a cache provider
        //if we've loaded data from online source -> save to cache
        if (m_cacheProvider.Initialised && loadedAdditionalInfo)
        {
          Log.Info("Store movie " + _movieId + " with " + m_cacheProvider.ToString());
          m_cacheProvider.SaveToCache(movie);
        }
        AddCacheProviderToBanner(movie, m_cacheProvider);
      }

      return movie;
    }

    public List<MovieDbMovie> SearchMovie(String _movieName, MovieDbLanguage _language)
    {
      return m_movieDownloader.MovieSearch(_movieName, _language);
    }

    public List<MovieDbMovie> SearchMovieByHash(String _movieHash)
    {

      return m_movieDownloader.MovieSearchByHash(_movieHash);
    }

    public List<MovieDbPerson> SearchPerson(String _personName, MovieDbLanguage _language)
    {
      return m_movieDownloader.PersonSearch(_personName, _language);
    }

    public MovieDbPerson GetPerson(int _personId, MovieDbLanguage _language)
    {
      Stopwatch watch = new Stopwatch();
      watch.Start();
      MovieDbPerson person = null;
      //Did I get the movie completely from cache or did I have to make an additional online request
      bool loadedAdditionalInfo = false;

      if (m_cacheProvider != null && m_cacheProvider.Initialised)
      {
        if (m_cacheProvider.IsPersonCached(_personId))
        {
          person = m_cacheProvider.LoadPersonFromCache(_personId);
        }
      }

      if (person == null)
      {
        person = m_movieDownloader.DownloadPerson(_personId, _language);
        loadedAdditionalInfo = true;
      }

      if (m_cacheProvider != null)
      {//we're using a cache provider
        //if we've loaded data from online source -> save to cache
        if (m_cacheProvider.Initialised && loadedAdditionalInfo)
        {
          Log.Info("Store person " + _personId + " with " + m_cacheProvider.ToString());
          m_cacheProvider.SaveToCache(person);
        }
        //Store a ref to the cacheprovider and series id in each banner, so the banners
        //can be stored/loaded to/from cache
        #region add cache provider/series id
        if (person != null && person.Images != null)
        {
          person.Images.ForEach(delegate(MovieDbBanner b)
          {
            b.CacheProvider = m_cacheProvider;
            b.ObjectId = person.Id;
          });
        }
        #endregion
      }

      return person;
    }
  }
}
