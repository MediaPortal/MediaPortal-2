#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using System.Collections.Generic;
using System.Reflection;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public abstract class MovieMatcher<TImg, TLang> : BaseMatcher<MovieMatch, string>
  {
    #region Init

    public MovieMatcher(string cachePath, TimeSpan maxCacheDuration)
    {
      _cachePath = cachePath;
      _matchesSettingsFile = Path.Combine(cachePath, "MovieMatches.xml");
      _maxCacheDuration = maxCacheDuration;
    }

    private new bool Init()
    {
      if (!base.Init())
        return false;

      if (_wrapper != null)
        return true;

      return InitWrapper();
    }

    public abstract bool InitWrapper();

    #endregion

    #region Constants

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    private DateTime _memoryCacheInvalidated = DateTime.MinValue;
    private ConcurrentDictionary<string, MovieInfo> _memoryCache = new ConcurrentDictionary<string, MovieInfo>(StringComparer.OrdinalIgnoreCase);
    private string _cachePath;
    private string _matchesSettingsFile;
    private TimeSpan _maxCacheDuration;

    /// <summary>
    /// Contains the initialized MovieWrapper.
    /// </summary>
    protected ApiWrapper<TImg, TLang> _wrapper = null;

    #endregion

    #region Metadata updaters

    /// <summary>
    /// Tries to lookup the Movie online and downloads images.
    /// </summary>
    /// <param name="movieInfo">Movie to check</param>
    /// <returns><c>true</c> if successful</returns>
    public virtual bool FindAndUpdateMovie(MovieInfo movieInfo, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        MovieInfo movieMatch = null;
        string movieId = null;
        bool matchFound = false;
        TLang language = FindBestMatchingLanguage(movieInfo);
        if (GetMovieId(movieInfo, out movieId))
        {
          // Prefer memory cache
          CheckCacheAndRefresh();
          if (_memoryCache.TryGetValue(movieId, out movieMatch))
            matchFound = true;
        }

        if(!matchFound)
        {
          // Load cache or create new list
          List<MovieMatch> matches = _storage.GetMatches();

          // Use cached values before doing online query
          MovieMatch match = matches.Find(m =>
            (string.Equals(m.ItemName, movieInfo.MovieName.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.OnlineName, movieInfo.MovieName.ToString(), StringComparison.OrdinalIgnoreCase)) &&
            (movieInfo.ReleaseDate.HasValue && m.Year == movieInfo.ReleaseDate.Value.Year || !movieInfo.ReleaseDate.HasValue || m.Year == 0));
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Try to lookup movie \"{0}\" from cache: {1}", movieInfo, match != null && !string.IsNullOrEmpty(match.Id));

          movieMatch = CloneProperties(movieInfo);
          if (match != null && match.Id != null)
          {
            if (SetMovieId(movieMatch, match.Id))
            {
              //If Id was found in cache the online movie info is probably also in the cache
              if (_wrapper.UpdateFromOnlineMovie(movieMatch, language, true))
                matchFound = true;
            }
          }

          if (!matchFound && !forceQuickMode)
          {
            //Try to update movie information from online source if online Ids are present
            if (!_wrapper.UpdateFromOnlineMovie(movieMatch, language, false))
            {
              //Search for the movie online and update the Ids if a match is found
              if(_wrapper.SearchMovieUniqueAndUpdate(movieMatch, language))
              {
                //Ids were updated now try to update movie information from online source
                if (_wrapper.UpdateFromOnlineMovie(movieMatch, language, false))
                  matchFound = true;
              }
            }
          }
        }

        //Always save match even if none to avoid retries
        StoreMovieMatch(movieInfo, movieMatch);

        if (matchFound)
        {
          MetadataUpdater.SetOrUpdateId(ref movieInfo.ImdbId, movieMatch.ImdbId);
          MetadataUpdater.SetOrUpdateId(ref movieInfo.MovieDbId, movieMatch.MovieDbId);
          MetadataUpdater.SetOrUpdateId(ref movieInfo.CollectionMovieDbId, movieMatch.CollectionMovieDbId);

          MetadataUpdater.SetOrUpdateString(ref movieInfo.MovieName, movieMatch.MovieName);
          MetadataUpdater.SetOrUpdateString(ref movieInfo.OriginalName, movieMatch.OriginalName);
          MetadataUpdater.SetOrUpdateString(ref movieInfo.Summary, movieMatch.Summary);
          MetadataUpdater.SetOrUpdateString(ref movieInfo.Certification, movieMatch.Certification);
          MetadataUpdater.SetOrUpdateString(ref movieInfo.CollectionName, movieMatch.CollectionName);
          MetadataUpdater.SetOrUpdateString(ref movieInfo.Tagline, movieMatch.Tagline);

          MetadataUpdater.SetOrUpdateValue(ref movieInfo.Budget, movieMatch.Budget);
          MetadataUpdater.SetOrUpdateValue(ref movieInfo.Revenue, movieMatch.Revenue);
          MetadataUpdater.SetOrUpdateValue(ref movieInfo.Runtime, movieMatch.Runtime);
          MetadataUpdater.SetOrUpdateValue(ref movieInfo.ReleaseDate, movieMatch.ReleaseDate);
          MetadataUpdater.SetOrUpdateValue(ref movieInfo.Popularity, movieMatch.Popularity);
          MetadataUpdater.SetOrUpdateValue(ref movieInfo.Score, movieMatch.Score);

          MetadataUpdater.SetOrUpdateRatings(ref movieInfo.TotalRating, ref movieInfo.RatingCount, movieMatch.TotalRating, movieMatch.RatingCount);

          MetadataUpdater.SetOrUpdateList(movieInfo.Awards, movieMatch.Awards, true);
          MetadataUpdater.SetOrUpdateList(movieInfo.Actors, movieMatch.Actors, true);
          MetadataUpdater.SetOrUpdateList(movieInfo.Characters, movieMatch.Characters, true);
          MetadataUpdater.SetOrUpdateList(movieInfo.Directors, movieMatch.Directors, true);
          MetadataUpdater.SetOrUpdateList(movieInfo.Genres, movieMatch.Genres, true);
          MetadataUpdater.SetOrUpdateList(movieInfo.ProductionCompanies, movieMatch.ProductionCompanies, true);
          MetadataUpdater.SetOrUpdateList(movieInfo.Writers, movieMatch.Writers, true);

          MetadataUpdater.SetOrUpdateValue(ref movieInfo.Thumbnail, movieMatch.Thumbnail);
          if (movieInfo.Thumbnail == null)
          {
            List<string> thumbs = GetFanArtFiles(movieInfo, FanArtMediaTypes.Movie, FanArtTypes.Poster);
            if (thumbs.Count > 0)
              movieInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
          }

          if (GetMovieId(movieInfo, out movieId))
          {
            _memoryCache.TryAdd(movieId, movieInfo);
            ScheduleDownload(movieId);
          }

          return true;
        }

        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing movie {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdatePersons(MovieInfo movieInfo, string occupation, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(movieInfo);
        bool updated = false;
        MovieInfo movieMatch = CloneProperties(movieInfo);
        List<PersonInfo> persons = new List<PersonInfo>();
        if (occupation == PersonAspect.OCCUPATION_ACTOR)
          persons = movieMatch.Actors;
        else if (occupation == PersonAspect.OCCUPATION_DIRECTOR)
          persons = movieMatch.Directors;
        else if (occupation == PersonAspect.OCCUPATION_WRITER)
          persons = movieMatch.Writers;
        foreach (PersonInfo person in persons)
        {
          //Try updating from cache
          if (!_wrapper.UpdateFromOnlineMoviePerson(person, language, true))
          {
            if (!forceQuickMode)
            {
              //Try to update person information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineMoviePerson(person, language, false))
              {
                //Search for the person online and update the Ids if a match is found
                if (_wrapper.SearchPersonUniqueAndUpdate(person, language))
                {
                  //Ids were updated now try to fetch the online person info
                  if (_wrapper.UpdateFromOnlineMoviePerson(person, language, false))
                    updated = true;
                }
              }
            }
          }
          else
          {
            updated = true;
          }
        }

        if (updated)
        {
          if (occupation == PersonAspect.OCCUPATION_ACTOR)
            MetadataUpdater.SetOrUpdateList(movieInfo.Actors, movieMatch.Actors, false);
          else if (occupation == PersonAspect.OCCUPATION_DIRECTOR)
            MetadataUpdater.SetOrUpdateList(movieInfo.Directors, movieMatch.Directors, false);
          else if (occupation == PersonAspect.OCCUPATION_WRITER)
            MetadataUpdater.SetOrUpdateList(movieInfo.Writers, movieMatch.Writers, false);
        }

        List<string> thumbs = new List<string>();
        if (occupation == PersonAspect.OCCUPATION_ACTOR)
        {
          foreach (PersonInfo person in movieInfo.Actors)
          {
            if (person.Thumbnail == null)
            {
              thumbs = GetFanArtFiles(person, FanArtMediaTypes.Actor, FanArtTypes.Thumbnail);
              if (thumbs.Count > 0)
                person.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }
        }
        else if (occupation == PersonAspect.OCCUPATION_DIRECTOR)
        {
          foreach (PersonInfo person in movieInfo.Directors)
          {
            if (person.Thumbnail == null)
            {
              thumbs = GetFanArtFiles(person, FanArtMediaTypes.Director, FanArtTypes.Thumbnail);
              if (thumbs.Count > 0)
                person.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }
        }
        else if (occupation == PersonAspect.OCCUPATION_WRITER)
        {
          foreach (PersonInfo person in movieInfo.Writers)
          {
            if (person.Thumbnail == null)
            {
              thumbs = GetFanArtFiles(person, FanArtMediaTypes.Writer, FanArtTypes.Thumbnail);
              if (thumbs.Count > 0)
                person.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing persons {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateCharacters(MovieInfo movieInfo, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(movieInfo);
        bool updated = false;
        MovieInfo movieMatch = CloneProperties(movieInfo);
        foreach (CharacterInfo character in movieMatch.Characters)
        {
          //Try updating from cache
          if (!_wrapper.UpdateFromOnlineMovieCharacter(character, language, true))
          {
            if (!forceQuickMode)
            {
              //Try to update character information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineMovieCharacter(character, language, false))
              {
                //Search for the character online and update the Ids if a match is found
                if (_wrapper.SearchCharacterUniqueAndUpdate(character, language))
                {
                  //Ids were updated now try to fetch the online character info
                  if (_wrapper.UpdateFromOnlineMovieCharacter(character, language, false))
                    updated = true;
                }
              }
            }
          }
          else
          {
            updated = true;
          }
        }

        if(updated)
          MetadataUpdater.SetOrUpdateList(movieInfo.Characters, movieMatch.Characters, false);

        List<string> thumbs = new List<string>();
        foreach (CharacterInfo character in movieInfo.Characters)
        {
          if (character.Thumbnail == null)
          {
            thumbs = GetFanArtFiles(character, FanArtMediaTypes.Character, FanArtTypes.Thumbnail);
            if (thumbs.Count > 0)
              character.Thumbnail = File.ReadAllBytes(thumbs[0]);
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing characters {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateCompanies(MovieInfo movieInfo, string companyType, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(movieInfo);
        bool updated = false;
        MovieInfo movieMatch = CloneProperties(movieInfo);
        List<CompanyInfo> companies = new List<CompanyInfo>();
        if (companyType == CompanyAspect.COMPANY_PRODUCTION)
          companies = movieMatch.ProductionCompanies;
        foreach (CompanyInfo company in companies)
        {
          //Try updating from cache
          if (!_wrapper.UpdateFromOnlineMovieCompany(company, language, true))
          {
            if (!forceQuickMode)
            {
              //Try to update company information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineMovieCompany(company, language, false))
              {
                //Search for the company online and update the Ids if a match is found
                if (_wrapper.SearchCompanyUniqueAndUpdate(company, language))
                {
                  //Ids were updated now try to fetch the online company info
                  if (_wrapper.UpdateFromOnlineMovieCompany(company, language, false))
                    updated = true;
                }
              }
            }
          }
          else
          {
            updated = true;
          }
        }

        if (updated)
        {
          if (companyType == CompanyAspect.COMPANY_PRODUCTION)
            MetadataUpdater.SetOrUpdateList(movieInfo.ProductionCompanies, movieMatch.ProductionCompanies, false);
        }

        List<string> thumbs = new List<string>();
        if (companyType == CompanyAspect.COMPANY_PRODUCTION)
        {
          foreach (CompanyInfo company in movieInfo.ProductionCompanies)
          {
            if (company.Thumbnail == null)
            {
              thumbs = GetFanArtFiles(company, FanArtMediaTypes.Company, FanArtTypes.Logo);
              if (thumbs.Count > 0)
                company.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing companies {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateCollection(MovieCollectionInfo movieCollectionInfo, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = default(TLang);
        bool updated = false;
        MovieCollectionInfo movieCollectionMatch = CloneProperties(movieCollectionInfo);
        //Try updating from cache
        if (!_wrapper.UpdateFromOnlineMovieCollection(movieCollectionMatch, language, true))
        {
          if (!forceQuickMode)
          {
            //Try to update movie collection information from online source
            if (_wrapper.UpdateFromOnlineMovieCollection(movieCollectionMatch, language, false))
              updated = true;
          }
        }
        else
        {
          updated = true;
        }

        if (updated)
        {
          MetadataUpdater.SetOrUpdateId(ref movieCollectionInfo.MovieDbId, movieCollectionMatch.MovieDbId);

          MetadataUpdater.SetOrUpdateString(ref movieCollectionInfo.CollectionName, movieCollectionMatch.CollectionName);

          MetadataUpdater.SetOrUpdateList(movieCollectionInfo.Movies, movieCollectionMatch.Movies, true);

          MetadataUpdater.SetOrUpdateValue(ref movieCollectionInfo.Thumbnail, movieCollectionMatch.Thumbnail);
        }

        if (movieCollectionInfo.Thumbnail == null)
        {
          List<string> thumbs = GetFanArtFiles(movieCollectionInfo, FanArtMediaTypes.MovieCollection, FanArtTypes.Poster);
          if (thumbs.Count > 0)
            movieCollectionInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
        }

        return updated;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing collection {0}", ex, movieCollectionInfo.ToString());
        return false;
      }
    }

    #endregion

    #region Metadata update helpers

    private T CloneProperties<T>(T obj)
    {
      if (obj == null)
        return default(T);
      Type type = obj.GetType();

      if (type.IsValueType || type == typeof(string))
      {
        return obj;
      }
      else if (type.IsArray)
      {
        Type elementType = obj.GetType().GetElementType();
        var array = obj as Array;
        Array arrayCopy = Array.CreateInstance(elementType, array.Length);
        for (int i = 0; i < array.Length; i++)
        {
          arrayCopy.SetValue(CloneProperties(array.GetValue(i)), i);
        }
        return (T)Convert.ChangeType(arrayCopy, obj.GetType());
      }
      else if (type.IsClass)
      {
        T newInstance = (T)Activator.CreateInstance(obj.GetType());
        FieldInfo[] fields = type.GetFields(BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
          object fieldValue = field.GetValue(obj);
          if (fieldValue == null)
            continue;
          field.SetValue(newInstance, CloneProperties(fieldValue));
        }
        return newInstance;
      }
      return default(T);
    }

    private void StoreMovieMatch(MovieInfo movieSearch, MovieInfo movieMatch)
    {
      if (movieMatch == null)
      {
        _storage.TryAddMatch(new MovieMatch()
        {
          ItemName = movieSearch.ToString()
        });
        return;
      }

      string idValue = null;
      if (GetMovieId(movieSearch, out idValue))
      {
        var onlineMatch = new MovieMatch
        {
          Id = idValue,
          ItemName = movieSearch.MovieName.ToString(),
          OnlineName = movieMatch.MovieName.ToString(),
          Year = movieSearch.ReleaseDate.HasValue ? movieSearch.ReleaseDate.Value.Year : 
            movieMatch.ReleaseDate.HasValue ? movieMatch.ReleaseDate.Value.Year : 0
        };
        _storage.TryAddMatch(onlineMatch);
      }
    }

    protected virtual TLang FindBestMatchingLanguage(MovieInfo movieInfo)
    {
      if (typeof(TLang) == typeof(string))
      {
        CultureInfo mpLocal = ServiceRegistration.Get<ILocalization>().CurrentCulture;
        // If we don't have movie languages available, or the MP2 setting language is available, prefer it.
        if (movieInfo.Languages.Count == 0 || movieInfo.Languages.Contains(mpLocal.TwoLetterISOLanguageName))
          return (TLang)Convert.ChangeType(mpLocal.TwoLetterISOLanguageName, typeof(TLang));

        // If there is only one language available, use this one.
        if (movieInfo.Languages.Count == 1)
          return (TLang)Convert.ChangeType(movieInfo.Languages[0], typeof(TLang));
      }
      // If there are multiple languages, that are different to MP2 setting, we cannot guess which one is the "best".
      // By returning null we allow fallback to the default language of the online source (en).
      return default(TLang);
    }

    #endregion

    #region Ids

    protected abstract bool GetMovieId(MovieInfo movie, out string id);

    protected abstract bool SetMovieId(MovieInfo movie, string id);

    protected virtual bool GetMovieCollectionId(MovieCollectionInfo movieCollection, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool GetPersonId(PersonInfo person, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool GetCharacterId(CharacterInfo character, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool GetCompanyId(CompanyInfo company, out string id)
    {
      id = null;
      return false;
    }

    #endregion

    #region Caching

    /// <summary>
    /// Check if the memory cache should be cleared and starts an online update of (file-) cached series information.
    /// </summary>
    private void CheckCacheAndRefresh()
    {
      if (DateTime.Now - _memoryCacheInvalidated <= _maxCacheDuration)
        return;
      _memoryCache.Clear();
      _memoryCacheInvalidated = DateTime.Now;

      RefreshCache();
    }

    protected virtual void RefreshCache()
    { }

    #endregion

    #region FanArt

    public virtual List<string> GetFanArtFiles<T>(T infoObject, string scope, string type)
    {
      List<string> fanartFiles = new List<string>();
      string path = null;
      string id;
      if (scope == FanArtMediaTypes.MovieCollection)
      {
        MovieCollectionInfo collection = infoObject as MovieCollectionInfo;
        if (collection != null && GetMovieCollectionId(collection, out id))
        {
          path = Path.Combine(_cachePath, id, string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtMediaTypes.Movie)
      {
        MovieInfo movie = infoObject as MovieInfo;
        if (movie != null && GetMovieId(movie, out id))
        {
          path = Path.Combine(_cachePath, id, string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtMediaTypes.Artist || scope == FanArtMediaTypes.Director || scope == FanArtMediaTypes.Writer)
      {
        PersonInfo person = infoObject as PersonInfo;
        if (person != null && GetPersonId(person, out id))
        {
          path = Path.Combine(_cachePath, id, string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtMediaTypes.Character)
      {
        CharacterInfo character = infoObject as CharacterInfo;
        if (character != null && GetCharacterId(character, out id))
        {
          path = Path.Combine(_cachePath, id, string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtMediaTypes.Company)
      {
        CompanyInfo company = infoObject as CompanyInfo;
        if (company != null && GetCompanyId(company, out id))
        {
          path = Path.Combine(_cachePath, id, string.Format(@"{0}\{1}\", scope, type));
        }
      }
      if (Directory.Exists(path))
      {
        fanartFiles.AddRange(Directory.GetFiles(path, "*.jpg"));
        while (fanartFiles.Count > MAX_FANART_IMAGES)
        {
          fanartFiles.RemoveAt(fanartFiles.Count - 1);
        }
      }
      return fanartFiles;
    }

    protected override void DownloadFanArt(string downloadId)
    {
      try
      {
        if (string.IsNullOrEmpty(downloadId))
          return;

        string movieId = downloadId;

        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Started for ID {0}", downloadId);

        MovieInfo movieInfo;
        if (!_memoryCache.TryGetValue(movieId, out movieInfo))
          return;

        if (!Init())
          return;

        TLang language = FindBestMatchingLanguage(movieInfo);
        ApiWrapperImageCollection<TImg> images;
        string scope = FanArtMediaTypes.Movie;
        if (_wrapper.GetFanArt(movieInfo, language, scope, out images) == false)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Failed getting images for movie ID {0}", downloadId);

          return;
        }

        if(images != null)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading movie images for ID {0}", downloadId);

          SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
          SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
          SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
          SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
          SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
          SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
          SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
          SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
        }

        scope = FanArtMediaTypes.MovieCollection;
        if (_wrapper.GetFanArt(movieInfo, language, scope, out images) == false)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Failed getting collection images for ID {0}", downloadId);

          return;
        }

        if (images != null)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading collection images for ID {0}", downloadId);

          SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
          SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
          SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
          SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
          SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
          SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
          SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
          SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
        }

        scope = FanArtMediaTypes.Actor;
        List<PersonInfo> persons = movieInfo.Actors;
        if(persons != null && persons.Count > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading actors images for ID {0}", downloadId);
          foreach (PersonInfo person in persons)
          {
            if (_wrapper.GetFanArt(person, language, scope, out images) == false)
            {
              if (images != null)
              {
                SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
                SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
                SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
                SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
                SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
                SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
                SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
                SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
              }
            }
          }
        }

        scope = FanArtMediaTypes.Director;
        persons = movieInfo.Directors;
        if (persons != null && persons.Count > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading director images for ID {0}", downloadId);
          foreach (PersonInfo person in persons)
          {
            if (_wrapper.GetFanArt(person, language, scope, out images) == false)
            {
              if (images != null)
              {
                SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
                SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
                SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
                SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
                SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
                SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
                SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
                SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
              }
            }
          }
        }

        scope = FanArtMediaTypes.Writer;
        persons = movieInfo.Writers;
        if (persons != null && persons.Count > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading writer images for ID {0}", downloadId);
          foreach (PersonInfo person in persons)
          {
            if (_wrapper.GetFanArt(person, language, scope, out images) == false)
            {
              if (images != null)
              {
                SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
                SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
                SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
                SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
                SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
                SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
                SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
                SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
              }
            }
          }
        }

        scope = FanArtMediaTypes.Character;
        List<CharacterInfo> characters = movieInfo.Characters;
        if (characters != null && characters.Count > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading character images for ID {0}", downloadId);
          foreach (CharacterInfo character in characters)
          {
            if (_wrapper.GetFanArt(character, language, scope, out images) == false)
            {
              if (images != null)
              {
                SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
                SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
                SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
                SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
                SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
                SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
                SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
                SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
              }
            }
          }
        }

        scope = FanArtMediaTypes.Company;
        List<CompanyInfo> companies = movieInfo.ProductionCompanies;
        if (companies != null && companies.Count > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading company images for ID {0}", downloadId);
          foreach (CompanyInfo company in companies)
          {
            if (_wrapper.GetFanArt(company, language, scope, out images) == false)
            {
              if (images != null)
              {
                SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
                SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
                SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
                SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
                SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
                SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
                SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
                SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
              }
            }
          }
        }

        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Finished saving images for ID {0}", downloadId);

        // Remember we are finished
        FinishDownloadFanArt(downloadId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Exception downloading images for ID {0}", ex, downloadId);
      }
    }

    protected virtual bool VerifyFanArtImage(TImg image)
    {
      return false;
    }

    protected virtual int SaveFanArtImages(string id, IEnumerable<TImg> images, string scope, string type)
    {
      if (images == null)
        return 0;

      int idx = 0;
      foreach (TImg img in images)
      {
        if (!VerifyFanArtImage(img))
          continue;
        if (idx >= MAX_FANART_IMAGES)
          break;
        if (_wrapper.DownloadFanArt(id, img, scope, type))
          idx++;
      }
      ServiceRegistration.Get<ILogger>().Debug(GetType().Name + @" Download: Saved {0} {1}\{2}", idx, scope, type);
      return idx;

    }

    #endregion
  }
}
