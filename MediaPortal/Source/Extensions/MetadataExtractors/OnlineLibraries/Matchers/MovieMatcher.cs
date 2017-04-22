#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using MediaPortal.Extensions.OnlineLibraries.Libraries;
using System.Linq;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public abstract class MovieMatcher<TImg, TLang> : BaseMatcher<MovieMatch, string>, IMovieMatcher
  {
    public class MovieMatcherSettings
    {
      public string LastRefresh { get; set; }

      public List<string> LastUpdatedMovies { get; set; }

      public List<string> LastUpdatedMovieCollections { get; set; }
    }

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

      Init();
    }

    public override bool Init()
    {
      if (!_enabled)
        return false;

      if (_wrapper != null)
        return true;

      if (!base.Init())
        return false;

      LoadConfig();

      if (InitWrapper(UseSecureWebCommunication))
      {
        if(_wrapper != null)
          _wrapper.CacheUpdateFinished += CacheUpdateFinished;
        return true;
      }
      return false;
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

    public abstract bool InitWrapper(bool useHttps);

    #endregion

    #region Constants

    public static string FANART_CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArt\");
    private TimeSpan CACHE_CHECK_INTERVAL = TimeSpan.FromMinutes(60);
    private const int MAX_PERSONS = 10;

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
    private bool _primary = false;
    private string _id = null;
    private bool _cacheRefreshable;
    private DateTime? _lastCacheRefresh;
    private DateTime _lastCacheCheck = DateTime.MinValue;
    private string _preferredLanguageCulture = "en-US";

    private SimpleNameMatcher _companyMatcher;
    private SimpleNameMatcher _actorMatcher;
    private SimpleNameMatcher _directorMatcher;
    private SimpleNameMatcher _writerMatcher;
    private SimpleNameMatcher _characterMatcher;

    /// <summary>
    /// Contains the initialized MovieWrapper.
    /// </summary>
    protected ApiWrapper<TImg, TLang> _wrapper = null;

    #endregion

    #region Properties

    public bool Enabled
    {
      get { return _enabled; }
      set { _enabled = value; }
    }

    public bool Primary
    {
      get { return _primary; }
      set { _primary = value; }
    }

    public string Id
    {
      get { return _id; }
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

    /// <summary>
    /// Tries to lookup the Movie online and downloads images.
    /// </summary>
    /// <param name="movieInfo">Movie to check</param>
    /// <returns><c>true</c> if successful</returns>
    public virtual bool FindAndUpdateMovie(MovieInfo movieInfo, bool importOnly)
    {
      try
      {
        // Try online lookup
        if (!Init())
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
          // Load cache or create new list
          List<MovieMatch> matches = _storage.GetMatches();

          // Use cached values before doing online query
          MovieMatch match = matches.Find(m =>
            (string.Equals(m.ItemName, movieInfo.MovieName.ToString(), StringComparison.OrdinalIgnoreCase) || string.Equals(m.OnlineName, movieInfo.MovieName.ToString(), StringComparison.OrdinalIgnoreCase)) &&
            ((movieInfo.ReleaseDate.HasValue && m.Year == movieInfo.ReleaseDate.Value.Year) || !movieInfo.ReleaseDate.HasValue));
          Logger.Debug(_id + ": Try to lookup movie \"{0}\" from cache: {1}", movieInfo, match != null && !string.IsNullOrEmpty(match.Id));

          movieMatch = movieInfo.Clone();
          if (match != null)
          {
            if (SetMovieId(movieMatch, match.Id))
            {
              //If Id was found in cache the online movie info is probably also in the cache
              if (_wrapper.UpdateFromOnlineMovie(movieMatch, language, true))
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

          if (!matchFound && !importOnly)
          {
            Logger.Debug(_id + ": Search for movie {0} online", movieInfo.ToString());

            //Try to update movie information from online source if online Ids are present
            if (!_wrapper.UpdateFromOnlineMovie(movieMatch, language, false))
            {
              //Search for the movie online and update the Ids if a match is found
              if (_wrapper.SearchMovieUniqueAndUpdate(movieMatch, language))
              {
                //Ids were updated now try to update movie information from online source
                if (_wrapper.UpdateFromOnlineMovie(movieMatch, language, false))
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
        if (!importOnly)
          StoreMovieMatch(movieInfo, movieMatch);

        if (matchFound)
        {
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref movieInfo.ImdbId, movieMatch.ImdbId);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref movieInfo.MovieDbId, movieMatch.MovieDbId);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref movieInfo.CollectionMovieDbId, movieMatch.CollectionMovieDbId);

          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref movieInfo.MovieName, movieMatch.MovieName);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref movieInfo.OriginalName, movieMatch.OriginalName);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref movieInfo.Summary, movieMatch.Summary);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref movieInfo.Certification, movieMatch.Certification);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref movieInfo.CollectionName, movieMatch.CollectionName);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref movieInfo.Tagline, movieMatch.Tagline);

          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref movieInfo.Budget, movieMatch.Budget);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref movieInfo.Revenue, movieMatch.Revenue);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref movieInfo.Runtime, movieMatch.Runtime);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref movieInfo.ReleaseDate, movieMatch.ReleaseDate);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref movieInfo.Popularity, movieMatch.Popularity);
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref movieInfo.Score, movieMatch.Score);

          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateRatings(ref movieInfo.Rating, movieMatch.Rating);
          if (movieInfo.Genres.Count == 0)
          {
            movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(movieInfo.Genres, movieMatch.Genres.Distinct().ToList(), true);
          }
          if (movieInfo.Genres.Count > 0)
          {
            movieInfo.HasChanged |= OnlineMatcherService.Instance.AssignMissingMovieGenreIds(movieInfo.Genres);
          }
          movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(movieInfo.Awards, movieMatch.Awards.Distinct().ToList(), true);

          //Limit the number of persons
          if(movieInfo.Actors.Count == 0 && movieMatch.Actors.Count > MAX_PERSONS)
            movieMatch.Actors.RemoveRange(MAX_PERSONS, movieMatch.Actors.Count - MAX_PERSONS);
          if (movieInfo.Characters.Count == 0 && movieMatch.Characters.Count > MAX_PERSONS)
            movieMatch.Characters.RemoveRange(MAX_PERSONS, movieMatch.Characters.Count - MAX_PERSONS);
          if (movieInfo.Directors.Count == 0 && movieMatch.Directors.Count > MAX_PERSONS)
            movieMatch.Directors.RemoveRange(MAX_PERSONS, movieMatch.Directors.Count - MAX_PERSONS);
          if (movieInfo.Writers.Count == 0 && movieMatch.Writers.Count > MAX_PERSONS)
            movieMatch.Writers.RemoveRange(MAX_PERSONS, movieMatch.Writers.Count - MAX_PERSONS);

          //These lists contain Ids and other properties that are not persisted, so they will always appear changed.
          //So changes to these lists will only be stored if something else has changed.
          MetadataUpdater.SetOrUpdateList(movieInfo.Actors, movieMatch.Actors.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), movieInfo.Actors.Count == 0);
          MetadataUpdater.SetOrUpdateList(movieInfo.Characters, movieMatch.Characters.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), movieInfo.Characters.Count == 0);
          MetadataUpdater.SetOrUpdateList(movieInfo.Directors, movieMatch.Directors.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), movieInfo.Directors.Count == 0);
          MetadataUpdater.SetOrUpdateList(movieInfo.ProductionCompanies, movieMatch.ProductionCompanies.Where(c => !string.IsNullOrEmpty(c.Name)).Distinct().ToList(), movieInfo.ProductionCompanies.Count == 0);
          MetadataUpdater.SetOrUpdateList(movieInfo.Writers, movieMatch.Writers.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), movieInfo.Writers.Count == 0);

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

    public virtual bool UpdatePersons(MovieInfo movieInfo, string occupation, bool importOnly)
    {
      try
      {
        // Try online lookup
        if (!Init())
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
          if (!_wrapper.UpdateFromOnlineMoviePerson(movieMatch, person, language, true))
          {
            if (!importOnly)
            {
              Logger.Debug(_id + ": Search for person {0} online", person.ToString());

              //Try to update person information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineMoviePerson(movieMatch, person, language, false))
              {
                //Search for the person online and update the Ids if a match is found
                if (_wrapper.SearchPersonUniqueAndUpdate(person, language))
                {
                  //Ids were updated now try to fetch the online person info
                  if (_wrapper.UpdateFromOnlineMoviePerson(movieMatch, person, language, false))
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
              if (!importOnly)
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
              if (!importOnly)
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
              if (!importOnly)
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

    public virtual bool UpdateCharacters(MovieInfo movieInfo, bool importOnly)
    {
      try
      {
        // Try online lookup
        if (!Init())
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
          if (!_wrapper.UpdateFromOnlineMovieCharacter(movieMatch, character, language, true))
          {
            if (!importOnly)
            {
              Logger.Debug(_id + ": Search for character {0} online", character.ToString());

              //Try to update character information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineMovieCharacter(movieMatch, character, language, false))
              {
                //Search for the character online and update the Ids if a match is found
                if (_wrapper.SearchCharacterUniqueAndUpdate(character, language))
                {
                  //Ids were updated now try to fetch the online character info
                  if (_wrapper.UpdateFromOnlineMovieCharacter(movieMatch, character, language, false))
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
            if (!importOnly)
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

    public virtual bool UpdateCompanies(MovieInfo movieInfo, string companyType, bool importOnly)
    {
      try
      {
        // Try online lookup
        if (!Init())
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
          if (!_wrapper.UpdateFromOnlineMovieCompany(movieMatch, company, language, true))
          {
            if (!importOnly)
            {
              Logger.Debug(_id + ": Search for company {0} online", company.ToString());

              //Try to update company information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineMovieCompany(movieMatch, company, language, false))
              {
                //Search for the company online and update the Ids if a match is found
                if (_wrapper.SearchCompanyUniqueAndUpdate(company, language))
                {
                  //Ids were updated now try to fetch the online company info
                  if (_wrapper.UpdateFromOnlineMovieCompany(movieMatch, company, language, false))
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
              if (!importOnly)
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

    public virtual bool UpdateCollection(MovieCollectionInfo movieCollectionInfo, bool updateMovieList, bool importOnly)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(movieCollectionInfo.Languages);
        bool updated = false;
        MovieCollectionInfo movieCollectionMatch = movieCollectionInfo.Clone();
        movieCollectionMatch.Movies.Clear();
        //Try updating from cache
        if (!_wrapper.UpdateFromOnlineMovieCollection(movieCollectionMatch, language, true))
        {
          if (!importOnly)
          {
            Logger.Debug(_id + ": Search for collection {0} online", movieCollectionInfo.ToString());

            //Try to update movie collection information from online source
            if (_wrapper.UpdateFromOnlineMovieCollection(movieCollectionMatch, language, false))
              updated = true;
          }
        }
        else
        {
          Logger.Debug(_id + ": Found collection {0} in cache", movieCollectionInfo.ToString());
          updated = true;
        }

        if (updated)
        {
          movieCollectionInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref movieCollectionInfo.MovieDbId, movieCollectionMatch.MovieDbId);

          movieCollectionInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref movieCollectionInfo.CollectionName, movieCollectionMatch.CollectionName);

          if (movieCollectionInfo.TotalMovies < movieCollectionMatch.TotalMovies)
          {
            movieCollectionInfo.HasChanged = true;
            movieCollectionInfo.TotalMovies = movieCollectionMatch.TotalMovies;
          }

          if (updateMovieList) //Comparing all movies can be quite time consuming
          {
            foreach (MovieInfo movie in movieCollectionMatch.Movies)
              OnlineMatcherService.Instance.AssignMissingMovieGenreIds(movie.Genres);

            MetadataUpdater.SetOrUpdateList(movieCollectionInfo.Movies, movieCollectionMatch.Movies.Distinct().ToList(), true);
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
      _config.LastUpdatedMovies.Clear();
      SaveConfig();
    }

    public List<MovieCollectionInfo> GetLastChangedMovieCollections()
    {
      List<MovieCollectionInfo> collections = new List<MovieCollectionInfo>();
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
      _config.LastUpdatedMovieCollections.Clear();
      SaveConfig();
    }

    #endregion

    #region FanArt

    public virtual bool ScheduleFanArtDownload(Guid mediaItemId, BaseInfo info, bool force)
    {
      string id;
      string mediaItem = mediaItemId.ToString().ToUpperInvariant();
      if (info is MovieInfo)
      {
        MovieInfo movieInfo = info as MovieInfo;
        if (GetMovieId(movieInfo, out id))
        {
          TLang language = FindBestMatchingLanguage(movieInfo.Languages);
          DownloadData data = new DownloadData()
          {
            FanArtMediaType = FanArtMediaTypes.Movie,
            ShortLanguage = language != null ? language.ToString() : "",
            MediaItemId = mediaItem,
            Name = movieInfo.ToString()
          };
          data.FanArtId[FanArtMediaTypes.Movie] = id;
          return ScheduleDownload(id, data.Serialize(), force);
        }
      }
      else if (info is MovieCollectionInfo)
      {
        MovieCollectionInfo movieCollectionInfo = info as MovieCollectionInfo;
        if (GetMovieCollectionId(movieCollectionInfo, out id))
        {
          TLang language = FindBestMatchingLanguage(movieCollectionInfo.Languages);
          DownloadData data = new DownloadData()
          {
            FanArtMediaType = FanArtMediaTypes.MovieCollection,
            ShortLanguage = language != null ? language.ToString() : "",
            MediaItemId = mediaItem,
            Name = movieCollectionInfo.ToString()
          };
          data.FanArtId[FanArtMediaTypes.MovieCollection] = id;
          return ScheduleDownload(id, data.Serialize(), force);
        }
      }
      else if (info is CompanyInfo)
      {
        CompanyInfo companyInfo = info as CompanyInfo;
        if (GetCompanyId(companyInfo, out id))
        {
          DownloadData data = new DownloadData()
          {
            FanArtMediaType = FanArtMediaTypes.Company,
            ShortLanguage = "",
            MediaItemId = mediaItem,
            Name = companyInfo.ToString()
          };
          data.FanArtId[FanArtMediaTypes.Company] = id;
          return ScheduleDownload(id, data.Serialize(), force);
        }
      }
      else if (info is CharacterInfo)
      {
        CharacterInfo characterInfo = info as CharacterInfo;
        if (GetCharacterId(characterInfo, out id))
        {
          DownloadData data = new DownloadData()
          {
            FanArtMediaType = FanArtMediaTypes.Character,
            ShortLanguage = "",
            MediaItemId = mediaItem,
            Name = characterInfo.ToString()
          };
          data.FanArtId[FanArtMediaTypes.Character] = id;

          string actorId;
          PersonInfo actor = characterInfo.CloneBasicInstance<PersonInfo>();
          if (GetPersonId(actor, out actorId))
          {
            data.FanArtId[FanArtMediaTypes.Actor] = actorId;
          }
          return ScheduleDownload(id, data.Serialize(), force);
        }
      }
      else if (info is PersonInfo)
      {
        PersonInfo personInfo = info as PersonInfo;
        if (GetPersonId(personInfo, out id))
        {
          DownloadData data = new DownloadData()
          {
            ShortLanguage = "",
            MediaItemId = mediaItem,
            Name = personInfo.ToString()
          };
          if (personInfo.Occupation == PersonAspect.OCCUPATION_ACTOR)
          {
            data.FanArtMediaType = FanArtMediaTypes.Actor;
            data.FanArtId[FanArtMediaTypes.Actor] = id;
          }
          else if (personInfo.Occupation == PersonAspect.OCCUPATION_DIRECTOR)
          {
            data.FanArtMediaType = FanArtMediaTypes.Director;
            data.FanArtId[FanArtMediaTypes.Director] = id;
          }
          else if (personInfo.Occupation == PersonAspect.OCCUPATION_WRITER)
          {
            data.FanArtMediaType = FanArtMediaTypes.Writer;
            data.FanArtId[FanArtMediaTypes.Writer] = id;
          }
          return ScheduleDownload(id, data.Serialize(), force);
        }
      }
      return false;
    }

    protected override void DownloadFanArt(FanartDownload<string> fanartDownload)
    {
      string name = fanartDownload.DownloadId;
      try
      {
        if (string.IsNullOrEmpty(fanartDownload.DownloadId))
          return;

        DownloadData data = new DownloadData();
        if (!data.Deserialize(fanartDownload.DownloadId))
          return;

        name = string.Format("{0} ({1})", data.MediaItemId, data.Name);

        if (!Init())
          return;

        try
        {
          TLang language = FindMatchingLanguage(data.ShortLanguage);
          Logger.Debug(_id + " Download: Started for media item {0}", name);
          ApiWrapperImageCollection<TImg> images = null;
          string Id = "";
          if (data.FanArtMediaType == FanArtMediaTypes.Movie)
          {
            Id = data.FanArtId[FanArtMediaTypes.Movie];
            MovieInfo movieInfo = new MovieInfo();
            if (SetMovieId(movieInfo, Id))
            {
              if (_wrapper.GetFanArt(movieInfo, language, data.FanArtMediaType, out images) == false)
              {
                Logger.Debug(_id + " Download: Failed getting images for movie ID {0} [{1}]", Id, name);
                return;
              }

              //Not used
              images.Thumbnails.Clear();
            }
          }
          else if (data.FanArtMediaType == FanArtMediaTypes.MovieCollection)
          {
            Id = data.FanArtId[FanArtMediaTypes.MovieCollection];
            MovieCollectionInfo movieCollectionInfo = new MovieCollectionInfo();
            if (SetMovieCollectionId(movieCollectionInfo, Id))
            {
              if (_wrapper.GetFanArt(movieCollectionInfo, language, data.FanArtMediaType, out images) == false)
              {
                Logger.Debug(_id + " Download: Failed getting images for movie collection ID {0} [{1}]", Id, name);
                return;
              }
            }
          }
          else if (data.FanArtMediaType == FanArtMediaTypes.Actor || data.FanArtMediaType == FanArtMediaTypes.Director || data.FanArtMediaType == FanArtMediaTypes.Writer)
          {
            if (OnlyBasicFanArt)
              return;

            Id = data.FanArtId[data.FanArtMediaType];
            PersonInfo personInfo = new PersonInfo();
            if (SetPersonId(personInfo, Id))
            {
              if (_wrapper.GetFanArt(personInfo, language, data.FanArtMediaType, out images) == false)
              {
                Logger.Debug(_id + " Download: Failed getting images for movie person ID {0} [{1}]", Id, name);
                return;
              }
            }
          }
          else if (data.FanArtMediaType == FanArtMediaTypes.Character)
          {
            if (OnlyBasicFanArt)
              return;

            Id = data.FanArtId[FanArtMediaTypes.Character];
            CharacterInfo characterInfo = new CharacterInfo();
            if (SetCharacterId(characterInfo, Id))
            {
              if (_wrapper.GetFanArt(characterInfo, language, data.FanArtMediaType, out images) == false)
              {
                Logger.Debug(_id + " Download: Failed getting images for movie character ID {0} [{1}]", Id, name);
                return;
              }
            }
          }
          else if (data.FanArtMediaType == FanArtMediaTypes.Company)
          {
            if (OnlyBasicFanArt)
              return;

            Id = data.FanArtId[FanArtMediaTypes.Company];
            CompanyInfo companyInfo = new CompanyInfo();
            if (SetCompanyId(companyInfo, Id))
            {
              if (_wrapper.GetFanArt(companyInfo, language, data.FanArtMediaType, out images) == false)
              {
                Logger.Debug(_id + " Download: Failed getting images for movie company ID {0} [{1}]", Id, name);
                return;
              }
            }
          }
          if (images != null)
          {
            Logger.Debug(_id + " Download: Downloading images for ID {0} [{1}]", Id, name);

            SaveFanArtImages(images.Id, images.Backdrops, language, data.MediaItemId, data.Name, FanArtTypes.FanArt);
            SaveFanArtImages(images.Id, images.Posters, language, data.MediaItemId, data.Name, FanArtTypes.Poster);
            SaveFanArtImages(images.Id, images.Banners, language, data.MediaItemId, data.Name, FanArtTypes.Banner);
            SaveFanArtImages(images.Id, images.Covers, language, data.MediaItemId, data.Name, FanArtTypes.Cover);
            SaveFanArtImages(images.Id, images.Thumbnails, language, data.MediaItemId, data.Name, FanArtTypes.Thumbnail);

            if (!OnlyBasicFanArt)
            {
              SaveFanArtImages(images.Id, images.ClearArt, language, data.MediaItemId, data.Name, FanArtTypes.ClearArt);
              SaveFanArtImages(images.Id, images.DiscArt, language, data.MediaItemId, data.Name, FanArtTypes.DiscArt);
              SaveFanArtImages(images.Id, images.Logos, language, data.MediaItemId, data.Name, FanArtTypes.Logo);
            }

            Logger.Debug(_id + " Download: Finished saving images for ID {0} [{1}]", Id, name);
          }
        }
        finally
        {
          // Remember we are finished
          FinishDownloadFanArt(fanartDownload);
        }
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + " Download: Exception downloading images for {0}", ex, name);
      }
    }

    protected virtual bool VerifyFanArtImage(TImg image, TLang language)
    {
      return image != null;
    }

    protected virtual int SaveFanArtImages(string id, IEnumerable<TImg> images, TLang language, string mediaItemId, string name, string fanartType)
    {
      try
      {
        if (images == null)
          return 0;

        int idx = 0;
        foreach (TImg img in images)
        {
          using (FanArtCache.FanArtCountLock countLock = FanArtCache.GetFanArtCountLock(mediaItemId, fanartType))
          {
            if (countLock.Count >= FanArtCache.MAX_FANART_IMAGES[fanartType])
              break;
            if (!VerifyFanArtImage(img, language))
              continue;
            if (idx >= FanArtCache.MAX_FANART_IMAGES[fanartType])
              break;
            FanArtCache.InitFanArtCache(mediaItemId, name);
            if (_wrapper.DownloadFanArt(id, img, Path.Combine(FANART_CACHE_PATH, mediaItemId, fanartType)))
            {
              countLock.Count++;
              idx++;
            }
            else
            {
              Logger.Warn(_id + " Download: Error downloading FanArt for ID {0} on media item {1} ({2}) of type {3}", id, mediaItemId, name, fanartType);
            }
          }
        }
        Logger.Debug(_id + @" Download: Saved {0} for media item {1} ({2}) of type {3}", idx, mediaItemId, name, fanartType);
        return idx;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + " Download: Exception downloading images for ID {0} [{1} ({2})]", ex, id, mediaItemId, name);
        return 0;
      }
    }

    #endregion
  }
}
