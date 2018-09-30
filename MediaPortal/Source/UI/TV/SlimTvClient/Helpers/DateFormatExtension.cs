#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Globalization;
using MediaPortal.Plugins.SlimTv.Client.Controls;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  public static class DateFormatExtension
  {
    public enum RoundingDirection { Up, Down, Nearest }

    public static DateTime RoundDateTime(this DateTime dt, int minutes, RoundingDirection direction)
    {
      TimeSpan t;
      switch (direction)
      {
        case RoundingDirection.Up:
          t = (dt.Subtract(DateTime.MinValue)).Add(new TimeSpan(0, minutes, 0));
          break;
        case RoundingDirection.Down:
          t = (dt.Subtract(DateTime.MinValue));
          break;
        default:
          t = (dt.Subtract(DateTime.MinValue)).Add(new TimeSpan(0, minutes / 2, 0));
          break;
      }
      return DateTime.MinValue.Add(new TimeSpan(0, (((int)t.TotalMinutes) / minutes) * minutes, 0));
    }

    public static DateTime GetDay(this DateTime dateTime)
    {
      return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0);
    }

    public static DateTime Today
    {
      get { return GetDay(DateTime.Now); }
    }

    public static string FormatProgramStartTime(this DateTime dateTime, CultureInfo cultureInfo = null)
    {
      return FormatProgramTime(dateTime, cultureInfo);
    }
    public static string FormatProgramEndTime(this DateTime dateTime, CultureInfo cultureInfo = null)
    {
      return FormatProgramTime(dateTime, cultureInfo, TvDateFormat.Time);
    }
    public static string FormatProgramTime(this DateTime dateTime, CultureInfo cultureInfo = null, TvDateFormat format = TvDateFormat.Default)
    {
      if (dateTime == DateTime.MinValue)
        return string.Empty;
      cultureInfo = cultureInfo ?? CultureInfo.CurrentUICulture;
      string result = "";

      // Date formats are exclusive
      if (format.HasFlag(TvDateFormat.DifferentDay) && GetDay(dateTime) != Today || format.HasFlag(TvDateFormat.Day))
      {
        result += dateTime.ToString("d", cultureInfo);
      }

      // Time format
      if (format.HasFlag(TvDateFormat.Time))
      {
        if (!string.IsNullOrEmpty(result))
          result += " ";
        result += dateTime.ToString("t", cultureInfo);
      }
      return result;
    }
  }
}
