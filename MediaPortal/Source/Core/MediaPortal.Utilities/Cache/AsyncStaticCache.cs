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
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MediaPortal.Utilities.Cache
{
  /// <summary>
  /// Asynchronous cache particularly to be used as static cache
  /// </summary>
  /// <remarks>
  /// This cache class does intentionally not take its ValueFactory as constructor parameter. Instead it is passed
  /// as parameter with each call to the <see cref="GetValue"/> method. That way it is possible to instantiate this
  /// class as a static field and use a non-static method as ValueFactory.
  /// </remarks>
  /// <typeparam name="TKey">Type of the cache's keys</typeparam>
  /// <typeparam name="TValue">Type of the cache's values</typeparam>
  public class AsyncStaticCache<TKey, TValue>
  {
    #region Private fields

    /// <summary>
    /// Dictionary containing the cached tasks
    /// </summary>
    protected readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _cache;

    #endregion

    #region Ctor

    /// <summary>
    /// Instantiates an AsyncStaticCache object
    /// </summary>
    public AsyncStaticCache()
    {
      _cache = new ConcurrentDictionary<TKey, Lazy<Task<TValue>>>();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets the number of items in the cache
    /// </summary>
    public int Count
    {
      get { return _cache.Count; }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Gets a Task representing the value for the specified key
    /// </summary>
    /// <param name="key">Key to get a value for</param>
    /// <param name="valueFactory">Factory used to create a task representing the value if it doesn't exist in the cache, yet</param>
    /// <returns>A Task for the value of the specified key</returns>
    public virtual Task<TValue> GetValue(TKey key, Func<TKey, Task<TValue>> valueFactory)
    {
      var value = new Lazy<Task<TValue>>(() => valueFactory(key));
      return _cache.GetOrAdd(key, value).Value;
    }

    #endregion
  }
}
