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
using System.Text;
using TvdbLib.Data;
using TvdbLib.Xml;
using TvdbLib.Data.Banner;
using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace TvdbLib.Cache
{
  /// <summary>
  /// XmlCacheProvider stores all the information that have been retrieved from http://thetvdb.com as human-readable xml files on the hard disk
  /// </summary>
  public class XmlCacheProvider : ICacheProvider
  {
    #region private fields
    TvdbXmlWriter m_xmlWriter;
    TvdbXmlReader m_xmlReader;
    String m_rootFolder;
    private bool m_initialised = false;
    #endregion

    /// <summary>
    /// Constructor for XmlCacheProvider
    /// </summary>
    /// <param name="_rootFolder">This is the folder on the disk where all the information are stored</param>
    public XmlCacheProvider(String _rootFolder)
    {
      m_xmlWriter = new TvdbXmlWriter();
      m_xmlReader = new TvdbXmlReader();
      m_rootFolder = _rootFolder;
    }

    /// <summary>
    /// Properly describe the CacheProvider for neat-reasons
    /// </summary>
    /// <returns>String describing the cache provider</returns>
    public override string ToString()
    {
      return "XmlCacheProvider (" + m_rootFolder + ")";
    }

    #region ICacheProvider Members

    /// <summary>
    /// Is the cache provider initialised
    /// </summary>
    public bool Initialised
    {
      get { return m_initialised; }
    }

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
        if (!Directory.Exists(m_rootFolder))
        {
          Directory.CreateDirectory(m_rootFolder);
        }

        TvdbData data = LoadUserDataFromCache();
        if (data == null)
        {//the cache has never been initialised before -> do it now
          data = new TvdbData();
          data.LanguageList = new List<TvdbLanguage>();
          data.LastUpdated = DateTime.Now;

          SaveToCache(data);
        }
        m_initialised = true;
        return data;
      }
      catch (Exception ex)
      {
        Log.Error("Couldn't initialise cache: " + ex.ToString());
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
    /// <param name="_content">settings</param>
    public void SaveToCache(TvdbData _content)
    {
      if (_content != null)
      {
        SaveToCache(_content.LanguageList);

        //store additional information
        //- time of last update
        //- more to come (eventually)
        XElement xml = new XElement("Data");
        xml.Add(new XElement("LastUpdated", Util.DotNetToUnix(_content.LastUpdated)));
        String data = xml.ToString();
        File.WriteAllText(m_rootFolder + Path.DirectorySeparatorChar + "data.xml", data);
      }
    }

    /// <summary>
    /// Save the language to cache
    /// </summary>
    /// <param name="_languageList">List of languages that are available on http://thetvdb.com</param>
    public void SaveToCache(List<TvdbLanguage> _languageList)
    {
      if (_languageList != null && _languageList.Count > 0)
      {
        if (!Directory.Exists(m_rootFolder)) Directory.CreateDirectory(m_rootFolder);
        m_xmlWriter.WriteLanguageFile(_languageList, m_rootFolder + Path.DirectorySeparatorChar + "languages.xml");
      }
    }

    /// <summary>
    /// Save the mirror info to cache
    /// </summary>
    /// <param name="_mirrorInfo">Mirrors</param>
    [Obsolete("Not used any more, however if won't delete the class since it could be useful at some point")]
    public void SaveToCache(List<TvdbMirror> _mirrorInfo)
    {
      if (_mirrorInfo != null && _mirrorInfo.Count > 0)
      {
        if (!Directory.Exists(m_rootFolder)) Directory.CreateDirectory(m_rootFolder);
        m_xmlWriter.WriteMirrorFile(_mirrorInfo, m_rootFolder + Path.DirectorySeparatorChar + "mirrors.xml");
      }
    }


    /// <summary>
    /// Saves the series to cache
    /// </summary>
    /// <param name="_series">The series to save</param>
    public void SaveToCache(TvdbSeries _series)
    {
      String root = m_rootFolder + Path.DirectorySeparatorChar + _series.Id;
      if (!Directory.Exists(root)) Directory.CreateDirectory(root);
      try
      {//delete old cached content
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

      TvdbLanguage currentLanguage = _series.Language;
      foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in _series.SeriesTranslations)
      {//write all languages to file
        String fName = root + Path.DirectorySeparatorChar + kvp.Key.Abbriviation +
                       (kvp.Value.EpisodesLoaded ? "_full" : "") + ".xml";
        m_xmlWriter.WriteSeriesContent(new TvdbSeries(kvp.Value), fName);
      }

      if (_series.BannersLoaded)
      {//write the banners file 
        m_xmlWriter.WriteSeriesBannerContent(_series.Banners, root + Path.DirectorySeparatorChar + "banners.xml");
      }

      if (_series.TvdbActorsLoaded)
      {//write the actors file
        m_xmlWriter.WriteActorFile(_series.TvdbActors, root + Path.DirectorySeparatorChar + "actors.xml");
      }
    }

    /// <summary>
    /// Loads the settings data from cache 
    /// </summary>
    /// <returns>The loaded TvdbData object</returns>
    public TvdbData LoadUserDataFromCache()
    {
      String fName = m_rootFolder + Path.DirectorySeparatorChar + "data.xml";
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

      return null;

    }

    /// <summary>
    /// Loads the available languages from cache
    /// </summary>
    /// <returns>List of available languages</returns>
    public List<TvdbLanguage> LoadLanguageListFromCache()
    {

      String file = m_rootFolder + Path.DirectorySeparatorChar + "languages.xml";
      if (File.Exists(file))
      {
        return m_xmlReader.ExtractLanguages(File.ReadAllText(file));
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Load the available mirrors from cache
    /// </summary>
    /// <returns>List of mirrors</returns>
    [Obsolete("Not used any more, however if won't delete the class since it could be useful at some point")]
    public List<TvdbMirror> LoadMirrorListFromCache()
    {
      String file = m_rootFolder + Path.DirectorySeparatorChar + "mirrors.xml";
      if (File.Exists(file))
      {
        return m_xmlReader.ExtractMirrors(File.ReadAllText(file));
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Loads all series from cache
    /// </summary>
    /// <returns>A list of TvdbSeries objects from cache or null</returns>
    public List<TvdbSeries> LoadAllSeriesFromCache()
    {
      List<TvdbSeries> retList = new List<TvdbSeries>();
      string[] dirs = Directory.GetDirectories(m_rootFolder);
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



    /// <summary>
    /// Load the give series from cache
    /// </summary>
    /// <param name="_seriesId">Id of the series to load</param>
    /// <returns>Series that has been loaded or null if series doesn't exist</returns>
    public TvdbSeries LoadSeriesFromCache(int _seriesId)
    {
      String seriesRoot = m_rootFolder + Path.DirectorySeparatorChar + _seriesId;
      if (!Directory.Exists(seriesRoot)) return null;
      TvdbSeries series = new TvdbSeries();

      #region load series in all available languages
      String[] seriesLanguages = Directory.GetFiles(seriesRoot, "*.xml");
      foreach (String l in seriesLanguages)
      {
        if (!l.EndsWith("actors.xml") && !l.EndsWith("banners.xml"))
        {
          String content = File.ReadAllText(l);
          List<TvdbSeriesFields> seriesList = m_xmlReader.ExtractSeriesFields(content);
          if (seriesList != null && seriesList.Count == 1)
          {
            TvdbSeriesFields s = seriesList[0];
            //Load episodes
            if (l.EndsWith("full.xml"))
            {
              List<TvdbEpisode> epList = m_xmlReader.ExtractEpisodes(content);
              s.EpisodesLoaded = true;
              s.Episodes.Clear();
              s.Episodes.AddRange(epList);
            }
            series.AddLanguage(s);
          }
        }
      }



      if (series.SeriesTranslations.Count > 0)
      {//change language of the series to the default language
        series.SetLanguage(series.SeriesTranslations.Keys.First());
      }
      else
      {//no series info could be loaded
        return null;
      }

      if (!series.BannerPath.Equals(""))
      {
        series.Banners.Add(new TvdbSeriesBanner(series.Id, series.BannerPath, series.Language, TvdbSeriesBanner.Type.graphical));
      }

      if (!series.PosterPath.Equals(""))
      {
        series.Banners.Add(new TvdbPosterBanner(series.Id, series.PosterPath, series.Language));
      }

      if (!series.FanartPath.Equals(""))
      {
        series.Banners.Add(new TvdbFanartBanner(series.Id, series.FanartPath, series.Language));
      }

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
            foreach (TvdbEpisode e in series.Episodes)
            {
              if (e.SeasonNumber == season && e.EpisodeNumber == episode)
              {
                if (epImageFile.Contains("thumb"))
                {
                  e.Banner.LoadThumb(Image.FromFile(epImageFile));
                }
                else
                {
                  e.Banner.LoadBanner(Image.FromFile(epImageFile));
                }
                break;
              }
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
      {//banners have been already loaded
        List<TvdbBanner> bannerList = m_xmlReader.ExtractBanners(File.ReadAllText(bannerFile));

        String[] banners = Directory.GetFiles(seriesRoot, "banner*.jpg");
        foreach (String b in banners)
        {
          try
          {
            int bannerId = Int32.Parse(b.Remove(b.IndexOf(".")).Remove(0, b.LastIndexOf("_") + 1));
            foreach (TvdbBanner banner in bannerList)
            {
              if (banner.Id == bannerId)
              {
                if (b.Contains("thumb") && banner.GetType().BaseType == typeof(TvdbBannerWithThumb))
                {
                  ((TvdbBannerWithThumb)banner).LoadThumb(Image.FromFile(b));
                }
                else if (b.Contains("vignette") && banner.GetType() == typeof(TvdbFanartBanner))
                {
                  ((TvdbFanartBanner)banner).LoadVignette(Image.FromFile(b));
                }
                else
                {
                  banner.LoadBanner(Image.FromFile(b));
                }
              }
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
        List<TvdbActor> actorList = m_xmlReader.ExtractActors(File.ReadAllText(actorFile));

        String[] banners = Directory.GetFiles(seriesRoot, "actor_*.jpg");
        foreach (String b in banners)
        {
          try
          {
            int actorId = Int32.Parse(b.Remove(b.IndexOf(".")).Remove(0, b.LastIndexOf("_") + 1));
            foreach (TvdbActor actor in actorList)
            {
              if (actor.Id == actorId)
              {
                actor.ActorImage.LoadBanner(Image.FromFile(b));
              }
            }

          }
          catch (Exception)
          {
            Log.Warn("Couldn't load image file " + b);
          }
        }
        series.TvdbActors = actorList;
      }
      #endregion

      return series;

    }

    /// <summary>
    /// Load user info from cache
    /// </summary>
    /// <param name="_userId">Id of the user</param>
    /// <returns>TvdbUser object or null if the user couldn't be loaded</returns>
    public TvdbUser LoadUserInfoFromCache(string _userId)
    {
      String seriesRoot = m_rootFolder;
      String xmlFile = seriesRoot + Path.DirectorySeparatorChar + "user_" + _userId + ".xml";
      if (!File.Exists(xmlFile)) return null;
      String content = File.ReadAllText(xmlFile);
      List<TvdbUser> userList = m_xmlReader.ExtractUser(content);
      if (userList != null && userList.Count == 1)
      {
        return userList[0];
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Saves the user data to cache
    /// </summary>
    /// <param name="_user">TvdbUser object</param>
    public void SaveToCache(TvdbUser _user)
    {
      if (_user != null)
      {
        if (!Directory.Exists(m_rootFolder)) Directory.CreateDirectory(m_rootFolder);
        m_xmlWriter.WriteUserData(_user, m_rootFolder + Path.DirectorySeparatorChar + "user_" + _user.UserIdentifier + ".xml");
      }
    }


    /// <summary>
    /// Receives a list of all series that have been cached
    /// </summary>
    /// <returns>A list of series that have been already stored with this cache provider</returns>
    public List<int> GetCachedSeries()
    {
      List<int> retList = new List<int>();
      if (Directory.Exists(m_rootFolder))
      {
        string[] dirs = Directory.GetDirectories(m_rootFolder);
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
    /// <param name="_seriesId">Id of the series</param>
    /// <param name="_lang">Language of the series</param>
    /// <param name="_episodesLoaded">are episodes loaded</param>
    /// <param name="_bannersLoaded">are banners loaded</param>
    /// <param name="_actorsLoaded">are actors loaded</param>
    /// <returns>true if the series is cached, false otherwise</returns>
    public bool IsCached(int _seriesId, TvdbLanguage _lang, bool _episodesLoaded,
                         bool _bannersLoaded, bool _actorsLoaded)
    {
      bool actorsLoaded = false;
      bool episodesLoaded = false;
      bool bannersLoaded = false;

      String seriesRoot = m_rootFolder + Path.DirectorySeparatorChar + _seriesId;
      if (Directory.Exists(seriesRoot))
      {
        if (File.Exists(seriesRoot + Path.DirectorySeparatorChar + _lang.Abbriviation + ".xml"))
        {
          episodesLoaded = false;
        }
        else if (File.Exists(seriesRoot + Path.DirectorySeparatorChar + _lang.Abbriviation + "_full.xml"))
        {
          episodesLoaded = true;
        }
        else
        {
          return false;
        }

        String bannerFile = seriesRoot + Path.DirectorySeparatorChar + "banners.xml";
        String actorFile = seriesRoot + Path.DirectorySeparatorChar + "actors.xml";

        //load cached banners
        if (File.Exists(bannerFile))
        {//banners have been already loaded
          bannersLoaded = true;
        }

        //load actor info
        if (File.Exists(actorFile))
        {
          actorsLoaded = true;
        }

        if (episodesLoaded || !_episodesLoaded &&
            bannersLoaded || !_bannersLoaded &&
            actorsLoaded || !_actorsLoaded)
        {
          return true;
        }
        else
        {
          return false;
        }
      }
      else
      {
        return false;
      }
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
      string[] folders = Directory.GetDirectories(m_rootFolder);
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

      if (File.Exists(m_rootFolder + Path.DirectorySeparatorChar + "languages.xml"))
      {
        Log.Info("Attempting to delete cached languages");
        try
        {
          File.Delete(m_rootFolder + Path.DirectorySeparatorChar + "languages.xml");
        }
        catch (Exception ex)
        {
          Log.Warn("Error deleting cached languages, please manually delete the " +
                   "cache folder since it's now inconsistent", ex);
          return false;
        }
      }

      if (File.Exists(m_rootFolder + Path.DirectorySeparatorChar + "data.xml"))
      {
        Log.Info("Attempting to delete cache settings");
        try
        {
          File.Delete(m_rootFolder + Path.DirectorySeparatorChar + "data.xml");
        }
        catch (Exception ex)
        {
          Log.Warn("Error deleting cached cache settings, please manually delete the " +
                   "cache folder since it's now inconsistent", ex);
          return false;
        }
      }

      Log.Info("Successfully deleted cache");
      return true;
    }

    /// <summary>
    /// Remove a specific series from cache
    /// </summary>
    /// <param name="_seriesId">the id of the series</param>
    /// <returns>true if the series was removed from the cache successfully, 
    ///          false otherwise (e.g. series not cached)</returns>
    public bool RemoveFromCache(int _seriesId)
    {
      String seriesRoot = m_rootFolder + Path.DirectorySeparatorChar + _seriesId;
      if (Directory.Exists(seriesRoot))
      {
        try
        {
          Directory.Delete(seriesRoot, true);
          return true;
        }
        catch (Exception ex)
        {
          Log.Error("Couldn't delete series " + _seriesId + " from cache ", ex);
          return false;
        }
      }
      else
      {//the series wasn't cached in the first place
        return false;
      }
    }

    /// <summary>
    /// Save the given image to cache
    /// </summary>
    /// <param name="_image">banner to save</param>
    /// <param name="_seriesId">id of series</param>
    /// <param name="_fileName">filename (will be the same name used by LoadImageFromCache)</param>
    public void SaveToCache(Image _image, int _seriesId, string _fileName)
    {
      String seriesRoot = m_rootFolder + Path.DirectorySeparatorChar + _seriesId;
      if (Directory.Exists(seriesRoot))
      {
        if (_image != null)
        {
          _image.Save(seriesRoot + Path.DirectorySeparatorChar + _fileName);
        }
      }
      else
      {
        Log.Warn("Couldn't save image " + _fileName + " for series " + _seriesId +
                 " because the series directory doesn't exist yet");
      }
    }

    /// <summary>
    /// Loads the specified image from the cache
    /// </summary>
    /// <param name="_seriesId">series id</param>
    /// <param name="_fileName">filename of the image (same one as used by SaveToCache)</param>
    /// <returns>The loaded image or null if the image wasn't found</returns>
    public Image LoadImageFromCache(int _seriesId, string _fileName)
    {
      String seriesRoot = m_rootFolder + Path.DirectorySeparatorChar + _seriesId;
      if (Directory.Exists(seriesRoot))
      {
        String fName = seriesRoot + Path.DirectorySeparatorChar + _fileName;
        if (File.Exists(fName))
        {
          try
          {
            return Image.FromFile(fName);
          }
          catch (Exception ex)
          {
            Log.Warn("Couldn't load image " + fName + " for series " + _seriesId, ex);
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Removes the specified image from cache (if it has been cached)
    /// </summary>
    /// <param name="_seriesId">id of series</param>
    /// <param name="_fileName">name of image</param>
    /// <returns>true if image was removed successfully, false otherwise (e.g. image didn't exist)</returns>
    public bool RemoveImageFromCache(int _seriesId, string _fileName)
    {
      String fName = m_rootFolder + Path.DirectorySeparatorChar + _seriesId +
                     Path.DirectorySeparatorChar + _fileName;

      if (File.Exists(fName))
      {//the image is cached
        try
        {//trying to delete the file
          File.Delete(fName);
          return true;
        }
        catch (Exception ex)
        {//error while deleting the image
          Log.Warn("Couldn't delete image " + _fileName + " for series " + _seriesId, ex);
          return false;
        }
      }
      else
      {//image isn't cached in the first place
        return false;
      }
    }

    #endregion
  }
}
