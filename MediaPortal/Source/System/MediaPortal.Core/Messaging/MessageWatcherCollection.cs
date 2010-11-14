#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;

namespace MediaPortal.Core.Messaging
{
  public class MessageWatcherCollection : IDisposable, ICollection<MessageWatcher>
  {
    protected IList<MessageWatcher> _watchers = new List<MessageWatcher>();

    public void Dispose()
    {
      DisposeAll();
    }

    public void DisposeAll()
    {
      foreach (MessageWatcher watcher in _watchers)
        watcher.Dispose();
    }

    public void RegisterAll()
    {
      foreach (MessageWatcher watcher in _watchers)
        watcher.Register();
    }

    public void UnregisterAll()
    {
      foreach (MessageWatcher watcher in _watchers)
        watcher.Unregister();
    }

    #region IEnumerable implementation

    public IEnumerator<MessageWatcher> GetEnumerator()
    {
      return _watchers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    #endregion

    #region ICollection<MessageWatcher> implementation

    public void Add(MessageWatcher item)
    {
      _watchers.Add(item);
    }

    public void Clear()
    {
      _watchers.Clear();
    }

    public bool Contains(MessageWatcher item)
    {
      return _watchers.Contains(item);
    }

    public void CopyTo(MessageWatcher[] array, int arrayIndex)
    {
      _watchers.CopyTo(array, arrayIndex);
    }

    public bool Remove(MessageWatcher item)
    {
      return _watchers.Remove(item);
    }

    public int Count
    {
      get { return _watchers.Count; }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    #endregion
  }
}