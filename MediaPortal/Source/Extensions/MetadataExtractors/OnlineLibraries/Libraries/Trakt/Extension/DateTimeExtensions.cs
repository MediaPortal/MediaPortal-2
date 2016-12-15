#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Collections.Generic;
using System.Globalization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Extension
{
  public static class DateTimeExtensions
  {
    /// <summary>
    /// Date Time extension method to return a unix epoch
    /// time as a long
    /// </summary>
    /// <returns> A long representing the Date Time as the number
    /// of seconds since 1/1/1970</returns>
    public static long ToEpoch(this DateTime dt)
    {
      return (long)(dt - new DateTime(1970, 1, 1)).TotalSeconds;
    }

    /// <summary>
    /// Long extension method to convert a Unix epoch
    /// time to a standard C# DateTime object.
    /// </summary>
    /// <returns>A DateTime object representing the unix
    /// time as seconds since 1/1/1970</returns>
    public static DateTime FromEpoch(this long unixTime)
    {
      return new DateTime(1970, 1, 1).AddSeconds(unixTime);
    }

    /// <summary>
    /// Converts string DateTime to ISO8601 format
    /// 2014-09-01T09:10:11.000Z
    /// </summary>
    /// <param name="dt">DateTime as string</param>
    /// <param name="hourShift">Number of hours to shift original time</param>
    /// <returns>ISO8601 Timestamp</returns>
    public static string ToISO8601(this string dt, double hourShift = 0, bool isLocal = false)
    {
      DateTime date;
      if (DateTime.TryParse(dt, out date))
      {
        if (isLocal)
          return date.AddHours(hourShift).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        else
          return date.AddHours(hourShift).ToString("yyyy-MM-ddTHH:mm:ssZ");
      }

      return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    public static string ToISO8601(this DateTime dt, double hourShift = 0)
    {
      string retValue = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

      if (dt == null)
        return retValue;

      return dt.AddHours(hourShift).ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    public static DateTime FromISO8601(this string dt)
    {
      DateTime date;
      if (DateTime.TryParse(dt, out date))
      {
        return date;
      }

      return DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the corresponding Olsen timezone e.g. 'Atlantic/Canary' into a Windows timezone e.g. 'GMT Standard Time'
    /// </summary>

    //commented because of missing Resources class

    //public static string OlsenToWindowsTimezone(this string olsenTimezone)
    //{
    //  if (olsenTimezone == null)
    //    return null;

    //  if (_timezoneMappings == null)
    //  {
    //    _timezoneMappings = Resources.OlsenToWindows.FromJSONDictionary<Dictionary<string, string>>();
    //  }

    //  string windowsTimezone;
    //  _timezoneMappings.TryGetValue(olsenTimezone, out windowsTimezone);

    //  return windowsTimezone;
    //}
    private static Dictionary<string, string> _timezoneMappings = null;

    public static string ToLocalisedDayOfWeek(this DateTime date)
    {
      return DateTimeFormatInfo.CurrentInfo.GetDayName(date.DayOfWeek);
    }
  }
}
