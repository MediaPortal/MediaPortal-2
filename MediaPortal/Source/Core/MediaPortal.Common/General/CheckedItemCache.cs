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
using System.Threading;

namespace MediaPortal.Common.General
{
  public class CheckedItemCache<T>
  {
    private const int CHECK_DELAY = 60000;

    private ConcurrentDictionary<T, T> _checkedItems = new ConcurrentDictionary<T, T>();
    private DateTime _lastCacheRefresh = DateTime.Now;
    private double _expirationAgeInHours = 1;
    private Timer _clearTimer;

    public CheckedItemCache(double expirationAgeInHours)
    {
      _expirationAgeInHours = expirationAgeInHours;
      _clearTimer = new Timer(ClearCache, null, CHECK_DELAY, CHECK_DELAY);
    }

    public bool IsItemChecked(T item)
    {
      if (!_checkedItems.ContainsKey(item))
      {
        _checkedItems.TryAdd(item, item);
        return false;
      }
      return true;
    }

    private void ClearCache(object state)
    {
      if ((DateTime.Now - _lastCacheRefresh).TotalHours > _expirationAgeInHours)
      {
        _lastCacheRefresh = DateTime.Now;
        _checkedItems.Clear();
      }
    }

    public bool TryGetCheckedItem(T item, out T checkedItem)
    {
      checkedItem = default(T);
      if (!_checkedItems.ContainsKey(item))
      {
        return false;
      }
      checkedItem = _checkedItems[item];
      return true;
    }

    public bool TryAddCheckedItem(T checkedItem)
    {
      return _checkedItems.TryAdd(checkedItem, checkedItem);
    }

    public void ClearCache()
    {
      _lastCacheRefresh = DateTime.Now;
      _checkedItems.Clear();
    }
  }
}
