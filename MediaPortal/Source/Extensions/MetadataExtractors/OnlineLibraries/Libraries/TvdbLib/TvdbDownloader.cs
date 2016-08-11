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
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Exceptions;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Xml;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib
{
  /// <summary>
  /// TvdbDownloader allows simple downloading of all informations stored
  /// on http://thetvdb.com. Unlike the class Tvdb TvdbDownloader doesn't
  /// include any logic like caching.
  /// </summary>
  public class TvdbDownloader
  {
    #region private properties
    private readonly String _apiKey;
    private readonly TvdbXmlReader _xmlHandler;
    #endregion

    /// <summary>
    /// TvdbDownloader constructor
    /// </summary>
    /// <param name="apiKey">The api key used for downloading data from thetvdb -> see http://thetvdb.com/wiki/index.php/Programmers_API</param>
    public TvdbDownloader(String apiKey, bool useHttps)
    {
      _apiKey = apiKey;
      _xmlHandler = new TvdbXmlReader();//xml handler (extract xml information into objects)
      if (useHttps)
        TvdbLinkCreator.BASE_SERVER = TvdbLinkCreator.SECURE_SERVER;
      else
        TvdbLinkCreator.BASE_SERVER = TvdbLinkCreator.NORMAL_SERVER;
    }

    protected string DownloadString(string url)
    {
      using (WebClient webClient = new CompressionWebClient { Encoding = Encoding.UTF8 })
        return webClient.DownloadString(url);
    }

    protected byte[] DownloadData(string url)
    {
      using (WebClient webClient = new CompressionWebClient { Encoding = Encoding.UTF8 })
        return webClient.DownloadData(url);
    }

    /// <summary>
    /// Download the episodes for the given series
    /// </summary>
    /// <param name="seriesId">the id of the series</param>
    /// <param name="language">the language in which the episodes should be downloaded</param>
    /// <returns>An episode object or null if no episodes could be found</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public List<TvdbEpisode> DownloadEpisodes(int seriesId, TvdbLanguage language)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateSeriesEpisodesLink(_apiKey, seriesId, language);
        xml = DownloadString(link);
        List<TvdbEpisode> epList = _xmlHandler.ExtractEpisodes(xml);
        return epList;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleWebException("retrieve episodes for series " + seriesId, ex);
      }
    }

    private static Exception HandleUserWebException(string action, WebException ex)
    {
      Log.Warn("Request not successful for {0}", ex, new object[] { action });
      if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        throw new TvdbUserNotFoundException("Couldn't connect to Thetvdb.com to " + action +", are you sure this is the correct user id?");
      throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to " + action +
                                          ", check your internet connection and the status of http://thetvdb.com");
    }

    private static Exception HandleContentWebException(string message, WebException ex)
    {
      Log.Warn("Request not successful", ex);
      if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        throw new TvdbContentNotFoundException(message);
      throw new TvdbNotAvailableException(message);
    }

    private static Exception HandleWebException(string action, WebException ex)
    {
      Log.Warn("Request not successful for {0}", ex, new object[] { action });
      if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to " + action +
                                             ", you may use an invalid api key or the series doesn't exists");
      throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to " + action +
                                          ", check your internet connection and the status of http://thetvdb.com");
    }

    /// <summary>
    /// <para>Download all available banners (only a list of available banners, not the actual images!)for the specified series.</para>
    /// <para>You can load the actual images by calling LoadBanner() (or LoadThumb(), LoadVignette()) on the banner object</para>
    /// </summary>
    /// <param name="seriesId">Id of series</param>
    /// <returns>List of all banners for the given series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public List<TvdbBanner> DownloadBanners(int seriesId)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateSeriesBannersLink(_apiKey, seriesId);
        xml = DownloadString(link);
        List<TvdbBanner> banners = _xmlHandler.ExtractBanners(xml);
        return banners;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleWebException("retrieve banners for series " + seriesId, ex);
      }
    }

    /// <summary>
    /// <para>Download series from tvdb (specified by series id and language)</para>
    /// </summary>
    /// <param name="seriesId">id of series</param>
    /// <param name="language">language of series</param>
    /// <param name="loadEpisodes">load episodes</param>
    /// <param name="loadActors">load actors</param>
    /// <param name="loadBanners">load banners</param>
    /// <returns>The series object or null if the series couldn't be found</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public TvdbSeries DownloadSeries(int seriesId, TvdbLanguage language, bool loadEpisodes, bool loadActors, bool loadBanners)
    {
      //download the xml data from this request
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateSeriesLink(_apiKey, seriesId, language, loadEpisodes, false);
        xml = DownloadString(link);

        //extract all series the xml file contains
        List<TvdbSeries> seriesList = _xmlHandler.ExtractSeries(xml);

        //if a request is made on a series id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (seriesList != null && seriesList.Count == 1)
        {
          TvdbSeries series = seriesList[0];
          if (loadEpisodes)
          {
            //add episode info to series
            List<TvdbEpisode> epList = _xmlHandler.ExtractEpisodes(xml);
            if (epList != null)
            {
              foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in series.SeriesTranslations.Where(kvp => kvp.Key.Abbriviation.Equals(language.Abbriviation)))
              {
                series.SeriesTranslations[kvp.Key].Episodes.Clear();
                series.SeriesTranslations[kvp.Key].Episodes.AddRange(epList);
                series.SeriesTranslations[kvp.Key].EpisodesLoaded = true;
                series.SetLanguage(language);
                break;
              }
            }
          }

          //also load actors
          if (loadActors)
          {
            List<TvdbActor> actors = DownloadActors(seriesId);
            if (actors != null)
            {
              series.TvdbActorsLoaded = true;
              series.TvdbActors = actors;
            }
          }

          //also load banner paths
          if (loadBanners)
          {
            List<TvdbBanner> banners = DownloadBanners(seriesId);
            if (banners != null)
            {
              series.Banners = banners;
              series.BannersLoaded = true;
            }
          }
          return series;
        }
        Log.Warn("More than one series returned when trying to retrieve series " + seriesId);
        return null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleWebException("retrieve series details for " + seriesId, ex);
      }
    }

    /// <summary>
    /// Download the series in the given language
    /// </summary>
    /// <param name="seriesId">id of series</param>
    /// <param name="language">language of series</param>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    /// <returns>the series object</returns>
    public TvdbSeries DownloadSeriesZipped(int seriesId, TvdbLanguage language)
    {
      //download the xml data from this request
      byte[] xml = null;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateSeriesLinkZipped(_apiKey, seriesId, language);
        xml = DownloadData(link);

        ZipInputStream zip = new ZipInputStream(new MemoryStream(xml));

        ZipEntry entry = zip.GetNextEntry();
        String seriesString = null;
        String actorsString = null;
        String bannersString = null;
        while (entry != null)
        {
          Log.Debug("Extracting " + entry.Name);
          byte[] buffer = new byte[zip.Length];
          int count = zip.Read(buffer, 0, (int)zip.Length);
          if (entry.Name.Equals(language.Abbriviation + ".xml"))
            seriesString = Encoding.UTF8.GetString(buffer);
          else if (entry.Name.Equals("banners.xml"))
            bannersString = Encoding.UTF8.GetString(buffer);
          else if (entry.Name.Equals("actors.xml"))
            actorsString = Encoding.UTF8.GetString(buffer);
          entry = zip.GetNextEntry();
        }
        zip.Close();

        //extract all series the xml file contains
        List<TvdbSeries> seriesList = _xmlHandler.ExtractSeries(seriesString);

        //if a request is made on a series id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (seriesList != null && seriesList.Count == 1)
        {
          TvdbSeries series = seriesList[0];
          //add episode info to series
          List<TvdbEpisode> epList = _xmlHandler.ExtractEpisodes(seriesString);
          if (epList != null)
          {
            foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in series.SeriesTranslations.Where(kvp => kvp.Key.Abbriviation.Equals(language.Abbriviation)))
            {
              series.SeriesTranslations[kvp.Key].Episodes.Clear();
              series.SeriesTranslations[kvp.Key].Episodes.AddRange(epList);
              series.SeriesTranslations[kvp.Key].EpisodesLoaded = true;
              series.SetLanguage(language);
              break;
            }
          }

          //also load actors
          List<TvdbActor> actors = _xmlHandler.ExtractActors(actorsString);
          if (actors != null)
          {
            series.TvdbActorsLoaded = true;
            series.TvdbActors = actors;
          }

          //also load banner paths
          List<TvdbBanner> banners = _xmlHandler.ExtractBanners(bannersString);
          if (banners != null)
          {
            series.BannersLoaded = true;
            series.Banners = banners;
          }

          return series;
        }
        Log.Warn("More than one series returned when trying to retrieve series " + seriesId);
        return null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + Encoding.Unicode.GetString(xml), ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleWebException("retrieve zipped series details for " + seriesId, ex);
      }
    }

    /// <summary>
    /// Download a series search for the id of an external site
    /// </summary>
    /// <param name="site">The site that provides the external id</param>
    /// <param name="id">The id that identifies the series on the external site</param>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    /// <returns>the series object that corresponds to the given site and id</returns>
    public TvdbSearchResult DownloadSeriesSearchByExternalId(ExternalId site, String id)
    {
      //download the xml data from this request
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateGetSeriesByIdLink(_apiKey, site, id);
        xml = DownloadString(link);

        //extract all series the xml file contains
        List<TvdbSearchResult> seriesList = _xmlHandler.ExtractSeriesSearchResults(xml);

        //if a request is made on a series id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (seriesList != null && seriesList.Count == 1)
        {
          TvdbSearchResult series = seriesList[0];
          return series;
        }
        Log.Warn("More than one series returned when trying to retrieve series by id " + id);
        return null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleWebException("retrieve " + id, ex);
      }
    }



    internal TvdbSeriesFields DownloadSeriesFields(int seriesId, TvdbLanguage language)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateSeriesLink(_apiKey, seriesId, language, false, false);
        xml = DownloadString(link);

        //extract all series the xml file contains
        List<TvdbSeriesFields> seriesList = _xmlHandler.ExtractSeriesFields(xml);

        return seriesList != null && seriesList.Count == 1 ? seriesList[0] : null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleWebException("retrieve fields for series " + seriesId, ex);
      }
    }

    /// <summary>
    /// Download the given episode from tvdb
    /// </summary>
    /// <param name="episodeId">Id of episode</param>
    /// <param name="language">Language in which the episode should be downloaded</param>
    /// <returns>The episode object</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public TvdbEpisode DownloadEpisode(int episodeId, TvdbLanguage language)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateEpisodeLink(_apiKey, episodeId, language, false);
        xml = DownloadString(link);
        List<TvdbEpisode> epList = _xmlHandler.ExtractEpisodes(xml);
        return epList != null && epList.Count == 1 ? epList[0] : null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleContentWebException("Couldn't download episode " + episodeId + "(" + language + "), maybe the episode doesn't exist", ex);
      }
    }

    /// <summary>
    /// <para>Download the episode (specified by series id, Season number, episode number, language and episode order) from http://thetvdb.com.</para>
    /// <para>It is possible to retrieve episodes by aired order (aka default order), DVD order and absolute order. For a detailled description of these
    /// options see: http://thetvdb.com/wiki/index.php/Category:Episodes</para>
    /// </summary>
    /// <param name="seriesId">series id</param>
    /// <param name="seasonNr">Season nr</param>
    /// <param name="episodeNr">episode nr</param>
    /// <param name="language">language</param>
    /// <param name="order">order</param>
    /// <returns>The episode object or null if the episode could't be found</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public TvdbEpisode DownloadEpisode(int seriesId, int seasonNr, int episodeNr, TvdbEpisode.EpisodeOrdering order, TvdbLanguage language)
    {
      String xml = "";
      String link = "";
      String orderString = null;
      switch (order)
      {
        case TvdbEpisode.EpisodeOrdering.AbsoluteOrder:
          orderString = "absolute";
          break;
        case TvdbEpisode.EpisodeOrdering.DefaultOrder:
          orderString = "default";
          break;
        case TvdbEpisode.EpisodeOrdering.DvdOrder:
          orderString = "dvd";
          break;
      }

      try
      {
        link = TvdbLinkCreator.CreateEpisodeLink(_apiKey, seriesId, seasonNr, episodeNr, orderString, language);
        xml = DownloadString(link);
        List<TvdbEpisode> epList = _xmlHandler.ExtractEpisodes(xml);
        return epList != null && epList.Count == 1 ? epList[0] : null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleContentWebException(
          string.Format("Couldn't download episode {0}/{1}/{2}/{3}/{4}, maybe the episode or the ordering doesn't exist", seriesId, order, seasonNr, episodeNr, language.Abbriviation),
          ex);
      }
    }


    /// <summary>
    /// Download the episode specified from http://thetvdb.com
    /// </summary>
    /// <param name="seriesId">series id</param>
    /// <param name="airDate">when did the episode air</param>
    /// <param name="language">language</param>
    /// <returns>Episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public TvdbEpisode DownloadEpisode(int seriesId, DateTime airDate, TvdbLanguage language)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateEpisodeLink(_apiKey, seriesId, airDate, language);
        xml = DownloadString(link);
        if (!xml.Contains("No Results from SP"))
        {
          List<TvdbEpisode> epList = _xmlHandler.ExtractEpisodes(xml);
          if (epList != null && epList.Count == 1)
          {
            epList[0].Banner.SeriesId = seriesId;
            return epList[0];
          }
        }
        return null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleContentWebException(
          string.Format("Couldn't download episode  for series {0} from {1}({2}), maybe the episode doesn't exist", seriesId, airDate.ToShortDateString(), language.Abbriviation), 
          ex);
      }
    }

    /// <summary>
    /// Download the preferred language of the user.
    /// </summary>
    /// <param name="userId">Id of user</param>
    /// <returns>The preferred language for this user as set on http://thetvdb.com</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public TvdbLanguage DownloadUserPreferredLanguage(String userId)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateUserLanguageLink(userId);
        xml = DownloadString(link);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleUserWebException("retrieve preferred language for user " + userId, ex);
      }
      List<TvdbLanguage> langList = _xmlHandler.ExtractLanguages(xml);
      return langList != null && langList.Count == 1 ? langList[0] : null;
    }

    /// <summary>
    /// Download the user favorite list
    /// </summary>
    /// <param name="userId">Id of user (register at http://thetvdb.com to get a user id)</param>
    /// <returns>Favorite list for specified user</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public List<int> DownloadUserFavoriteList(String userId)
    {
      return DownloadUserFavoriteList(userId, Util.UserFavouriteAction.None, 0);
    }

    /// <summary>
    /// Download the user favorite list
    /// </summary>
    /// <param name="userId">Id of user</param>
    /// <param name="type">Type of message</param>
    /// <param name="seriesId">id of series</param>
    /// <returns>List of user favorites</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    internal List<int> DownloadUserFavoriteList(String userId, Util.UserFavouriteAction type, int seriesId)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateUserFavouriteLink(userId, type, seriesId);
        xml = DownloadString(link);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleUserWebException("retrieve favorite list for user "+ userId, ex);
      }
      List<int> favList = _xmlHandler.ExtractSeriesFavorites(xml);
      return favList;
    }

    /// <summary>
    /// Download an Update
    /// </summary>
    /// <param name="updateSeries">updated series to return</param>
    /// <param name="updateEpisodes">updated episodes to return</param>
    /// <param name="updateBanners">updated banners to return</param>
    /// <param name="interval">interval to download (0=day, 1=week, 2=month)</param>
    /// <param name="zipped">use zip</param>
    /// <returns>Time of the update</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public DateTime DownloadUpdate(out List<TvdbSeries> updateSeries, out List<TvdbEpisode> updateEpisodes,
                                   out List<TvdbBanner> updateBanners, int interval,
                                    bool zipped)
    {
      return DownloadUpdate(out updateSeries, out updateEpisodes, out updateBanners, (Interval)interval, zipped);
    }

    /// <summary>
    /// Download an Update
    /// </summary>
    /// <param name="updateSeries">updated series to return</param>
    /// <param name="updateEpisodes">updated episodes to return</param>
    /// <param name="updateBanners">updated banners to return</param>
    /// <param name="interval">interval to download</param>
    /// <param name="zipped">use zip</param>
    /// <returns>Time of the update</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public DateTime DownloadUpdate(out List<TvdbSeries> updateSeries, out List<TvdbEpisode> updateEpisodes,
                                     out List<TvdbBanner> updateBanners, Interval interval, bool zipped)
    {

      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateUpdateLink(_apiKey, interval, zipped);
        if (zipped)
        {
          byte[] data = DownloadData(link);
          ZipInputStream zip = new ZipInputStream(new MemoryStream(data));
          zip.GetNextEntry();
          byte[] buffer = new byte[zip.Length];
          int count = zip.Read(buffer, 0, (int)zip.Length);
          xml = Encoding.UTF8.GetString(buffer);
        }
        else
        {
          xml = DownloadString(link);
        }

        updateEpisodes = _xmlHandler.ExtractEpisodeUpdates(xml);
        updateSeries = _xmlHandler.ExtractSeriesUpdates(xml);
        updateBanners = _xmlHandler.ExtractBannerUpdates(xml);

        return _xmlHandler.ExtractUpdateTime(xml);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (ZipException ex)
      {
        Log.Error("Error unzipping the xml file " + link, ex);
        throw new TvdbInvalidXmlException("Error unzipping the xml file " + link);
      }
      catch (WebException ex)
      {
        throw HandleWebException("retrieve updates for interval " + interval, ex);
      }
    }

    /// <summary>
    /// Download list available languages.
    /// </summary>
    /// <returns>A list of TvdbLanguage objects</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public List<TvdbLanguage> DownloadLanguages()
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateLanguageLink(_apiKey);
        xml = DownloadString(link);
        return _xmlHandler.ExtractLanguages(xml);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleWebException("retrieve the list of available languages", ex);
      }
    }

    /// <summary>
    /// Download search results for a series search in the default language (english)
    /// </summary>
    /// <param name="name">name of the series</param>
    /// <returns>List of possible matches for the search</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public List<TvdbSearchResult> DownloadSearchResults(String name)
    {
      return DownloadSearchResults(name, TvdbLanguage.DefaultLanguage);
    }

    /// <summary>
    /// Download search results for a series search
    /// </summary>
    /// <param name="name">name of the series</param>
    /// <param name="language">language of the search</param>
    /// <returns>List of possible matches for the search</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public List<TvdbSearchResult> DownloadSearchResults(String name, TvdbLanguage language)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateSearchLink(name, language);
        xml = DownloadString(link);
        return _xmlHandler.ExtractSeriesSearchResults(xml);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleWebException("retrieve search results for " + name, ex);
      }
    }

    /// <summary>
    /// Make the request for rating a series
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="seriesId">The id of the series</param>
    /// <param name="rating">The rating for this series</param>
    /// <returns>A double value with the current rating for this series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public double RateSeries(String userId, int seriesId, int rating)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateUserSeriesRating(userId, seriesId, rating);
        xml = DownloadString(link);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleUserWebException("rate series " + seriesId, ex);
      }
      return _xmlHandler.ExtractRating(xml);
    }

    /// <summary>
    /// Make the request for rating an episode
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="episodeId">The id of the episode</param>
    /// <param name="rating">The rating for this series</param>
    /// <returns>A double value with the current rating for this series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public double RateEpisode(String userId, int episodeId, int rating)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateUserEpisodeRating(userId, episodeId, rating);
        xml = DownloadString(link);
        return _xmlHandler.ExtractRating(xml);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleUserWebException("rate episode " + episodeId, ex);
      }
    }

    /// <summary>
    /// Download the series rating without rating the item.
    /// </summary>
    /// <param name="userId">id of user</param>
    /// <param name="seriesId">id of series</param>
    /// <returns>Current rating for the series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public double DownloadSeriesRating(String userId, int seriesId)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateUserSeriesRating(userId, seriesId);
        xml = DownloadString(link);
        return _xmlHandler.ExtractRating(xml);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleUserWebException("download rating for series " + seriesId, ex);
      }
    }

    /// <summary>
    /// Download the episode rating without rating
    /// </summary>
    /// <param name="userId">id of the user</param>
    /// <param name="episodeId">id of the episode</param>
    /// <returns>Current rating of this episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public double DownloadEpisodeRating(String userId, int episodeId)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateUserEpisodeRating(userId, episodeId);
        xml = DownloadString(link);
        return _xmlHandler.ExtractRating(xml);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleUserWebException("retrieve rating of series " + episodeId, ex);
      }
    }

    /// <summary>
    /// Download the list of actors
    /// </summary>
    /// <param name="seriesId">Id of series</param>
    /// <returns>List of actors for the given series</returns>
    public List<TvdbActor> DownloadActors(int seriesId)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateActorLink(seriesId, _apiKey);
        xml = DownloadString(link);
        //add series id to actors
        List<TvdbActor> actors = _xmlHandler.ExtractActors(xml);

        return actors;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleWebException("retrieve actor info of series " + seriesId, ex);
      }
    }

    /// <summary>
    /// Gets all series this user has already ratet
    /// </summary>
    /// <returns>All series ratings the user has made so far</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public Dictionary<int, TvdbRating> DownloadAllSeriesRatings(String userId)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateAllSeriesRatingsLink(_apiKey, userId);
        xml = DownloadString(link);
        return _xmlHandler.ExtractRatings(xml, TvdbRating.ItemType.Series);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleWebException("retrieve rating of all series", ex);
      }
    }

    /// <summary>
    /// Download the user rating for the given series (episodes and series itself)
    /// </summary>
    /// <param name="userId">Id of user</param>
    /// <param name="seriesId">Id of series</param>
    /// <returns>Dictionary of all ratings</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public Dictionary<int, TvdbRating> DownloadRatingsForSeries(string userId, int seriesId)
    {
      String xml = string.Empty;
      String link = string.Empty;
      try
      {
        link = TvdbLinkCreator.CreateSeriesRatingsLink(_apiKey, userId, seriesId);
        xml = DownloadString(link);
        Dictionary<int, TvdbRating> retList = _xmlHandler.ExtractRatings(xml, TvdbRating.ItemType.Series);
        Dictionary<int, TvdbRating> episodeList = _xmlHandler.ExtractRatings(xml, TvdbRating.ItemType.Episode);
        if (retList != null && episodeList != null && retList.Count > 0)
        {
          foreach (KeyValuePair<int, TvdbRating> r in episodeList)
            if (!retList.ContainsKey(r.Key))
              retList.Add(r.Key, r.Value);
          return retList;
        }
        return null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        throw HandleUserWebException("retrieve rating of all series", ex);
      }
    }
  }
}
