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

using System.Collections.Generic;

namespace MediaPortal.Utilities.Cache
{
  public delegate void ObjectPrunedDlgt<TKey, TValue>(ILRUCache<TKey, TValue> sender, TKey key, TValue value);

  /// <summary>
  /// LRU cache which stores a fixed maximum amount of (key; value) mapping entries. An entry which
  /// is not among the last used entries will be discarded from the cache.
  /// </summary>
  /// <typeparam name="TKey">Key type parameter.</typeparam>
  /// <typeparam name="TValue">Value type parameter.</typeparam>
  public interface ILRUCache<TKey, TValue>
  {
    event ObjectPrunedDlgt<TKey, TValue> ObjectPruned;

    /// <summary>
    /// Gets or sets the cache size. The cache will be truncated if its size is bigger than
    /// the value set to this property.
    /// </summary>
    int CacheSize { get; set; }

    /// <summary>
    /// Returns a collection with all available values.
    /// </summary>
    ICollection<TValue> Values { get; }

    /// <summary>
    /// Returns a collection with all available keys.
    /// </summary>
    ICollection<TKey> Keys { get; }

    /// <summary>
    /// Adds the given key; value pair to this cache.
    /// </summary>
    /// <remarks>
    /// If the given <paramref name="key"/> already exists, the new entry will replace the old entry.
    /// </remarks>
    /// <param name="key">Key of the new entry.</param>
    /// <param name="value">Value of the new entry.</param>
    void Add(TKey key, TValue value);

    /// <summary>
    /// Removes the entry with the given key. The <see cref="ObjectPruned"/> event will not be fired here.
    /// </summary>
    /// <param name="key">Key of the entry to remove.</param>
    void Remove(TKey key);

    /// <summary>
    /// Returns the information if the given <paramref name="key"/> is contained in this cache.
    /// </summary>
    /// <param name="key">Key to search.</param>
    /// <returns><c>true</c>, if the given <paramref name="key"/> is contained in this cache, else <c>false</c>.</returns>
    bool Contains(TKey key);

    /// <summary>
    /// Returns the value which is stored in the cache for the specified <paramref name="key"/> if
    /// it is present in the cache.
    /// </summary>
    /// <param name="key">Key to retrieve the value for.</param>
    /// <param name="value">Contained value if the key is found in the cache.</param>
    /// <returns><c>true</c>, if the given <paramref name="key"/> is found in the cache, else <c>false</c>.</returns>
    bool TryGetValue(TKey key, out TValue value);

    /// <summary>
    /// This method has to be called to notify the cache that an entry was used. It will set the entry to the
    /// beginning of the backing LRU list.
    /// </summary>
    /// <param name="key">Key of the used entry.</param>
    void Touch(TKey key);

    /// <summary>
    /// Removes all entries from this cache. The <see cref="ObjectPruned"/> event will not be fired here.
    /// </summary>
    void Clear();
  }
}