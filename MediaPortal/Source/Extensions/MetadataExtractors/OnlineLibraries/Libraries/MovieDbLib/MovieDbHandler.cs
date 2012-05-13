using System;
using System.Collections.Generic;
using System.Diagnostics;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Cache;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Persons;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib
{
  public class MovieDbHandler
  {
    private readonly MovieDbDownloader _movieDownloader;
    private readonly ICacheProvider _cacheProvider;

    public MovieDbHandler(String apiKey)
    {
      _movieDownloader = new MovieDbDownloader(apiKey);
    }

    public MovieDbHandler(ICacheProvider cacheProvider, String apiKey)
      : this(apiKey)
    {
      _cacheProvider = cacheProvider;
    }

    public MovieDbMovie GetMovie(String imdbId, MovieDbLanguage language)
    {
      Stopwatch watch = new Stopwatch();
      watch.Start();
      MovieDbMovie movie = null;
      //Did I get the movie completely from cache or did I have to make an additional online request
      bool loadedAdditionalInfo = false;

      if (movie == null)
      {
        movie = _movieDownloader.DownloadMovie(imdbId, language);
        loadedAdditionalInfo = true;
      }
      else if (!movie.GetAvailableLanguages().Contains(language))
      {//add additional movie to already existing movie

      }

      if (movie != null && _cacheProvider != null)
      {
        //we're using a cache provider
        //if we've loaded data from online source -> save to cache
        if (_cacheProvider.Initialised && loadedAdditionalInfo)
        {
          Log.Info("Store movie " + movie.Id + " with " + _cacheProvider);
          _cacheProvider.SaveToCache(movie);
        }

        AddCacheProviderToBanner(movie, _cacheProvider);
      }
      return movie;
    }

    private void AddCacheProviderToBanner(MovieDbMovie movie, ICacheProvider cacheProvider)
    {
      //Store a ref to the cacheprovider and series id in each banner, so the banners
      //can be stored/loaded to/from cache
      #region add cache provider/series id
      if (movie.Banners != null)
      {
        movie.Banners.ForEach(delegate(MovieDbBanner b)
        {
          b.CacheProvider = cacheProvider;
          b.ObjectId = movie.Id;
        });
      }

      if (movie.Cast != null)
      {
        movie.Cast.ForEach(delegate(MovieDbCast p)
        {
          if (p.Images != null)
          {
            p.Images.ForEach(delegate(MovieDbBanner b)
            {
              b.CacheProvider = cacheProvider;
              b.ObjectId = p.Id;
            });
          }
        });
      }
      #endregion
    }

    public MovieDbMovie GetMovie(int movieId, MovieDbLanguage language)
    {
      Stopwatch watch = new Stopwatch();
      watch.Start();
      MovieDbMovie movie = null;
      //Did I get the movie completely from cache or did I have to make an additional online request
      bool loadedAdditionalInfo = false;

      if (_cacheProvider != null && _cacheProvider.Initialised)
      {
        if (_cacheProvider.IsMovieCached(movieId))
        {
          movie = _cacheProvider.LoadMovieFromCache(movieId);
        }
      }

      if (movie == null)
      {
        movie = _movieDownloader.DownloadMovie(movieId, language);
        loadedAdditionalInfo = true;
      }
      else if (!movie.GetAvailableLanguages().Contains(language))
      {
        //add additional movie to already existing movie
      }

      if (_cacheProvider != null)
      {
        //we're using a cache provider
        //if we've loaded data from online source -> save to cache
        if (_cacheProvider.Initialised && loadedAdditionalInfo)
        {
          Log.Info("Store movie " + movieId + " with " + _cacheProvider.ToString());
          _cacheProvider.SaveToCache(movie);
        }
        AddCacheProviderToBanner(movie, _cacheProvider);
      }

      return movie;
    }

    public List<MovieDbMovie> SearchMovie(String movieName, MovieDbLanguage language)
    {
      return _movieDownloader.MovieSearch(movieName, language);
    }

    public List<MovieDbMovie> SearchMovieByHash(String movieHash)
    {

      return _movieDownloader.MovieSearchByHash(movieHash);
    }

    public List<MovieDbPerson> SearchPerson(String personName, MovieDbLanguage language)
    {
      return _movieDownloader.PersonSearch(personName, language);
    }

    public MovieDbPerson GetPerson(int personId, MovieDbLanguage language)
    {
      Stopwatch watch = new Stopwatch();
      watch.Start();
      MovieDbPerson person = null;
      //Did I get the movie completely from cache or did I have to make an additional online request
      bool loadedAdditionalInfo = false;

      if (_cacheProvider != null && _cacheProvider.Initialised)
      {
        if (_cacheProvider.IsPersonCached(personId))
        {
          person = _cacheProvider.LoadPersonFromCache(personId);
        }
      }

      if (person == null)
      {
        person = _movieDownloader.DownloadPerson(personId, language);
        loadedAdditionalInfo = true;
      }

      if (_cacheProvider != null)
      {//we're using a cache provider
        //if we've loaded data from online source -> save to cache
        if (_cacheProvider.Initialised && loadedAdditionalInfo)
        {
          Log.Info("Store Person " + personId + " with " + _cacheProvider);
          _cacheProvider.SaveToCache(person);
        }
        //Store a ref to the cacheprovider and series id in each banner, so the banners
        //can be stored/loaded to/from cache
        #region add cache provider/series id
        if (person != null && person.Images != null)
        {
          person.Images.ForEach(delegate(MovieDbBanner b)
          {
            b.CacheProvider = _cacheProvider;
            b.ObjectId = person.Id;
          });
        }
        #endregion
      }

      return person;
    }
  }
}
