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
using TvdbLib.Cache;
using System.Drawing;
using System.Globalization;
using TvdbLib.Data.Banner;

namespace TvdbLib
{
  #region enums
  /// <summary>
  /// ID's of external sites
  /// </summary>
  public enum ExternalId
  {
    /// <summary>
    /// Id for the popular movie/tv site www.imdb.com
    /// </summary>
    ImdbId = 0
  }

  #endregion

  /// <summary>
  /// Update interval
  /// </summary>
  public enum Interval
  {
    /// <summary>
    /// updated content since the last day
    /// </summary>
    day = 0,
    /// <summary>
    /// updated content since the last week
    /// </summary>
    week = 1,
    /// <summary>
    /// updated content since the last month
    /// </summary>
    month = 2,
    /// <summary>
    /// the interval is determined automatically
    /// </summary>
    automatic = 3
  };

  internal class Util
  {
    /// <summary>
    /// Type when handling user favorites
    /// </summary>
    internal enum UserFavouriteAction { none, add, remove }




    #region private fields
    private static List<TvdbLanguage> m_languageList;
    private static NumberFormatInfo m_formatProvider;

    #endregion

    /// <summary>
    /// List of available languages -> needed for some methods
    /// </summary>
    public static List<TvdbLanguage> LanguageList
    {
      get { return m_languageList; }
      set { m_languageList = value; }
    }

    /// <summary>
    /// Parses an integer string and returns the number or -99 if the format
    /// is invalid
    /// </summary>
    /// <param name="_number"></param>
    /// <returns></returns>
    internal static int Int32Parse(String _number)
    {
      //check this or we have a badass performance problem because everytime we have
      //an empty field an exception would be thrown
      if (_number.Equals("")) return -99;

      int result;
      if (Int32.TryParse(_number, out result))
      {
        return result;
      }
      else
      {
        return -99;
      }
    }

    /// <summary>
    /// Parses an double string and returns the number or -99 if the format
    /// is invalid
    /// </summary>
    /// <param name="_number"></param>
    /// <returns></returns>
    internal static double DoubleParse(string _number)
    {
      try
      {
        if (m_formatProvider == null)
        {//format provider, so we can parse 23.23 as well as 23,23
          m_formatProvider = new NumberFormatInfo();
          m_formatProvider.NumberGroupSeparator = ".";
        }
        //check this or we have a badass performance problem because everytime we have
        //an empty field an exception would be thrown
        if (_number.Equals("")) return -99;
        _number = _number.Replace(',', '.');

        double result;
        if (Double.TryParse(_number,NumberStyles.Float, m_formatProvider, out result))
        {
          return result;
        }
        else
        {
          return -99;
        }
      }
      catch (FormatException)
      {
        return -99;
      }
    }

    /// <summary>
    /// Splits a tvdb string (having the format | item1 | item2 | item3 |)
    /// </summary>
    /// <param name="_text"></param>
    /// <returns></returns>
    internal static List<String> SplitTvdbString(String _text)
    {
      List<String> list = new List<string>();
      String[] values = _text.Split('|');
      foreach (String v in values)
      {
        if (!v.Equals("")) list.Add(v);
      }

      return list;
    }

    /// <summary>
    /// Parse the short description of a tvdb language and returns the proper
    /// object. If no such language exists yet (maybe the list of available
    /// languages hasn't been downloaded yet), a placeholder is created
    /// </summary>
    /// <param name="_shortLanguageDesc"></param>
    /// <returns></returns>
    internal static TvdbLanguage ParseLanguage(String _shortLanguageDesc)
    {
      if (m_languageList != null)
      {
        foreach (TvdbLanguage l in m_languageList)
        {
          if (l.Abbriviation == _shortLanguageDesc)
          {
            return l;
          }
        }
      }
      else
      {
        m_languageList = new List<TvdbLanguage>();
      }

      //the language doesn't exist yet -> create placeholder
      TvdbLanguage lang = new TvdbLanguage(-99, "unknown", _shortLanguageDesc);
      m_languageList.Add(lang);
      return lang;
    }

    /// <summary>
    /// Converts a unix timestamp (used on tvdb) into a .net datetime object
    /// </summary>
    /// <param name="_unixTimestamp">Timestamp to convert</param>
    /// <returns>.net DateTime object</returns>
    internal static DateTime UnixToDotNet(String _unixTimestamp)
    {
      System.DateTime date = System.DateTime.Parse("1/1/1970");

      //remove , of float values
      int index = _unixTimestamp.IndexOf(',');
      if (index != -1) _unixTimestamp = _unixTimestamp.Remove(index);

      //remove , of float values
      index = _unixTimestamp.IndexOf('.');
      if (index != -1) _unixTimestamp = _unixTimestamp.Remove(index);

      int seconds;
      if(Int32.TryParse(_unixTimestamp, out seconds))
      {
        return date.AddSeconds(seconds);
      }
      else
      {
        Log.Warn("Couldn't convert " + _unixTimestamp + " to DateTime");
        return new DateTime();
      }      
    }

    /// <summary>
    /// Converts a .net datetime object into a unix timestamp (used on tvdb)  
    /// </summary>
    /// <param name="_date">Date to convert</param>
    /// <returns>Unix timestamp</returns>
    internal static String DotNetToUnix(DateTime _date)
    {
      System.TimeSpan span = new System.TimeSpan(System.DateTime.Parse("1/1/1970").Ticks);
      System.DateTime time = _date.Subtract(span);
      int t = (int)(time.Ticks / 10000000);

      return t.ToString();
      //TimeSpan span = (_date - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
      //return ((int)span.TotalSeconds).ToString();
    }

    /// <summary>
    /// returns a day of the week object parsed from the string
    /// </summary>
    /// <param name="_dayOfWeek">String representation of this day of the week</param>
    /// <returns>.net DayOfWeek enum</returns>
    internal static DayOfWeek? GetDayOfWeek(string _dayOfWeek)
    {
      switch (_dayOfWeek.ToLower())
      {
        case "monday":
        case "montag":
        case "mo":
          return DayOfWeek.Monday;
        case "tuesday":
        case "dienstag":
        case "di":
          return DayOfWeek.Tuesday;
        case "wednesday":
        case "mittwoch":
        case "mi":
          return DayOfWeek.Wednesday;
        case "thursday":
        case "donnerstag":
        case "do":
          return DayOfWeek.Thursday;
        case "friday":
        case "freitag":
        case "fr":
          return DayOfWeek.Friday;
        case "saturday":
        case "samstag":
        case "sa":
          return DayOfWeek.Saturday;
        case "sunday":
        case "sonntag":
        case "so":
          return DayOfWeek.Sunday;
        default:
          return null;
      }
    }

    /// <summary>
    /// Returns a List of colors parsed from the _text
    /// </summary>
    /// <param name="_text"></param>
    /// <returns></returns>
    internal static List<Color> ParseColors(String _text)
    {
      List<Color> retList = new List<Color>();
      List<String> colorList = SplitTvdbString(_text);
      for (int i = 0; i < colorList.Count; i++)
      {
        String[] color = colorList[i].Split(',');
        int red;
        int green;
        int blue;
        if (Int32.TryParse(color[0], out red) && Int32.TryParse(color[1], out green) && 
            Int32.TryParse(color[2], out blue))
        {
          retList.Add(Color.FromArgb(red, green, blue));
        }
      }
      return null;
      //throw new NotImplementedException();
    }

    /// <summary>
    /// Returns a point objects parsed from _text
    /// </summary>
    /// <param name="_text"></param>
    /// <returns></returns>
    internal static Point ParseResolution(String _text)
    {
      String[] res = _text.Split('x');
      int x;
      int y;
      if (Int32.TryParse(res[0], out x) && Int32.TryParse(res[1], out y))
      {
        return new Point(x, y);
      }
      else
      {
        Log.Warn("Couldn't parse resolution" + _text);
        return new Point();
      }
      //throw new NotImplementedException();
    }

    /// <summary>
    /// Parse a boolean value from thetvdb xml files
    /// </summary>
    /// <param name="_boolean">Boolean value to parse</param>
    /// <returns></returns>
    internal static bool ParseBoolean(String _boolean)
    {
      bool value = false;
      if (Boolean.TryParse(_boolean, out value))
      {
        return value;
      }
      else
      {
        Log.Warn("Couldn't parse bool value of string " + _boolean);
        return false;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_type"></param>
    /// <returns></returns>
    internal static TvdbSeasonBanner.Type ParseSeasonBannerType(String _type)
    {
      if (_type.Equals("season")) return TvdbLib.Data.Banner.TvdbSeasonBanner.Type.season;
      else if (_type.Equals("seasonwide")) return TvdbLib.Data.Banner.TvdbSeasonBanner.Type.seasonwide;
      else return TvdbLib.Data.Banner.TvdbSeasonBanner.Type.none;
    }

    /// <summary>
    /// Returns the fitting SeriesBanner type from parameter
    /// </summary>
    /// <param name="_type"></param>
    /// <returns></returns>
    internal static TvdbSeriesBanner.Type ParseSeriesBannerType(String _type)
    {
      if (_type.Equals("season")) return TvdbLib.Data.Banner.TvdbSeriesBanner.Type.blank;
      else if (_type.Equals("graphical")) return TvdbLib.Data.Banner.TvdbSeriesBanner.Type.graphical;
      else if (_type.Equals("text")) return TvdbLib.Data.Banner.TvdbSeriesBanner.Type.text;
      else return TvdbLib.Data.Banner.TvdbSeriesBanner.Type.none;
    }


    /// <summary>
    /// Add the episode to the series
    /// </summary>
    /// <param name="_episode"></param>
    /// <param name="_series"></param>
    internal static void AddEpisodeToSeries(TvdbEpisode _episode, TvdbSeries _series)
    {
      bool episodeFound = false; ;
      for (int i = 0; i < _series.Episodes.Count; i++)
      {
        if (_series.Episodes[i].Id == _episode.Id)
        {//we have already stored this episode -> overwrite it
          _series.Episodes[i].UpdateEpisodeInfo(_episode);
          episodeFound = true;
          break;
        }
      }
      if (!episodeFound)
      {//the episode doesn't exist yet
        _series.Episodes.Add(_episode);
        if (!_series.EpisodesLoaded) _series.EpisodesLoaded = true;
      }
    }


      /// <summary>
      /// Parse a datetime value from thetvdb
      /// </summary>
      /// <param name="_date">The date string that needs parsing</param>
      /// <returns>DateTime object of the parsed date</returns>
    internal static DateTime ParseDateTime(string _date)
    {
        DateTime retVal;
        DateTime.TryParse(_date, out retVal);
        return retVal;
    }

    /// <summary>
    /// Tries to find an episode by a given id from a list of episodes
    /// </summary>
    /// <param name="_episodeId">Id of the episode we're looking for</param>
    /// <param name="_episodeList">List of episodes</param>
    /// <returns>The first found TvdbEpisode object or null if nothing was found</returns>
    internal static TvdbEpisode FindEpisodeInList(int _episodeId, List<TvdbEpisode> _episodeList)
    {
      foreach (TvdbEpisode e in _episodeList)
      {
        if (e.Id == _episodeId)
        {//found episode
          return e;
        }
      }
      return null;//no episode found
    }

    /// <summary>
    /// Tries to find a series by a given id from a list of series
    /// </summary>
    /// <param name="_seriesId">Id of the series we're looking for</param>
    /// <param name="_seriesList">List of series objects</param>
    /// <returns>The first found TvdbSeries object or null if nothing was found</returns>
    internal static TvdbSeries FindSeriesInList(int _seriesId, List<TvdbSeries> _seriesList)
    {
      foreach (TvdbSeries s in _seriesList)
      {
        if (s.Id == _seriesId)
        {//series found
          return s;
        }
      }
      return null;//no series found
    }
  }
}
