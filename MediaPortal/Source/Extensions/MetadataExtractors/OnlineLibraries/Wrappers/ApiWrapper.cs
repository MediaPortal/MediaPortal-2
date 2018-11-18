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
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  public abstract class ApiWrapper<TImg, TLang>
  {
    private TLang _preferredLanguage;
    private TLang _defaultLanguage;
    private List<TLang> _regionLanguages;
    private string _cachePath;

    public const int MAX_LEVENSHTEIN_DIST = 4;

    private enum AudioValueToCheck
    {
      ArtistLax,
      AlbumLax,
      Year,
      TrackNum,
      ArtistStrict,
      AlbumStrict,
      Compilation,
      MBGroup,
      Barcode,
      Art,
      Discs,
      Language,
    }

    #region Language

    /// <summary>
    /// Sets the preferred language.
    /// </summary>
    /// <param name="lang">Language used by API</param>
    public void SetPreferredLanguage(TLang lang)
    {
      _preferredLanguage = lang;
    }

    /// <summary>
    /// Returns the language that matches the value set by <see cref="SetPreferredLanguage"/> or the default language.
    /// </summary>
    public TLang PreferredLanguage
    {
      get { return _preferredLanguage == null ? _defaultLanguage : _preferredLanguage; }
    }

    /// <summary>
    /// Sets the default language to use when no matches are found.
    /// </summary>
    /// <param name="lang">Language used by API</param>
    public void SetDefaultLanguage(TLang lang)
    {
      _defaultLanguage = lang;
    }

    /// <summary>
    /// Sets the languages describing regions to use when no matches are found.
    /// </summary>
    /// <param name="lang">Language used by API</param>
    public void SetRegionLanguages(List<TLang> langs)
    {
      _regionLanguages = langs;
    }

    /// <summary>
    /// Returns the language that matches the value set by <see cref="SetDefaultLanguage"/>.
    /// </summary>
    public TLang DefaultLanguage
    {
      get { return _defaultLanguage; }
    }

    /// <summary>
    /// Returns the languages that matches the value set by <see cref="SetRegionLanguages"/>.
    /// </summary>
    public List<TLang> RegionLanguages
    {
      get { return _regionLanguages; }
    }

    #endregion

    #region Cache

    /// <summary>
    /// Sets path to use for caching downloads.
    /// </summary>
    /// <param name="path">The path to use for caching downloads</param>
    public void SetCachePath(string path)
    {
      _cachePath = path;
    }

    /// <summary>
    /// The path to use for caching downloads as set by <see cref="SetCachePath"/>.
    /// </summary>
    public string CachePath
    {
      get { return _cachePath; }
    }

    /// <summary>
    /// Refreshes or deletes any outdated cache files.
    /// </summary>
    public virtual bool RefreshCache(DateTime lastRefresh)
    {
      return false;
    }

    protected virtual bool IsCacheChanged(DateTime itemLastChanged, string filename)
    {
      try
      {
        FileInfo file = new FileInfo(filename);
        if (!file.Exists)
          return false;
        if (file.CreationTime > itemLastChanged)
          return true;
        return false;
      }
      catch
      {
        return false;
      }
    }

    public virtual bool IsCacheChangedForItem(BaseInfo info, TLang language)
    {
      foreach (string filename in GetCacheFiles(info, language))
      {
        if (info.LastChanged.HasValue && IsCacheChanged(info.LastChanged.Value, filename))
          return true;
      }
      return false;
    }

    public virtual string[] GetCacheFiles(BaseInfo info, TLang language)
    {
      return new string[0];
    }

    #endregion

    #region Events

    public enum UpdateType
    {
      Series,
      Season,
      Episode,
      Movie,
      MovieCollection,
      Audio,
      AudioAlbum,
      Person,
      Actor,
      Director,
      Writer,
      Artist,
      Composer,
      Company,
      TVNetwork,
      MusicLabel
    };

    /// <summary>
    /// EventArgs used when an update has finished, contains start date, end date and 
    /// an overview of all updated content
    /// </summary>
    public class UpdateFinishedEventArgs : EventArgs
    {
      /// <summary>
      /// Constructor for UpdateFinishedEventArgs
      /// </summary>
      /// <param name="started">When did the update start</param>
      /// <param name="ended">When did the update finish</param>
      /// <param name="updateType">The type items that were updated</param>
      /// <param name="updatedItems">List of all items (ids) that were updated</param>
      public UpdateFinishedEventArgs(DateTime started, DateTime ended, UpdateType updateType, List<string> updatedItems)
      {
        UpdateStarted = started;
        UpdateFinished = ended;
        UpdatedItemType = updateType;
        UpdatedItems = updatedItems;
      }
      /// <summary>
      /// When did the update start
      /// </summary>
      public DateTime UpdateStarted { get; set; }

      /// <summary>
      /// When did the update finish
      /// </summary>
      public DateTime UpdateFinished { get; set; }

      /// <summary>
      /// The type of items updated
      /// </summary>
      public UpdateType UpdatedItemType { get; set; }

      /// <summary>
      /// List of all items (ids) that were updated
      /// </summary>
      public List<string> UpdatedItems { get; set; }
    }

    /// <summary>
    /// Delegate for UpdateFinished event
    /// </summary>
    /// <param name="_event">EventArgs</param>
    public delegate void UpdateFinishedDelegate(UpdateFinishedEventArgs _event);

    /// <summary>
    /// Called when a running update finishes, UpdateFinishedEventArgs gives an overview
    /// of the update
    /// </summary>
    public event UpdateFinishedDelegate CacheUpdateFinished;

    protected void FireCacheUpdateFinished(DateTime started, DateTime ended, UpdateType updateType, List<string> updatedItems)
    {
      if(CacheUpdateFinished != null)
      {
        CacheUpdateFinished(new UpdateFinishedEventArgs(started, ended, updateType, updatedItems));
      }
    }

    #endregion

    #region Movies

    /// <summary>
    /// Search for Movie.
    /// </summary>
    /// <param name="movieSearch">Movie search parameters</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns>A list of matches..</returns>
    public virtual Task<List<MovieInfo>> SearchMovieAsync(MovieInfo movieSearch, TLang language)
    {
      return Task.FromResult<List<MovieInfo>>(null);
    }

    /// <summary>
    /// Search for matches of Movie. This method tries to find the any matching Movies in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// - If movies name contains " - ", it splits on this and tries to runs again using the first part (combined titles)
    /// </summary>
    /// <param name="movieSearch">Movie search parameters</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns>A list of all matching movies found.</returns>
    public async Task<IEnumerable<MovieInfo>> SearchMovieMatchesAsync(MovieInfo movieSearch, TLang language)
    {
      language = language != null ? language : PreferredLanguage;
      List<MovieInfo> movies = await SearchMovieAsync(movieSearch, language).ConfigureAwait(false);
      if (movies?.Count > 0)
        return movies;

      if (!language.Equals(_defaultLanguage))
      {
        movies = await SearchMovieAsync(movieSearch, _defaultLanguage).ConfigureAwait(false);
        if (movies?.Count > 0)
          return movies;

        // If also no match in default language is found, we will look for combined movies names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        SimpleTitle originalName = movieSearch.MovieName;
        string namePart = movieSearch.MovieName.Text.Split(new[] { '-' })[0].Trim();
        movieSearch.MovieName = new SimpleTitle(namePart);
        movies = await SearchMovieAsync(movieSearch, _defaultLanguage).ConfigureAwait(false);
        movieSearch.MovieName = originalName;
        if (movies?.Count > 0)
          return movies;
      }
      return null;
    }

    /// <summary>
    /// Search for unique matches of Movie. This method tries to find the best matching Movie in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// - If movies name contains " - ", it splits on this and tries to runs again using the first part (combined titles)
    /// </summary>
    /// <param name="movieSearch">Movie search parameters</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns><c>true</c> if at exactly one Movie was found.</returns>
    public async Task<bool> SearchMovieUniqueAndUpdateAsync(MovieInfo movieSearch, TLang language)
    {
      language = language != null ? language : PreferredLanguage;
      List<MovieInfo> movies = await SearchMovieAsync(movieSearch, language).ConfigureAwait(false);
      if (movies == null)
        return false;
      if (TestMovieMatch(movieSearch, ref movies))
      {
        movieSearch.CopyIdsFrom(movies[0]);
        return true;
      }

      if (movies.Count == 0 && !language.Equals(_defaultLanguage))
      {
        movies = await SearchMovieAsync(movieSearch, _defaultLanguage).ConfigureAwait(false);
        if (movies == null)
          return false;

        // If also no match in default language is found, we will look for combined movies names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        if (!TestMovieMatch(movieSearch, ref movies) && movieSearch.MovieName.Text.Contains("-"))
        {
          SimpleTitle originalName = movieSearch.MovieName;
          string namePart = movieSearch.MovieName.Text.Split(new[] { '-' })[0].Trim();
          movieSearch.MovieName = new SimpleTitle(namePart);
          if (await SearchMovieUniqueAndUpdateAsync(movieSearch, language).ConfigureAwait(false))
            return true;
          movieSearch.MovieName = originalName;
          return false;
        }
        if (movies.Count == 1)
        {
          movieSearch.CopyIdsFrom(movies[0]);
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Tests for movie matches. 
    /// </summary>
    /// <param name="movieSearch">Movie search parameters</param>
    /// <param name="movies">Potential online matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    protected virtual bool TestMovieMatch(MovieInfo movieSearch, ref List<MovieInfo> movies)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Test Match for \"{0}\"", movieSearch);

      if (movies.Count == 1)
      {
        if (movieSearch.MovieName.IsEmpty || GetLevenshteinDistance(movies[0], movieSearch) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", movieSearch);
          return true;
        }
        if (NamesAreMostlyEqual(movies[0], movieSearch))
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", movieSearch);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        movies.Clear();
        return false;
      }

      // Multiple matches
      if (movies.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches for \"{0}\" ({1}). Try to find exact name match.", movieSearch, movies.Count);

        // Try to match the year, if available
        if (movieSearch.ReleaseDate.HasValue)
        {
          var yearFiltered = movies.Where(s => s.ReleaseDate.HasValue && s.ReleaseDate.Value.Year == movieSearch.ReleaseDate.Value.Year).ToList();
          if (yearFiltered.Count == 1)
          {
            ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", movieSearch);
            movies = yearFiltered;
            return true;
          }
          else if(yearFiltered.Count > 0)
          {
            movies = yearFiltered;
          }
        }

        var exactMatches = movies.Where(s => !s.MovieName.IsEmpty && 
          (s.MovieName.IsEmpty && s.MovieName.Text == movieSearch.MovieName.Text || s.OriginalName == movieSearch.MovieName.Text || GetLevenshteinDistance(s, movieSearch) == 0)).ToList();
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", movieSearch);
          movies = exactMatches;
          return true;
        }

        exactMatches = movies.Where(s => NamesAreMostlyEqual(s, movieSearch)).ToList();
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", movieSearch);
          movies = exactMatches;
          return true;
        }

        if (PreferredLanguage != null && exactMatches.Count > 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches for exact name \"{0}\" ({1}). Try to find match for preferred language {2}.", movieSearch, exactMatches.Count, PreferredLanguage);
          movies = exactMatches.FindAll(s => s.Languages.Contains(PreferredLanguage.ToString()) || s.Languages.Count == 0);
        }

        if (movies.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches found for \"{0}\" (count: {1})", movieSearch, movies.Count);

        return movies.Count == 1;
      }
      return false;
    }

    public virtual Task<bool> UpdateFromOnlineMovieAsync(MovieInfo movie, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineMovieAsync(MovieInfo movie, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineMovieCollectionAsync(MovieCollectionInfo collection, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineMovieCollectionAsync(MovieCollectionInfo collection, TLang language)
    {
      return Task.FromResult(false);
    }

    #endregion

    #region Series

    /// <summary>
    /// Search for Series.
    /// </summary>
    /// <param name="episodeSearch">Episode search parameters.</param>    /// 
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns>A list of matches.</returns>
    public virtual Task<List<EpisodeInfo>> SearchSeriesEpisodeAsync(EpisodeInfo episodeSearch, TLang language)
    {
      return Task.FromResult<List<EpisodeInfo>>(null);
    }

    /// <summary>
    /// Search for any matches of Series episode names. This method tries to find the best matching Series episode in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// - If series name contains " - ", it splits on this and tries to runs again using the first part (combined titles)
    /// </summary>
    /// <param name="episodeSearch">Episode search parameters.</param>
    /// <param name = "language" > Language, if <c>null</c> it takes the<see cref="PreferredLanguage"/></param>
    /// <returns>List of matching episodes found.</returns>
    public async Task<List<EpisodeInfo>> SearchSeriesEpisodeMatchesAsync(EpisodeInfo episodeSearch, TLang language)
    {
      language = language != null ? language : PreferredLanguage;
      List<EpisodeInfo> episodes = await SearchSeriesEpisodeAsync(episodeSearch, language).ConfigureAwait(false);
      if (episodes?.Count > 0)
        return episodes;

      if (!language.Equals(_defaultLanguage))
      {
        episodes = await SearchSeriesEpisodeAsync(episodeSearch, _defaultLanguage).ConfigureAwait(false);
        if (episodes?.Count > 0)
          return episodes;

        // If also no match in default language is found, we will look for combined movies names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        SimpleTitle originalName = episodeSearch.SeriesName;
        string namePart = episodeSearch.SeriesName.Text.Split(new[] { '-' })[0].Trim();
        episodeSearch.SeriesName = new SimpleTitle(namePart);
        episodes = await SearchSeriesEpisodeAsync(episodeSearch, _defaultLanguage).ConfigureAwait(false);
        episodeSearch.SeriesName = originalName;
        if (episodes?.Count > 0)
          return episodes;
      }
      return null;
    }

    /// <summary>
    /// Search for unique matches of Series names. This method tries to find the best matching Series in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// - If series name contains " - ", it splits on this and tries to runs again using the first part (combined titles)
    /// </summary>
    /// <param name="episodeSearch">Episode search parameters.</param>
    /// <param name = "language" > Language, if <c>null</c> it takes the<see cref="PreferredLanguage"/></param>
    /// <returns><c>true</c> if only one Episode was found.</returns>
    public async Task<bool> SearchSeriesEpisodeUniqueAndUpdateAsync(EpisodeInfo episodeSearch, TLang language)
    {
      language = language != null ? language : PreferredLanguage;

      List<EpisodeInfo> episodes = await SearchSeriesEpisodeAsync(episodeSearch, language).ConfigureAwait(false);
      if (episodes == null || episodes.Count == 0)
        return false;
      if (TestSeriesEpisodeMatch(episodeSearch, ref episodes))
      {
        episodeSearch.CopyIdsFrom(episodes[0]);
        return true;
      }

      if (episodes.Count == 0 && !language.Equals(_defaultLanguage))
      {
        episodes = await SearchSeriesEpisodeAsync(episodeSearch, _defaultLanguage).ConfigureAwait(false);
        if (episodes == null || episodes.Count == 0)
          return false;

        // If also no match in default language is found, we will look for combined movies names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        if (!TestSeriesEpisodeMatch(episodeSearch, ref episodes) && episodeSearch.SeriesName.Text.Contains("-"))
        {
          SimpleTitle originalName = episodeSearch.SeriesName;
          string namePart = episodeSearch.SeriesName.Text.Split(new[] { '-' })[0].Trim();
          episodeSearch.SeriesName = new SimpleTitle(namePart);
          if (await SearchSeriesEpisodeUniqueAndUpdateAsync(episodeSearch, language).ConfigureAwait(false))
            return true;
          episodeSearch.SeriesName = originalName;
          return false;
        }
        if (episodes.Count == 1)
        {
          episodeSearch.CopyIdsFrom(episodes[0]);
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Tests for episode matches. 
    /// </summary>
    /// <param name="seriesSearch">Series search parameters.</param>
    /// <param name="episodes">Potential online matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    protected virtual bool TestSeriesEpisodeMatch(EpisodeInfo episodeSearch, ref List<EpisodeInfo> episodes)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Test Match for \"{0}\"", episodeSearch);

      if (episodes.Count == 1)
      {
        if (episodes[0].EpisodeNumbers.Count > 0 && episodeSearch.EpisodeNumbers.Count > 0 &&
          episodes[0].FirstEpisodeNumber == episodeSearch.FirstEpisodeNumber &&
          episodes[0].SeasonNumber.HasValue && episodeSearch.SeasonNumber.HasValue &&
          episodes[0].SeasonNumber.Value == episodeSearch.SeasonNumber.Value)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", episodeSearch);
          return true;
        }
        if (episodeSearch.EpisodeName.IsEmpty || GetLevenshteinDistance(episodes[0], episodeSearch) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", episodeSearch);
          return true;
        }
        if (NamesAreMostlyEqual(episodes[0], episodeSearch))
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", episodeSearch);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        episodes.Clear();
        return false;
      }

      if (episodeSearch.EpisodeNumbers.Count > 0)
      {
        var episodeFiltered = episodes.FindAll(e => episodeSearch.EpisodeNumbers.All(i => e.EpisodeNumbers.Contains(i)) && e.SeasonNumber == episodeSearch.SeasonNumber);
        if (episodeFiltered.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", episodeSearch);
          episodes = episodeFiltered;
          return true;
        }
      }

      // Multiple matches
      if (episodes.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches for \"{0}\" ({1}). Try to find exact name match.", episodeSearch, episodes.Count);
        var exactMatches = episodes.FindAll(e => !e.EpisodeName.IsEmpty && (e.EpisodeName.Text == episodeSearch.EpisodeName.Text || GetLevenshteinDistance(e, episodeSearch) == 0));
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", episodeSearch);
          episodes = exactMatches;
          return true;
        }
        exactMatches = episodes.FindAll(e => NamesAreMostlyEqual(e, episodeSearch));
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", episodeSearch);
          episodes = exactMatches;
          return true;
        }

        if (exactMatches.Count > 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches for exact name \"{0}\" ({1}). Try to find match for preferred language {2}.", episodeSearch, exactMatches.Count, PreferredLanguage);
          episodes = exactMatches.FindAll(s => s.Languages.Contains(PreferredLanguage.ToString()) || s.Languages.Count == 0);
        }

        if (episodes.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches found for \"{0}\" (count: {1})", episodeSearch, episodes.Count);
      }
      return episodes.Count == 1;
    }

    /// <summary>
    /// Search for Series.
    /// </summary>
    /// <param name="seriesSearch">Series search parameters.</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns>A list of matches.</returns>
    public virtual Task<List<SeriesInfo>> SearchSeriesAsync(SeriesInfo seriesSearch, TLang language)
    {
      return Task.FromResult<List<SeriesInfo>>(null);
    }

    /// <summary>
    /// Search for any matches of Series names. This method tries to find the best matching Series in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// - If series name contains " - ", it splits on this and tries to runs again using the first part (combined titles)
    /// </summary>
    /// <param name="seriesSearch">Series search parameters.</param>
    /// <param name = "language" > Language, if <c>null</c> it takes the<see cref="PreferredLanguage"/></param>
    /// <returns>List of matching series
    public async Task<List<SeriesInfo>> SearchSeriesMatchesAsync(SeriesInfo seriesSearch, TLang language)
    {
      language = language != null ? language : PreferredLanguage;
      List<SeriesInfo> series = await SearchSeriesAsync(seriesSearch, language).ConfigureAwait(false);
      if (series?.Count > 0)
        return series;

      if (!language.Equals(_defaultLanguage))
      {
        series = await SearchSeriesAsync(seriesSearch, _defaultLanguage).ConfigureAwait(false);
        if (series?.Count > 0)
          return series;

        // If also no match in default language is found, we will look for combined movies names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        SimpleTitle originalName = seriesSearch.SeriesName;
        string namePart = seriesSearch.SeriesName.Text.Split(new[] { '-' })[0].Trim();
        seriesSearch.SeriesName = new SimpleTitle(namePart);
        series = await SearchSeriesAsync(seriesSearch, _defaultLanguage).ConfigureAwait(false);
        seriesSearch.SeriesName = originalName;
        if (series?.Count > 0)
          return series;
      }
      return null;
    }

    /// <summary>
    /// Search for unique matches of Series names. This method tries to find the best matching Series in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// - If series name contains " - ", it splits on this and tries to runs again using the first part (combined titles)
    /// </summary>
    /// <param name="seriesSearch">Series search parameters.</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns><c>true</c> if only one Series was found.</returns>
    public async Task<bool> SearchSeriesUniqueAndUpdateAsync(SeriesInfo seriesSearch, TLang language)
    {
      language = language != null ? language : PreferredLanguage;

      List<SeriesInfo> series = await SearchSeriesAsync(seriesSearch, language).ConfigureAwait(false);
      if (series == null || series.Count == 0)
        return false;
      if (TestSeriesMatch(seriesSearch, ref series))
      {
        seriesSearch.CopyIdsFrom(series[0]);
        return true;
      }

      if (series.Count == 0 && !language.Equals(_defaultLanguage))
      {
        series = await SearchSeriesAsync(seriesSearch, _defaultLanguage).ConfigureAwait(false);
        if (series == null || series.Count == 0)
          return false;

        // If also no match in default language is found, we will look for combined movies names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        if (!TestSeriesMatch(seriesSearch, ref series) && seriesSearch.SeriesName.Text.Contains("-"))
        {
          SimpleTitle originalName = seriesSearch.SeriesName;
          string namePart = seriesSearch.SeriesName.Text.Split(new[] { '-' })[0].Trim();
          seriesSearch.SeriesName = new SimpleTitle(namePart);
          if (await SearchSeriesUniqueAndUpdateAsync(seriesSearch, language).ConfigureAwait(false))
            return true;
          seriesSearch.SeriesName = originalName;
          return false;
        }
        if (series.Count == 1)
        {
          seriesSearch.CopyIdsFrom(series[0]);
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Tests for series matches. 
    /// </summary>
    /// <param name="seriesSearch">Series search parameters.</param>
    /// <param name="series">Potential online matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    protected virtual bool TestSeriesMatch(SeriesInfo seriesSearch, ref List<SeriesInfo> series)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Test Match for \"{0}\"", seriesSearch);

      if (series.Count == 1)
      {
        if (seriesSearch.SeriesName.IsEmpty || GetLevenshteinDistance(series[0], seriesSearch) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", seriesSearch);
          return true;
        }
        if (NamesAreMostlyEqual(series[0], seriesSearch))
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", seriesSearch);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        series.Clear();
        return false;
      }

      // Multiple matches
      if (series.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches for \"{0}\" ({1}). Try to find exact name match.", seriesSearch, series.Count);

        // Try to match the year, if available
        if (seriesSearch.FirstAired.HasValue)
        {
          var yearFiltered = series.FindAll(s => s.FirstAired.HasValue && s.FirstAired.Value.Year == seriesSearch.FirstAired.Value.Year);
          if (yearFiltered.Count == 1)
          {
            ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", seriesSearch);
            series = yearFiltered;
            return true;
          }
          else if (yearFiltered.Count > 0)
          {
            series = yearFiltered;
          }
        }

        var exactMatches = series.FindAll(s => !s.SeriesName.IsEmpty && 
          (s.SeriesName.Text == seriesSearch.SeriesName.Text || s.OriginalName == seriesSearch.SeriesName.Text || GetLevenshteinDistance(s, seriesSearch) == 0));
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", seriesSearch);
          series = exactMatches;
          return true;
        }

        exactMatches = series.FindAll(s => NamesAreMostlyEqual(s, seriesSearch));
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", seriesSearch);
          series = exactMatches;
          return true;
        }

        if (PreferredLanguage != null && exactMatches.Count > 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches for exact name \"{0}\" ({1}). Try to find match for preferred language {2}.", seriesSearch, exactMatches.Count, PreferredLanguage);
          series = exactMatches.FindAll(s => s.Languages.Contains(PreferredLanguage.ToString()) || s.Languages.Count == 0);
        }

        if (series.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches found for \"{0}\" (count: {1})", seriesSearch, series.Count);
      }
      return series.Count == 1;
    }

    public virtual Task<bool> UpdateFromOnlineSeriesAsync(SeriesInfo series, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineSeriesAsync(SeriesInfo series, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineSeriesSeasonAsync(SeasonInfo season, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineSeriesSeasonAsync(SeasonInfo season, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineSeriesEpisodeAsync(EpisodeInfo episode, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineSeriesEpisodeAsync(EpisodeInfo episode, TLang language)
    {
      return Task.FromResult(false);
    }

    protected virtual void SetMultiEpisodeDetails(EpisodeInfo episodeInfo, List<EpisodeInfo> episodeMatches)
    {
      EpisodeInfo firstEpisodeMatch = episodeMatches.First();
      episodeInfo.ImdbId = firstEpisodeMatch.ImdbId;
      episodeInfo.TvdbId = firstEpisodeMatch.TvdbId;
      episodeInfo.MovieDbId = firstEpisodeMatch.MovieDbId;
      episodeInfo.TvMazeId = firstEpisodeMatch.TvMazeId;
      episodeInfo.TvRageId = firstEpisodeMatch.TvRageId;

      episodeInfo.SeriesImdbId = firstEpisodeMatch.SeriesImdbId;
      episodeInfo.SeriesMovieDbId = firstEpisodeMatch.SeriesMovieDbId;
      episodeInfo.SeriesTvdbId = firstEpisodeMatch.SeriesTvdbId;
      episodeInfo.SeriesTvRageId = firstEpisodeMatch.SeriesTvRageId;
      episodeInfo.SeriesTvMazeId = firstEpisodeMatch.SeriesTvMazeId;
      episodeInfo.SeriesName = firstEpisodeMatch.SeriesName;
      episodeInfo.SeriesFirstAired = firstEpisodeMatch.SeriesFirstAired;

      episodeInfo.SeasonNumber = firstEpisodeMatch.SeasonNumber;
      episodeInfo.EpisodeNumbers = episodeMatches.SelectMany(x => x.EpisodeNumbers).Distinct().ToList();
      episodeInfo.DvdEpisodeNumbers = episodeMatches.SelectMany(x => x.DvdEpisodeNumbers).Distinct().ToList();
      episodeInfo.FirstAired = firstEpisodeMatch.FirstAired;
      episodeInfo.Rating = new SimpleRating(episodeMatches.Where(e => !e.Rating.IsEmpty).
        Sum(e => e.Rating.RatingValue.Value) / episodeMatches.Where(e => !e.Rating.IsEmpty).Count()); // Average rating
      episodeInfo.Rating.VoteCount = episodeMatches.Where(e => !e.Rating.IsEmpty && e.Rating.VoteCount.HasValue).Sum(e => e.Rating.VoteCount.Value); // Total rating count
      episodeInfo.EpisodeName = string.Join("; ", episodeMatches.OrderBy(e => e.FirstEpisodeNumber).Select(e => e.EpisodeName.Text).ToArray());
      episodeInfo.EpisodeName.DefaultLanguage = episodeMatches.First().EpisodeName.DefaultLanguage;
      episodeInfo.Summary = string.Join("\r\n\r\n", episodeMatches.OrderBy(e => e.FirstEpisodeNumber).
        Select(e => string.Format("{0,02}) {1}", e.FirstEpisodeNumber, e.Summary.Text)).ToArray());
      episodeInfo.Summary.DefaultLanguage = firstEpisodeMatch.Summary.DefaultLanguage;

      episodeInfo.Genres = episodeMatches.SelectMany(e => e.Genres).Distinct().ToList();
      episodeInfo.Actors = episodeMatches.SelectMany(e => e.Actors).Distinct().ToList();
      episodeInfo.Directors = episodeMatches.SelectMany(e => e.Directors).Distinct().ToList();
      episodeInfo.Writers = episodeMatches.SelectMany(e => e.Writers).Distinct().ToList();
      episodeInfo.Characters = episodeMatches.SelectMany(e => e.Characters).Distinct().ToList();

      episodeInfo.Thumbnail = firstEpisodeMatch.Thumbnail;
    }

    protected virtual void SetEpisodeDetails(EpisodeInfo episodeInfo, EpisodeInfo episodeMatch)
    {
      episodeInfo.ImdbId = episodeMatch.ImdbId;
      episodeInfo.TvdbId = episodeMatch.TvdbId;
      episodeInfo.MovieDbId = episodeMatch.MovieDbId;
      episodeInfo.TvMazeId = episodeMatch.TvMazeId;
      episodeInfo.TvRageId = episodeMatch.TvRageId;

      episodeInfo.SeriesImdbId = episodeMatch.SeriesImdbId;
      episodeInfo.SeriesMovieDbId = episodeMatch.SeriesMovieDbId;
      episodeInfo.SeriesTvdbId = episodeMatch.SeriesTvdbId;
      episodeInfo.SeriesTvRageId = episodeMatch.SeriesTvRageId;
      episodeInfo.SeriesTvMazeId = episodeMatch.SeriesTvMazeId;
      episodeInfo.SeriesName = episodeMatch.SeriesName;
      episodeInfo.SeriesFirstAired = episodeMatch.SeriesFirstAired;

      episodeInfo.SeasonNumber = episodeMatch.SeasonNumber;
      episodeInfo.EpisodeNumbers = episodeMatch.EpisodeNumbers.ToList();
      episodeInfo.DvdEpisodeNumbers = episodeMatch.DvdEpisodeNumbers.ToList();
      episodeInfo.FirstAired = episodeMatch.FirstAired;
      episodeInfo.Rating = episodeMatch.Rating;
      episodeInfo.EpisodeName = episodeMatch.EpisodeName;
      episodeInfo.Summary = episodeMatch.Summary;

      episodeInfo.Genres = episodeMatch.Genres.ToList();
      episodeInfo.Actors = episodeMatch.Actors.ToList();
      episodeInfo.Directors = episodeMatch.Directors.ToList();
      episodeInfo.Writers = episodeMatch.Writers.ToList();
      episodeInfo.Characters = episodeMatch.Characters.ToList();

      episodeInfo.Thumbnail = episodeMatch.Thumbnail;
    }

    #endregion

    #region Persons

    /// <summary>
    /// Search for Person.
    /// </summary>
    /// <param name="personSearch">Person search parameters.</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns> A list of matches.</returns>
    public virtual Task<List<PersonInfo>> SearchPersonAsync(PersonInfo personSearch, TLang language)
    {
      return Task.FromResult<List<PersonInfo>>(null);
    }

    /// <summary>
    /// Search for unique matches of Persons. This method tries to find the best matching Person in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// </summary>
    /// <param name="personSearch">Person search parameters.</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns><c>true</c> if at exactly one Person was found.</returns>
    public async Task<bool> SearchPersonUniqueAndUpdateAsync(PersonInfo personSearch, TLang language)
    {
      language = language != null ? language : PreferredLanguage;

      List<PersonInfo> persons = await SearchPersonAsync(personSearch, language).ConfigureAwait(false);
      if (persons == null || persons.Count == 0)
        return false;
      if (TestPersonMatch(personSearch, ref persons))
      {
        personSearch.CopyIdsFrom(persons[0]);
        return true;
      }

      if (persons.Count == 0 && !language.Equals(_defaultLanguage))
      {
        persons = await SearchPersonAsync(personSearch, _defaultLanguage).ConfigureAwait(false);
        if (persons == null || persons.Count == 0)
          return false;
        if (TestPersonMatch(personSearch, ref persons))
        {
          personSearch.CopyIdsFrom(persons[0]);
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Tests for person matches. 
    /// </summary>
    /// <param name="personSearch">Person search parameters.</param>
    /// <param name="persons">Potential online matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    protected virtual bool TestPersonMatch(PersonInfo personSearch, ref List<PersonInfo> persons)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Test Match for \"{0}\"", personSearch);

      if (persons.Count == 1)
      {
        if (string.IsNullOrEmpty(personSearch.Name) || GetLevenshteinDistance(persons[0], personSearch) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", personSearch);
          return true;
        }
        if (NamesAreMostlyEqual(persons[0], personSearch))
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", personSearch);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        persons.Clear();
        return false;
      }

      // Multiple matches
      if (persons.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches for \"{0}\" ({1}). Try to find exact name match.", personSearch, persons.Count);
        var exactMatches = persons.FindAll(p => p.Name == personSearch.Name || GetLevenshteinDistance(p, personSearch) == 0);
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", personSearch);
          persons = exactMatches;
          return true;
        }
        exactMatches = persons.FindAll(p => NamesAreMostlyEqual(p, personSearch));
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", personSearch);
          persons = exactMatches;
          return true;
        }
        if (exactMatches.Count == 2)
        {
          //Check if the 2 matches are actually the same person with different names
          if (GetLevenshteinDistance(exactMatches[0], exactMatches[1]) == 0 || NamesAreMostlyEqual(exactMatches[0], exactMatches[1]) ||
            GetLevenshteinDistance(exactMatches[1], exactMatches[0]) == 0 || NamesAreMostlyEqual(exactMatches[1], exactMatches[0]))
          {
            ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", personSearch);
            persons.Clear();
            persons.Add(exactMatches[0]);
            return true;
          }
        }

        if (persons.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches found for \"{0}\" (count: {1})", personSearch, persons.Count);

      }
      return persons.Count == 1;
    }

    public virtual Task<bool> UpdateFromOnlineMoviePersonAsync(MovieInfo movieInfo, PersonInfo person, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineMoviePersonAsync(MovieInfo movieInfo, PersonInfo person, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineSeriesPersonAsync(SeriesInfo seriesInfo, PersonInfo person, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineSeriesPersonAsync(SeriesInfo seriesInfo, PersonInfo person, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineSeriesEpisodePersonAsync(EpisodeInfo episodeInfo, PersonInfo person, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineSeriesEpisodePersonAsync(EpisodeInfo episodeInfo, PersonInfo person, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineMusicTrackAlbumPersonAsync(AlbumInfo albumInfo, PersonInfo person, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineMusicTrackAlbumPersonAsync(AlbumInfo albumInfo, PersonInfo person, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineMusicTrackPersonAsync(TrackInfo trackInfo, PersonInfo person, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineMusicTrackPersonAsync(TrackInfo trackInfo, PersonInfo person, TLang language)
    {
      return Task.FromResult(false);
    }

    #endregion

    #region Characters

    /// <summary>
    /// Search for Character.
    /// </summary>
    /// <param name="characterSearch">Character search parameters.</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns>A list of matches.</returns>
    public virtual Task<List<CharacterInfo>> SearchCharacterAsync(CharacterInfo characterSearch, TLang language)
    {
      return Task.FromResult<List<CharacterInfo>>(null);
    }

    /// <summary>
    /// Search for unique matches of Character. This method tries to find the best matching Movie in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// </summary>
    /// <param name="characterSearch">Character search parameters.</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns><c>true</c> if at exactly one Person was found.</returns>
    public async Task<bool> SearchCharacterUniqueAndUpdateAsync(CharacterInfo characterSearch, TLang language)
    {
      language = language != null ? language : PreferredLanguage;

      List<CharacterInfo> characters = await SearchCharacterAsync(characterSearch, language).ConfigureAwait(false);
      if (characters == null || characters.Count == 0)
        return false;
      if (TestCharacterMatch(characterSearch, ref characters))
      {
        characterSearch.CopyIdsFrom(characters[0]);
        return true;
      }

      if (characters.Count == 0 && !language.Equals(_defaultLanguage))
      {
        characters = await SearchCharacterAsync(characterSearch, _defaultLanguage).ConfigureAwait(false);
        if (characters == null || characters.Count == 0)
          return false;
        if (TestCharacterMatch(characterSearch, ref characters))
        {
          characterSearch.CopyIdsFrom(characters[0]);
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Tests for Character matches. 
    /// </summary>
    /// <param name="characterSearch">Character search parameters.</param>
    /// <param name="persons">Potential online matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    protected virtual bool TestCharacterMatch(CharacterInfo characterSearch, ref List<CharacterInfo> characters)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Test Match for \"{0}\"", characterSearch);

      if (characters.Count == 1)
      {
        if (string.IsNullOrEmpty(characterSearch.Name) || GetLevenshteinDistance(characters[0], characterSearch) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", characterSearch);
          return true;
        }
        if (NamesAreMostlyEqual(characters[0], characterSearch))
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", characterSearch);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        characters.Clear();
        return false;
      }

      // Multiple matches
      if (characters.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches for \"{0}\" ({1}). Try to find exact name match.", characterSearch, characters.Count);
        var exactMatches = characters.FindAll(p => p.Name == characterSearch.Name || GetLevenshteinDistance(p, characterSearch) <= MAX_LEVENSHTEIN_DIST);
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", characterSearch);
          characters = exactMatches;
          return true;
        }
        exactMatches = characters.FindAll(p => NamesAreMostlyEqual(p, characterSearch));
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", characterSearch);
          characters = exactMatches;
          return true;
        }

        if (characters.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches found for \"{0}\" (count: {1})", characterSearch, characters.Count);

      }
      return characters.Count == 1;
    }

    public virtual Task<bool> UpdateFromOnlineMovieCharacterAsync(MovieInfo movieInfo, CharacterInfo character, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineMovieCharacterAsync(MovieInfo movieInfo, CharacterInfo character, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineSeriesCharacterAsync(SeriesInfo seriesInfo, CharacterInfo character, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineSeriesCharacterAsync(SeriesInfo seriesInfo, CharacterInfo character, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineSeriesEpisodeCharacterAsync(EpisodeInfo episodeInfo, CharacterInfo character, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineSeriesEpisodeCharacterAsync(EpisodeInfo episodeInfo, CharacterInfo character, TLang language)
    {
      return Task.FromResult(false);
    }

    #endregion

    #region Companies

    /// <summary>
    /// Search for Company.
    /// </summary>
    /// <param name="companySearch">Company search parameters.</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns>A list of matches.</returns>
    public virtual Task<List<CompanyInfo>> SearchCompanyAsync(CompanyInfo companySearch, TLang language)
    {
      return Task.FromResult<List<CompanyInfo>>(null);
    }

    /// <summary>
    /// Search for unique matches of Company names. This method tries to find the best matching Movie in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// </summary>
    /// <param name="companySearch">Company search parameters.</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns><c>true</c> if at exactly one Company was found.</returns>
    public async Task<bool> SearchCompanyUniqueAndUpdateAsync(CompanyInfo companySearch, TLang language)
    {
      language = language != null ? language : PreferredLanguage;

      List<CompanyInfo> companies = await SearchCompanyAsync(companySearch, language).ConfigureAwait(false);
      if (companies == null || companies.Count == 0)
        return false;
      if (TestCompanyMatch(companySearch, ref companies))
      {
        companySearch.CopyIdsFrom(companies[0]);
        return true;
      }

      if (companies.Count == 0 && !language.Equals(_defaultLanguage))
      {
        companies = await SearchCompanyAsync(companySearch, _defaultLanguage).ConfigureAwait(false);
        if (companies == null || companies.Count == 0)
          return false;

        if (TestCompanyMatch(companySearch, ref companies))
        {
          companySearch.CopyIdsFrom(companies[0]);
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Tests for Company matches. 
    /// </summary>
    /// <param name="companySearch">Company search parameters.</param>
    /// <param name="companies">Potential online matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    protected virtual bool TestCompanyMatch(CompanyInfo companySearch, ref List<CompanyInfo> companies)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Test Match for \"{0}\"", companySearch);

      if (companies.Count == 1)
      {
        if (string.IsNullOrEmpty(companySearch.Name) || GetLevenshteinDistance(companies[0], companySearch) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", companySearch);
          return true;
        }
        if (NamesAreMostlyEqual(companies[0], companySearch))
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", companySearch);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        companies.Clear();
        return false;
      }

      // Multiple matches
      if (companies.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches for \"{0}\" ({1}). Try to find exact name match.", companySearch, companies.Count);
        var exactMatches = companies.FindAll(c => c.Name == companySearch.Name || GetLevenshteinDistance(c, companySearch) == 0);
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", companySearch);
          companies = exactMatches;
          return true;
        }
        exactMatches = companies.FindAll(c => NamesAreMostlyEqual(c, companySearch));
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", companySearch);
          companies = exactMatches;
          return true;
        }

        if (companies.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches found for \"{0}\" (count: {1})", companySearch, companies.Count);

      }
      return companies.Count == 1;
    }

    public virtual Task<bool> UpdateFromOnlineMovieCompanyAsync(MovieInfo movieInfo, CompanyInfo company, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineMovieCompanyAsync(MovieInfo movieInfo, CompanyInfo company, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineSeriesCompanyAsync(SeriesInfo seriesInfo, CompanyInfo company, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineSeriesCompanyAsync(SeriesInfo seriesInfo, CompanyInfo company, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineMusicTrackAlbumCompanyAsync(AlbumInfo albumInfo, CompanyInfo company, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineMusicTrackAlbumCompanyAsync(AlbumInfo albumInfo, CompanyInfo company, TLang language)
    {
      return Task.FromResult(false);
    }

    #endregion

    #region Music

    /// <summary>
    /// Search for track.
    /// </summary>
    /// <param name="characterSearch">Track search parameters.</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns>A list of matches.</returns>
    public virtual Task<List<TrackInfo>> SearchTrackAsync(TrackInfo trackSearch, TLang language)
    {
      return Task.FromResult<List<TrackInfo>>(null);
    }

    public async Task<List<TrackInfo>> SearchTrackMatchesAsync(TrackInfo trackSearch, TLang language)
    {
      //Don't try to search for a track without artists
      if (trackSearch.AlbumArtists.Count == 0 && trackSearch.Artists.Count == 0)
        return null;

      language = language != null ? language : PreferredLanguage;
      List<TrackInfo> tracks = await SearchTrackAsync(trackSearch, language).ConfigureAwait(false);
      if (tracks?.Count == 0)
        return tracks;

      if (!language.Equals(_defaultLanguage))
      {
        tracks = await SearchTrackAsync(trackSearch, _defaultLanguage).ConfigureAwait(false);
        if (tracks?.Count == 0)
          return tracks;
      }
      return null;
    }

    public async Task<bool> SearchTrackUniqueAndUpdateAsync(TrackInfo trackSearch, TLang language)
    {
      //Don't try to search for a track without artists
      if (trackSearch.AlbumArtists.Count == 0 && trackSearch.Artists.Count == 0)
        return false;

      language = language != null ? language : PreferredLanguage;

      List<TrackInfo> tracks = await SearchTrackAsync(trackSearch, language).ConfigureAwait(false);
      if (tracks == null || tracks.Count == 0)
        return false;
      if (TestTrackMatch(trackSearch, ref tracks))
      {
        trackSearch.CopyIdsFrom(tracks[0]);
        return true;
      }

      if (tracks.Count == 0 && !language.Equals(_defaultLanguage))
      {
        tracks = await SearchTrackAsync(trackSearch, _defaultLanguage).ConfigureAwait(false);
        if (tracks == null || tracks.Count == 0)
          return false;
        if (TestTrackMatch(trackSearch, ref tracks))
        {
          trackSearch.CopyIdsFrom(tracks[0]);
          return true;
        }
      }
      return false;
    }

    public virtual bool TestTrackMatch(TrackInfo trackSearch, ref List<TrackInfo> tracks)
    {
      if (tracks.Count == 1)
      {
        if (!string.IsNullOrEmpty(trackSearch.TrackName) && GetLevenshteinDistance(tracks[0], trackSearch) > MAX_LEVENSHTEIN_DIST)
        {
          // No valid match, clear list to allow further detection ways
          tracks.Clear();
          return false;
        }
        if (!NamesAreMostlyEqual(tracks[0], trackSearch))
        {
          // No valid match, clear list to allow further detection ways
          tracks.Clear();
          return false;
        }
      }

      // Multiple matches
      if (tracks.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches for \"{0}\" ({1}). Try to find exact name match.", trackSearch, tracks.Count);
        var exactMatches = tracks.FindAll(t => !string.IsNullOrEmpty(t.TrackName) && (t.TrackName == trackSearch.TrackName || GetLevenshteinDistance(t, trackSearch) == 0));
        if (exactMatches.Count > 0)
        {
          tracks = exactMatches;
        }
        else
        {
          exactMatches = tracks.FindAll(t => NamesAreMostlyEqual(t, trackSearch));
          if (exactMatches.Count > 0)
          {
            tracks = exactMatches;
          }
        }

        if (tracks.Count > 1)
        {
          var lastGood = tracks;
          foreach (AudioValueToCheck checkValue in Enum.GetValues(typeof(AudioValueToCheck)))
          {
            if (checkValue == AudioValueToCheck.ArtistLax && trackSearch.Artists != null && trackSearch.Artists.Count > 0)
              exactMatches = exactMatches.FindAll(t => CompareArtists(t.Artists, trackSearch.Artists, false));

            if (checkValue == AudioValueToCheck.AlbumLax && !string.IsNullOrEmpty(trackSearch.Album))
              exactMatches = exactMatches.FindAll(t => GetLevenshteinDistance(t.CloneBasicInstance<AlbumInfo>(), trackSearch.CloneBasicInstance<AlbumInfo>()) <= MAX_LEVENSHTEIN_DIST);

            if (checkValue == AudioValueToCheck.ArtistStrict && trackSearch.Artists != null && trackSearch.Artists.Count > 0)
              exactMatches = exactMatches.FindAll(t => CompareArtists(t.Artists, trackSearch.Artists, true));

            if (checkValue == AudioValueToCheck.AlbumStrict && !string.IsNullOrEmpty(trackSearch.Album))
              exactMatches = exactMatches.FindAll(t => t.Album == trackSearch.Album || GetLevenshteinDistance(t.CloneBasicInstance<AlbumInfo>(), trackSearch.CloneBasicInstance<AlbumInfo>()) == 0);

            if (checkValue == AudioValueToCheck.Year && trackSearch.ReleaseDate.HasValue)
              exactMatches = exactMatches.FindAll(t => t.ReleaseDate.HasValue && t.ReleaseDate.Value.Year == trackSearch.ReleaseDate.Value.Year);

            if (checkValue == AudioValueToCheck.TrackNum && trackSearch.TrackNum > 0)
              exactMatches = exactMatches.FindAll(t => t.TrackNum > 0 && t.TrackNum == trackSearch.TrackNum);

            if (checkValue == AudioValueToCheck.Discs)
              exactMatches = exactMatches.FindAll(t => t.DiscNum > 0 || t.TotalDiscs > 0);

            if (checkValue == AudioValueToCheck.Language && PreferredLanguage != null)
            {
              exactMatches = exactMatches.FindAll(t => t.Languages.Contains(PreferredLanguage.ToString()) || t.Languages.Count == 0);
              if (exactMatches.Count == 0)
              {
                foreach (TLang lang in RegionLanguages)
                {
                  var matches = lastGood.FindAll(t => t.Languages.Contains(lang.ToString()) || t.Languages.Count == 0);
                  if (matches.Count > 0)
                  {
                    exactMatches = matches;
                    break;
                  }
                }
              }
              if (exactMatches.Count == 0)
                exactMatches = lastGood.FindAll(t => t.Languages.Contains(DefaultLanguage.ToString()) || t.Languages.Count == 0);
            }

            if (checkValue == AudioValueToCheck.Barcode)
              exactMatches = exactMatches.FindAll(s => s.AlbumHasBarcode);

            if (checkValue == AudioValueToCheck.MBGroup)
              exactMatches = exactMatches.FindAll(s => !string.IsNullOrEmpty(s.AlbumMusicBrainzGroupId));

            if (checkValue == AudioValueToCheck.Art)
              exactMatches = exactMatches.FindAll(s => s.AlbumHasOnlineCover);

            if (exactMatches.Count == 0) //Too many were removed restore last good
              exactMatches = lastGood;
            else
              lastGood = exactMatches;

            if (exactMatches.Count == 1)
            {
              ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Filtered match found \"{0}\" [{1}]!", trackSearch, checkValue.ToString());
              tracks = exactMatches;
              break;
            }
          }

          tracks = lastGood;
        }

        if (tracks.Count > 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches found for \"{0}\" (count: {1})", trackSearch, tracks.Count);
          tracks.Clear();
          foreach (TrackInfo track in tracks)
          {
            //Track matching is also done strictly on name so allow a strict match
            if (trackSearch.Equals(track))
            {
              tracks.Add(track);
              ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Strict match found \"{0}\"!", trackSearch);
              break;
            }
          }
        }
      }

      if (tracks.Count == 1)
      {
        if (string.Compare(trackSearch.Album, tracks[0].Album, true) == 0)
        {
          //All good
        }
        else if (trackSearch.AlbumArtists.Count > 0 && tracks[0].AlbumArtists.Count > 0 && tracks[0].AlbumArtists.Intersect(trackSearch.AlbumArtists).Any())
        {
          //All good
        }
        else if (trackSearch.Artists.Count > 0 && tracks[0].Artists.Count > 0 && tracks[0].Artists.Intersect(trackSearch.Artists).Any())
        {
          //All good
        }
        else if (trackSearch.ReleaseDate.HasValue && tracks[0].ReleaseDate.HasValue && trackSearch.ReleaseDate.Value.Year != tracks[0].ReleaseDate.Value.Year)
        {
          tracks.Clear();
          return false;
        }
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", trackSearch);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Search for album.
    /// </summary>
    /// <param name="characterSearch">Album search parameters.</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns>A list of matches.</returns>
    public virtual Task<List<AlbumInfo>> SearchTrackAlbumAsync(AlbumInfo albumSearch, TLang language)
    {
      return Task.FromResult<List<AlbumInfo>>(null);
    }

    public async Task<List<AlbumInfo>> SearchTrackAlbumMacthesAsync(AlbumInfo albumSearch, TLang language)
    {
      //Don't try to search for an album without artists
      if (albumSearch.Artists.Count == 0)
        return null;

      language = language != null ? language : PreferredLanguage;
      List<AlbumInfo> albums = await SearchTrackAlbumAsync(albumSearch, language).ConfigureAwait(false);
      if (albums?.Count > 0)
        return albums;

      if (!language.Equals(_defaultLanguage))
      {
        albums = await SearchTrackAlbumAsync(albumSearch, _defaultLanguage).ConfigureAwait(false);
        if (albums?.Count > 0)
          return albums;
      }
      return null;
    }

    public async Task<bool> SearchTrackAlbumUniqueAndUpdateAsync(AlbumInfo albumSearch, TLang language)
    {
      //Don't try to search for an album without artists
      if (albumSearch.Artists.Count == 0)
        return false;

      language = language != null ? language : PreferredLanguage;

      List<AlbumInfo> albums = await SearchTrackAlbumAsync(albumSearch, language).ConfigureAwait(false);
      if (albums == null || albums.Count == 0)
        return false;
      if (TestAlbumMatch(albumSearch, ref albums))
      {
        albumSearch.CopyIdsFrom(albums[0]);
        return true;
      }

      if (albums.Count == 0 && !language.Equals(_defaultLanguage))
      {
        albums = await SearchTrackAlbumAsync(albumSearch, _defaultLanguage).ConfigureAwait(false);
        if (albums == null || albums.Count == 0)
          return false;
        if (TestAlbumMatch(albumSearch, ref albums))
        {
          albumSearch.CopyIdsFrom(albums[0]);
          return true;
        }
      }
      return false;
    }

    protected virtual bool TestAlbumMatch(AlbumInfo albumSearch, ref List<AlbumInfo> albums)
    {
      if (albums.Count == 1)
      {
        if (!string.IsNullOrEmpty(albumSearch.Album) && GetLevenshteinDistance(albums[0], albumSearch) > MAX_LEVENSHTEIN_DIST)
        {
          // No valid match, clear list to allow further detection ways
          albums.Clear();
          return false;
        }
        if (!NamesAreMostlyEqual(albums[0], albumSearch))
        {
          // No valid match, clear list to allow further detection ways
          albums.Clear();
          return false;
        }
      }

      // Multiple matches
      if (albums.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches for \"{0}\" ({1}). Try to find exact name match.", albumSearch, albums.Count);
        var exactMatches = albums.FindAll(t => !string.IsNullOrEmpty(t.Album) && (t.Album == albumSearch.Album || GetLevenshteinDistance(t, albumSearch) == 0));
        if (exactMatches.Count > 0)
        {
          albums = exactMatches;
        }
        else
        {
          albums = albums.FindAll(t => NamesAreMostlyEqual(t, albumSearch));
          if (albums.Count == 0)
          {
            albums.Clear();
            return false;
          }
        }

        if (albums.Count > 1)
        {
          var lastGood = albums;
          foreach (AudioValueToCheck checkValue in Enum.GetValues(typeof(AudioValueToCheck)))
          {
            if (checkValue == AudioValueToCheck.ArtistLax && albumSearch.Artists != null && albumSearch.Artists.Count > 0)
              exactMatches = exactMatches.FindAll(a => CompareArtists(a.Artists, albumSearch.Artists, false));

            if (checkValue == AudioValueToCheck.AlbumLax && !string.IsNullOrEmpty(albumSearch.Album))
              exactMatches = exactMatches.FindAll(a => GetLevenshteinDistance(a, albumSearch) <= MAX_LEVENSHTEIN_DIST);

            if (checkValue == AudioValueToCheck.ArtistStrict && albumSearch.Artists != null && albumSearch.Artists.Count > 0)
              exactMatches = exactMatches.FindAll(a => CompareArtists(a.Artists, albumSearch.Artists, true));

            if (checkValue == AudioValueToCheck.AlbumStrict && !string.IsNullOrEmpty(albumSearch.Album))
              exactMatches = exactMatches.FindAll(a => a.Album == albumSearch.Album || GetLevenshteinDistance(a, albumSearch) == 0);

            if (checkValue == AudioValueToCheck.Year && albumSearch.ReleaseDate.HasValue)
              exactMatches = exactMatches.FindAll(a => a.ReleaseDate.HasValue && a.ReleaseDate.Value.Year == albumSearch.ReleaseDate.Value.Year);

            if (checkValue == AudioValueToCheck.TrackNum && albumSearch.TotalTracks > 0)
              exactMatches = exactMatches.FindAll(a => a.TotalTracks > 0 && a.TotalTracks == albumSearch.TotalTracks);

            if (checkValue == AudioValueToCheck.Discs)
              exactMatches = exactMatches.FindAll(a => a.DiscNum > 0 || a.TotalDiscs > 0);

            if (checkValue == AudioValueToCheck.Language && PreferredLanguage != null)
            {
              exactMatches = exactMatches.FindAll(a => a.Languages.Contains(PreferredLanguage.ToString()) || a.Languages.Count == 0);
              if (exactMatches.Count == 0)
              {
                foreach (TLang lang in RegionLanguages)
                {
                  var matches = lastGood.FindAll(a => a.Languages.Contains(lang.ToString()) || a.Languages.Count == 0);
                  if (matches.Count > 0)
                  {
                    exactMatches = matches;
                    break;
                  }
                }
              }
              if (exactMatches.Count == 0)
                exactMatches = lastGood.FindAll(a => a.Languages.Contains(DefaultLanguage.ToString()) || a.Languages.Count == 0);
            }

            if (checkValue == AudioValueToCheck.Compilation)
              exactMatches = exactMatches.FindAll(s => !s.Compilation);

            if (checkValue == AudioValueToCheck.Barcode)
              exactMatches = exactMatches.FindAll(s => s.HasBarcode);

            if (checkValue == AudioValueToCheck.MBGroup)
              exactMatches = exactMatches.FindAll(s => !string.IsNullOrEmpty(s.MusicBrainzGroupId));

            if (checkValue == AudioValueToCheck.Art)
              exactMatches = exactMatches.FindAll(s => s.HasOnlineCover);

            if (exactMatches.Count == 0) //Too many were removed restore last good
              exactMatches = lastGood;
            else
              lastGood = exactMatches;

            if (exactMatches.Count == 1)
            {
              ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Filtered match found \"{0}\" [{1}]!", albumSearch, checkValue.ToString());
              albums = exactMatches;
              break;
            }
          }

          albums = lastGood;
        }

        if (albums.Count > 1)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Multiple matches found for \"{0}\" (count: {1})", albumSearch, albums.Count);
          albums.Clear();
          foreach (AlbumInfo album in albums)
          {
            //Album matching is also done strictly on name so allow a strict match
            if (albumSearch.Equals(album))
            {
              albums.Add(album);
              ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Strict match found \"{0}\"!", albumSearch);
              break;
            }
          }
        }
      }

      if (albums.Count == 1)
      {
        //We need to check further to avoid accidental matches
        if (albumSearch.Artists.Count > 0 && albums[0].Artists.Count > 0 && albums[0].Artists.Intersect(albumSearch.Artists).Any())
        {
          //All good
        }
        else if (albumSearch.ReleaseDate.HasValue && albums[0].ReleaseDate.HasValue && albumSearch.ReleaseDate.Value.Year != albums[0].ReleaseDate.Value.Year)
        {
          albums.Clear();
          return false;
        }
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Unique match found \"{0}\"!", albumSearch);
        return true;
      }
      return false;
    }

    public virtual Task<bool> UpdateFromOnlineMusicTrackAsync(TrackInfo track, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineMusicTrackAsync(TrackInfo track, TLang language)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> UpdateFromOnlineMusicTrackAlbumAsync(AlbumInfo album, TLang language, bool cacheOnly)
    {
      return Task.FromResult(false);
    }

    public virtual Task<bool> IsCacheChangedForOnlineMusicTrackAlbumAsync(AlbumInfo album, TLang language)
    {
      return Task.FromResult(false);
    }

    private bool CompareArtists(List<PersonInfo> trackArtists, List<PersonInfo> searchArtists, bool strict)
    {
      if (strict)
      {
        foreach (PersonInfo trackArtist in trackArtists)
        {
          bool artistFound = false;
          foreach (PersonInfo searchArtist in searchArtists)
            if (trackArtist.Name == searchArtist.Name || trackArtist.Name == searchArtist.AlternateName ||
              trackArtist.AlternateName == searchArtist.Name || GetLevenshteinDistance(trackArtist, searchArtist) == 0)
            {
              artistFound = true;
              break;
            }
          if (!artistFound)
            return false;
        }
      }
      else
      {
        foreach (PersonInfo trackArtist in trackArtists)
        {
          foreach (PersonInfo searchArtist in searchArtists)
          {
            if (GetLevenshteinDistance(trackArtist, searchArtist) <= MAX_LEVENSHTEIN_DIST)
              return true;
          }
        }
      }
      return false;
    }

    #endregion

    #region FanArt

    public virtual Task<ApiWrapperImageCollection<TImg>> GetFanArtAsync<T>(T infoObject, TLang language, string fanartMediaType) where T : BaseInfo
    {
      return Task.FromResult<ApiWrapperImageCollection<TImg>>(null);
    }

    public virtual Task<bool> DownloadFanArtAsync(string id, TImg image, string folderPath)
    {
      return Task.FromResult(false);
    }

    #endregion

    #region Name comparing

    /// <summary>
    /// Removes special characters and compares the remaining strings. Strings are processed by <see cref="RemoveCharacters"/> before comparing.
    /// The result is <c>true</c>, if the cleaned strings are equal or have a Levenshtein distance less or equal to <see cref="MAX_LEVENSHTEIN_DIST"/>.
    /// </summary>
    /// <param name="name1">Name 1</param>
    /// <param name="name2">Name 2</param>
    /// <returns><c>true</c> if similar or equal</returns>
    public static bool IsSimilarOrEqual(string name1, string name2)
    {
      return string.Equals(RemoveCharacters(name1), RemoveCharacters(name2)) || StringUtils.GetLevenshteinDistance(name1, name2) <= MAX_LEVENSHTEIN_DIST;
    }

    public static int GetLevenshteinDistance(MovieInfo movieOnline, MovieInfo movieSearch)
    {
      if (movieOnline.MovieName.IsEmpty || movieSearch.MovieName.IsEmpty)
        return MAX_LEVENSHTEIN_DIST + 1;

      string cleanedName = RemoveCharacters(movieSearch.MovieName.Text);
      if (string.IsNullOrEmpty(movieOnline.OriginalName))
        return StringUtils.GetLevenshteinDistance(RemoveCharacters(movieOnline.MovieName.Text), cleanedName);
      else
        return Math.Min(
        StringUtils.GetLevenshteinDistance(RemoveCharacters(movieOnline.MovieName.Text), cleanedName),
        StringUtils.GetLevenshteinDistance(RemoveCharacters(movieOnline.OriginalName), cleanedName)
        );
    }

    public static int GetLevenshteinDistance(EpisodeInfo episodeOnline, EpisodeInfo episodeSearch)
    {
      if (episodeOnline.EpisodeName.IsEmpty || episodeSearch.EpisodeName.IsEmpty)
        return MAX_LEVENSHTEIN_DIST + 1;

      string cleanedName = RemoveCharacters(episodeSearch.EpisodeName.Text);
      return StringUtils.GetLevenshteinDistance(RemoveCharacters(episodeOnline.EpisodeName.Text), cleanedName);
    }

    public static int GetLevenshteinDistance(SeriesInfo seriesOnline, SeriesInfo seriesSearch)
    {
      if (seriesOnline.SeriesName.IsEmpty || seriesSearch.SeriesName.IsEmpty)
        return MAX_LEVENSHTEIN_DIST + 1;

      string cleanedName = RemoveCharacters(seriesSearch.SeriesName.Text);
      if (string.IsNullOrEmpty(seriesOnline.OriginalName))
        return StringUtils.GetLevenshteinDistance(RemoveCharacters(seriesOnline.SeriesName.Text), cleanedName);
      else
        return Math.Min(
          StringUtils.GetLevenshteinDistance(RemoveCharacters(seriesOnline.SeriesName.Text), cleanedName),
          StringUtils.GetLevenshteinDistance(RemoveCharacters(seriesOnline.OriginalName), cleanedName)
        );
    }

    public static int GetLevenshteinDistance(PersonInfo personOnline, PersonInfo personSearch)
    {
      if (string.IsNullOrEmpty(personOnline.Name) || string.IsNullOrEmpty(personSearch.Name))
        return MAX_LEVENSHTEIN_DIST + 1;
      if (personOnline.Occupation != personSearch.Occupation)
        return MAX_LEVENSHTEIN_DIST + 1;

      string cleanedName = RemoveCharacters(personSearch.Name);
      if (string.IsNullOrEmpty(personOnline.AlternateName))
        return StringUtils.GetLevenshteinDistance(RemoveCharacters(personOnline.Name), cleanedName);
      else
        return Math.Min(
          StringUtils.GetLevenshteinDistance(RemoveCharacters(personOnline.Name), cleanedName),
          StringUtils.GetLevenshteinDistance(RemoveCharacters(personOnline.AlternateName), cleanedName)
        );
    }

    public static int GetLevenshteinDistance(CharacterInfo characterOnline, CharacterInfo characterSearch)
    {
      if (string.IsNullOrEmpty(characterOnline.Name) || string.IsNullOrEmpty(characterSearch.Name))
        return MAX_LEVENSHTEIN_DIST + 1;

      string cleanedName = RemoveCharacters(characterSearch.Name);
      return StringUtils.GetLevenshteinDistance(RemoveCharacters(characterOnline.Name), cleanedName);
    }

    public static int GetLevenshteinDistance(CompanyInfo companyOnline, CompanyInfo companySearch)
    {
      if (string.IsNullOrEmpty(companyOnline.Name) || string.IsNullOrEmpty(companySearch.Name))
        return MAX_LEVENSHTEIN_DIST + 1;
      if (companyOnline.Type != companySearch.Type)
        return MAX_LEVENSHTEIN_DIST + 1;

      string cleanedName = RemoveCharacters(companySearch.Name);
      return StringUtils.GetLevenshteinDistance(RemoveCharacters(companyOnline.Name), cleanedName);
    }

    public static int GetLevenshteinDistance(TrackInfo trackOnline, TrackInfo trackSearch)
    {
      if (string.IsNullOrEmpty(trackOnline.TrackName) || string.IsNullOrEmpty(trackSearch.TrackName))
        return MAX_LEVENSHTEIN_DIST + 1;

      string cleanedName = RemoveCharacters(trackSearch.TrackName);
      int trackDistance = StringUtils.GetLevenshteinDistance(RemoveCharacters(trackOnline.TrackName), cleanedName);
      if(!string.IsNullOrEmpty(trackSearch.Album))
      {
        int albumDistance = GetLevenshteinDistance(trackOnline.CloneBasicInstance<AlbumInfo>(), trackSearch.CloneBasicInstance<AlbumInfo>());
        return Math.Max(trackDistance, albumDistance);
      }
      return trackDistance;
    }

    public static int GetLevenshteinDistance(AlbumInfo albumOnline, AlbumInfo albumSearch)
    {
      if (string.IsNullOrEmpty(albumOnline.Album) || string.IsNullOrEmpty(albumSearch.Album) || !albumOnline.AlbumVolumesAreEqual(albumSearch))
        return MAX_LEVENSHTEIN_DIST + 1;

      string cleanedName = RemoveCharacters(albumSearch.Album);
      return StringUtils.GetLevenshteinDistance(RemoveCharacters(albumOnline.Album), cleanedName);
    }

    public static bool NamesAreMostlyEqual(MovieInfo movieOnline, MovieInfo movieSearch)
    {
      if (movieOnline.MovieName.IsEmpty || movieSearch.MovieName.IsEmpty)
        return false;

      if (string.IsNullOrEmpty(movieOnline.OriginalName))
        return movieSearch.OnlineMatchNames(movieOnline.MovieName.Text, movieSearch.MovieName.Text);
      else
      {
        if (movieSearch.OnlineMatchNames(movieOnline.MovieName.Text, movieSearch.MovieName.Text))
          return true;
        if (movieSearch.OnlineMatchNames(movieOnline.OriginalName, movieSearch.MovieName.Text))
          return true;
      }
      return false;
    }

    public static bool NamesAreMostlyEqual(EpisodeInfo episodeOnline, EpisodeInfo episodeSearch)
    {
      if (episodeOnline.EpisodeName.IsEmpty || episodeSearch.EpisodeName.IsEmpty)
        return false;

      return episodeSearch.OnlineMatchNames(episodeOnline.EpisodeName.Text, episodeSearch.EpisodeName.Text);
    }

    public static bool NamesAreMostlyEqual(SeriesInfo seriesOnline, SeriesInfo seriesSearch)
    {
      if (seriesOnline.SeriesName.IsEmpty || seriesSearch.SeriesName.IsEmpty)
        return false;

      if (string.IsNullOrEmpty(seriesOnline.OriginalName))
        return seriesSearch.OnlineMatchNames(seriesOnline.SeriesName.Text, seriesSearch.SeriesName.Text);
      else
      {
        if (seriesSearch.OnlineMatchNames(seriesOnline.SeriesName.Text, seriesSearch.SeriesName.Text))
          return true;
        if (seriesSearch.OnlineMatchNames(seriesOnline.OriginalName, seriesSearch.SeriesName.Text))
          return true;
      }
      return false;
    }

    public static bool NamesAreMostlyEqual(PersonInfo personOnline, PersonInfo personSearch)
    {
      if (string.IsNullOrEmpty(personOnline.Name) || string.IsNullOrEmpty(personSearch.Name))
        return false;
      if (personOnline.Occupation != personSearch.Occupation)
        return false;

      if (string.IsNullOrEmpty(personOnline.AlternateName))
        return personSearch.OnlineMatchNames(personOnline.Name, personSearch.Name);
      else
      {
        if (personSearch.OnlineMatchNames(personOnline.Name, personSearch.Name))
          return true;
        if (personSearch.OnlineMatchNames(personOnline.AlternateName, personSearch.Name))
          return true;
      }
      return false;
    }

    public static bool NamesAreMostlyEqual(CharacterInfo characterOnline, CharacterInfo characterSearch)
    {
      if (string.IsNullOrEmpty(characterOnline.Name) || string.IsNullOrEmpty(characterSearch.Name))
        return false;

      return characterSearch.OnlineMatchNames(characterOnline.Name, characterSearch.Name);
    }

    public static bool NamesAreMostlyEqual(CompanyInfo companyOnline, CompanyInfo companySearch)
    {
      if (string.IsNullOrEmpty(companyOnline.Name) || string.IsNullOrEmpty(companySearch.Name))
        return false;
      if (companyOnline.Type != companySearch.Type)
        return false;

      return companySearch.OnlineMatchNames(companyOnline.Name, companySearch.Name);
    }

    public static bool NamesAreMostlyEqual(TrackInfo trackOnline, TrackInfo trackSearch)
    {
      if (string.IsNullOrEmpty(trackOnline.TrackName) || string.IsNullOrEmpty(trackSearch.TrackName))
        return false;

      bool trackMatch = trackSearch.OnlineMatchNames(trackOnline.TrackName, trackSearch.TrackName);
      if (!string.IsNullOrEmpty(trackSearch.Album) && trackMatch)
      {
        return NamesAreMostlyEqual(trackOnline.CloneBasicInstance<AlbumInfo>(), trackSearch.CloneBasicInstance<AlbumInfo>());
      }
      return trackMatch;
    }

    public static bool NamesAreMostlyEqual(AlbumInfo albumOnline, AlbumInfo albumSearch)
    {
      if (string.IsNullOrEmpty(albumOnline.Album) || string.IsNullOrEmpty(albumSearch.Album) || !albumOnline.AlbumVolumesAreEqual(albumSearch))
        return false;      
      return albumSearch.OnlineMatchNames(albumOnline.Album, albumSearch.Album);
    }

    /// <summary>
    /// Replaces characters that are not necessary for comparing (like whitespaces) and diacritics. The result is returned as <see cref="string.ToLowerInvariant"/>.
    /// </summary>
    /// <param name="name">Name to clean up</param>
    /// <returns>Cleaned string</returns>
    public static string RemoveCharacters(string name)
    {
      if (string.IsNullOrEmpty(name))
        return name;

      name = name.ToLowerInvariant();
      string result = new[] { "-", ",", "/", ":", " ", " ", ".", "'", "(", ")", "[", "]", "teil", "part" }.Aggregate(name, (current, s) => current.Replace(s, ""));
      result = result.Replace("&", "and");
      return StringUtils.RemoveDiacritics(result);
    }

    #endregion
  }
}
