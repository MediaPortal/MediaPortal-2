#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public abstract class MovieMatcher<TImg, TLang> : BaseMatcher<MovieMatch, string, TImg, TLang>, IMovieMatcher
  {
    public class MovieMatcherSettings
    {
      public string LastRefresh { get; set; }

      public List<string> LastUpdatedMovies { get; set; }

      public List<string> LastUpdatedMovieCollections { get; set; }
    }

    protected readonly SemaphoreSlim _initSyncObj = new SemaphoreSlim(1, 1);
    protected bool _isInit = false;

    #region Init

    public MovieMatcher(string cachePath, TimeSpan maxCacheDuration, bool cacheRefreshable)
    {
      _cachePath = cachePath;
      _matchesSettingsFile = Path.Combine(cachePath, "MovieMatches.xml");
      _maxCacheDuration = maxCacheDuration;
      _id = GetType().Name;
      _cacheRefreshable = cacheRefreshable;

      _actorMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "ActorMatches.xml"));
      _directorMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "DirectorMatches.xml"));
      _writerMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "WriterMatches.xml"));
      _characterMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "CharacterMatches.xml"));
      _companyMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "CompanyMatches.xml"));
      _configFile = Path.Combine(cachePath, "MovieConfig.xml");
    }

    public override async Task<bool> InitAsync()
    {
      if (!_enabled)
        return false;

      await _initSyncObj.WaitAsync().ConfigureAwait(false);
      try
      {
        if (_isInit)
          return true;

        if (!await base.InitAsync().ConfigureAwait(false))
          return false;

        LoadConfig();

        if (await InitWrapperAsync(UseSecureWebCommunication).ConfigureAwait(false))
        {
          if (_wrapper != null)
            _wrapper.CacheUpdateFinished += CacheUpdateFinished;
          _isInit = true;
          return true;
        }
        return false;
      }
      finally
      {
        _initSyncObj.Release();
      }
    }

    private void LoadConfig()
    {
      _config = Settings.Load<MovieMatcherSettings>(_configFile);
      if (_config == null)
        _config = new MovieMatcherSettings();
      if (_config.LastRefresh != null)
        _lastCacheRefresh = DateTime.ParseExact(_config.LastRefresh, CONFIG_DATE_FORMAT, CultureInfo.InvariantCulture);
      if (_config.LastUpdatedMovies == null)
        _config.LastUpdatedMovies = new List<string>();
      if (_config.LastUpdatedMovieCollections == null)
        _config.LastUpdatedMovieCollections = new List<string>();
    }

    private void SaveConfig()
    {
      Settings.Save(_configFile, _config);
    }

    public abstract Task<bool> InitWrapperAsync(bool useHttps);

    #endregion

    #region Constants
    
    private TimeSpan CACHE_CHECK_INTERVAL = TimeSpan.FromMinutes(60);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    private DateTime _memoryCacheInvalidated = DateTime.MinValue;
    private ConcurrentDictionary<string, MovieInfo> _memoryCache = new ConcurrentDictionary<string, MovieInfo>(StringComparer.OrdinalIgnoreCase);
    private MovieMatcherSettings _config = new MovieMatcherSettings();
    private string _cachePath;
    private string _matchesSettingsFile;
    private string _configFile;
    private TimeSpan _maxCacheDuration;
    private bool _enabled = true;
    private bool _cacheRefreshable;
    private DateTime? _lastCacheRefresh;
    private DateTime _lastCacheCheck = DateTime.MinValue;
    private string _preferredLanguageCulture = "en-US";

    private SimpleNameMatcher _companyMatcher;
    private SimpleNameMatcher _actorMatcher;
    private SimpleNameMatcher _directorMatcher;
    private SimpleNameMatcher _writerMatcher;
    private SimpleNameMatcher _characterMatcher;

    #endregion

    #region Properties

    public bool Enabled
    {
      get { return _enabled; }
      set { _enabled = value; }
    }

    public bool CacheRefreshable
    {
      get { return _cacheRefreshable; }
    }

    public string PreferredLanguageCulture
    {
      get { return _preferredLanguageCulture; }
      set { _preferredLanguageCulture = value; }
    }

    #endregion

    #region External match storage

    public virtual void StoreActorMatch(PersonInfo person)
    {
      string id;
      if (GetPersonId(person, out id))
        _actorMatcher.StoreNameMatch(id, person.Name, person.Name);
    }

    public virtual void StoreDirectorMatch(PersonInfo person)
    {
      string id;
      if (GetPersonId(person, out id))
        _directorMatcher.StoreNameMatch(id, person.Name, person.Name);
    }

    public virtual void StoreWriterMatch(PersonInfo person)
    {
      string id;
      if (GetPersonId(person, out id))
        _writerMatcher.StoreNameMatch(id, person.Name, person.Name);
    }

    public virtual void StoreCharacterMatch(CharacterInfo character)
    {
      string id;
      if (GetCharacterId(character, out id))
        _characterMatcher.StoreNameMatch(id, character.Name, character.Name);
    }

    public virtual void StoreCompanyMatch(CompanyInfo company)
    {
      string id;
      if (GetCompanyId(company, out id))
        _companyMatcher.StoreNameMatch(id, company.Name, company.Name);
    }

    #endregion

    #region Metadata updaters

    private MovieMatch GetStroredMatch(MovieInfo movieInfo)
    {
      // Load cache or create new list
      List<MovieMatch> matches = _storage.GetMatches();

      // Use cached values before doing online query
      MovieMatch match = matches.Find(m =>
        (string.Equals(m.ItemName, movieInfo.MovieName.ToString(), StringComparison.OrdinalIgnoreCase) || string.Equals(m.OnlineName, movieInfo.MovieName.ToString(), StringComparison.OrdinalIgnoreCase)) &&
        ((movieInfo.ReleaseDate.HasValue && m.Year == movieInfo.ReleaseDate.Value.Year) || !movieInfo.ReleaseDate.HasValue));

      return match;
    }

    /// <summary>
    /// Tries to lookup the Movie online and downloads images.
    /// </summary>
    /// <param name="movieInfo">Movie to check</param>
    /// <returns><c>true</c> if successful</returns>
    public virtual async Task<IEnumerable<MovieInfo>> FindMatchingMoviesAsync(MovieInfo movieInfo)
    {
      List<MovieInfo> matches = new List<MovieInfo>();
      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
          return matches;

        MovieInfo movieSearch = movieInfo.Clone();
        TLang language = FindBestMatchingLanguage(movieInfo.Languages);

        IEnumerable<MovieInfo> onlineMatches = null;
        if (GetMovieId(movieInfo, out string movieId))
        {
          Logger.Debug(_id + ": Get movie from id {0} online", movieId);
          if (await _wrapper.UpdateFromOnlineMovieAsync(movieSearch, language, false))
            onlineMatches = new MovieInfo[] { movieSearch };
        }
        if (onlineMatches == null)
        {
          Logger.Debug(_id + ": Search for movie {0} online", movieInfo.ToString());
          onlineMatches = await _wrapper.SearchMovieMatchesAsync(movieSearch, language).ConfigureAwait(false);
        }
        if (onlineMatches?.Count() > 0)
          matches.AddRange(onlineMatches.Where(m => m.IsBaseInfoPresent));

        return matches;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while matching movie {0}", ex, movieInfo.ToString());
        return matches;
      }
    }

    /// <summary>
    /// Tries to lookup the Movie online and downloads images.
    /// </summary>
    /// <param name="movieInfo">Movie to check</param>
    /// <returns><c>true</c> if successful</returns>
    public virtual async Task<bool> FindAndUpdateMovieAsync(MovieInfo movieInfo)
    {
      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
          return false;

        MovieInfo movieMatch = null;
        string movieId = null;
        bool matchFound = false;
        TLang language = FindBestMatchingLanguage(movieInfo.Languages);

        if (GetMovieId(movieInfo, out movieId))
        {
          // Prefer memory cache
          CheckCacheAndRefresh();
          if (_memoryCache.TryGetValue(movieId, out movieMatch))
          {
            matchFound = true;
          }
        }

        if (!matchFound)
        {
          MovieMatch match = GetStroredMatch(movieInfo);
          movieMatch = movieInfo.Clone();
          if (string.IsNullOrEmpty(movieId))
          {
            Logger.Debug(_id + ": Try to lookup movie \"{0}\" from cache: {1}", movieInfo, match != null && !string.IsNullOrEmpty(match.Id));

            if (match != null)
            {
              if (SetMovieId(movieMatch, match.Id))
              {
                //If Id was found in cache the online movie info is probably also in the cache
                if (await _wrapper.UpdateFromOnlineMovieAsync(movieMatch, language, true).ConfigureAwait(false))
                {
                  Logger.Debug(_id + ": Found movie {0} in cache", movieInfo.ToString());
                  matchFound = true;
                }
              }
              else if (string.IsNullOrEmpty(movieId))
              {
                //Match was found but with invalid Id probably to avoid a retry
                //No Id is available so online search will probably fail again
                return false;
              }
            }
          }
          else
          {
            if (match != null && movieId != match.Id)
            {
              //Id was changed so remove it so it can be updated
              _storage.TryRemoveMatch(match);
            }
          }

          if (!matchFound)
          {
            Logger.Debug(_id + ": Search for movie {0} online", movieInfo.ToString());

            //Try to update movie information from online source if online Ids are present
            if (!await _wrapper.UpdateFromOnlineMovieAsync(movieMatch, language, false).ConfigureAwait(false))
            {
              //Search for the movie online and update the Ids if a match is found
              if (await _wrapper.SearchMovieUniqueAndUpdateAsync(movieMatch, language).ConfigureAwait(false))
              {
                //Ids were updated now try to update movie information from online source
                if (await _wrapper.UpdateFromOnlineMovieAsync(movieMatch, language, false).ConfigureAwait(false))
                  matchFound = true;
              }
            }
            else
            {
              matchFound = true;
            }
          }
        }

        //Always save match even if none to avoid retries
        StoreMovieMatch(movieInfo, movieMatch);

        if (matchFound)
        {
          movieInfo.MergeWith(movieMatch, true);

          //Store person matches
          foreach (PersonInfo person in movieInfo.Actors)
          {
            string id;
            if (GetPersonId(person, out id))
              _actorMatcher.StoreNameMatch(id, person.Name, person.Name);
          }
          foreach (PersonInfo person in movieInfo.Directors)
          {
            string id;
            if (GetPersonId(person, out id))
              _directorMatcher.StoreNameMatch(id, person.Name, person.Name);
          }
          foreach (PersonInfo person in movieInfo.Writers)
          {
            string id;
            if (GetPersonId(person, out id))
              _writerMatcher.StoreNameMatch(id, person.Name, person.Name);
          }

          //Store character matches
          foreach (CharacterInfo character in movieInfo.Characters)
          {
            string id;
            if (GetCharacterId(character, out id))
              _characterMatcher.StoreNameMatch(id, character.Name, character.Name);
          }

          //Store company matches
          foreach (CompanyInfo company in movieInfo.ProductionCompanies)
          {
            string id;
            if (GetCompanyId(company, out id))
              _companyMatcher.StoreNameMatch(id, company.Name, company.Name);
          }

          if (GetMovieId(movieInfo, out movieId))
          {
            _memoryCache.TryAdd(movieId, movieInfo);
          }

          return true;
        }

        return false;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing movie {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    public virtual async Task<bool> UpdatePersonsAsync(MovieInfo movieInfo, string occupation)
    {
      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
          return false;

        TLang language = FindBestMatchingLanguage(movieInfo.Languages);
        bool updated = false;
        MovieInfo movieMatch = movieInfo.Clone();
        List<PersonInfo> persons = new List<PersonInfo>();
        if (occupation == PersonAspect.OCCUPATION_ACTOR)
        {
          foreach (PersonInfo person in movieMatch.Actors)
          {
            string id;
            if (_actorMatcher.GetNameMatch(person.Name, out id))
            {
              if (SetPersonId(person, id))
              {
                //Only add if Id valid if not then it is to avoid a retry
                //and the person should be ignored
                persons.Add(person);
                updated = true;
              }
            }
            else
            {
              persons.Add(person);
            }
          }
        }
        else if (occupation == PersonAspect.OCCUPATION_DIRECTOR)
        {
          foreach (PersonInfo person in movieMatch.Directors)
          {
            string id;
            if (_directorMatcher.GetNameMatch(person.Name, out id))
            {
              if (SetPersonId(person, id))
              {
                //Only add if Id valid if not then it is to avoid a retry
                //and the person should be ignored
                persons.Add(person);
                updated = true;
              }
            }
            else
            {
              persons.Add(person);
            }
          }
        }
        else if (occupation == PersonAspect.OCCUPATION_WRITER)
        {
          foreach (PersonInfo person in movieMatch.Writers)
          {
            string id;
            if (_writerMatcher.GetNameMatch(person.Name, out id))
            {
              if (SetPersonId(person, id))
              {
                //Only add if Id valid if not then it is to avoid a retry
                //and the person should be ignored
                persons.Add(person);
                updated = true;
              }
            }
            else
            {
              persons.Add(person);
            }
          }
        }

        if (persons.Count == 0)
          return true;

        foreach (PersonInfo person in persons)
        {
          //Try updating from cache
          if (!await _wrapper.UpdateFromOnlineMoviePersonAsync(movieMatch, person, language, true).ConfigureAwait(false))
          {
            Logger.Debug(_id + ": Search for person {0} online", person.ToString());

            //Try to update person information from online source if online Ids are present
            if (!await _wrapper.UpdateFromOnlineMoviePersonAsync(movieMatch, person, language, false).ConfigureAwait(false))
            {
              //Search for the person online and update the Ids if a match is found
              if (await _wrapper.SearchPersonUniqueAndUpdateAsync(person, language).ConfigureAwait(false))
              {
                //Ids were updated now try to fetch the online person info
                if (await _wrapper.UpdateFromOnlineMoviePersonAsync(movieMatch, person, language, false).ConfigureAwait(false))
                {
                  //Set as changed because cache has changed and might contain new/updated data
                  movieInfo.HasChanged = true;
                  updated = true;
                }
              }
            }
            else
            {
              updated = true;
            }
          }
          else
          {
            Logger.Debug(_id + ": Found person {0} in cache", person.ToString());
            updated = true;
          }
        }

        if (updated)
        {
          //These lists contain Ids and other properties that are not loaded, so they will always appear changed.
          //So these changes will be ignored and only stored if there is any other reason for it to have changed.
          if (occupation == PersonAspect.OCCUPATION_ACTOR)
            MetadataUpdater.SetOrUpdateList(movieInfo.Actors, movieMatch.Actors.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), false);
          else if (occupation == PersonAspect.OCCUPATION_DIRECTOR)
            MetadataUpdater.SetOrUpdateList(movieInfo.Directors, movieMatch.Directors.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), false);
          else if (occupation == PersonAspect.OCCUPATION_WRITER)
            MetadataUpdater.SetOrUpdateList(movieInfo.Writers, movieMatch.Writers.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), false);
        }

        List<string> thumbs = new List<string>();
        if (occupation == PersonAspect.OCCUPATION_ACTOR)
        {
          foreach (PersonInfo person in movieInfo.Actors)
          {
            string id;
            if (GetPersonId(person, out id))
            {
              _actorMatcher.StoreNameMatch(id, person.Name, person.Name);
            }
            else
            {
              //Store empty match so he/she is not retried
              _actorMatcher.StoreNameMatch("", person.Name, person.Name);
            }
          }
        }
        else if (occupation == PersonAspect.OCCUPATION_DIRECTOR)
        {
          foreach (PersonInfo person in movieInfo.Directors)
          {
            string id;
            if (GetPersonId(person, out id))
            {
              _directorMatcher.StoreNameMatch(id, person.Name, person.Name);
            }
            else
            {
              //Store empty match so he/she is not retried
              _directorMatcher.StoreNameMatch("", person.Name, person.Name);
            }
          }
        }
        else if (occupation == PersonAspect.OCCUPATION_WRITER)
        {
          foreach (PersonInfo person in movieInfo.Writers)
          {
            string id;
            if (GetPersonId(person, out id))
            {
              _writerMatcher.StoreNameMatch(id, person.Name, person.Name);
            }
            else
            {
              //Store empty match so he/she is not retried
              _writerMatcher.StoreNameMatch("", person.Name, person.Name);
            }
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing persons {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    public virtual async Task<bool> UpdateCharactersAsync(MovieInfo movieInfo)
    {
      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
          return false;

        TLang language = FindBestMatchingLanguage(movieInfo.Languages);
        bool updated = false;
        MovieInfo movieMatch = movieInfo.Clone();
        foreach (CharacterInfo character in movieMatch.Characters)
        {
          string id;
          if (_characterMatcher.GetNameMatch(character.Name, out id))
          {
            if (SetCharacterId(character, id))
              updated = true;
            else
              continue;
          }

          //Try updating from cache
          if (!await _wrapper.UpdateFromOnlineMovieCharacterAsync(movieMatch, character, language, true).ConfigureAwait(false))
          {
            Logger.Debug(_id + ": Search for character {0} online", character.ToString());

            //Try to update character information from online source if online Ids are present
            if (!await _wrapper.UpdateFromOnlineMovieCharacterAsync(movieMatch, character, language, false).ConfigureAwait(false))
            {
              //Search for the character online and update the Ids if a match is found
              if (await _wrapper.SearchCharacterUniqueAndUpdateAsync(character, language).ConfigureAwait(false))
              {
                //Ids were updated now try to fetch the online character info
                if (await _wrapper.UpdateFromOnlineMovieCharacterAsync(movieMatch, character, language, false).ConfigureAwait(false))
                {
                  //Set as changed because cache has changed and might contain new/updated data
                  movieInfo.HasChanged = true;
                  updated = true;
                }
              }
            }
            else
            {
              updated = true;
            }
          }
          else
          {
            Logger.Debug(_id + ": Found character {0} in cache", character.ToString());
            updated = true;
          }
        }

        if (updated)
        {
          //These lists contain Ids and other properties that are not loaded, so they will always appear changed.
          //So these changes will be ignored and only stored if there is any other reason for it to have changed.
          MetadataUpdater.SetOrUpdateList(movieInfo.Characters, movieMatch.Characters.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), false);
        }

        List<string> thumbs = new List<string>();
        foreach (CharacterInfo character in movieInfo.Characters)
        {
          string id;
          if (GetCharacterId(character, out id))
          {
            _characterMatcher.StoreNameMatch(id, character.Name, character.Name);
          }
          else
          {
            //Store empty match so he/she is not retried
            _characterMatcher.StoreNameMatch("", character.Name, character.Name);
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing characters {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    public virtual async Task<bool> UpdateCompaniesAsync(MovieInfo movieInfo, string companyType)
    {
      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
          return false;

        TLang language = FindBestMatchingLanguage(movieInfo.Languages);
        bool updated = false;
        MovieInfo movieMatch = movieInfo.Clone();
        List<CompanyInfo> companies = new List<CompanyInfo>();
        if (companyType == CompanyAspect.COMPANY_PRODUCTION)
        {
          foreach (CompanyInfo company in movieMatch.ProductionCompanies)
          {
            string id;
            if (_companyMatcher.GetNameMatch(company.Name, out id))
            {
              if (SetCompanyId(company, id))
              {
                //Only add if Id valid if not then it is to avoid a retry
                //and the company should be ignored
                companies.Add(company);
                updated = true;
              }
            }
            else
            {
              companies.Add(company);
            }
          }
        }

        if (companies.Count == 0)
          return true;

        foreach (CompanyInfo company in companies)
        {
          //Try updating from cache
          if (!await _wrapper.UpdateFromOnlineMovieCompanyAsync(movieMatch, company, language, true).ConfigureAwait(false))
          {
            Logger.Debug(_id + ": Search for company {0} online", company.ToString());

            //Try to update company information from online source if online Ids are present
            if (!await _wrapper.UpdateFromOnlineMovieCompanyAsync(movieMatch, company, language, false).ConfigureAwait(false))
            {
              //Search for the company online and update the Ids if a match is found
              if (await _wrapper.SearchCompanyUniqueAndUpdateAsync(company, language).ConfigureAwait(false))
              {
                //Ids were updated now try to fetch the online company info
                if (await _wrapper.UpdateFromOnlineMovieCompanyAsync(movieMatch, company, language, false).ConfigureAwait(false))
                {
                  //Set as changed because cache has changed and might contain new/updated data
                  movieInfo.HasChanged = true;
                  updated = true;
                }
              }
            }
            else
            {
              updated = true;
            }
          }
          else
          {
            Logger.Debug(_id + ": Found company {0} in cache", company.ToString());
            updated = true;
          }
        }

        if (updated)
        {
          //These lists contain Ids and other properties that are not loaded, so they will always appear changed.
          //So these changes will be ignored and only stored if there is any other reason for it to have changed.
          if (companyType == CompanyAspect.COMPANY_PRODUCTION)
            MetadataUpdater.SetOrUpdateList(movieInfo.ProductionCompanies, movieMatch.ProductionCompanies.Where(c => !string.IsNullOrEmpty(c.Name)).Distinct().ToList(), false);
        }

        List<string> thumbs = new List<string>();
        if (companyType == CompanyAspect.COMPANY_PRODUCTION)
        {
          foreach (CompanyInfo company in movieInfo.ProductionCompanies)
          {
            string id;
            if (GetCompanyId(company, out id))
            {
              _companyMatcher.StoreNameMatch(id, company.Name, company.Name);
            }
            else
            {
              //Store empty match so it is not retried
              _companyMatcher.StoreNameMatch("", company.Name, company.Name);
            }
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing companies {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    public virtual async Task<bool> UpdateCollectionAsync(MovieCollectionInfo movieCollectionInfo, bool updateMovieList)
    {
      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
          return false;

        TLang language = FindBestMatchingLanguage(movieCollectionInfo.Languages);
        bool updated = false;
        MovieCollectionInfo movieCollectionMatch = movieCollectionInfo.Clone();
        movieCollectionMatch.Movies.Clear();
        //Try updating from cache
        if (!await _wrapper.UpdateFromOnlineMovieCollectionAsync(movieCollectionMatch, language, true).ConfigureAwait(false))
        {
          Logger.Debug(_id + ": Search for collection {0} online", movieCollectionInfo.ToString());

          //Try to update movie collection information from online source
          if (await _wrapper.UpdateFromOnlineMovieCollectionAsync(movieCollectionMatch, language, false).ConfigureAwait(false))
            updated = true;
        }
        else
        {
          Logger.Debug(_id + ": Found collection {0} in cache", movieCollectionInfo.ToString());
          updated = true;
        }

        if (updated)
        {
          movieCollectionInfo.MergeWith(movieCollectionMatch, true, updateMovieList);

          if (updateMovieList)
          {
            foreach (MovieInfo movie in movieCollectionMatch.Movies)
            {
              IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
              foreach (var genre in movie.Genres)
              {
                if (!genre.Id.HasValue && converter.GetGenreId(genre.Name, GenreCategory.Movie, null, out int genreId))
                  genre.Id = genreId;
              }
            }
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing collection {0}", ex, movieCollectionInfo.ToString());
        return false;
      }
    }

    #endregion

    #region Metadata update helpers

    private void StoreMovieMatch(MovieInfo movieSearch, MovieInfo movieMatch)
    {
      if (movieSearch.MovieName.IsEmpty)
        return;

      string idValue = null;
      if (movieMatch == null || !GetMovieId(movieMatch, out idValue) || movieMatch.MovieName.IsEmpty)
      {
        _storage.TryAddMatch(new MovieMatch()
        {
          ItemName = movieSearch.MovieName.ToString()
        });
        return;
      }

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

    protected virtual TLang FindBestMatchingLanguage(List<string> mediaLanguages)
    {
      if (typeof(TLang) == typeof(string))
      {
        CultureInfo mpLocal = new CultureInfo(_preferredLanguageCulture);
        // If we don't have movie languages available, or the MP2 setting language is available, prefer it.
        if (mediaLanguages.Count == 0 || mediaLanguages.Contains(mpLocal.TwoLetterISOLanguageName))
          return (TLang)Convert.ChangeType(mpLocal.TwoLetterISOLanguageName, typeof(TLang));

        // If there is only one language available, use this one.
        if (mediaLanguages.Count == 1)
          return (TLang)Convert.ChangeType(mediaLanguages[0], typeof(TLang));

        // If there are multiple languages, that are different to MP2 setting, we cannot guess which one is the "best".
        // Use the preferred language.
        return (TLang)Convert.ChangeType(mpLocal.TwoLetterISOLanguageName, typeof(TLang));
      }
      // By returning null we allow fallback to the default language of the online source (en).
      return default(TLang);
    }

    protected virtual TLang FindMatchingLanguage(string shortLanguageString)
    {
      if (typeof(TLang) == typeof(string) && !string.IsNullOrEmpty(shortLanguageString))
      {
        return (TLang)Convert.ChangeType(shortLanguageString, typeof(TLang));
      }
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

    protected virtual bool SetMovieCollectionId(MovieCollectionInfo movieCollection, string id)
    {
      return false;
    }

    protected virtual bool GetPersonId(PersonInfo person, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool SetPersonId(PersonInfo person, string id)
    {
      return false;
    }

    protected virtual bool GetCharacterId(CharacterInfo character, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool SetCharacterId(CharacterInfo character, string id)
    {
      return false;
    }

    protected virtual bool GetCompanyId(CompanyInfo company, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool SetCompanyId(CompanyInfo company, string id)
    {
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
    {
      if (CacheRefreshable && Enabled)
      {
        if (!_lastCacheRefresh.HasValue)
        {
          if (string.IsNullOrEmpty(_config.LastRefresh))
            _config.LastRefresh = DateTime.Now.ToString(CONFIG_DATE_FORMAT);

          _lastCacheRefresh = DateTime.ParseExact(_config.LastRefresh, CONFIG_DATE_FORMAT, CultureInfo.InvariantCulture);
        }

        if (DateTime.Now - _lastCacheCheck <= CACHE_CHECK_INTERVAL)
          return;

        _lastCacheCheck = DateTime.Now;

        IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>(false);
        if (threadPool != null)
        {
          Logger.Debug(_id + ": Checking local cache");
          threadPool.Add(() =>
          {
            if (_wrapper != null)
            {
              if (_wrapper.RefreshCache(_lastCacheRefresh.Value))
              {
                _lastCacheRefresh = DateTime.Now;
                _config.LastRefresh = _lastCacheRefresh.Value.ToString(CONFIG_DATE_FORMAT, CultureInfo.InvariantCulture);
              }
            }
          });
        }
        SaveConfig();
      }
    }

    private void CacheUpdateFinished(ApiWrapper<TImg, TLang>.UpdateFinishedEventArgs _event)
    {
      try
      {
        if (_event.UpdatedItemType == ApiWrapper<TImg, TLang>.UpdateType.Movie)
        {
          _config.LastUpdatedMovies.AddRange(_event.UpdatedItems);
          SaveConfig();
        }
        if (_event.UpdatedItemType == ApiWrapper<TImg, TLang>.UpdateType.MovieCollection)
        {
          _config.LastUpdatedMovieCollections.AddRange(_event.UpdatedItems);
          SaveConfig();
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex);
      }
    }

    public List<MovieInfo> GetLastChangedMovies()
    {
      List<MovieInfo> movies = new List<MovieInfo>();

      if (!InitAsync().Result)
        return movies;

      foreach (string id in _config.LastUpdatedMovies)
      {
        MovieInfo m = new MovieInfo();
        if (SetMovieId(m, id) && !movies.Contains(m))
          movies.Add(m);
      }
      return movies;
    }

    public void ResetLastChangedMovies()
    {
      if (!InitAsync().Result)
        return;
      _config.LastUpdatedMovies.Clear();
      SaveConfig();
    }

    public List<MovieCollectionInfo> GetLastChangedMovieCollections()
    {
      List<MovieCollectionInfo> collections = new List<MovieCollectionInfo>();

      if (!InitAsync().Result)
        return collections;

      foreach (string id in _config.LastUpdatedMovieCollections)
      {
        MovieCollectionInfo c = new MovieCollectionInfo();
        if (SetMovieCollectionId(c, id) && !collections.Contains(c))
          collections.Add(c);
      }
      return collections;
    }

    public void ResetLastChangedMovieCollections()
    {
      if (!InitAsync().Result)
        return;

      _config.LastUpdatedMovieCollections.Clear();
      SaveConfig();
    }

    #endregion

    #region FanArt

    protected override bool TryGetFanArtInfo(BaseInfo info, out TLang language, out string fanArtMediaType, out bool includeThumbnails)
    {
      language = default(TLang);
      fanArtMediaType = null;
      includeThumbnails = true;

      MovieInfo movieInfo = info as MovieInfo;
      if (movieInfo != null)
      {
        language = FindBestMatchingLanguage(movieInfo.Languages);
        fanArtMediaType = FanArtMediaTypes.Movie;
        includeThumbnails = false;
        return true;
      }

      MovieCollectionInfo movieCollectionInfo = info as MovieCollectionInfo;
      if (movieCollectionInfo != null)
      {
        language = FindBestMatchingLanguage(movieCollectionInfo.Languages);
        fanArtMediaType = FanArtMediaTypes.MovieCollection;
        return true;
      }

      if (OnlyBasicFanArt)
        return false;

      CompanyInfo companyInfo = info as CompanyInfo;
      if (companyInfo != null)
      {
        language = FindMatchingLanguage(string.Empty);
        fanArtMediaType = FanArtMediaTypes.Company;
        return true;
      }

      CharacterInfo characterInfo = info as CharacterInfo;
      if (characterInfo != null)
      {
        language = FindMatchingLanguage(string.Empty);
        fanArtMediaType = FanArtMediaTypes.Character;
        return true;
      }

      PersonInfo personInfo = info as PersonInfo;
      if (personInfo != null)
      {
        if (personInfo.Occupation == PersonAspect.OCCUPATION_ACTOR)
          fanArtMediaType = FanArtMediaTypes.Actor;
        else if (personInfo.Occupation == PersonAspect.OCCUPATION_DIRECTOR)
          fanArtMediaType = FanArtMediaTypes.Director;
        else if (personInfo.Occupation == PersonAspect.OCCUPATION_WRITER)
          fanArtMediaType = FanArtMediaTypes.Writer;
        else
          return false;
        language = FindMatchingLanguage(string.Empty);
        return true;
      }
      return false;
    }

    #endregion
  }
}
