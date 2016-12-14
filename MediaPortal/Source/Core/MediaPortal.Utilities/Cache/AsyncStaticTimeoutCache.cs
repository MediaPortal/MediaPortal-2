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
  /// Asynchronous cache that automatically removes items after a timeout
  /// </summary>
  /// <typeparam name="TKey">Type of the cache's keys</typeparam>
  /// <typeparam name="TValue">Type of the cache's values</typeparam>
  public class AsyncStaticTimeoutCache<TKey, TValue> : AsyncStaticCache<TKey, TValue>
  {
    #region Protected fields

    /// <summary>
    /// Dictionaty that holds the tasks that remove chached items after the timeout
    /// </summary>
    protected readonly ConcurrentDictionary<TKey, Task> _timeoutTasks;

    /// <summary>
    /// Timespan after which cached items are automatically removed
    /// </summary>
    protected readonly TimeSpan _timeout;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates an instance of this class
    /// </summary>
    /// <param name="timeout">Timespan after which cached items are automatically removed</param>
    public AsyncStaticTimeoutCache(TimeSpan timeout)
    {
      _timeoutTasks = new ConcurrentDictionary<TKey, Task>();
      _timeout = timeout;
    }

    #endregion

    #region Base overrides

    /// <summary>
    /// Gets a Task representing the value for the specified key and, if necessary,
    /// starts a task that removes the item from the cache after the timeout has elapsed.
    /// </summary>
    /// <param name="key">Key to get a value for</param>
    /// <param name="valueFactory">Factory used to create a task representing the value if it doesn't exist in the cache, yet</param>
    /// <returns>A Task for the value of the specified key</returns>
    public override Task<TValue> GetValue(TKey key, Func<TKey, Task<TValue>> valueFactory)
    {
      var value = new Lazy<Task<TValue>>(() => valueFactory(key));
      var valueInCache = _cache.GetOrAdd(key, value);
      if (ReferenceEquals(value, valueInCache))
      {
        // We just added a new value to the cache and need to create a timeout task for it
        _timeoutTasks[key] = Task.Run(async () =>
        {
          await valueInCache.Value.ConfigureAwait(false);
          await Task.Delay(_timeout).ConfigureAwait(false);
          Task timeoutTask;
          _timeoutTasks.TryRemove(key, out timeoutTask);
          Lazy<Task<TValue>> valueToRemove;
          _cache.TryRemove(key, out valueToRemove);
        });
      }
      return valueInCache.Value;
    }

    #endregion
  }
}
