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
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using System.Drawing;
using System.Globalization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib
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
    Day = 0,
    /// <summary>
    /// updated content since the last week
    /// </summary>
    Week = 1,
    /// <summary>
    /// updated content since the last month
    /// </summary>
    Month = 2,
    /// <summary>
    /// the interval is determined automatically
    /// </summary>
    Automatic = 3
  };

  internal class Util
  {
    /// <summary>
    /// Type when handling user favorites
    /// </summary>
    internal enum UserFavouriteAction { None, Add, Remove }




    #region private fields
    private static List<TvdbLanguage> _languageList;
    private static NumberFormatInfo _formatProvider;

    #endregion

    /// <summary>
    /// List of available languages -> needed for some methods
    /// </summary>
    public static List<TvdbLanguage> LanguageList
    {
      get { return _languageList; }
      set { _languageList = value; }
    }

    /// <summary>
    /// Parses an integer string and returns the number or -99 if the format
    /// is invalid
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    internal static int Int32Parse(String number)
    {
      //check this or we have a badass performance problem because everytime we have
      //an empty field an exception would be thrown
      if (number.Equals("")) return -99;

      int result;
      if (Int32.TryParse(number, out result))
      {
        return result;
      }
      return -99;
    }

    /// <summary>
    /// Parses an double string and returns the number or -99 if the format
    /// is invalid
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    internal static double DoubleParse(string number)
    {
      try
      {
        if (_formatProvider == null)
        {//format provider, so we can parse 23.23 as well as 23,23
          _formatProvider = new NumberFormatInfo { NumberGroupSeparator = "." };
        }
        //check this or we have a badass performance problem because everytime we have
        //an empty field an exception would be thrown
        if (number.Equals("")) return -99;
        number = number.Replace(',', '.');

        double result;
        if (Double.TryParse(number, NumberStyles.Float, _formatProvider, out result))
        {
          return result;
        }
        return -99;
      }
      catch (FormatException)
      {
        return -99;
      }
    }

    /// <summary>
    /// Splits a tvdb string (having the format | item1 | item2 | item3 |)
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static List<String> SplitTvdbString(String text)
    {
      String[] values = text.Split('|');
      return values.Where(v => !v.Equals("")).ToList();
    }

    /// <summary>
    /// Parse the short description of a tvdb language and returns the proper
    /// object. If no such language exists yet (maybe the list of available
    /// languages hasn't been downloaded yet), a placeholder is created
    /// </summary>
    /// <param name="shortLanguageDesc"></param>
    /// <returns></returns>
    internal static TvdbLanguage ParseLanguage(String shortLanguageDesc)
    {
      if (_languageList != null)
      {
        foreach (TvdbLanguage l in _languageList.Where(l => l.Abbriviation == shortLanguageDesc))
          return l;
      }
      else
        _languageList = new List<TvdbLanguage>();

      //the language doesn't exist yet -> create placeholder
      TvdbLanguage lang = new TvdbLanguage(-99, "unknown", shortLanguageDesc);
      _languageList.Add(lang);
      return lang;
    }

    /// <summary>
    /// Converts a unix timestamp (used on tvdb) into a .net datetime object
    /// </summary>
    /// <param name="unixTimestamp">Timestamp to convert</param>
    /// <returns>.net DateTime object</returns>
    internal static DateTime UnixToDotNet(String unixTimestamp)
    {
      DateTime date = DateTime.Parse("1/1/1970");

      //remove , of float values
      int index = unixTimestamp.IndexOf(',');
      if (index != -1) unixTimestamp = unixTimestamp.Remove(index);

      //remove , of float values
      index = unixTimestamp.IndexOf('.');
      if (index != -1) unixTimestamp = unixTimestamp.Remove(index);

      int seconds;
      if (Int32.TryParse(unixTimestamp, out seconds))
        return date.AddSeconds(seconds);

      Log.Warn("Couldn't convert " + unixTimestamp + " to DateTime");
      return new DateTime();
    }

    /// <summary>
    /// Converts a .net datetime object into a unix timestamp (used on tvdb)  
    /// </summary>
    /// <param name="date">Date to convert</param>
    /// <returns>Unix timestamp</returns>
    internal static String DotNetToUnix(DateTime date)
    {
      TimeSpan span = new TimeSpan(DateTime.Parse("1/1/1970").Ticks);
      DateTime time = date.Subtract(span);
      int t = (int)(time.Ticks / 10000000);

      return t.ToString();
    }

    /// <summary>
    /// returns a day of the week object parsed from the string
    /// </summary>
    /// <param name="dayOfWeek">String representation of this day of the week</param>
    /// <returns>.net DayOfWeek enum</returns>
    internal static DayOfWeek? GetDayOfWeek(string dayOfWeek)
    {
      switch (dayOfWeek.ToLower())
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
    /// <param name="text"></param>
    /// <returns></returns>
    internal static List<Color> ParseColors(String text)
    {
      List<Color> retList = new List<Color>();
      List<String> colorList = SplitTvdbString(text);
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
    }

    /// <summary>
    /// Returns a point objects parsed from _text
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static Point ParseResolution(String text)
    {
      String[] res = text.Split('x');
      int x;
      int y;
      if (Int32.TryParse(res[0], out x) && Int32.TryParse(res[1], out y))
        return new Point(x, y);

      Log.Warn("Couldn't parse resolution" + text);
      return new Point();
    }

    /// <summary>
    /// Parse a boolean value from thetvdb xml files
    /// </summary>
    /// <param name="boolean">Boolean value to parse</param>
    /// <returns></returns>
    internal static bool ParseBoolean(String boolean)
    {
      bool value;
      if (Boolean.TryParse(boolean, out value))
        return value;
      Log.Warn("Couldn't parse bool value of string " + boolean);
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static TvdbSeasonBanner.Type ParseSeasonBannerType(String type)
    {
      if (type.Equals("Season")) return TvdbSeasonBanner.Type.Season;
      if (type.Equals("SeasonWide")) return TvdbSeasonBanner.Type.SeasonWide;
      return TvdbSeasonBanner.Type.None;
    }

    /// <summary>
    /// Returns the fitting SeriesBanner type from parameter
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static TvdbSeriesBanner.Type ParseSeriesBannerType(String type)
    {
      if (type.Equals("Season")) return TvdbSeriesBanner.Type.Blank;
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
    /// Parse a datetime value from thetvdb
    /// </summary>
    /// <param name="date">The date string that needs parsing</param>
    /// <returns>DateTime object of the parsed date</returns>
    internal static DateTime ParseDateTime(string date)
    {
      DateTime retVal;
      DateTime.TryParse(date, out retVal);
      return retVal;
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
