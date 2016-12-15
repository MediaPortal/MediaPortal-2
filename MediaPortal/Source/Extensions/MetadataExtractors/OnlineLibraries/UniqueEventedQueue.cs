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
using System.Threading;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// UniqueEventedQueue implements a generic queue that can block access until new items are enqueued. It informs listeners using the <see cref="OnEnqueued"/> event.
  /// Additionally it checks for unique items: items that where enqueued before are rejected for additional tries.
  /// </summary>
  /// <typeparam name="T">Type</typeparam>
  public class UniqueEventedQueue<T> : Queue<T>
  {
    // Internal hashset to remember already processed item.
    protected readonly HashSet<T> _index = new HashSet<T>();

    /// <summary>
    /// OnEnqueued is fired, if a new item was added to the queue.
    /// </summary>
    public readonly AutoResetEvent OnEnqueued = new AutoResetEvent(false);

    /// <summary>
    /// SyncObject for synchronizing threaded access to this queue.
    /// </summary>
    public readonly object SyncObj = new object();

    /// <summary>
    /// Adds a new item to the queue, if it was not processed before.
    /// </summary>
    /// <param name="item">Item</param>
    public new virtual void Enqueue(T item)
    {
      TryEnqueue(item);
    }

    /// <summary>
    /// Adds a new item to the queue, if it was not processed before.
    /// </summary>
    /// <param name="item">Item</param>
    /// <returns><c>true</c> if added</returns>
    public virtual bool TryEnqueue(T item)
    {
      if (_index.Contains(item))
        return false;

      _index.Add(item);
      base.Enqueue(item);
      OnEnqueued.Set();
      return true;
    }
  }
}