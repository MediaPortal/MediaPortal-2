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