/*
 *   MovieDbLib: A library to retrieve information and media from http://TheMovieDb.org
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
using System.Drawing;
using System.Globalization;
using System.Security.Cryptography;
using System.IO;

namespace MovieDbLib
{
  public class MovieDbUtils
  {
    /// <summary>
    /// Update interval
    /// </summary>
    internal enum UpdateInterval { day = 0, week = 1, month = 2 };

    /// <summary>
    /// Type when handling user favorites
    /// </summary>
    internal enum UserFavouriteAction { none, add, remove }

    #region private fields
    private static NumberFormatInfo m_formatProvider;

    #endregion

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
      if (_number == null || _number.Equals("")) return -99;

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
        if (_number == null || _number.Equals("")) return -99;
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

    internal static DateTime DateTimeParse(string p)
    {
      if (p == null || p.Equals("")) return new DateTime();
      return DateTime.Parse(p);
    }

    public static string GetMovieHashString(string filename)
    {
      string hash;
      try
      {
        byte[] moviehash = ComputeMovieHash(filename);
        hash = ToHexadecimal(moviehash);
      }
      catch (Exception e)
      {
        Log.Error("Error while generating FileHash for: " + filename, e);
        hash = null;
      }
      return hash;
    }

    private static byte[] ComputeMovieHash(string filename)
    {
      byte[] result;
      using (Stream input = File.OpenRead(filename))
      {
        result = ComputeMovieHash(input);
      }
      return result;
    }

    private static byte[] ComputeMovieHash(Stream input)
    {
      ulong lhash;
      long streamsize;
      streamsize = input.Length;
      lhash = (ulong)streamsize;

      long i = 0;
      byte[] buffer = new byte[sizeof(long)];
      input.Position = 0;
      while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
      {
        i++;
        unchecked { lhash += BitConverter.ToUInt64(buffer, 0); }
      }

      input.Position = Math.Max(0, streamsize - 65536);
      i = 0;
      while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
      {
        i++;
        unchecked { lhash += BitConverter.ToUInt64(buffer, 0); }
      }
      byte[] result = BitConverter.GetBytes(lhash);
      Array.Reverse(result);
      return result;
    }

    private static string ToHexadecimal(byte[] bytes)
    {
      StringBuilder hexBuilder = new StringBuilder();
      for (int i = 0; i < bytes.Length; i++)
      {
        hexBuilder.Append(bytes[i].ToString("x2"));
      }
      return hexBuilder.ToString();
    }

    private static string[] VIDEO_EXT = { ".mov", ".mkv", ".avi", ".wmv", ".mpeg", ".mpg" };
    public static bool IsVideoFile(FileInfo _file)
    {
      foreach (string f in VIDEO_EXT)
      {
        if (_file.Extension.ToLower().Equals(f)) return true;
      }
      return false;
    }
  }
}
