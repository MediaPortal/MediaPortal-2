#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  public class FormatHelper
  {
    public enum RoundingDirection { Up, Down, Nearest }

    public static DateTime RoundDateTime(DateTime dt, int minutes, RoundingDirection direction)
    {
      TimeSpan t;

      switch (direction)
      {
        case RoundingDirection.Up:
          t = (dt.Subtract(DateTime.MinValue)).Add(new TimeSpan(0, minutes, 0)); break;
        case RoundingDirection.Down:
          t = (dt.Subtract(DateTime.MinValue)); break;
        default:
          t = (dt.Subtract(DateTime.MinValue)).Add(new TimeSpan(0, minutes / 2, 0)); break;
      }

      return DateTime.MinValue.Add(new TimeSpan(0,
             (((int)t.TotalMinutes) / minutes) * minutes, 0));
    }

    public static DateTime GetDay(DateTime dateTime)
    {
      return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0);
    }

    public static DateTime Today
    {
      get { return GetDay(DateTime.Now); }
    }

    public static String FormatProgramTime(DateTime dateTime)
    {
      if (GetDay(dateTime) != Today)
        return String.Format("{0} {1}",
                     dateTime.ToString("d"),
                     dateTime.ToString("t"));

      return dateTime.ToString("t");
    }
  }
}
