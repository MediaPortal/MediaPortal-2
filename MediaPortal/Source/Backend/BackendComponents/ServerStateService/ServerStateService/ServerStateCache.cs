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

using MediaPortal.Plugins.ServerStateService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.ServerStateService
{
  public class ServerStateCache
  {
    #region CacheEntry

    protected class CacheEntry
    {
      public CacheEntry(ServerState state, uint cacheKey)
      {
        State = state;
        CacheKey = cacheKey;
      }

      public ServerState State { get; protected set; }
      public ulong CacheKey { get; protected set; }
    }

    #endregion
    
    protected uint _currentCacheKey = 1;
    protected Dictionary<Guid, CacheEntry> _cachedStates = new Dictionary<Guid, CacheEntry>();

    public void UpdateState(ServerState state)
    {
      if (++_currentCacheKey == 0)
        _currentCacheKey = 1;
      _cachedStates[state.Id] = new CacheEntry(state, _currentCacheKey);
    }
    
    public List<ServerState> GetStates(ref uint cacheKey)
    {
      if (cacheKey == _currentCacheKey)
        return new List<ServerState>();

      uint oldCacheKey = cacheKey;
      cacheKey = _currentCacheKey;
      if (oldCacheKey == 0 || oldCacheKey > _currentCacheKey)
        return _cachedStates.Values.Select(s => s.State).ToList();
      else
        return _cachedStates.Values.Where(s => s.CacheKey > oldCacheKey).Select(s => s.State).ToList();
    }
  }
}