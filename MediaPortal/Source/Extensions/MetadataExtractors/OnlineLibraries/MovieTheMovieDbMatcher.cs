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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.TheMovieDB;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class MovieTheMovieDbMatcher : BaseMatcher<MovieMatch, string>
  {
    #region Static instance

    public static MovieTheMovieDbMatcher Instance
    {
      get { return ServiceRegistration.Get<MovieTheMovieDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheMovieDB\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");
    protected static string _collectionMatchesFile = Path.Combine(CACHE_PATH, "CollectionMatches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    readonly MatchStorage<MovieCollectionMatch, int> _collectionStorage = new MatchStorage<MovieCollectionMatch, int>(_collectionMatchesFile);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, Movie> _memoryCache = new ConcurrentDictionary<string, Movie>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Contains the initialized TheMovieDbWrapper.
    /// </summary>
    private TheMovieDbWrapper _movieDb;

    private bool _disposed;

    #endregion

    /// <summary>
    /// Tries to lookup the Movie from TheMovieDB and updates the given <paramref name="movieInfo"/> with the online information.
    /// </summary>
    /// <param name="movieInfo">Movie to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateMovie(MovieInfo movieInfo)
    {
      try
      {
        string preferredLookupLanguage = FindBestMatchingLanguage(movieInfo);
        Movie movieDetails;
        if (
          /* Best way is to get details by an unique IMDB id */
          MatchByImdbId(movieInfo, out movieDetails) ||
          TryMatch(movieInfo.MovieName, movieInfo.ReleaseDate.HasValue ? movieInfo.ReleaseDate.Value.Year : 0, preferredLookupLanguage, false, out movieDetails) ||
          /* Prefer passed year, if no year given, try to process movie title and split between title and year */
          movieInfo.ReleaseDate.HasValue && TryMatch(movieInfo.MovieName, movieInfo.ReleaseDate.Value.Year,
          preferredLookupLanguage, false, out movieDetails)
          )
        {
          int movieDbId = 0;
          if (movieDetails != null)
          {
            movieDbId = movieDetails.Id;

            MetadataUpdater.SetOrUpdateId(ref movieInfo.ImDbId, movieDetails.ImdbId);
            MetadataUpdater.SetOrUpdateId(ref movieInfo.MovieDbId, movieDetails.Id);

            MetadataUpdater.SetOrUpdateString(ref movieInfo.MovieName, movieDetails.Title, false);
            MetadataUpdater.SetOrUpdateString(ref movieInfo.OriginalName, movieDetails.OriginalTitle, false);
            MetadataUpdater.SetOrUpdateString(ref movieInfo.Summary, movieDetails.Overview, false);
            MetadataUpdater.SetOrUpdateString(ref movieInfo.Tagline, movieDetails.Tagline, false);

            MetadataUpdater.SetOrUpdateValue(ref movieInfo.ReleaseDate, movieDetails.ReleaseDate);
            MetadataUpdater.SetOrUpdateValue(ref movieInfo.Budget, movieDetails.Budget.HasValue ? movieDetails.Budget.Value : 0);
            MetadataUpdater.SetOrUpdateValue(ref movieInfo.Revenue, movieDetails.Revenue.HasValue ? movieDetails.Revenue.Value : 0);
            MetadataUpdater.SetOrUpdateValue(ref movieInfo.Runtime, movieDetails.Runtime.HasValue ? movieDetails.Runtime.Value : 0);
            MetadataUpdater.SetOrUpdateValue(ref movieInfo.Popularity, movieDetails.Popularity.HasValue ? movieDetails.Popularity.Value : 0);
            MetadataUpdater.SetOrUpdateRatings(ref movieInfo.TotalRating, ref movieInfo.RatingCount, movieDetails.Rating, movieDetails.RatingCount);

            MetadataUpdater.SetOrUpdateList(movieInfo.Genres, movieDetails.Genres.Select(p => p.Name).ToList(), true, false);
            MetadataUpdater.SetOrUpdateList(movieInfo.ProductionCompanies, ConvertToCompanies(movieDetails.ProductionCompanies, CompanyAspect.COMPANY_PRODUCTION), true, false);

            MovieCasts movieCasts;
            if (_movieDb.GetMovieCast(movieDbId, out movieCasts))
            {
              MetadataUpdater.SetOrUpdateList(movieInfo.Actors, ConvertToPersons(movieCasts.Cast, PersonAspect.OCCUPATION_ACTOR), true, false);
              MetadataUpdater.SetOrUpdateList(movieInfo.Writers, ConvertToPersons(movieCasts.Crew.Where(p => p.Job == "Author").ToList(), PersonAspect.OCCUPATION_WRITER), true, false);
              MetadataUpdater.SetOrUpdateList(movieInfo.Directors, ConvertToPersons(movieCasts.Crew.Where(p => p.Job == "Director").ToList(), PersonAspect.OCCUPATION_DIRECTOR), true, false);
              MetadataUpdater.SetOrUpdateList(movieInfo.Characters, ConvertToCharacters(movieInfo.MovieDbId, movieInfo.MovieName, movieCasts.Cast), true, false);
            }

            if (movieDetails.Collection != null && movieDetails.Collection.Id > 0)
            {
              MetadataUpdater.SetOrUpdateId(ref movieInfo.CollectionMovieDbId, movieDetails.Collection.Id);
              MetadataUpdater.SetOrUpdateString(ref movieInfo.CollectionName, movieDetails.Collection.Name, false);
            }

            ImageCollection imageCollection;
            if (movieInfo.Thumbnail == null &&
              _movieDb.GetMovieFanArt(movieInfo.MovieDbId, out imageCollection))
            {
              movieInfo.Thumbnail = GetImage(imageCollection.Posters, "Posters");
            }
          }

          if (movieDbId > 0)
            ScheduleDownload(movieDbId.ToString());
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception while processing movie {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    public bool UpdateMoviePersons(MovieInfo movieInfo, string occupation)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        MovieCasts movieCasts;
        if (movieInfo.MovieDbId > 0 && _movieDb.GetMovieCast(movieInfo.MovieDbId, out movieCasts))
        {
          if (occupation == PersonAspect.OCCUPATION_ACTOR)
          {
            MetadataUpdater.SetOrUpdateList(movieInfo.Actors, ConvertToPersons(movieCasts.Cast, occupation), false, false);
            foreach(PersonInfo person in movieInfo.Actors) UpdatePerson(movieInfo, person);
            return true;
          }
          else if (occupation == PersonAspect.OCCUPATION_WRITER)
          {
            MetadataUpdater.SetOrUpdateList(movieInfo.Writers, ConvertToPersons(movieCasts.Crew.Where(p => p.Job == "Author").ToList(), occupation), false, false);
            foreach (PersonInfo person in movieInfo.Writers) UpdatePerson(movieInfo, person);
            return true;
          }
          else if (occupation == PersonAspect.OCCUPATION_DIRECTOR)
          {
            MetadataUpdater.SetOrUpdateList(movieInfo.Directors, ConvertToPersons(movieCasts.Crew.Where(p => p.Job == "Director").ToList(), occupation), false, false);
            foreach (PersonInfo person in movieInfo.Directors) UpdatePerson(movieInfo, person);
            return true;
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception while processing persons {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    private void UpdatePerson(MovieInfo movie, PersonInfo person)
    {
      if (person.MovieDbId <= 0)
      {
        List<IdResult> results = null;
        if (person.TvRageId > 0)
          results = _movieDb.FindPersonByTvRageId(person.TvRageId);
        else if (!string.IsNullOrEmpty(person.ImdbId))
          results = _movieDb.FindPersonByImdbId(person.ImdbId);

        if (results != null && results.Count == 1)
        {
          person.MovieDbId = results[0].Id;
        }
        else
        {
          string preferredLookupLanguage = FindBestMatchingLanguage(movie);
          List<PersonSearchResult> personsFound;
          if (_movieDb.SearchPersonUnique(person.Name, preferredLookupLanguage, out personsFound))
            person.MovieDbId = personsFound[0].Id;
        }
      }
      if (person.MovieDbId > 0)
      {
        Person personDetail;
        if (_movieDb.GetPerson(person.MovieDbId, out personDetail))
        {
          person.Name = personDetail.Name;
          person.Biography = personDetail.Biography;
          person.DateOfBirth = personDetail.DateOfBirth;
          person.DateOfDeath = personDetail.DateOfDeath;
          person.Orign = personDetail.PlaceOfBirth;
          person.ImdbId = personDetail.ExternalId.ImDbId ?? person.ImdbId;
          person.TvdbId = personDetail.ExternalId.TvDbId.HasValue ? personDetail.ExternalId.TvDbId.Value : 0;
          person.TvRageId = personDetail.ExternalId.TvRageId.HasValue ? personDetail.ExternalId.TvRageId.Value : 0;
        }

        ImageCollection imageCollection;
        if (person.Thumbnail == null && _movieDb.GetPersonFanArt(person.MovieDbId, out imageCollection))
        {
          person.Thumbnail = GetImage(imageCollection.Profiles, "Thumbnails");
        }
      }
    }

    public bool UpdateMovieCharacters(MovieInfo movieInfo)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        MovieCasts movieCasts;
        if (movieInfo.MovieDbId > 0 && _movieDb.GetMovieCast(movieInfo.MovieDbId, out movieCasts))
        {
          MetadataUpdater.SetOrUpdateList(movieInfo.Characters, ConvertToCharacters(movieInfo.MovieDbId, movieInfo.MovieName, movieCasts.Cast), false, false);

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception while processing characters {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    public bool UpdateMovieCompanies(MovieInfo movieInfo, string type)
    {
      try
      {
        Movie movieDetails;

        // Try online lookup
        if (!Init())
          return false;

        if (type != CompanyAspect.COMPANY_PRODUCTION)
          return false;

        if (movieInfo.MovieDbId > 0 && _movieDb.GetMovie(movieInfo.MovieDbId, out movieDetails))
        {
          if (type == CompanyAspect.COMPANY_PRODUCTION)
          {
            MetadataUpdater.SetOrUpdateList(movieInfo.ProductionCompanies, ConvertToCompanies(movieDetails.ProductionCompanies, type), false, false);
            foreach (CompanyInfo company in movieInfo.ProductionCompanies) UpdateCompany(movieInfo, company);
            return true;
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception while processing companies {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    private void UpdateCompany(MovieInfo movie, CompanyInfo company)
    {
      if (company.MovieDbId <= 0)
      {
        string preferredLookupLanguage = FindBestMatchingLanguage(movie);
        List<CompanySearchResult> companiesFound;
        if (_movieDb.SearchCompanyUnique(company.Name, preferredLookupLanguage, out companiesFound))
          company.MovieDbId = companiesFound[0].Id;
      }
      if (company.MovieDbId > 0)
      {
        Company companyDetail;
        if (_movieDb.GetCompany(company.MovieDbId, out companyDetail))
        {
          company.Name = companyDetail.Name;
          company.Description = companyDetail.Description;
        }
        
        if (company.Thumbnail == null)
        {
          ImageItem image = new ImageItem();
          image.Id = company.MovieDbId;
          image.FilePath = companyDetail.LogoPath;
          company.Thumbnail = GetImage(new ImageItem[] { image }, "Logos");
        }
      }
    }

    public bool UpdateCollection(MovieCollectionInfo collectionInfo)
    {
      try
      {
        MovieCollection collectionDetails;

        // Try online lookup
        if (!Init())
          return false;

        if (collectionInfo.MovieDbId > 0 && _movieDb.GetCollection(collectionInfo.MovieDbId, out collectionDetails))
        {
          MetadataUpdater.SetOrUpdateString(ref collectionInfo.Name, collectionDetails.Name, false);
          MetadataUpdater.SetOrUpdateList(collectionInfo.Movies, ConvertToMovies(collectionDetails.Movies), true, false);

          if (collectionInfo.Thumbnail == null)
          {
            collectionInfo.Thumbnail = _movieDb.GetImage(collectionDetails, true);
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception while processing collection {0}", ex, collectionInfo.ToString());
        return false;
      }
    }

    private byte[] GetImage(IEnumerable<ImageItem> images, string category)
    {
      if (images == null)
        return null;

      foreach (ImageItem image in images.Where(b => b.Language == null || b.Language == _movieDb.PreferredLanguage))
      {
        if (_movieDb.DownloadImage(image, category))
        {
          return _movieDb.GetImage(image, category);
        }
      }
      return null;
    }

    private List<MovieInfo> ConvertToMovies(List<MovieSearchResult> movies)
    {
      if (movies == null || movies.Count == 0)
        return new List<MovieInfo>();

      List<MovieInfo> retValue = new List<MovieInfo>();
      foreach (MovieSearchResult movie in movies)
      {
        retValue.Add(new MovieInfo()
        {
          MovieDbId = movie.Id,
          MovieName = movie.Title,
          OriginalName = movie.OriginalTitle,
          ReleaseDate = movie.ReleaseDate,
          Order = retValue.Count
        });
      }
      return retValue;
    }

    private List<PersonInfo> ConvertToPersons(List<CrewItem> crew, string occupation)
    {
      if (crew == null || crew.Count == 0)
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (CrewItem person in crew)
      {
        retValue.Add(new PersonInfo()
        {
          MovieDbId = person.PersonId,
          Name = person.Name,
          Occupation = occupation
        });
      }
      return retValue;
    }

    private List<PersonInfo> ConvertToPersons(List<CastItem> cast, string occupation)
    {
      if (cast == null || cast.Count == 0)
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (CastItem person in cast)
      {
        retValue.Add(new PersonInfo()
        {
          MovieDbId = person.PersonId,
          Name = person.Name,
          Occupation = occupation,
          Order = person.Order
        });
      }
      return retValue;
    }

    private List<CharacterInfo> ConvertToCharacters(int movieId, string movieTitle, List<CastItem> characters)
    {
      if (characters == null || characters.Count == 0)
        return new List<CharacterInfo>();

      List<CharacterInfo> retValue = new List<CharacterInfo>();
      foreach (CastItem person in characters)
        retValue.Add(new CharacterInfo()
        {
          ActorMovieDbId = person.PersonId,
          ActorName = person.Name,
          Name = person.Character,
          Order = person.Order
        });
      return retValue;
    }

    private List<CompanyInfo> ConvertToCompanies(List<ProductionCompany> companies, string type)
    {
      if (companies == null || companies.Count == 0)
        return new List<CompanyInfo>();

      List<CompanyInfo> retValue = new List<CompanyInfo>();
      foreach (ProductionCompany company in companies)
      {
        retValue.Add(new CompanyInfo()
        {
          MovieDbId = company.Id,
          Name = company.Name,
          Type = type
        });
      }
      return retValue;
    }

    private static string FindBestMatchingLanguage(MovieInfo movieInfo)
    {
      CultureInfo mpLocal = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      // If we don't have movie languages available, or the MP2 setting language is available, prefer it.
      if (movieInfo.Languages.Count == 0 || movieInfo.Languages.Contains(mpLocal.TwoLetterISOLanguageName))
        return mpLocal.TwoLetterISOLanguageName;

      // If there is only one language available, use this one.
      if (movieInfo.Languages.Count == 1)
        return movieInfo.Languages[0];

      // If there are multiple languages, that are different to MP2 setting, we cannot guess which one is the "best".
      // By returning null we allow fallback to the default language of the online source (en).
      return null;
    }

    private bool MatchByImdbId(MovieInfo movieInfo, out Movie movieDetails)
    {
      if (!string.IsNullOrEmpty(movieInfo.ImDbId) && _movieDb.GetMovie(movieInfo.ImDbId, out movieDetails))
      {
        SaveMatchToPersistentCache(movieDetails, movieDetails.Title);
        return true;
      }
      movieDetails = null;
      return false;
    }

    public bool TryGetCollectionId(string collectionName, out int collectionId)
    {
      MovieCollectionMatch match = _collectionStorage.GetMatches().Find(m => string.Equals(m.ItemName, collectionName, StringComparison.OrdinalIgnoreCase));
      collectionId = match == null ? 0 : match.Id;
      return collectionId != 0;
    }

    public bool TryGetMovieDbId(string movieName, out int movieDbId)
    {
      Movie movieDetails;
      if (TryMatch(movieName, 0, null, true, out movieDetails))
      {
        movieDbId = movieDetails.Id;
        return true;
      }
      movieDbId = 0;
      return false;
    }

    protected bool TryMatch(string movieName, int year, string language, bool cacheOnly, out Movie movieDetail)
    {
      movieDetail = null;
      try
      {
        // Prefer memory cache
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(movieName, out movieDetail))
          return true;

        // Load cache or create new list
        List<MovieMatch> matches = _storage.GetMatches();

        // Init empty
        movieDetail = null;

        // Use cached values before doing online query
        MovieMatch match = matches.Find(m => 
          string.Equals(m.ItemName, movieName, StringComparison.OrdinalIgnoreCase) || 
          string.Equals(m.MovieDBName, movieName, StringComparison.OrdinalIgnoreCase));
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Try to lookup movie \"{0}\" from cache: {1}", movieName, match != null && !string.IsNullOrEmpty(match.Id));

        // Try online lookup
        if (!Init())
          return false;

        int tmDb = 0;
        if (match != null && !string.IsNullOrEmpty(match.Id))
        {
          if (int.TryParse(match.Id, out tmDb))
          {
            // If this is a known movie, only return the movie details.
            if (match != null)
              return !string.IsNullOrEmpty(match.Id) && _movieDb.GetMovie(tmDb, out movieDetail);
          }
        }

        if (cacheOnly)
          return false;

        List<MovieSearchResult> movies;
        if (_movieDb.SearchMovieUnique(movieName, year, language, out movies))
        {
          MovieSearchResult movieResult = movies[0];
          ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Found unique online match for \"{0}\": \"{1}\"", movieName, movieResult.Title);
          if (_movieDb.GetMovie(movies[0].Id, out movieDetail))
          {
            SaveMatchToPersistentCache(movieDetail, movieName);
            return true;
          }
        }
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: No unique match found for \"{0}\"", movieName);
        // Also save "non matches" to avoid retrying
        _storage.TryAddMatch(new MovieMatch { ItemName = movieName });
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception while processing movie {0}", ex, movieName);
        return false;
      }
      finally
      {
        if (movieDetail != null)
          _memoryCache.TryAdd(movieName, movieDetail);
      }
    }

    private void SaveMatchToPersistentCache(Movie movieDetails, string movieName)
    {
      var onlineMatch = new MovieMatch
      {
        Id = movieDetails.Id.ToString(),
        ItemName = movieName,
        MovieDBName = movieDetails.Title
      };
      _storage.TryAddMatch(onlineMatch);

      // Save collection mapping, if available
      if (movieDetails.Collection != null)
      {
        var collectionMatch = new MovieCollectionMatch
        {
          Id = movieDetails.Collection.Id,
          ItemName = movieDetails.Collection.Name
        };
        _collectionStorage.TryAddMatch(collectionMatch);
      }
    }

    /// <summary>
    /// Check if the memory cache should be cleared and starts an online update of (file-) cached series information.
    /// </summary>
    private void CheckCacheAndRefresh()
    {
      if (DateTime.Now - _memoryCacheInvalidated <= MAX_MEMCACHE_DURATION)
        return;
      _memoryCache.Clear();
      _memoryCacheInvalidated = DateTime.Now;

      // TODO: when updating movie information is implemented, start here a job to do it
    }

    public override bool Init()
    {
      if (!base.Init())
        return false;

      if (_movieDb != null)
        return true;

      _movieDb = new TheMovieDbWrapper();
      // Try to lookup online content in the configured language
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      _movieDb.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
      return _movieDb.Init(CACHE_PATH);
    }

    protected override void DownloadFanArt(string movieDbId)
    {
      try
      {
        if (string.IsNullOrEmpty(movieDbId))
          return;

        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Started for ID {0}", movieDbId);

        if (!Init())
          return;

        int tmDb = 0;
        if (!int.TryParse(movieDbId, out tmDb))
          return;

        if (tmDb <= 0)
          return;

        // If movie belongs to a collection, also download collection poster and fanart
        Movie movie;
        if (!_movieDb.GetMovie(tmDb, out movie))
          return;

        if(movie.Collection != null)
          SaveBanners(movie.Collection);

        ImageCollection imageCollection;
        if (!_movieDb.GetMovieFanArt(tmDb, out imageCollection))
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Begin saving banners for ID {0}", movieDbId);
        SaveBanners(imageCollection.Backdrops, "Backdrops");
        SaveBanners(imageCollection.Posters, "Posters");

        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Begin saving cast and crew for ID {0}", movieDbId);
        MovieCasts movieCasts;
        if (_movieDb.GetMovieCast(tmDb, out movieCasts))
        {
          foreach(CastItem actor in movieCasts.Cast)
          {
            if(_movieDb.GetPersonFanArt(actor.PersonId, out imageCollection))
            {
              SaveBanners(imageCollection.Profiles, "Thumbnails");
            }
          }
          foreach (CrewItem crew in movieCasts.Crew.Where(p => p.Job == "Director").ToList())
          {
            if (_movieDb.GetPersonFanArt(crew.PersonId, out imageCollection))
            {
              SaveBanners(imageCollection.Profiles, "Thumbnails");
            }
          }
          foreach (CrewItem crew in movieCasts.Crew.Where(p => p.Job == "Author").ToList())
          {
            if (_movieDb.GetPersonFanArt(crew.PersonId, out imageCollection))
            {
              SaveBanners(imageCollection.Profiles, "Thumbnails");
            }
          }
        }

        //Save company banners
        Company company;
        foreach (ProductionCompany proCompany in movie.ProductionCompanies)
        {
          if (_movieDb.GetCompany(proCompany.Id, out company))
          {
            ImageItem image = new ImageItem();
            image.Id = company.Id;
            image.FilePath = company.LogoPath;
            SaveBanners(new ImageItem[] { image }, "Logos");
          }
        }

        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Finished saving banners for ID {0}", movieDbId);

        // Remember we are finished
        FinishDownloadFanArt(movieDbId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception downloading FanArt for ID {0}", ex, movieDbId);
      }
    }

    private void SaveBanners(MovieCollection movieCollection)
    {
      bool result = _movieDb.DownloadImages(movieCollection);
      ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download Collection: Saved {0} {1}", movieCollection.Name, result);
    }

    private int SaveBanners(IEnumerable<ImageItem> banners, string category)
    {
      if (banners == null)
        return 0;

      int idx = 0;
      foreach (ImageItem banner in banners.Where(b => b.Language == null || b.Language == _movieDb.PreferredLanguage))
      {
        if (idx >= MAX_FANART_IMAGES)
          break;
        if (_movieDb.DownloadImage(banner, category))
          idx++;
      }
      ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Saved {0} {1}", idx, category);
      return idx;
    }

    protected override void Dispose(bool disposing)
    {
      if (_disposed)
        return;
      if (disposing)
      {
        // We need to call EndDownloads here (as well as in base.Dispose)
        // to make sure the downloads have stopped before we dispose _collectionStorage.
        EndDownloads();
        _collectionStorage.Dispose();
      }
      base.Dispose(disposing);
      _disposed = true;
    }
  }
}
