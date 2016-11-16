/*
 *   TvdbLib: A library to retrieve information and media from http://thetvdb.com
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
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using System.Drawing;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Cache
{
  /// <summary>
  /// A cache provider stores and loads the data that has been previously retrieved from http://thetvdb.com.
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
    TvdbData InitCache();

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
    /// <param name="seriesId">the id of the series</param>
    /// <returns>true if the series was removed from the cache successfully, 
    ///          false otherwise (e.g. series not cached)</returns>
    bool RemoveFromCache(int seriesId);

    /// <summary>
    /// Loads all cached series from cache -> can take a while
    /// </summary>
    /// <returns>The loaded TvdbData object</returns>
    TvdbData LoadUserDataFromCache();

    /// <summary>
    /// Loads the available languages from cache
    /// </summary>
    /// <returns>A list of TvdbLanguage objects from cache or null</returns>
    List<TvdbLanguage> LoadLanguageListFromCache();

    /// <summary>
    /// Loads all series from cache
    /// </summary>
    /// <returns>A list of TvdbSeries objects from cache or null</returns>
    List<TvdbSeries> LoadAllSeriesFromCache();

    /// <summary>
    /// Load the give series from cache
    /// </summary>
    /// <param name="seriesId">Id of the series to load</param>
    /// <returns>The TvdbSeries object from cache or null</returns>
    TvdbSeries LoadSeriesFromCache(int seriesId);

    /// <summary>
    /// Return path to series cache file
    /// </summary>
    /// <param name="seriesId">Id of the series to load</param>
    /// <returns>The TvdbSeries cache file</returns>
    string[] GetSeriesCacheFiles(int seriesId);

    /// <summary>
    /// Load user info from cache
    /// </summary>
    /// <param name="userId">Id of the user</param>
    /// <returns>TvdbUser object or null if the user couldn't be loaded</returns>
    TvdbUser LoadUserInfoFromCache(String userId);

    /// <summary>
    /// Saves cache settings
    /// </summary>
    /// <param name="content">settings</param>
    void SaveToCache(TvdbData content);

    /// <summary>
    /// Save the language to cache
    /// </summary>
    /// <param name="languageList">List of languages that are available on http://thetvdb.com</param>
    void SaveToCache(List<TvdbLanguage> languageList);

    /// <summary>
    /// Saves the series to cache
    /// </summary>
    /// <param name="series">TvdbSeries object</param>
    void SaveToCache(TvdbSeries series);

    /// <summary>
    /// Saves the user data to cache
    /// </summary>
    /// <param name="user">TvdbUser object</param>
    void SaveToCache(TvdbUser user);

    /// <summary>
    /// Save the given image to cache
    /// </summary>
    /// <param name="image">banner to save</param>
    /// <param name="seriesId">id of series</param>
    /// <param name="folderName">folder name</param>
    /// <param name="fileName">filename (will be the same name used by LoadImageFromCache)</param>
    void SaveToCache(Image image, int seriesId, string folderName, string fileName);

    /// <summary>
    /// Loads the specified image from the cache
    /// </summary>
    /// <param name="seriesId">series id</param>
    /// <param name="folderName">folder name</param>
    /// <param name="fileName">filename of the image (same one as used by SaveToCache)</param>
    /// <returns>The loaded image or null if the image wasn't found</returns>
    Image LoadImageFromCache(int seriesId, string folderName, String fileName);

    /// <summary>
    /// Receives a list of all series that have been cached
    /// </summary>
    /// <returns>A list of series that have been already stored with this cache provider</returns>
    List<int> GetCachedSeries();

    /// <summary>
    /// Check if the series is cached in the given configuration
    /// </summary>
    /// <param name="seriesId">Id of the series</param>
    /// <param name="lang">Language of the series</param>
    /// <param name="checkEpisodesLoaded">are episodes loaded</param>
    /// <param name="checkBannersLoaded">are banners loaded</param>
    /// <param name="checkActorsLoaded">are actors loaded</param>
    /// <returns>true if the series is cached, false otherwise</returns>
    bool IsCached(int seriesId, TvdbLanguage lang, bool checkEpisodesLoaded, bool checkBannersLoaded, bool checkActorsLoaded);

    /// <summary>
    /// Removes the specified image from cache (if it has been cached)
    /// </summary>
    /// <param name="seriesId">id of series</param>
    /// <param name="folderName">folder name</param>
    /// <param name="fileName">name of image</param>
    /// <returns>true if image was removed successfully, false otherwise (e.g. image didn't exist)</returns>
    bool RemoveImageFromCache(int seriesId, string folderName, string fileName);
  }
}
