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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib
{
 
  internal class TvDbUtils
  {
    #region private fields

    private static List<TvdbLanguage> _languageList;
    private static object _syncObj = new object();

    #endregion

    /// <summary>
    /// List of available languages -> needed for some methods
    /// </summary>
    public static List<TvdbLanguage> LanguageList
    {
      get { lock (_syncObj) return _languageList; }
      set { lock (_syncObj) _languageList = value; }
    }

    /// <summary>
    /// Parse the short description of a tvdb language and returns the proper
    /// object. If no such language exists yet (maybe the list of available
    /// languages hasn't been downloaded yet), a placeholder is created
    /// </summary>
    /// <param name="shortLanguageDesc"></param>
    /// <returns></returns>
    public static TvdbLanguage ParseLanguage(String shortLanguageDesc)
    {
      lock (_syncObj)
      {
        if (_languageList != null)
        {
          foreach (TvdbLanguage l in _languageList.Where(l => l.Abbriviation == shortLanguageDesc))
            return l;
        }
        else
          _languageList = new List<TvdbLanguage>();

        //the language doesn't exist yet -> create placeholder
        TvdbLanguage lang = new TvdbLanguage(Util.NO_VALUE, "unknown", shortLanguageDesc);
        _languageList.Add(lang);
        return lang;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static TvdbSeasonBanner.Type ParseSeasonBannerType(String type)
    {
      if (type.Equals("season")) return TvdbSeasonBanner.Type.Season;
      if (type.Equals("seasonwide")) return TvdbSeasonBanner.Type.SeasonWide;
      return TvdbSeasonBanner.Type.None;
    }

    /// <summary>
    /// Returns the fitting SeriesBanner type from parameter
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static TvdbSeriesBanner.Type ParseSeriesBannerType(String type)
    {
      if (type.Equals("season")) return TvdbSeriesBanner.Type.Blank;
      if (type.Equals("graphical")) return TvdbSeriesBanner.Type.Graphical;
      if (type.Equals("text")) return TvdbSeriesBanner.Type.Text;
      return TvdbSeriesBanner.Type.None;
    }


    /// <summary>
    /// Add the episode to the series
    /// </summary>
    /// <param name="episode"></param>
    /// <param name="series"></param>
    internal static void AddEpisodeToSeries(TvdbEpisode episode, TvdbSeries series)
    {
      bool episodeFound = false;
      for (int i = 0; i < series.Episodes.Count; i++)
      {
        if (series.Episodes[i].Id == episode.Id)
        {//we have already stored this episode -> overwrite it
          series.Episodes[i].UpdateEpisodeInfo(episode);
          episodeFound = true;
          break;
        }
      }
      if (!episodeFound)
      {//the episode doesn't exist yet
        series.Episodes.Add(episode);
        if (!series.EpisodesLoaded) series.EpisodesLoaded = true;
      }
    }
    
    /// <summary>
    /// Tries to find an episode by a given id from a list of episodes
    /// </summary>
    /// <param name="episodeId">Id of the episode we're looking for</param>
    /// <param name="episodeList">List of episodes</param>
    /// <returns>The first found TvdbEpisode object or null if nothing was found</returns>
    internal static TvdbEpisode FindEpisodeInList(int episodeId, List<TvdbEpisode> episodeList)
    {
      return episodeList.FirstOrDefault(e => e.Id == episodeId);
    }

    /// <summary>
    /// Tries to find a series by a given id from a list of series
    /// </summary>
    /// <param name="seriesId">Id of the series we're looking for</param>
    /// <param name="seriesList">List of series objects</param>
    /// <returns>The first found TvdbSeries object or null if nothing was found</returns>
    internal static TvdbSeries FindSeriesInList(int seriesId, List<TvdbSeries> seriesList)
    {
      return seriesList.FirstOrDefault(s => s.Id == seriesId);
    }
  }
}
