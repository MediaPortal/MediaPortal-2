#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using TvMosaic.API;

namespace TvMosaic.Shared
{

  public static class DateExtensions
  {
    public static DateTime FromUnixTime(this long ut)
    {
      if (ut == 0) return DateTime.MinValue;
      long l = ut;
      l += (long)(369 * 365 + 89) * 86400;
      l *= 10000000;
      return DateTime.FromFileTime(l);
    }

    public static uint ToUnixTime(this DateTime val)
    {
      uint ut;
      try
      {
        if (val == DateTime.MinValue)
          ut = 0;
        else
        {
          long l = val.ToFileTime();
          l /= 10000000;
          l -= (long)(369 * 365 + 89) * 86400;
          ut = (uint)l;
        }
      }
      catch
      {
        ut = 0;
      }

      return ut;
    }
    public static int ToUnixTime(this DateTime? val)
    {
      return val.HasValue ? (int)val.Value.ToUnixTime() : (int)EpgSearcher.EPG_INVALID_TIME; // Unbound in TvMosaic API
    }
  }
}
