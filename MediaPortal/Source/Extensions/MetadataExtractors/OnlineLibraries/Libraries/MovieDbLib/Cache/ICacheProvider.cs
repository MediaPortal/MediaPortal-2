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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using MovieDbLib.Data;
using MovieDbLib.Data.Banner;

namespace MovieDbLib.Cache
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
    /// <param name="_movieId">the id of the movie</param>
    /// <returns>true if the series was removed from the cache successfully, 
    ///          false otherwise (e.g. series not cached)</returns>
    bool RemoveFromCache(int _movieId);

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
    /// <param name="_movieId">Id of the movie to load</param>
    /// <returns>The MovieDbMovie object from cache or null</returns>
    MovieDbMovie LoadMovieFromCache(int _movieId);


    /// <summary>
    /// Load the given person from cache
    /// </summary>
    /// <param name="_movieId">Id of the person to load</param>
    /// <returns>The MovieDbPerson object from cache or null</returns>
    MovieDbPerson LoadPersonFromCache(int _personId);

    /// <summary>
    /// Save the language to cache
    /// </summary>
    /// <param name="_languageList">List of languages that are available on http://TheMovieDb.org</param>
    void SaveToCache(List<MovieDbLanguage> _languageList);

    /// <summary>
    /// Saves the movie to cache
    /// </summary>
    /// <param name="_movie">MovieDbMovie object</param>
    void SaveToCache(MovieDbMovie _movie);


    /// <summary>
    /// Saves the person to cache
    /// </summary>
    /// <param name="person">MovieDbPerson object</param>
    void SaveToCache(MovieDbPerson person);

    /// <summary>
    /// Save the given image to cache
    /// </summary>
    /// <param name="_image">banner to save</param>
    /// <param name="_objectId">id of movie/person/...</param>
    /// <param name="_bannerId">id of banner</param>
    /// <param name="_type">type of the banner</param>
    /// <param name="_size">size of the banner</param>
    void SaveToCache(Image _image, int _objectId, string _bannerId, MovieDbBanner.BannerTypes _type, MovieDbBanner.BannerSizes _size);

    /// <summary>
    /// Loads the specified image from the cache
    /// </summary>
    /// <param name="_seriesId">series id</param>
    /// <param name="_fileName">filename of the image (same one as used by SaveToCache)</param>
    /// <returns>The loaded image or null if the image wasn't found</returns>
    Image LoadImageFromCache(int _objectId, string _bannerId, MovieDbBanner.BannerTypes _type, MovieDbBanner.BannerSizes _size);

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
    /// <param name="_movieId">Id of the movie</param>
    /// <returns>true if the series is cached, false otherwise</returns>
    bool IsMovieCached(int _movieId);

    /// <summary>
    /// Removes the specified image from cache (if it has been cached)
    /// </summary>
    /// <param name="_seriesId">id of series</param>
    /// <param name="_fileName">name of image</param>
    /// <returns>true if image was removed successfully, false otherwise (e.g. image didn't exist)</returns>
    bool RemoveImageFromCache(int _movieId, string _fileName);

    /// <summary>
    /// Check if the person is already cached
    /// </summary>
    /// <param name="_personId">Id of the person</param>
    /// <returns>true if the person is cached, false otherwise</returns>
    bool IsPersonCached(int _personId);


  }
}
