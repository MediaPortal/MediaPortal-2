/*
 *   MovieDbLib: A library to retrieve information and media from http://TheMovieDb.org
 * 
 *   Copyright (C) 2008  Benjamin Gmeiner
 * 
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Persons;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Cache
{
  /// <summary>
  /// A cache provider stores and loads the data that has been previously retrieved from http://TheMovieDb.org.
  /// </summary>
  public interface ICacheProvider
  {
    
    /// <summary>
    /// Is the cache provider initialised
    /// </summary>
    bool Initialised { get; }

    /// <summary>
    /// Initialises the cache, should do the following things
    /// - initialise connections used for this cache provider (db connections, network shares,...)
    /// - create folder structure / db tables / ...  if they are not created already
    /// - if this is the first time the cache has been initialised (built), mark last_updated with the
    ///   current date
    /// </summary>
    /// <returns>TvdbData object</returns>
    bool InitCache();

    /// <summary>
    /// Closes the cache (e.g. close open connection, etc.)
    /// </summary>
    /// <returns>true if successful, false otherwise</returns>
    bool CloseCache();

    /// <summary>
    /// Completely refreshes the cache (all stored information is lost)
    /// </summary>
    /// <returns>true if the cache was cleared successfully, 
    ///          false otherwise (e.g. no write rights,...)</returns>
    bool ClearCache();

    /// <summary>
    /// Remove a specific series from cache
    /// </summary>
    /// <param name="seriesId">the id of the movie</param>
    /// <returns>true if the series was removed from the cache successfully, 
    ///          false otherwise (e.g. series not cached)</returns>
    bool RemoveFromCache(int seriesId);

    /// <summary>
    /// Loads the available languages from cache
    /// </summary>
    /// <returns>A list of TvdbLanguage objects from cache or null</returns>
    List<MovieDbLanguage> LoadLanguageListFromCache();

    /// <summary>
    /// Loads all series from cache
    /// </summary>
    /// <returns>A list of TvdbSeries objects from cache or null</returns>
    List<MovieDbMovie> LoadAllMoviesFromCache();

    /// <summary>
    /// Load the given movie from cache
    /// </summary>
    /// <param name="movieId">Id of the movie to load</param>
    /// <returns>The MovieDbMovie object from cache or null</returns>
    MovieDbMovie LoadMovieFromCache(int movieId);


    /// <summary>
    /// Load the given Person from cache
    /// </summary>
    /// <param name="personId">Id of the Person to load</param>
    /// <returns>The MovieDbPerson object from cache or null</returns>
    MovieDbPerson LoadPersonFromCache(int personId);

    /// <summary>
    /// Save the language to cache
    /// </summary>
    /// <param name="languageList">List of languages that are available on http://TheMovieDb.org</param>
    void SaveToCache(List<MovieDbLanguage> languageList);

    /// <summary>
    /// Saves the movie to cache
    /// </summary>
    /// <param name="movie">MovieDbMovie object</param>
    void SaveToCache(MovieDbMovie movie);


    /// <summary>
    /// Saves the Person to cache
    /// </summary>
    /// <param name="person">MovieDbPerson object</param>
    void SaveToCache(MovieDbPerson person);

    /// <summary>
    /// Save the given image to cache
    /// </summary>
    /// <param name="image">banner to save</param>
    /// <param name="objectId">id of movie/Person/...</param>
    /// <param name="bannerId">id of banner</param>
    /// <param name="type">type of the banner</param>
    /// <param name="size">size of the banner</param>
    void SaveToCache(Image image, int objectId, string bannerId, MovieDbBanner.BannerTypes type, MovieDbBanner.BannerSizes size);

    /// <summary>
    /// Loads the specified image from the cache
    /// </summary>
    /// <param name="objectId"> </param>
    /// <param name="bannerId"> </param>
    /// <param name="type"> </param>
    /// <param name="size"> </param>
    /// <returns>The loaded image or null if the image wasn't found</returns>
    Image LoadImageFromCache(int objectId, string bannerId, MovieDbBanner.BannerTypes type, MovieDbBanner.BannerSizes size);

    /// <summary>
    /// Returns a list of all series that have been cached
    /// </summary>
    /// <returns>A list of IDs from all movies that have been already stored with this cache provider</returns>
    List<int> GetCachedMovies();

    /// <summary>
    /// Returns a list of all persons that have been cached
    /// </summary>
    /// <returns>A list of IDs from all persons that have been already stored with this cache provider</returns>
    List<int> GetCachedPersons();

    /// <summary>
    /// Check if the movie is already cached
    /// </summary>
    /// <param name="movieId">Id of the movie</param>
    /// <returns>true if the series is cached, false otherwise</returns>
    bool IsMovieCached(int movieId);

    /// <summary>
    /// Removes the specified image from cache (if it has been cached)
    /// </summary>
    /// <param name="movieId">id of movie</param>
    /// <param name="fileName">name of image</param>
    /// <returns>true if image was removed successfully, false otherwise (e.g. image didn't exist)</returns>
    bool RemoveImageFromCache(int movieId, string fileName);

    /// <summary>
    /// Check if the Person is already cached
    /// </summary>
    /// <param name="personId">Id of the Person</param>
    /// <returns>true if the Person is cached, false otherwise</returns>
    bool IsPersonCached(int personId);
  }
}
