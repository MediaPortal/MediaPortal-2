using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.Matchers
{
  public class MemoryCache<T1, T2>
  {
    protected ConcurrentDictionary<T1, T2> _storage = new ConcurrentDictionary<T1, T2>();
    protected TimeSpan _cacheDuration = TimeSpan.FromHours(12);
    protected DateTime _cacheInvalidated = DateTime.MinValue;

    public void Add(T1 key, T2 value)
    {
      _storage.TryAdd(key, value);
    }

    public bool TryGetValue(T1 key, out T2 value)
    {
      CheckCacheValid();
      return _storage.TryGetValue(key, out value);
    }

    protected void CheckCacheValid()
    {
      if ((DateTime.Now - _cacheInvalidated) > _cacheDuration)
      {
        _storage.Clear();
        _cacheInvalidated = DateTime.Now;
      }
    }
  }
}