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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using System.Drawing;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Cache
{
  /// <summary>
  /// Binary cache provider saves all the cached info into 
  /// 
  /// broken at the moment -> use CacheProvider
  /// </summary>
  public class BinaryCacheProvider : ICacheProvider
  {
    #region class that holds configuration for each series
    /// <summary>
    /// Class to store what parts of the cached series has been loaded
    /// </summary>
    [Serializable]
    internal class SeriesConfiguration
    {
      #region private fields

      #endregion

      /// <summary>
      /// Are actors loaded
      /// </summary>
      internal bool ActorsLoaded { get; set; }

      /// <summary>
      /// Are banners loaded
      /// </summary>
      internal bool BannersLoaded { get; set; }

      /// <summary>
      /// Are episodes loaded
      /// </summary>
      internal bool EpisodesLoaded { get; set; }

      /// <summary>
      /// Id of series
      /// </summary>
      internal int SeriesId { get; set; }

      /// <summary>
      /// constructor
      /// </summary>
      /// <param name="seriesId">Id of series</param>
      /// <param name="episodesLoaded">Are episodes loaded</param>
      /// <param name="bannersLoaded">Are banners loaded</param>
      /// <param name="actorsLoaded">Are actors loaded</param>
      internal SeriesConfiguration(int seriesId, bool episodesLoaded, bool bannersLoaded, bool actorsLoaded)
      {
        SeriesId = seriesId;
        EpisodesLoaded = episodesLoaded;
        BannersLoaded = bannersLoaded;
        ActorsLoaded = actorsLoaded;
      }
    }
    #endregion

    #region private fields
    private readonly BinaryFormatter _formatter;//Formatter to serialize/deserialize messages
    private readonly String _rootFolder;
    private FileStream _filestream;

    #endregion

    /// <summary>
    /// BinaryCacheProvider constructor
    /// </summary>
    /// <param name="root">The root folder where the cached data should be stored</param>
    public BinaryCacheProvider(String root)
    {
      _formatter = new BinaryFormatter(); // the formatter that will serialize my object on my stream
      _rootFolder = root;
    }

    #region ICacheProvider Members

    /// <summary>
    /// Load the cached data
    /// </summary>
    /// <returns>TvdbData object</returns>
    public TvdbData LoadUserDataFromCache()
    {
      List<TvdbLanguage> languages = LoadLanguageListFromCache();
      DateTime lastUpdated = LoadLastUpdatedFromCache();
      TvdbData data = new TvdbData(languages) { LastUpdated = lastUpdated };
      return data;
    }

    /// <summary>
    /// Initialises the cache, should do the following things
    /// - initialise connections used for this cache provider (db connections, network shares,...)
    /// - create folder structure / db tables / ...  if they are not created already
    /// - if this is the first time the cache has been initialised (built), mark last_updated with the
    ///   current date
    /// </summary>
    /// <returns>Tvdb Data object</returns>
    public TvdbData InitCache()
    {
      try
      {
        if (!Directory.Exists(_rootFolder))
          Directory.CreateDirectory(_rootFolder);

        TvdbData data = LoadUserDataFromCache();
        if (data == null)
        {
          //the cache has never been initialised before -> do it now
          data = new TvdbData { LanguageList = new List<TvdbLanguage>(), LastUpdated = DateTime.Now };
          SaveToCache(data);
        }
        Initialised = true;
        return data;
      }
      catch (Exception ex)
      {
        Log.Error("Couldn't initialise cache: " + ex);
        return null;
      }
    }

    /// <summary>
    /// Closes the cache (e.g. close open connection, etc.)
    /// </summary>
    /// <returns>true if successful, false otherwise</returns>
    public bool CloseCache()
    {
      return true;
    }

    /// <summary>
    /// Saves cache settings
    /// </summary>
    /// <param name="content">settings</param>
    public void SaveToCache(TvdbData content)
    {
      SaveToCache(content.LanguageList);
      SaveToCache(content.LastUpdated);
    }

    /// <summary>
    /// Saves the time of the last update to cache
    /// </summary>
    /// <param name="time">time of last update</param>
    private void SaveToCache(DateTime time)
    {
      if (!Directory.Exists(_rootFolder)) Directory.CreateDirectory(_rootFolder);
      _filestream = new FileStream(_rootFolder + Path.DirectorySeparatorChar + "lastUpdated.ser", FileMode.Create);
      _formatter.Serialize(_filestream, time);
      _filestream.Close();
    }


    /// <summary>
    /// Save the language to cache
    /// </summary>
    /// <param name="languageList">List of languages</param>
    public void SaveToCache(List<TvdbLanguage> languageList)
    {
      if (languageList != null)
      {
        if (!Directory.Exists(_rootFolder)) Directory.CreateDirectory(_rootFolder);
        _filestream = new FileStream(_rootFolder + Path.DirectorySeparatorChar + "languageInfo.ser", FileMode.Create);
        _formatter.Serialize(_filestream, languageList);
        _filestream.Close();
      }
    }

    /// <summary>
    /// Save the mirror info to cache
    /// </summary>
    /// <param name="mirrorInfo">list of mirrors</param>
    [Obsolete("Not used any more, however if won't delete the class since it could be useful at some point")]
    public void SaveToCache(List<TvdbMirror> mirrorInfo)
    {
      if (mirrorInfo != null)
      {
        if (!Directory.Exists(_rootFolder)) Directory.CreateDirectory(_rootFolder);
        _filestream = new FileStream(_rootFolder + Path.DirectorySeparatorChar + "mirrorInfo.ser", FileMode.Create);
        _formatter.Serialize(_filestream, mirrorInfo);
        _filestream.Close();
      }
    }

    /// <summary>
    /// Loads the available languages from cache
    /// </summary>
    /// <returns>List of available languages</returns>
    public List<TvdbLanguage> LoadLanguageListFromCache()
    {
      if (File.Exists(_rootFolder + Path.DirectorySeparatorChar + "languageInfo.ser"))
      {
        try
        {
          FileStream fs = new FileStream(_rootFolder + Path.DirectorySeparatorChar + "languageInfo.ser", FileMode.Open);
          List<TvdbLanguage> retValue = (List<TvdbLanguage>)_formatter.Deserialize(fs);
          fs.Close();
          return retValue;
        }
        catch (SerializationException)
        {
          return null;

        }
      }
      return null;
    }

    /// <summary>
    /// Load the available mirrors from cache
    /// </summary>
    /// <returns>List of available mirrors</returns>
    [Obsolete("Not used any more, however if won't delete the class since it could be useful at some point")]
    public List<TvdbMirror> LoadMirrorListFromCache()
    {
      if (File.Exists(_rootFolder + Path.DirectorySeparatorChar + "mirrorInfo.ser"))
      {
        try
        {
          FileStream fs = new FileStream(_rootFolder + Path.DirectorySeparatorChar + "mirrorInfo.ser", FileMode.Open);
          List<TvdbMirror> retValue = (List<TvdbMirror>)_formatter.Deserialize(fs);
          fs.Close();
          return retValue;
        }
        catch (SerializationException)
        {
          return null;

        }
      }
      return null;
    }

    /// <summary>
    /// Load the give series from cache
    /// </summary>
    /// <param name="seriesId">id of series to load</param>
    /// <returns>loaded series, or null if not successful</returns>
    public TvdbSeries LoadSeriesFromCache(int seriesId)
    {
      String seriesFile = _rootFolder + Path.DirectorySeparatorChar + seriesId +
                          Path.DirectorySeparatorChar + "series_" + seriesId + ".ser";
      if (File.Exists(seriesFile))
      {
        try
        {
          FileStream fs = new FileStream(seriesFile, FileMode.Open);
          TvdbSeries retValue = (TvdbSeries)_formatter.Deserialize(fs);
          fs.Close();
          return retValue;
        }
        catch (SerializationException)
        {
          return null;

        }
      }
      return null;
    }

    public string[] GetSeriesCacheFiles(int seriesId)
    {
      return new[] { _rootFolder + Path.DirectorySeparatorChar + seriesId +
                          Path.DirectorySeparatorChar + "series_" + seriesId + ".ser" };
    }

    /// <summary>
    /// Saves the series to cache
    /// </summary>
    /// <param name="series">Tvdb series</param>
    public void SaveToCache(TvdbSeries series)
    {
      if (series != null)
      {
        String seriesRoot = _rootFolder + Path.DirectorySeparatorChar + series.Id;
        if (!Directory.Exists(seriesRoot)) Directory.CreateDirectory(seriesRoot);

        #region delete all loaded images (since they should be already cached)

        //delete banners
        foreach (TvdbBanner b in series.Banners)
        {
          if (b.IsLoaded)
          {//banner is loaded
            b.UnloadBanner();
          }

          //remove the ref to the cacheprovider
          b.CacheProvider = null;

          if (b.GetType() == typeof(TvdbBannerWithThumb))
          {//thumb is loaded
            if (((TvdbBannerWithThumb)b).IsThumbLoaded)
            {
              ((TvdbBannerWithThumb)b).UnloadThumb();
            }
          }

          if (b.GetType() == typeof(TvdbFanartBanner))
          {//vignette is loaded
            if (((TvdbFanartBanner)b).IsVignetteLoaded)
            {
              ((TvdbFanartBanner)b).UnloadVignette();
            }
          }
        }

        //delete Actor Images
        if (series.TvdbActorsLoaded)
        {
          foreach (TvdbActor a in series.TvdbActors)
          {
            if (a.ActorImage.IsLoaded)
            {
              a.ActorImage.UnloadBanner();
            }
            //remove the ref to the cacheprovider
            a.ActorImage.CacheProvider = null;
          }
        }

        //delete episode images
        if (series.EpisodesLoaded)
        {
          foreach (TvdbEpisode e in series.Episodes)
          {
            if (e.Banner.IsLoaded)
            {
              e.Banner.UnloadBanner();
            }
            //remove the ref to the cacheprovider
            e.Banner.CacheProvider = null;
          }
        }
        #endregion
        //serialize series to hdd
        _filestream = new FileStream(seriesRoot + Path.DirectorySeparatorChar + "series_" + series.Id + ".ser", FileMode.Create);
        _formatter.Serialize(_filestream, series);
        _filestream.Close();

        //serialize series config to hdd
        SeriesConfiguration cfg = new SeriesConfiguration(series.Id, series.EpisodesLoaded,
                                                  series.BannersLoaded, series.TvdbActorsLoaded);
        _filestream = new FileStream(seriesRoot + Path.DirectorySeparatorChar + "series_" + series.Id + ".cfg", FileMode.Create);
        _formatter.Serialize(_filestream, cfg);
        _filestream.Close();
      }
    }

    /// <summary>
    /// Saves the user data to cache
    /// </summary>
    /// <param name="user">TvdbUser</param>
    public void SaveToCache(TvdbUser user)
    {
      if (user != null)
      {
        if (!Directory.Exists(_rootFolder)) Directory.CreateDirectory(_rootFolder);
        _filestream = new FileStream(_rootFolder + Path.DirectorySeparatorChar + "user_" + user.UserIdentifier + ".ser", FileMode.Create);
        _formatter.Serialize(_filestream, user);
        _filestream.Close();
      }
    }

    /// <summary>
    /// Loads all series from cache
    /// </summary>
    /// <returns>List that contains all series object that had been previously cached</returns>
    public List<TvdbSeries> LoadAllSeriesFromCache()
    {
      if (Directory.Exists(_rootFolder))
      {
        List<TvdbSeries> retList = new List<TvdbSeries>();
        string[] dirs = Directory.GetDirectories(_rootFolder);
        foreach (String d in dirs)
        {
          int seriesId;
          if (Int32.TryParse(d.Remove(0, d.LastIndexOf(Path.DirectorySeparatorChar) + 1), out seriesId))
          {
            TvdbSeries series = LoadSeriesFromCache(seriesId);
            if (series != null) retList.Add(series);
          }
          else
          {
            Log.Error("Couldn't parse " + d + " when loading series from cache");
          }
        }
        return retList;
      }
      return null;
    }

    /// <summary>
    /// Load the userinfo from the cache
    /// </summary>
    /// <param name="userId">Id of user</param>
    /// <returns>TvdbUser object</returns>
    public TvdbUser LoadUserInfoFromCache(String userId)
    {
      if (File.Exists(_rootFolder + Path.DirectorySeparatorChar + "user_" + userId + ".ser"))
      {
        try
        {
          FileStream fs = new FileStream(_rootFolder + Path.DirectorySeparatorChar + "user_" + userId + ".ser", FileMode.Open);
          TvdbUser retValue = (TvdbUser)_formatter.Deserialize(fs);
          fs.Close();
          return retValue;
        }
        catch (SerializationException)
        {
          return null;

        }
      }
      return null;
    }

    /// <summary>
    /// Receives a list of all series that have been cached
    /// </summary>
    /// <returns>Ids of series that are already cached</returns>
    public List<int> GetCachedSeries()
    {
      List<int> retList = new List<int>();
      if (Directory.Exists(_rootFolder))
      {
        string[] dirs = Directory.GetDirectories(_rootFolder);
        foreach (String d in dirs)
        {
          int seriesId;
          if (Int32.TryParse(d.Remove(0, d.LastIndexOf(Path.DirectorySeparatorChar) + 1), out seriesId))
          {
            retList.Add(seriesId);
          }
          else
          {
            Log.Error("Couldn't parse " + d + " when loading list of cached series");
          }
        }
      }
      return retList;
    }

    /// <summary>
    /// Check if the series is cached in the given configuration
    /// </summary>
    /// <param name="seriesId">Id of the series</param>
    /// <param name="lang">Language of the series</param>
    /// <param name="checkEpisodesLoaded">are episodes loaded</param>
    /// <param name="checkBannersLoaded">are banners loaded</param>
    /// <param name="checkActorsLoaded">are actors loaded</param>
    /// <returns>true if the series is cached, false otherwise</returns>
    public bool IsCached(int seriesId, TvdbLanguage lang, bool checkEpisodesLoaded,
                         bool checkBannersLoaded, bool checkActorsLoaded)
    {
      String fName = _rootFolder + Path.DirectorySeparatorChar + seriesId +
                     Path.DirectorySeparatorChar + "series_" + seriesId + ".cfg";
      if (File.Exists(fName))
      {
        try
        {
          FileStream fs = new FileStream(fName, FileMode.Open);
          SeriesConfiguration config = (SeriesConfiguration)_formatter.Deserialize(fs);
          fs.Close();

          return config.EpisodesLoaded || !checkEpisodesLoaded &&
                 config.BannersLoaded || !checkBannersLoaded &&
                 config.ActorsLoaded || !checkActorsLoaded;
        }
        catch (SerializationException)
        {
          Log.Warn("Cannot deserialize SeriesConfiguration object");
          return false;
        }
      }
      return false;
    }

    /// <summary>
    /// Is the cache provider initialised
    /// </summary>
    public bool Initialised { get; private set; }

    /// <summary>
    /// Completely refreshes the cached (all stored information is lost). 
    /// </summary>
    /// <returns>true if the cache was cleared successfully, 
    ///          false otherwise (e.g. no write rights,...)</returns>
    public bool ClearCache()
    {
      //Delete all series info
      Log.Info("Attempting to delete all series");
      string[] folders = Directory.GetDirectories(_rootFolder);
      foreach (String f in folders)
      {
        try
        {
          Directory.Delete(f);
        }
        catch (Exception ex)
        {
          Log.Warn("Error deleting series " + f + ", please manually delete the " +
                   "cache folder since it's now inconsistent", ex);
          return false;
        }
      }

      return true;
    }

    /// <summary>
    /// Remove a specific series from cache
    /// </summary>
    /// <param name="seriesId">the id of the series</param>
    /// <returns>true if the series was removed from the cache successfully, 
    ///          false otherwise (e.g. series not cached)</returns>
    public bool RemoveFromCache(int seriesId)
    {
      String seriesDir = _rootFolder + Path.DirectorySeparatorChar + seriesId;
      if (Directory.Exists(seriesDir))
      {
        try
        {
          Directory.Delete(seriesDir);
          return true;
        }
        catch (Exception ex)
        {
          Log.Warn("Error deleting series " + seriesId, ex);
          return false;
        }
      }
      Log.Debug("Series couldn't be deleted because it doesn't exist");
      return false;
    }

    /// <summary>
    /// Save the given image to cache
    /// </summary>
    /// <param name="image">banner to save</param>
    /// <param name="seriesId">id of series</param>
    /// <param name="fileName">filename (will be the same name used by LoadImageFromCache)</param>
    public void SaveToCache(Image image, int seriesId, string folderName, string fileName)
    {
      String imageRoot = _rootFolder + Path.DirectorySeparatorChar + seriesId;
      if (!string.IsNullOrEmpty(folderName))
      {
        imageRoot = folderName;
        if (!Directory.Exists(imageRoot))
          Directory.CreateDirectory(imageRoot);
      }
      if (Directory.Exists(imageRoot))
      {
        if (image != null)
          image.Save(imageRoot + Path.DirectorySeparatorChar + fileName);
      }
      else
      {
        Log.Warn("Couldn't save image " + fileName + " for series " + seriesId +
                 " because the series directory doesn't exist yet");
      }
    }


    /// <summary>
    /// Loads the specified image from the cache
    /// </summary>
    /// <param name="seriesId">series id</param>
    /// <param name="fileName">filename of the image (same one as used by SaveToCache)</param>
    /// <returns>The loaded image or null if the image wasn't found</returns>
    public Image LoadImageFromCache(int seriesId, string folderName, string fileName)
    {
      String imageRoot = _rootFolder + Path.DirectorySeparatorChar + seriesId;
      if (!string.IsNullOrEmpty(folderName))
        imageRoot = folderName;
      if (Directory.Exists(imageRoot))
      {
        String fName = imageRoot + Path.DirectorySeparatorChar + fileName;
        if (File.Exists(fName))
        {
          try
          {
            return Image.FromFile(fName);
          }
          catch (Exception ex)
          {
            Log.Warn("Couldn't load image " + fName + " for series " + seriesId, ex);
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Removes the specified image from cache (if it has been cached)
    /// </summary>
    /// <param name="seriesId">id of series</param>
    /// <param name="fileName">name of image</param>
    /// <returns>true if image was removed successfully, false otherwise (e.g. image didn't exist)</returns>
    public bool RemoveImageFromCache(int seriesId, string folderName, string fileName)
    {
      string imageRoot = _rootFolder + Path.DirectorySeparatorChar + seriesId;
      if (!string.IsNullOrEmpty(folderName))
        imageRoot = folderName;
      string fName = imageRoot + Path.DirectorySeparatorChar + fileName;

      if (File.Exists(fName))
      {//the image is cached
        try
        {//trying to delete the file
          File.Delete(fName);
          return true;
        }
        catch (Exception ex)
        {//error while deleting the image
          Log.Warn("Couldn't delete image " + fileName + " for series " + seriesId, ex);
          return false;
        }
      }
      else
      {//image isn't cached in the first place
        return false;
      }
    }

    #endregion

    #region Helper methods

    /// <summary>
    /// Load the time when the cache was updated last
    /// </summary>
    /// <returns>DateTime of last update</returns>
    private DateTime LoadLastUpdatedFromCache()
    {
      if (File.Exists(_rootFolder + Path.DirectorySeparatorChar + "lastUpdated.ser"))
      {
        try
        {
          FileStream fs = new FileStream(_rootFolder + Path.DirectorySeparatorChar + "lastUpdated.ser", FileMode.Open);
          DateTime retValue = (DateTime)_formatter.Deserialize(fs);
          fs.Close();
          return retValue;
        }
        catch (SerializationException)
        {
          return DateTime.Now;
        }
      }
      return DateTime.Now;
    }

    #endregion

  }
}
