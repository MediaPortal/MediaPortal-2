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
using System.Linq;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Xml;
using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Threading;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Cache
{
  /// <summary>
  /// XmlCacheProvider stores all the information that have been retrieved from http://thetvdb.com as human-readable xml files on the hard disk
  /// </summary>
  public class XmlCacheProvider : ICacheProvider
  {
    #region private fields

    private readonly TvdbXmlWriter _xmlWriter;
    private readonly TvdbXmlReader _xmlReader;
    private readonly String _rootFolder;

    // TODO: Lock on file level instead of on type level
    private ReaderWriterLockSlim _languageLock = new ReaderWriterLockSlim();
    private ReaderWriterLockSlim _mirrorLock = new ReaderWriterLockSlim();
    private ReaderWriterLockSlim _seriesLock = new ReaderWriterLockSlim();
    private ReaderWriterLockSlim _imageLock = new ReaderWriterLockSlim();
    private ReaderWriterLockSlim _dataLock = new ReaderWriterLockSlim();
    private ReaderWriterLockSlim _userLock = new ReaderWriterLockSlim();

    #endregion

    /// <summary>
    /// Constructor for XmlCacheProvider
    /// </summary>
    /// <param name="rootFolder">This is the folder on the disk where all the information are stored</param>
    public XmlCacheProvider(String rootFolder)
    {
      Initialised = false;
      _xmlWriter = new TvdbXmlWriter();
      _xmlReader = new TvdbXmlReader();
      _rootFolder = rootFolder.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Properly describe the CacheProvider for neat-reasons
    /// </summary>
    /// <returns>String describing the cache provider</returns>
    public override string ToString()
    {
      return "XmlCacheProvider (" + _rootFolder + ")";
    }

    #region ICacheProvider Members

    /// <summary>
    /// Is the cache provider initialised
    /// </summary>
    public bool Initialised { get; private set; }

    /// <summary>
    /// Initialises the cache, should do the following things
    /// - initialise connections used for this cache provider (db connections, network shares,...)
    /// - create folder structure / db tables / ...  if they are not created already
    /// - if this is the first time the cache has been initialised (built), mark last_updated with the
    ///   current date
    /// </summary>
    /// <returns>TvdbData object</returns>
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
      if (content == null)
        return;
      SaveToCache(content.LanguageList);

      _dataLock.EnterWriteLock();
      try
      {
        //store additional information
        //- time of last update
        //- more to come (eventually)
        XElement xml = new XElement("Data");
        xml.Add(new XElement("LastUpdated", Util.DotNetToUnix(content.LastUpdated)));
        String data = xml.ToString();
        File.WriteAllText(_rootFolder + Path.DirectorySeparatorChar + "data.xml", data);
      }
      finally
      {
        _dataLock.ExitWriteLock();
      }
    }

    /// <summary>
    /// Save the language to cache
    /// </summary>
    /// <param name="languageList">List of languages that are available on http://thetvdb.com</param>
    public void SaveToCache(List<TvdbLanguage> languageList)
    {
      if (languageList != null && languageList.Count > 0)
      {
        if (!Directory.Exists(_rootFolder)) Directory.CreateDirectory(_rootFolder);
        _languageLock.EnterWriteLock();
        try
        {
          _xmlWriter.WriteLanguageFile(languageList, _rootFolder + Path.DirectorySeparatorChar + "languages.xml");
        }
        finally
        {
          _languageLock.ExitWriteLock();
        }
      }
    }

    /// <summary>
    /// Save the mirror info to cache
    /// </summary>
    /// <param name="mirrorInfo">Mirrors</param>
    [Obsolete("Not used any more, however if won't delete the class since it could be useful at some point")]
    public void SaveToCache(List<TvdbMirror> mirrorInfo)
    {
      if (mirrorInfo != null && mirrorInfo.Count > 0)
      {
        if (!Directory.Exists(_rootFolder)) Directory.CreateDirectory(_rootFolder);
        _mirrorLock.EnterWriteLock();
        try
        {
          _xmlWriter.WriteMirrorFile(mirrorInfo, _rootFolder + Path.DirectorySeparatorChar + "mirrors.xml");
        }
        finally
        {
          _mirrorLock.ExitWriteLock();
        }
      }
    }


    /// <summary>
    /// Saves the series to cache
    /// </summary>
    /// <param name="series">The series to save</param>
    public void SaveToCache(TvdbSeries series)
    {
      String root = _rootFolder + Path.DirectorySeparatorChar + series.Id;
      if (!Directory.Exists(root)) Directory.CreateDirectory(root);
      _seriesLock.EnterWriteLock();
      try
      {
        try
        {
          //delete old cached content
          String[] files = Directory.GetFiles(root, "*.xml");
          foreach (String f in files)
          {
            File.Delete(f);
          }
        }
        catch (Exception ex)
        {
          Log.Warn("Couldn't delete old cache files", ex);
        }

        foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in series.SeriesTranslations)
        {
          //write all languages to file
          String fName = root + Path.DirectorySeparatorChar + kvp.Key.Abbriviation +
                         (kvp.Value.EpisodesLoaded ? "_full" : "") + ".xml";
          _xmlWriter.WriteSeriesContent(new TvdbSeries(kvp.Value), fName);
        }

        if (series.BannersLoaded)
        {
          //write the banners file 
          _xmlWriter.WriteSeriesBannerContent(series.Banners, root + Path.DirectorySeparatorChar + "banners.xml");
        }

        if (series.TvdbActorsLoaded)
        {
          //write the actors file
          _xmlWriter.WriteActorFile(series.TvdbActors, root + Path.DirectorySeparatorChar + "actors.xml");
        }
      }
      finally
      {
        _seriesLock.ExitWriteLock();
      }
    }

    /// <summary>
    /// Loads the settings data from cache 
    /// </summary>
    /// <returns>The loaded TvdbData object</returns>
    public TvdbData LoadUserDataFromCache()
    {
      String fName = _rootFolder + Path.DirectorySeparatorChar + "data.xml";
      _dataLock.EnterReadLock();
      try
      {
        if (File.Exists(fName))
        {
          String xmlData = File.ReadAllText(fName);
          XDocument xml = XDocument.Parse(xmlData);

          var info = from dataNode in xml.Descendants("Data")
                     select new
                     {
                       lu = dataNode.Element("LastUpdated").Value
                     };
          if (info.Count() == 1)
          {
            TvdbData data = new TvdbData();
            DateTime lastUpdated = new DateTime();
            try
            {
              lastUpdated = Util.UnixToDotNet(info.First().lu);
            }
            catch (FormatException ex)
            {
              Log.Warn("Couldn't parse date of last update", ex);
            }
            data.LastUpdated = lastUpdated;
            data.LanguageList = LoadLanguageListFromCache();
            //if (data.SeriesList == null) data.SeriesList = new List<TvdbSeries>();
            return data;
          }
        }
      }
      finally
      {
        _dataLock.ExitReadLock();
      }

      return null;

    }

    /// <summary>
    /// Loads the available languages from cache
    /// </summary>
    /// <returns>List of available languages</returns>
    public List<TvdbLanguage> LoadLanguageListFromCache()
    {
      String file = _rootFolder + Path.DirectorySeparatorChar + "languages.xml";
      _languageLock.EnterReadLock();
      try
      {
        return File.Exists(file) ? _xmlReader.ExtractLanguages(File.ReadAllText(file)) : null;
      }
      finally
      {
        _languageLock.ExitReadLock();
      }
    }

    /// <summary>
    /// Load the available mirrors from cache
    /// </summary>
    /// <returns>List of mirrors</returns>
    [Obsolete("Not used any more, however if won't delete the class since it could be useful at some point")]
    public List<TvdbMirror> LoadMirrorListFromCache()
    {
      String file = _rootFolder + Path.DirectorySeparatorChar + "mirrors.xml";
      _mirrorLock.EnterReadLock();
      try
      {
        return File.Exists(file) ? _xmlReader.ExtractMirrors(File.ReadAllText(file)) : null;
      }
      finally
      {
        _mirrorLock.ExitReadLock();
      }
    }

    /// <summary>
    /// Loads all series from cache
    /// </summary>
    /// <returns>A list of TvdbSeries objects from cache or null</returns>
    public List<TvdbSeries> LoadAllSeriesFromCache()
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
          Log.Error("Couldn't parse " + d + " when loading series from cache");
      }
      return retList;
    }



    /// <summary>
    /// Load the give series from cache
    /// </summary>
    /// <param name="seriesId">Id of the series to load</param>
    /// <returns>Series that has been loaded or null if series doesn't exist</returns>
    public TvdbSeries LoadSeriesFromCache(int seriesId)
    {
      String seriesRoot = _rootFolder + Path.DirectorySeparatorChar + seriesId;
      if (!Directory.Exists(seriesRoot)) return null;
      TvdbSeries series = new TvdbSeries();

      _seriesLock.EnterReadLock();

      try
      {
        #region load series in all available languages
        String[] seriesLanguages = Directory.GetFiles(seriesRoot, "*.xml");
        foreach (String l in seriesLanguages)
        {
          if (!l.EndsWith("actors.xml") && !l.EndsWith("banners.xml"))
          {
            String content = File.ReadAllText(l);
            List<TvdbSeriesFields> seriesList = _xmlReader.ExtractSeriesFields(content);
            if (seriesList != null && seriesList.Count == 1)
            {
              TvdbSeriesFields s = seriesList[0];
              //Load episodes
              if (l.EndsWith("full.xml"))
              {
                List<TvdbEpisode> epList = _xmlReader.ExtractEpisodes(content);
                s.EpisodesLoaded = true;
                s.Episodes.Clear();
                s.Episodes.AddRange(epList);
              }
              series.AddLanguage(s);
            }
          }
        }

        if (series.SeriesTranslations.Count > 0)
        {
          //change language of the series to the default language
          series.SetLanguage(series.SeriesTranslations.Keys.First());
        }
        else
        {
          //no series info could be loaded
          return null;
        }

        if (!series.BannerPath.Equals(""))
          series.Banners.Add(new TvdbSeriesBanner(series.Id, series.BannerPath, series.Language, TvdbSeriesBanner.Type.Graphical));

        if (!series.PosterPath.Equals(""))
          series.Banners.Add(new TvdbPosterBanner(series.Id, series.PosterPath, series.Language));

        if (!series.FanartPath.Equals(""))
          series.Banners.Add(new TvdbFanartBanner(series.Id, series.FanartPath, series.Language));

        Regex rex = new Regex("S(\\d+)E(\\d+)");
        if (Directory.Exists(seriesRoot + Path.DirectorySeparatorChar + "EpisodeImages"))
        {
          String[] episodeFiles = Directory.GetFiles(seriesRoot + Path.DirectorySeparatorChar + "EpisodeImages", "ep_*.jpg");
          foreach (String epImageFile in episodeFiles)
          {
            try
            {
              Match match = rex.Match(epImageFile);
              int season = Int32.Parse(match.Groups[1].Value);
              int episode = Int32.Parse(match.Groups[2].Value);
              foreach (TvdbEpisode e in series.Episodes.Where(e => e.SeasonNumber == season && e.EpisodeNumber == episode))
              {
                if (epImageFile.Contains("thumb"))
                  e.Banner.LoadThumb(Image.FromFile(epImageFile));
                else
                  e.Banner.LoadBanner(Image.FromFile(epImageFile));
                break;
              }
            }
            catch (Exception)
            {
              Log.Warn("Couldn't load episode image file " + epImageFile);
            }
          }
        }

        #endregion

        #region Banner loading
        String bannerFile = seriesRoot + Path.DirectorySeparatorChar + "banners.xml";
        //load cached banners
        if (File.Exists(bannerFile))
        {
          //banners have been already loaded
          List<TvdbBanner> bannerList = _xmlReader.ExtractBanners(File.ReadAllText(bannerFile));

          String[] banners = Directory.GetFiles(seriesRoot, "banner*.jpg");
          foreach (String b in banners)
          {
            try
            {
              int bannerId = Int32.Parse(b.Remove(b.IndexOf(".")).Remove(0, b.LastIndexOf("_") + 1));
              foreach (TvdbBanner banner in bannerList.Where(banner => banner.Id == bannerId))
              {
                if (b.Contains("thumb") && banner.GetType().BaseType == typeof(TvdbBannerWithThumb))
                  ((TvdbBannerWithThumb)banner).LoadThumb(Image.FromFile(b));
                else if (b.Contains("vignette") && banner.GetType() == typeof(TvdbFanartBanner))
                  ((TvdbFanartBanner)banner).LoadVignette(Image.FromFile(b));
                else
                  banner.LoadBanner(Image.FromFile(b));
              }
            }
            catch (Exception)
            {
              Log.Warn("Couldn't load image file " + b);
            }
          }
          series.Banners = bannerList;
        }
        #endregion

        #region actor loading
        //load actor info
        String actorFile = seriesRoot + Path.DirectorySeparatorChar + "actors.xml";
        if (File.Exists(actorFile))
        {
          List<TvdbActor> actorList = _xmlReader.ExtractActors(File.ReadAllText(actorFile));

          String[] banners = Directory.GetFiles(seriesRoot, "actor_*.jpg");
          foreach (String b in banners)
          {
            try
            {
              int actorId = Int32.Parse(b.Remove(b.IndexOf(".")).Remove(0, b.LastIndexOf("_") + 1));
              foreach (TvdbActor actor in actorList.Where(actor => actor.Id == actorId))
                actor.ActorImage.LoadBanner(Image.FromFile(b));
            }
            catch (Exception)
            {
              Log.Warn("Couldn't load image file " + b);
            }
          }
          series.TvdbActors = actorList;
        }
        #endregion
      }
      finally
      {
        _seriesLock.ExitReadLock();
      }

      return series;

    }

    public string[] GetSeriesCacheFiles(int seriesId)
    {
      String seriesRoot = _rootFolder + Path.DirectorySeparatorChar + seriesId;
      return Directory.GetFiles(seriesRoot, "*.xml");
    }

    /// <summary>
    /// Load user info from cache
    /// </summary>
    /// <param name="userId">Id of the user</param>
    /// <returns>TvdbUser object or null if the user couldn't be loaded</returns>
    public TvdbUser LoadUserInfoFromCache(string userId)
    {
      String seriesRoot = _rootFolder;
      String xmlFile = seriesRoot + Path.DirectorySeparatorChar + "user_" + userId + ".xml";
      if (!File.Exists(xmlFile)) return null;

      _userLock.EnterReadLock();
      try
      {
        String content = File.ReadAllText(xmlFile);
        List<TvdbUser> userList = _xmlReader.ExtractUser(content);
        return userList != null && userList.Count == 1 ? userList[0] : null;
      }
      finally
      {
        _userLock.ExitReadLock();
      }
    }

    /// <summary>
    /// Saves the user data to cache
    /// </summary>
    /// <param name="user">TvdbUser object</param>
    public void SaveToCache(TvdbUser user)
    {
      if (user == null)
        return;
      if (!Directory.Exists(_rootFolder)) Directory.CreateDirectory(_rootFolder);

      _userLock.EnterWriteLock();
      try
      {
        _xmlWriter.WriteUserData(user, _rootFolder + Path.DirectorySeparatorChar + "user_" + user.UserIdentifier + ".xml");
      }
      finally
      {
        _userLock.ExitWriteLock();
      }
    }


    /// <summary>
    /// Receives a list of all series that have been cached
    /// </summary>
    /// <returns>A list of series that have been already stored with this cache provider</returns>
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
            retList.Add(seriesId);
          else
            Log.Error("Couldn't parse " + d + " when loading list of cached series");
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
      bool actorsLoaded = false;
      bool bannersLoaded = false;

      String seriesRoot = _rootFolder + Path.DirectorySeparatorChar + seriesId;
      if (!Directory.Exists(seriesRoot))
        return false;

      bool episodesLoaded;
      if (File.Exists(seriesRoot + Path.DirectorySeparatorChar + lang.Abbriviation + ".xml"))
        episodesLoaded = false;
      else if (File.Exists(seriesRoot + Path.DirectorySeparatorChar + lang.Abbriviation + "_full.xml"))
        episodesLoaded = true;
      else
        return false;

      String bannerFile = seriesRoot + Path.DirectorySeparatorChar + "banners.xml";
      String actorFile = seriesRoot + Path.DirectorySeparatorChar + "actors.xml";

      //load cached banners
      if (File.Exists(bannerFile))
        //banners have been already loaded
        bannersLoaded = true;

      //load actor info
      if (File.Exists(actorFile))
        actorsLoaded = true;

      return episodesLoaded || !checkEpisodesLoaded &&
             bannersLoaded || !checkBannersLoaded &&
             actorsLoaded || !checkActorsLoaded;
    }

    /// <summary>
    /// Completely refreshes the cache (all stored information is lost)
    /// </summary>
    /// <returns>true if the cache was cleared successfully, 
    ///          false otherwise (e.g. no write rights,...)</returns>
    public bool ClearCache()
    {
      //Delete all series info
      Log.Info("Attempting to delete all series");
      _seriesLock.EnterWriteLock();
      try
      {
        string[] folders = Directory.GetDirectories(_rootFolder);
        foreach (String f in folders)
        {
          try
          {
            Directory.Delete(f, true);
          }
          catch (Exception ex)
          {
            Log.Warn("Error deleting series " + f + ", please manually delete the " +
                     "cache folder since it's now inconsistent", ex);
            return false;
          }
        }
      }
      finally
      {
        _seriesLock.ExitWriteLock();
      }

      _languageLock.EnterWriteLock();
      try
      {
        if (File.Exists(_rootFolder + Path.DirectorySeparatorChar + "languages.xml"))
        {
          Log.Info("Attempting to delete cached languages");
          try
          {
            File.Delete(_rootFolder + Path.DirectorySeparatorChar + "languages.xml");
          }
          catch (Exception ex)
          {
            Log.Warn("Error deleting cached languages, please manually delete the " +
                     "cache folder since it's now inconsistent", ex);
            return false;
          }
        }
      }
      finally
      {
        _languageLock.ExitWriteLock();
      }

      _dataLock.EnterWriteLock();
      try
      {
        if (File.Exists(_rootFolder + Path.DirectorySeparatorChar + "data.xml"))
        {
          Log.Info("Attempting to delete cache settings");
          try
          {
            File.Delete(_rootFolder + Path.DirectorySeparatorChar + "data.xml");
          }
          catch (Exception ex)
          {
            Log.Warn("Error deleting cached cache settings, please manually delete the " +
                     "cache folder since it's now inconsistent", ex);
            return false;
          }
        }
      }
      finally
      {
        _dataLock.ExitWriteLock();
      }

      Log.Info("Successfully deleted cache");
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
      String seriesRoot = _rootFolder + Path.DirectorySeparatorChar + seriesId;
      _seriesLock.EnterWriteLock();
      try
      {
        if (Directory.Exists(seriesRoot))
        {
          try
          {
            Directory.Delete(seriesRoot, true);
            return true;
          }
          catch (Exception ex)
          {
            Log.Error("Couldn't delete series " + seriesId + " from cache ", ex);
            return false;
          }
        }
      }
      finally
      {
        _seriesLock.ExitWriteLock();
      }
      //the series wasn't cached in the first place
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
      _imageLock.EnterWriteLock();
      try
      {
        if (Directory.Exists(imageRoot))
        {
          if (image != null)
            image.Save(imageRoot + Path.DirectorySeparatorChar + fileName);
        }
        else
        {
          Log.Warn("Couldn't save image " + fileName + " for series " + seriesId + " because the series directory doesn't exist yet");
        }
      }
      finally
      {
        _imageLock.ExitWriteLock();
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
      _imageLock.EnterReadLock();
      try
      {
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
      }
      finally
      {
        _imageLock.ExitReadLock();
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

      _imageLock.EnterWriteLock();
      try
      {
        if (File.Exists(fName))
        {
          //the image is cached
          try
          {
            //trying to delete the file
            File.Delete(fName);
            return true;
          }
          catch (Exception ex)
          {
            //error while deleting the image
            Log.Warn("Couldn't delete image " + fileName + " for series " + seriesId, ex);
            return false;
          }
        }
      }
      finally
      {
        _imageLock.ExitWriteLock();
      }
      //image isn't cached in the first place
      return false;
    }

    #endregion
  }
}
