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
using TvdbLib.Data;

namespace TvdbLib
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
  /// |           |     |---- <season#>/<episode#>/
  /// |           |           |---- <language>.xml  (Base Episode Record)
  /// |           |
  /// |           |---- dvd/  (sorts using the dvd ordering method)
  /// |           |     |---- <season#>/<episode#>/
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

    internal static String CreateSeriesLink(String _apiKey, int _seriesId, TvdbLanguage _lang, bool _full, bool _zipped)
    {
      return String.Format("{0}/api/{1}/series/{2}{3}{4}.xml", BASE_SERVER, _apiKey,
                           _seriesId, (_full ? "/all/" : "/"), (_lang != null ? _lang.Abbriviation : "en"));
    }

    internal static String CreateSeriesLinkZipped(String _apiKey, int _seriesId, TvdbLanguage _lang)
    {
      String link = String.Format("{0}/api/{1}/series/{2}/all/{3}.zip", BASE_SERVER, _apiKey, _seriesId,
                                  (_lang != null ? _lang.Abbriviation : "en"));

      return link;
    }

    internal static String CreateSeriesBannersLink(String _apiKey, int _seriesId)
    {
      String link = String.Format("{0}/api/{1}/series/{2}/banners.xml", BASE_SERVER, _apiKey, _seriesId);
      return link;
    }

    internal static String CreateSeriesEpisodesLink(String _apiKey, int _seriesId, TvdbLanguage _lang)
    {
      //this in fact returns the "full series page (http://thetvdb.com/wiki/index.php/API:Full_Series_Record)
      //which sucks because to retrieve all episodes I have to also download the series information (which isn't)
      //all that big on the other hand
      String link = String.Format("{0}/api/{1}/series/{2}/all/{3}.xml", BASE_SERVER, _apiKey, _seriesId,
                                  (_lang != null ? _lang.Abbriviation : "en"));
      return link;
    }

    internal static String CreateEpisodeLink(string _apiKey, int _episodeId, String _lang, bool p)
    {
      String link = String.Format("{0}/api/{1}/episodes/{2}/{3}.xml", BASE_SERVER, _apiKey, _episodeId, _lang);
      return link;
    }


    internal static String CreateEpisodeLink(string _apiKey, int _episodeId, TvdbLanguage _lang, bool p)
    {
      return CreateEpisodeLink(_apiKey, _episodeId, (_lang != null ? _lang.Abbriviation : "en"), p);
    }

    internal static string CreateEpisodeLink(string _apiKey, int _seriesId, int _seasonNr,
                                             int _episodeNr, string _order, TvdbLanguage _lang)
    {
      String link = String.Format("{0}/api/{1}/series/{2}/{3}/{4}//{5}/{6}.xml", BASE_SERVER, _apiKey,
                                  _seriesId, _order, _seasonNr, _episodeNr, (_lang != null ? _lang.Abbriviation : "en"));
      return link;
    }

    internal static string CreateEpisodeLink(string _apiKey, int _seriesId, DateTime _airDate, TvdbLanguage _language)
    {
      String link = String.Format("{0}/api/GetEpisodeByAirDate.php?apikey={1}&seriesid={2}&airdate={3}&language={4}",
                                  BASE_SERVER, _apiKey, _seriesId, _airDate.ToShortDateString(),
                                  _language.Abbriviation);
      return link;
    }

    internal static String CreateUpdateLink(string _apiKey, Interval _interval, bool _zipped)
    {
      String link = String.Format("{0}/api/{1}/updates/updates_{2}{3}", BASE_SERVER, _apiKey,
                                  _interval, (_zipped ? ".zip" : ".xml"));
      return link;
    }

    internal static String CreateSearchLink(String _searchString, TvdbLanguage _language)
    {
      String link = String.Format("{0}/api/GetSeries.php?seriesname={1}&language={2}", BASE_SERVER.Trim('/'), _searchString, _language.Abbriviation);
      return link;
    }


    internal static string CreateBannerLink(string _bannerPath)
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

      String link = BASE_SERVER + "/banners/" + _bannerPath;
      return link;

    }

    internal static string CreateLanguageLink(string _apiKey)
    {
      String link = String.Format("{0}/api/{1}/languages.xml", BASE_SERVER, _apiKey);
      return link;
    }

    internal static String CreateUserLanguageLink(String _identifier)
    {
      String link = String.Format("{0}/api/User_PreferredLanguage.php?accountid={1}", BASE_SERVER.Trim('/'), _identifier);
      return link;
    }

    /// <summary>
    /// Creates link which (depending on params) gets user favorites, adds a series to user
    /// favorites or removes a series from the favorite lis
    /// </summary>
    /// <param name="_identifier"></param>
    /// <param name="_type"></param>
    /// <param name="_seriesId"></param>
    /// <returns>Link</returns>
    internal static String CreateUserFavouriteLink(String _identifier, Util.UserFavouriteAction _type, int _seriesId)
    {
      String link = String.Format("{0}/api/User_Favorites.php?accountid={1}{2}", BASE_SERVER.Trim('/'), _identifier,
                                  ((_type == Util.UserFavouriteAction.none) ? "" : ("&type=" + _type +
                                  "&seriesid=" + _seriesId)));
      return link;
    }
    /// <summary>
    /// Creates link which only retrieves the user favourites
    /// </summary>
    /// <param name="_identifier"></param>
    /// <returns>Link</returns>
    internal static String CreateUserFavouriteLink(String _identifier)
    {
      return CreateUserFavouriteLink(_identifier, Util.UserFavouriteAction.none, 0);
    }

    #region Rating

    private static String CreateBasicRating(String _identifier)
    {
      String link = String.Format("{0}/api/User_Rating.php?accountid={1}", BASE_SERVER.Trim('/'), _identifier);
      return link;
    }

    internal static String CreateUserSeriesRating(String _identifier, int _seriesId)
    {
      return CreateBasicRating(_identifier) + "&itemtype=series&itemid=" + _seriesId;
    }

    internal static String CreateUserSeriesRating(String _identifier, int _seriesId, int _rating)
    {
      return CreateUserSeriesRating(_identifier, _seriesId) + "&rating=" + _rating;
    }

    internal static String CreateUserEpisodeRating(String _identifier, int _episodeId)
    {
      return CreateBasicRating(_identifier) + "&itemtype=episode&itemid=" + _episodeId;
    }

    internal static String CreateUserEpisodeRating(String _identifier, int _episodeId, int _rating)
    {
      return CreateUserEpisodeRating(_identifier, _episodeId) + "&rating=" + _rating;
    }

    #endregion

    /// <summary>
    /// Create link to get actor info
    /// </summary>
    /// <param name="_seriesId">series id</param>
    /// <param name="_apiKey">api key</param>
    /// <returns>Link</returns>
    internal static String CreateActorLink(int _seriesId, String _apiKey)
    {
      String link = String.Format("{0}/api/{1}/series/{2}/actors.xml", BASE_SERVER, _apiKey, _seriesId);
      return link;
    }

    /// <summary>
    /// create a link to all series rated by the user
    /// </summary>
    /// <param name="_apiKey">api key</param>
    /// <param name="_userIdentifier">user identifier</param>
    /// <returns>Link</returns>
    internal static String CreateAllSeriesRatingsLink(String _apiKey, String _userIdentifier)
    {
      String link = String.Format("{0}/api/GetRatingsForUser.php?apikey={1}&accountid={2}",
                                  BASE_SERVER, _apiKey, _userIdentifier);
      return link;
    }

    /// <summary>
    /// create a link to all items rated by the user for this series
    /// </summary>
    /// <param name="_apiKey">api key</param>
    /// <param name="_userIdentifier">user identifier</param>
    /// <param name="_seriesId">id of the series</param>
    /// <returns>Link</returns>
    internal static String CreateSeriesRatingsLink(String _apiKey, String _userIdentifier, int _seriesId)
    {
      String link = CreateAllSeriesRatingsLink(_apiKey, _userIdentifier) + "&seriesid=" + _seriesId;
      return link;
    }

    /// <summary>
    /// Creates a link to retrieve a series by external id (currently only imdb id supported)
    /// 
    /// http://forums.thetvdb.com/viewtopic.php?f=8&t=3724&start=0
    /// </summary>
    /// <param name="_apiKey">api key</param>
    /// <param name="_site">type of external site</param>
    /// <param name="_id">id on the site</param>
    /// <returns></returns>
    internal static String CreateGetSeriesByIdLink(String _apiKey, ExternalId _site, String _id)
    {
      String siteString = "";
      switch(_site)
      {
        case ExternalId.ImdbId:
          siteString = "imdbid";
          break;
        default:
          return "";//unknown site
      }

      String link = String.Format("{0}/api/GetSeriesByRemoteID.php?{1}={2}", BASE_SERVER, siteString, _id);
      return link;
      //http://thetvdb.com/api/GetSeriesByRemoteID.php?imdbid=tt0411008
    }


  }
}
