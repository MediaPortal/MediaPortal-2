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

using TvdbLib.Exceptions;
using TvdbLib.ICSharpCode.SharpZipLib.Zip;
using TvdbLib.SharpZipLib.Zip;
using TvdbLib.Xml;
using TvdbLib.Data;

namespace TvdbLib
{
  /// <summary>
  /// TvdbDownloader allows simple downloading of all informations stored
  /// on http://thetvdb.com. Unlike the class Tvdb TvdbDownloader doesn't
  /// include any logic like caching.
  /// </summary>
  public class TvdbDownloader
  {
    #region private properties
    private String m_apiKey;
    private WebClient m_webClient;
    private TvdbXmlReader m_xmlHandler;
    #endregion

    /// <summary>
    /// TvdbDownloader constructor
    /// </summary>
    /// <param name="_apiKey">The api key used for downloading data from thetvdb -> see http://thetvdb.com/wiki/index.php/Programmers_API</param>
    public TvdbDownloader(String _apiKey)
    {
      m_apiKey = _apiKey;
      m_webClient = new WebClient();//initialise webclient for downloading xml files
      m_webClient.Encoding = Encoding.UTF8;
      m_xmlHandler = new TvdbXmlReader();//xml handler (extract xml information into objects)
    }

    /// <summary>
    /// Download the episodes for the given series
    /// </summary>
    /// <param name="_seriesId">the id of the series</param>
    /// <param name="_language">the language in which the episodes should be downloaded</param>
    /// <returns>An episode object or null if no episodes could be found</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public List<TvdbEpisode> DownloadEpisodes(int _seriesId, TvdbLanguage _language)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateSeriesEpisodesLink(m_apiKey, _seriesId, _language);
        xml = m_webClient.DownloadString(link);
        List<TvdbEpisode> epList = m_xmlHandler.ExtractEpisodes(xml);
        return epList;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to retrieve episodes fo " + _seriesId +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve episodes for" + _seriesId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }



    }

    /// <summary>
    /// <para>Download all available banners (only a list of available banners, not the actual images!)for the specified series.</para>
    /// <para>You can load the actual images by calling LoadBanner() (or LoadThumb(), LoadVignette()) on the banner object</para>
    /// </summary>
    /// <param name="_seriesId">Id of series</param>
    /// <returns>List of all banners for the given series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public List<TvdbBanner> DownloadBanners(int _seriesId)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateSeriesBannersLink(m_apiKey, _seriesId);
        xml = m_webClient.DownloadString(link);
        List<TvdbBanner> banners = m_xmlHandler.ExtractBanners(xml);
        return banners;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to retrieve banners fo " + _seriesId +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve banners for" + _seriesId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }

    /// <summary>
    /// <para>Download series from tvdb (specified by series id and language)</para>
    /// </summary>
    /// <param name="_seriesId">id of series</param>
    /// <param name="_language">language of series</param>
    /// <param name="_loadEpisodes">load episodes</param>
    /// <param name="_loadActors">load actors</param>
    /// <param name="_loadBanners">load banners</param>
    /// <returns>The series object or null if the series couldn't be found</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public TvdbSeries DownloadSeries(int _seriesId, TvdbLanguage _language, bool _loadEpisodes, bool _loadActors, bool _loadBanners)
    {
      //download the xml data from this request
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateSeriesLink(m_apiKey, _seriesId, _language, _loadEpisodes, false);
        xml = m_webClient.DownloadString(link);

        //extract all series the xml file contains
        List<TvdbSeries> seriesList = m_xmlHandler.ExtractSeries(xml);

        //if a request is made on a series id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (seriesList != null && seriesList.Count == 1)
        {
          TvdbSeries series = seriesList[0];
          if (_loadEpisodes)
          {
            //add episode info to series
            List<TvdbEpisode> epList = m_xmlHandler.ExtractEpisodes(xml);
            if (epList != null)
            {
              foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in series.SeriesTranslations)
              {
                if (kvp.Key.Abbriviation.Equals(_language.Abbriviation))
                {
                  series.SeriesTranslations[kvp.Key].Episodes = epList;
                  series.SeriesTranslations[kvp.Key].EpisodesLoaded = true;
                  series.SetLanguage(_language);
                  break;
                }
              }
            }
            
          }

          //also load actors
          if (_loadActors)
          {
            List<TvdbActor> actors = DownloadActors(_seriesId);
            if (actors != null)
            {
              series.TvdbActorsLoaded = true;
              series.TvdbActors = actors;
            }
          }

          //also load banner paths
          if (_loadBanners)
          {
            List<TvdbBanner> banners = DownloadBanners(_seriesId);
            if (banners != null)
            {
              series.Banners = banners;
              series.BannersLoaded = true;
            }
          }
          return series;
        }
        else
        {
          Log.Warn("More than one series returned when trying to retrieve series " + _seriesId);
          return null;
        }
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to retrieve " + _seriesId +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve " + _seriesId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }

    /// <summary>
    /// Download the series in the given language
    /// </summary>
    /// <param name="_seriesId">id of series</param>
    /// <param name="_language">language of series</param>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    /// <returns>the series object</returns>
    public TvdbSeries DownloadSeriesZipped(int _seriesId, TvdbLanguage _language)
    {
      //download the xml data from this request
      byte[] xml = null;
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateSeriesLinkZipped(m_apiKey, _seriesId, _language);
        xml = m_webClient.DownloadData(link);

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
          if (entry.Name.Equals(_language.Abbriviation + ".xml"))
          {
            seriesString = Encoding.UTF8.GetString(buffer);
          }
          else if (entry.Name.Equals("banners.xml"))
          {
            bannersString = Encoding.UTF8.GetString(buffer);
          }
          else if (entry.Name.Equals("actors.xml"))
          {
            actorsString = Encoding.UTF8.GetString(buffer);
          }
          entry = zip.GetNextEntry();
        }
        zip.Close();

        //extract all series the xml file contains
        List<TvdbSeries> seriesList = m_xmlHandler.ExtractSeries(seriesString);

        //if a request is made on a series id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (seriesList != null && seriesList.Count == 1)
        {
          TvdbSeries series = seriesList[0];
          //add episode info to series
          List<TvdbEpisode> epList = m_xmlHandler.ExtractEpisodes(seriesString);
          if (epList != null)
          {
            foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in series.SeriesTranslations)
            {
              if (kvp.Key.Abbriviation.Equals(_language.Abbriviation))
              {
                series.SeriesTranslations[kvp.Key].Episodes = epList;
                series.SeriesTranslations[kvp.Key].EpisodesLoaded = true;
                series.SetLanguage(_language);
                break;
              }
            }
          }

          //also load actors
          List<TvdbActor> actors = m_xmlHandler.ExtractActors(actorsString);
          if (actors != null)
          {
            series.TvdbActorsLoaded = true;
            series.TvdbActors = actors;
          }

          //also load banner paths
          List<TvdbBanner> banners = m_xmlHandler.ExtractBanners(bannersString);
          if (banners != null)
          {
            series.BannersLoaded = true;
            series.Banners = banners;
          }

          return series;
        }
        else
        {
          Log.Warn("More than one series returned when trying to retrieve series " + _seriesId);
          return null;
        }
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + Encoding.Unicode.GetString(xml), ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to retrieve " + _seriesId +
                                               ", you may an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve " + _seriesId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }

    /// <summary>
    /// Download a series search for the id of an external site
    /// </summary>
    /// <param name="_site">The site that provides the external id</param>
    /// <param name="_id">The id that identifies the series on the external site</param>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    /// <returns>the series object that corresponds to the given site and id</returns>
    public TvdbSearchResult DownloadSeriesSearchByExternalId(ExternalId _site, String _id)
    {
      //download the xml data from this request
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateGetSeriesByIdLink(m_apiKey, _site, _id);
        xml = m_webClient.DownloadString(link);

        //extract all series the xml file contains
        List<TvdbSearchResult> seriesList = m_xmlHandler.ExtractSeriesSearchResults(xml);

        //if a request is made on a series id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (seriesList != null && seriesList.Count == 1)
        {
          TvdbSearchResult series = seriesList[0];

          return series;
        }
        else
        {
          Log.Warn("More than one series returned when trying to retrieve series by id " + _id);
          return null;
        }
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to retrieve " + _id +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve " + _id +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }

    }



    internal TvdbSeriesFields DownloadSeriesFields(int _seriesId, TvdbLanguage _language)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateSeriesLink(m_apiKey, _seriesId, _language, false, false);
        xml = m_webClient.DownloadString(link);

        //extract all series the xml file contains
        List<TvdbSeriesFields> seriesList = m_xmlHandler.ExtractSeriesFields(xml);

        if (seriesList != null && seriesList.Count == 1)
        {
          return seriesList[0];
        }
        else
        {
          return null;
        }
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to retrieve " + _seriesId +
                                               ", you may use an invalid api key or the series doesn't exists");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve " + _seriesId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }

    /// <summary>
    /// Download the given episode from tvdb
    /// </summary>
    /// <param name="_episodeId">Id of episode</param>
    /// <param name="_language">Language in which the episode should be downloaded</param>
    /// <returns>The episode object</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public TvdbEpisode DownloadEpisode(int _episodeId, TvdbLanguage _language)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateEpisodeLink(m_apiKey, _episodeId, _language, false);
        xml = m_webClient.DownloadString(link);
        List<TvdbEpisode> epList = m_xmlHandler.ExtractEpisodes(xml);

        if (epList != null && epList.Count == 1)
        {
          return epList[0];
        }
        else
        {
          return null;
        }
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbContentNotFoundException("Couldn't download episode " + _episodeId + "(" + _language +
                                                 "), maybe the episode doesn't exist");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve " + _episodeId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }

    /// <summary>
    /// <para>Download the episode (specified by series id, season number, episode number, language and episode order) from http://thetvdb.com.</para>
    /// <para>It is possible to retrieve episodes by aired order (aka default order), DVD order and absolute order. For a detailled description of these
    /// options see: http://thetvdb.com/wiki/index.php/Category:Episodes</para>
    /// </summary>
    /// <param name="_seriesId">series id</param>
    /// <param name="_seasonNr">season nr</param>
    /// <param name="_episodeNr">episode nr</param>
    /// <param name="_language">language</param>
    /// <param name="_order">order</param>
    /// <returns>The episode object or null if the episode could't be found</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public TvdbEpisode DownloadEpisode(int _seriesId, int _seasonNr, int _episodeNr, TvdbEpisode.EpisodeOrdering _order, TvdbLanguage _language)
    {
      String xml = "";
      String link = "";
      String order = null;
      switch (_order)
      {
        case TvdbEpisode.EpisodeOrdering.AbsoluteOrder:
          order = "absolute";
          break;
        case TvdbEpisode.EpisodeOrdering.DefaultOrder:
          order = "default";
          break;
        case TvdbEpisode.EpisodeOrdering.DvdOrder:
          order = "dvd";
          break;
      }

      try
      {
        link = TvdbLinkCreator.CreateEpisodeLink(m_apiKey, _seriesId, _seasonNr, _episodeNr, order, _language);
        xml = m_webClient.DownloadString(link);
        List<TvdbEpisode> epList = m_xmlHandler.ExtractEpisodes(xml);
        if (epList != null && epList.Count == 1)
        {
          return epList[0];
        }
        else
        {
          return null;
        }
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbContentNotFoundException("Couldn't download episode " + _seriesId + "/" +
                                                 _order + "/" + _seasonNr + "/" + _episodeNr + "/" + _language.Abbriviation +
                                                 ", maybe the episode or the ordering doesn't exist");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve " + _seriesId + "/" +
                                              _order + "/" + _seasonNr + "/" + _episodeNr + "/" + _language.Abbriviation +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }


    /// <summary>
    /// Download the episode specified from http://thetvdb.com
    /// </summary>
    /// <param name="_seriesId">series id</param>
    /// <param name="_airDate">when did the episode air</param>
    /// <param name="_language">language</param>
    /// <returns>Episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public TvdbEpisode DownloadEpisode(int _seriesId, DateTime _airDate, TvdbLanguage _language)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateEpisodeLink(m_apiKey, _seriesId, _airDate, _language);
        xml = m_webClient.DownloadString(link);
        if (!xml.Contains("No Results from SP"))
        {
          List<TvdbEpisode> epList = m_xmlHandler.ExtractEpisodes(xml);
          if (epList != null && epList.Count == 1)
          {
            epList[0].Banner.SeriesId = _seriesId;
            return epList[0];
          }
          else
          {
            return null;
          }
        }
        else
        {
          return null;
        }
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbContentNotFoundException("Couldn't download episode  for series " + _seriesId + " from " +
                                                 _airDate.ToShortDateString() + "(" + _language.Abbriviation +
                                                 "), maybe the episode doesn't exist");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't download episode  for series " + _seriesId + " from " +
                                                 _airDate.ToShortDateString() + "(" + _language.Abbriviation +
                                                 "), maybe the episode doesn't exist");
        }
      }
    }

    /// <summary>
    /// Download the preferred language of the user.
    /// </summary>
    /// <param name="_userId">Id of user</param>
    /// <returns>The preferred language for this user as set on http://thetvdb.com</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public TvdbLanguage DownloadUserPreferredLanguage(String _userId)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateUserLanguageLink(_userId);
        xml = m_webClient.DownloadString(link);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbUserNotFoundException("Couldn't connect to Thetvdb.com to retrieve preferred language for user " + _userId +
                                               ", are you sure this is the correct user id?");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve preferred languae for user " + _userId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
      List<TvdbLanguage> langList = m_xmlHandler.ExtractLanguages(xml);
      if (langList != null && langList.Count == 1)
      {
        return langList[0];
      }
      return null;
    }

    /// <summary>
    /// Download the user favorite list
    /// </summary>
    /// <param name="_userId">Id of user (register at http://thetvdb.com to get a user id)</param>
    /// <returns>Favorite list for specified user</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public List<int> DownloadUserFavoriteList(String _userId)
    {
      return DownloadUserFavoriteList(_userId, Util.UserFavouriteAction.none, 0);
    }

    /// <summary>
    /// Download the user favorite list
    /// </summary>
    /// <param name="_userId">Id of user</param>
    /// <param name="_type">Type of action</param>
    /// <param name="_seriesId">id of series</param>
    /// <returns>List of user favorites</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    internal List<int> DownloadUserFavoriteList(String _userId, Util.UserFavouriteAction _type, int _seriesId)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateUserFavouriteLink(_userId, _type, _seriesId);
        xml = m_webClient.DownloadString(link);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbUserNotFoundException("Couldn't connect to Thetvdb.com to retrieve favorite list for user " + _userId +
                                               ", are you sure this is the correct user id?");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve favorite list for user " + _userId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
      List<int> favList = m_xmlHandler.ExtractSeriesFavorites(xml);
      return favList;
    }

    /// <summary>
    /// Download an Update
    /// </summary>
    /// <param name="_updateSeries">updated series to return</param>
    /// <param name="_updateEpisodes">updated episodes to return</param>
    /// <param name="_updateBanners">updated banners to return</param>
    /// <param name="_interval">interval to download (0=day, 1=week, 2=month)</param>
    /// <param name="_zipped">use zip</param>
    /// <returns>Time of the update</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public DateTime DownloadUpdate(out List<TvdbSeries> _updateSeries, out List<TvdbEpisode> _updateEpisodes,
                                   out List<TvdbBanner> _updateBanners, int _interval,
                                    bool _zipped)
    {
      return DownloadUpdate(out _updateSeries, out _updateEpisodes, out _updateBanners, (Interval)_interval, _zipped);
    }

    /// <summary>
    /// Download an Update
    /// </summary>
    /// <param name="_updateSeries">updated series to return</param>
    /// <param name="_updateEpisodes">updated episodes to return</param>
    /// <param name="_updateBanners">updated banners to return</param>
    /// <param name="_interval">interval to download</param>
    /// <param name="_zipped">use zip</param>
    /// <returns>Time of the update</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public DateTime DownloadUpdate(out List<TvdbSeries> _updateSeries, out List<TvdbEpisode> _updateEpisodes,
                                     out List<TvdbBanner> _updateBanners, Interval _interval, bool _zipped)
    {

      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateUpdateLink(m_apiKey, _interval, _zipped);
        if (_zipped)
        {
          byte[] data = m_webClient.DownloadData(link);
          ZipInputStream zip = new ZipInputStream(new MemoryStream(data));
          zip.GetNextEntry();
          byte[] buffer = new byte[zip.Length];
          int count = zip.Read(buffer, 0, (int)zip.Length);
          xml = Encoding.UTF8.GetString(buffer);
        }
        else
        {
          xml = m_webClient.DownloadString(link);
        }

        _updateEpisodes = m_xmlHandler.ExtractEpisodeUpdates(xml);
        _updateSeries = m_xmlHandler.ExtractSeriesUpdates(xml);
        _updateBanners = m_xmlHandler.ExtractBannerUpdates(xml);

        return m_xmlHandler.ExtractUpdateTime(xml);
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
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to retrieve updates for " + _interval +
                                               ", you may use an invalid api key");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve updates for " + _interval +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
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
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateLanguageLink(m_apiKey);
        xml = m_webClient.DownloadString(link);
        return m_xmlHandler.ExtractLanguages(xml);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to retrieve the list of available languages" +
                                               ", you may use an invalid api key");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve the list of available languages" +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }

    /// <summary>
    /// Download search results for a series search in the default language (english)
    /// </summary>
    /// <param name="_name">name of the series</param>
    /// <returns>List of possible matches for the search</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public List<TvdbSearchResult> DownloadSearchResults(String _name)
    {
      return DownloadSearchResults(_name, TvdbLanguage.DefaultLanguage);
    }

    /// <summary>
    /// Download search results for a series search
    /// </summary>
    /// <param name="_name">name of the series</param>
    /// <param name="_language">language of the search</param>
    /// <returns>List of possible matches for the search</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public List<TvdbSearchResult> DownloadSearchResults(String _name, TvdbLanguage _language)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateSearchLink(_name, _language);
        xml = m_webClient.DownloadString(link);
        return m_xmlHandler.ExtractSeriesSearchResults(xml);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to retrieve search results for " + _name +
                                               ", you may use an invalid api key");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve search results for " + _name +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }

    /// <summary>
    /// Make the request for rating a series
    /// </summary>
    /// <param name="_userId">The id of the user</param>
    /// <param name="_seriesId">The id of the series</param>
    /// <param name="_rating">The rating for this series</param>
    /// <returns>A double value with the current rating for this series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public double RateSeries(String _userId, int _seriesId, int _rating)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateUserSeriesRating(_userId, _seriesId, _rating);
        xml = m_webClient.DownloadString(link);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbUserNotFoundException("Couldn't connect to Thetvdb.com to rate series " + _seriesId +
                                               ", you may use an invalid user id.");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to rate series " + _seriesId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
      return m_xmlHandler.ExtractRating(xml);
    }

    /// <summary>
    /// Make the request for rating an episode
    /// </summary>
    /// <param name="_userId">The id of the user</param>
    /// <param name="_episodeId">The id of the episode</param>
    /// <param name="_rating">The rating for this series</param>
    /// <returns>A double value with the current rating for this series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public double RateEpisode(String _userId, int _episodeId, int _rating)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateUserEpisodeRating(_userId, _episodeId, _rating);
        xml = m_webClient.DownloadString(link);
        return m_xmlHandler.ExtractRating(xml);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbUserNotFoundException("Couldn't connect to Thetvdb.com to rate episode " + _episodeId +
                                               ", you may use an invalid user id or the episode doesn't exists");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to rate episode " + _episodeId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }

    /// <summary>
    /// Download the series rating without rating the item.
    /// </summary>
    /// <param name="_userId">id of user</param>
    /// <param name="_seriesId">id of series</param>
    /// <returns>Current rating for the series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public double DownloadSeriesRating(String _userId, int _seriesId)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateUserSeriesRating(_userId, _seriesId);
        xml = m_webClient.DownloadString(link);
        return m_xmlHandler.ExtractRating(xml);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbUserNotFoundException("Couldn't connect to Thetvdb.com to retrieve rating of series " + _seriesId +
                                               ", you may use an invalid user id or the series doesn't exists");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve rating of series " + _seriesId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }

    /// <summary>
    /// Download the episode rating without rating
    /// </summary>
    /// <param name="_userId">id of the user</param>
    /// <param name="_episodeId">id of the episode</param>
    /// <returns>Current rating of this episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public double DownloadEpisodeRating(String _userId, int _episodeId)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateUserEpisodeRating(_userId, _episodeId);
        xml = m_webClient.DownloadString(link);
        return m_xmlHandler.ExtractRating(xml);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbUserNotFoundException("Couldn't connect to Thetvdb.com to retrieve rating of series " + _episodeId +
                                               ", you may use an invalid user id or the episode doesn't exists");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve rating of series " + _episodeId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }

    /// <summary>
    /// Download the list of actors
    /// </summary>
    /// <param name="_seriesId">Id of series</param>
    /// <returns>List of actors for the given series</returns>
    public List<TvdbActor> DownloadActors(int _seriesId)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateActorLink(_seriesId, m_apiKey);
        xml = m_webClient.DownloadString(link);
        //add series id to actors
        List<TvdbActor> actors = m_xmlHandler.ExtractActors(xml);

        return actors;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Couldn't download actor info from thetvdb.com", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to retrieve actor info of series " + _seriesId +
                                               ", you may use an invalid api key or the series doesn't exists");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve actor info of series " + _seriesId +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
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
    public Dictionary<int, TvdbRating> DownloadAllSeriesRatings(String _userId)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateAllSeriesRatingsLink(m_apiKey, _userId);
        xml = m_webClient.DownloadString(link);
        return m_xmlHandler.ExtractRatings(xml, TvdbRating.ItemType.Series);
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbInvalidApiKeyException("Couldn't connect to Thetvdb.com to retrieve rating of all series " +
                                               ", you may use an invalid api key");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve rating of all series " +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }

    /// <summary>
    /// Download the user rating for the given series (episodes and series itself)
    /// </summary>
    /// <param name="_userId">Id of user</param>
    /// <param name="_seriesId">Id of series</param>
    /// <returns>Dictionary of all ratings</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public Dictionary<int, TvdbRating> DownloadRatingsForSeries(string _userId, int _seriesId)
    {
      String xml = "";
      String link = "";
      try
      {
        link = TvdbLinkCreator.CreateSeriesRatingsLink(m_apiKey, _userId, _seriesId);
        xml = m_webClient.DownloadString(link);
        Dictionary<int, TvdbRating> retList = m_xmlHandler.ExtractRatings(xml, TvdbRating.ItemType.Series);
        Dictionary<int, TvdbRating> episodeList = m_xmlHandler.ExtractRatings(xml, TvdbRating.ItemType.Episode);
        if (retList != null && episodeList != null && retList.Count > 0)
        {
          foreach (KeyValuePair<int, TvdbRating> r in episodeList)
          {
            if (!retList.ContainsKey(r.Key))
            {
              retList.Add(r.Key, r.Value);
            }
          }
          return retList;
        }
        else return null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new TvdbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new TvdbUserNotFoundException("Couldn't connect to Thetvdb.com to retrieve rating of all series " +
                                               ", you may use an invalid api key");
        }
        else
        {
          throw new TvdbNotAvailableException("Couldn't connect to Thetvdb.com to retrieve rating of all series " +
                                              ", check your internet connection and the status of http://thetvdb.com");
        }
      }
    }


  }
}
