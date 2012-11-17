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
using System.Web;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib
{
  /// <summary>
  /// Information on server structure and mirrors of tvdb
  /// 
  /// <![CDATA[
  /// <mirrorpath>/api/<apikey>/
  /// |---- mirrors.xml
  /// |---- languages.xml
  /// |
  /// |---- series/
  /// |     |---- <seriesid>/
  /// |           |---- <language>.xml  (Base Series Record)
  /// |           |---- banners.xml  (All banners related to this series)
  /// |           |
  /// |           |---- all/
  /// |           |     |---- <language>.xml  (Full Series Record)
  /// |           |     |---- <language>.zip  (Zipped version of Full Series Record and banners.xml)
  /// |           |
  /// |           |---- default/  (sorts using the default ordering method)
  /// |           |     |---- <Season#>/<episode#>/
  /// |           |           |---- <language>.xml  (Base Episode Record)
  /// |           |
  /// |           |---- dvd/  (sorts using the dvd ordering method)
  /// |           |     |---- <Season#>/<episode#>/
  /// |           |           |---- <language>.xml  (Base Episode Record)
  /// |           |
  /// |           |---- absolute/  (sorts using the absolute ordering method)
  /// |                 |---- <absolute#>/
  /// |                   |---- <language>.xml  (Base Episode Record)
  /// |
  /// |---- episodes
  /// |     |---- <episodeid>/  (will return en.xml by default)
  /// |           |---- <language>.xml  (Base Episode Record)
  /// |
  /// |---- (updates)
  ///       |---- s<timeframe>.xml
  ///       |---- updates_<timeframe>.zip
  /// ]]>
  /// </summary>
  internal class TvdbLinkCreator
  {
    /// <summary>
    /// Base server where all operations start
    /// </summary>
    internal const String BASE_SERVER = "http://thetvdb.com";

    /// <summary>
    /// Path of file where we get the available languages
    /// </summary>
    internal const String LANG_PATH = "/languages.xml";

    internal static String CreateSeriesLink(String apiKey, int seriesId, TvdbLanguage lang, bool full, bool zipped)
    {
      return String.Format("{0}/api/{1}/series/{2}{3}{4}.xml", BASE_SERVER, apiKey,
                           seriesId, (full ? "/all/" : "/"), (lang != null ? lang.Abbriviation : "en"));
    }

    internal static String CreateSeriesLinkZipped(String apiKey, int seriesId, TvdbLanguage lang)
    {
      String link = String.Format("{0}/api/{1}/series/{2}/all/{3}.zip", BASE_SERVER, apiKey, seriesId,
                                  (lang != null ? lang.Abbriviation : "en"));

      return link;
    }

    internal static String CreateSeriesBannersLink(String apiKey, int seriesId)
    {
      String link = String.Format("{0}/api/{1}/series/{2}/banners.xml", BASE_SERVER, apiKey, seriesId);
      return link;
    }

    internal static String CreateSeriesEpisodesLink(String apiKey, int seriesId, TvdbLanguage lang)
    {
      //this in fact returns the "full series page (http://thetvdb.com/wiki/index.php/API:Full_Series_Record)
      //which sucks because to retrieve all episodes I have to also download the series information (which isn't)
      //all that big on the other hand
      String link = String.Format("{0}/api/{1}/series/{2}/all/{3}.xml", BASE_SERVER, apiKey, seriesId,
                                  (lang != null ? lang.Abbriviation : "en"));
      return link;
    }

    internal static String CreateEpisodeLink(string apiKey, int episodeId, String lang, bool p)
    {
      String link = String.Format("{0}/api/{1}/episodes/{2}/{3}.xml", BASE_SERVER, apiKey, episodeId, lang);
      return link;
    }


    internal static String CreateEpisodeLink(string apiKey, int episodeId, TvdbLanguage lang, bool p)
    {
      return CreateEpisodeLink(apiKey, episodeId, (lang != null ? lang.Abbriviation : "en"), p);
    }

    internal static string CreateEpisodeLink(string apiKey, int seriesId, int seasonNr,
                                             int episodeNr, string order, TvdbLanguage lang)
    {
      String link = String.Format("{0}/api/{1}/series/{2}/{3}/{4}//{5}/{6}.xml", BASE_SERVER, apiKey,
                                  seriesId, order, seasonNr, episodeNr, (lang != null ? lang.Abbriviation : "en"));
      return link;
    }

    internal static string CreateEpisodeLink(string apiKey, int seriesId, DateTime airDate, TvdbLanguage language)
    {
      String link = String.Format("{0}/api/GetEpisodeByAirDate.php?apikey={1}&seriesid={2}&airdate={3}&language={4}",
                                  BASE_SERVER, apiKey, seriesId, airDate.ToShortDateString(),
                                  language.Abbriviation);
      return link;
    }

    internal static String CreateUpdateLink(string apiKey, Interval interval, bool zipped)
    {
      String link = String.Format("{0}/api/{1}/updates/updates_{2}{3}", BASE_SERVER, apiKey,
                                  interval, (zipped ? ".zip" : ".xml"));
      return link;
    }

    internal static String CreateSearchLink(String searchString, TvdbLanguage language)
    {
      String link = String.Format("{0}/api/GetSeries.php?seriesname={1}&language={2}", BASE_SERVER.Trim('/'), HttpUtility.UrlEncode(searchString), language.Abbriviation);
      return link;
    }


    internal static string CreateBannerLink(string bannerPath)
    {
      //this was to test a random mirror choosing, which is done server-side now as it seems
      /*String mirror = null;

      if (m_mirrorList != null)
      {
        //int count = m_random.Next(0, m_mirrorList.Count);
        int count = 1;
        mirror = SelectMirror(count).MirrorPath.ToString().Trim('/');
      }
      {
        mirror = BASE_SERVER;
      }*/

      String link = BASE_SERVER + "/banners/" + bannerPath;
      return link;

    }

    internal static string CreateLanguageLink(string apiKey)
    {
      String link = String.Format("{0}/api/{1}/languages.xml", BASE_SERVER, apiKey);
      return link;
    }

    internal static String CreateUserLanguageLink(String identifier)
    {
      String link = String.Format("{0}/api/User_PreferredLanguage.php?accountid={1}", BASE_SERVER.Trim('/'), identifier);
      return link;
    }

    /// <summary>
    /// Creates link which (depending on params) gets user favorites, adds a series to user
    /// favorites or removes a series from the favorite lis
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="type"></param>
    /// <param name="seriesId"></param>
    /// <returns>Link</returns>
    internal static String CreateUserFavouriteLink(String identifier, Util.UserFavouriteAction type, int seriesId)
    {
      String link = String.Format("{0}/api/User_Favorites.php?accountid={1}{2}", BASE_SERVER.Trim('/'), identifier,
                                  ((type == Util.UserFavouriteAction.None) ? "" : ("&type=" + type +
                                  "&seriesid=" + seriesId)));
      return link;
    }
    /// <summary>
    /// Creates link which only retrieves the user favourites
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns>Link</returns>
    internal static String CreateUserFavouriteLink(String identifier)
    {
      return CreateUserFavouriteLink(identifier, Util.UserFavouriteAction.None, 0);
    }

    #region Rating

    private static String CreateBasicRating(String identifier)
    {
      String link = String.Format("{0}/api/User_Rating.php?accountid={1}", BASE_SERVER.Trim('/'), identifier);
      return link;
    }

    internal static String CreateUserSeriesRating(String identifier, int seriesId)
    {
      return CreateBasicRating(identifier) + "&itemtype=series&itemid=" + seriesId;
    }

    internal static String CreateUserSeriesRating(String identifier, int seriesId, int rating)
    {
      return CreateUserSeriesRating(identifier, seriesId) + "&rating=" + rating;
    }

    internal static String CreateUserEpisodeRating(String identifier, int episodeId)
    {
      return CreateBasicRating(identifier) + "&itemtype=episode&itemid=" + episodeId;
    }

    internal static String CreateUserEpisodeRating(String identifier, int episodeId, int rating)
    {
      return CreateUserEpisodeRating(identifier, episodeId) + "&rating=" + rating;
    }

    #endregion

    /// <summary>
    /// Create link to get actor info
    /// </summary>
    /// <param name="seriesId">series id</param>
    /// <param name="apiKey">api key</param>
    /// <returns>Link</returns>
    internal static String CreateActorLink(int seriesId, String apiKey)
    {
      String link = String.Format("{0}/api/{1}/series/{2}/actors.xml", BASE_SERVER, apiKey, seriesId);
      return link;
    }

    /// <summary>
    /// create a link to all series rated by the user
    /// </summary>
    /// <param name="apiKey">api key</param>
    /// <param name="userIdentifier">user identifier</param>
    /// <returns>Link</returns>
    internal static String CreateAllSeriesRatingsLink(String apiKey, String userIdentifier)
    {
      String link = String.Format("{0}/api/GetRatingsForUser.php?apikey={1}&accountid={2}",
                                  BASE_SERVER, apiKey, userIdentifier);
      return link;
    }

    /// <summary>
    /// create a link to all items rated by the user for this series
    /// </summary>
    /// <param name="apiKey">api key</param>
    /// <param name="userIdentifier">user identifier</param>
    /// <param name="seriesId">id of the series</param>
    /// <returns>Link</returns>
    internal static String CreateSeriesRatingsLink(String apiKey, String userIdentifier, int seriesId)
    {
      String link = CreateAllSeriesRatingsLink(apiKey, userIdentifier) + "&seriesid=" + seriesId;
      return link;
    }

    /// <summary>
    /// Creates a link to retrieve a series by external id (currently only imdb id supported)
    /// 
    /// http://forums.thetvdb.com/viewtopic.php?f=8&t=3724&start=0
    /// </summary>
    /// <param name="apiKey">api key</param>
    /// <param name="site">type of external site</param>
    /// <param name="id">id on the site</param>
    /// <returns></returns>
    internal static String CreateGetSeriesByIdLink(String apiKey, ExternalId site, String id)
    {
      String siteString;
      switch (site)
      {
        case ExternalId.ImdbId:
          siteString = "imdbid";
          break;
        default:
          return "";//unknown site
      }

      String link = String.Format("{0}/api/GetSeriesByRemoteID.php?{1}={2}", BASE_SERVER, siteString, id);
      return link;
      //http://thetvdb.com/api/GetSeriesByRemoteID.php?imdbid=tt0411008
    }
  }
}
