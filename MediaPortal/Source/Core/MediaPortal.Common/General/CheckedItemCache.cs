#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.Concurrent;

namespace MediaPortal.Common.General
{
  public class CheckedItemCache<T>
  {
    private ConcurrentDictionary<T, DateTime> _checkedSeries = new ConcurrentDictionary<T, DateTime>();
    private DateTime _lastCacheRefresh = DateTime.Now;
    private double _expirationAgeInHours = 1;

    public CheckedItemCache(double expirationAgeInHours)
    {
      _expirationAgeInHours = expirationAgeInHours;
    }

    public bool IsItemChecked(T item)
    {
      if ((DateTime.Now - _lastCacheRefresh).TotalHours > _expirationAgeInHours)
        _checkedSeries.Clear();

      if (!_checkedSeries.ContainsKey(item))
      {
        _checkedSeries.TryAdd(item, DateTime.Now);
        return false;
      }
      return true;
    }
  }
}
