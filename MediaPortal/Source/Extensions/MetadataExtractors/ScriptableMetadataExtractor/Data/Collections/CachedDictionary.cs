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
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Collections
{
  /// <summary>
  /// Stores a value for a limited period of time. Once an item in the CachedDictionary has
  /// been in the collection for a specified Timeout length without access, it will be 
  /// automatically removed.
  /// </summary>
  /// <typeparam name="TKey"></typeparam>
  /// <typeparam name="TValue"></typeparam>
  public class CachedDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
  {
    protected Dictionary<TKey, DateTime> LastAccessed
    {
      get
      {
        if (_lastAccessed == null)
          _lastAccessed = new Dictionary<TKey, DateTime>();

        return _lastAccessed;
      }
    }
    private Dictionary<TKey, DateTime> _lastAccessed;

    /// <summary>
    /// Get/set the value after which items should expire
    /// </summary>
    public TimeSpan Timeout
    {
      get { return ttl; }
      set
      {
        if (value == null)
          ttl = TimeSpan.Zero;
        else
          ttl = value;
      }
    }
    private TimeSpan ttl = new TimeSpan(0, 60, 0);

    /// <summary>
    /// Purge all expired items from memory. Items otherwise will not be removed
    /// until attempted access.
    /// </summary>
    public void Compact()
    {
      foreach (TKey currKey in Keys)
      {
        CheckExpiration(currKey);
      }
    }

    // remove key / value pair if the given key exists and has expired
    private void CheckExpiration(TKey key)
    {
      if (LastAccessed.ContainsKey(key) && DateTime.Now - LastAccessed[key] > Timeout)
      {
        Remove(key);
      }
    }

    #region Dictionary methods

    public virtual void Add(TKey key, TValue value)
    {
      LastAccessed.Add(key, DateTime.Now);
      base.TryAdd(key, value);
    }

    public virtual bool Remove(TKey key)
    {
      LastAccessed.Remove(key);
      return base.TryRemove(key, out _);
    }

    public virtual new TValue this[TKey key]
    {
      get
      {
        CheckExpiration(key);

        if (LastAccessed.ContainsKey(key)) LastAccessed[key] = DateTime.Now;
        return base[key];
      }
      set
      {
        LastAccessed[key] = DateTime.Now;
        base[key] = value;
      }
    }

    public virtual new void Clear()
    {
      LastAccessed.Clear();
      base.Clear();
    }

    public new bool ContainsKey(TKey key)
    {
      CheckExpiration(key);

      if (LastAccessed.ContainsKey(key)) LastAccessed[key] = DateTime.Now;
      return base.ContainsKey(key);
    }

    public virtual new bool TryGetValue(TKey key, out TValue value)
    {
      CheckExpiration(key);

      if (LastAccessed.ContainsKey(key)) LastAccessed[key] = DateTime.Now;
      return base.TryGetValue(key, out value);
    }

    #endregion

  }
}
