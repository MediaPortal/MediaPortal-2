#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.General;

namespace MediaPortal.UI.Presentation.DataObjects
{
  /// <summary>
  /// List of <see cref="ListItem"/> instances to be shown in the GUI.
  /// This class is thread-safe.
  /// </summary>
  public class ItemsList : IList<ListItem>, IObservable, ISynchronizable
  {
    protected SynchronizedCollection<ListItem> _backingList = new SynchronizedCollection<ListItem>();
    protected WeakEventMulticastDelegate _objectChanged = new WeakEventMulticastDelegate();
    protected AbstractProperty _countProperty;

    public ItemsList()
    {
      _countProperty = new SProperty(typeof(int), 0);
    }

    /// <summary>
    /// Event which gets fired when the collection changes.
    /// </summary>
    public event ObjectChangedDlgt ObjectChanged
    {
      add { _objectChanged.Attach(value); }
      remove { _objectChanged.Detach(value); }
    }

    public void FireChange()
    {
      UpdateCount();
      _objectChanged.Fire(new object[] { this });
    }

    protected void UpdateCount()
    {
      Count = _backingList.Count;
    }

    public object SyncRoot
    {
      get { return _backingList.SyncRoot; }
    }

    #region IEnumerable implementation

    public IEnumerator<ListItem> GetEnumerator()
    {
      return _backingList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    #endregion

    #region ICollection<ListItem> implementation

    public void Add(ListItem item)
    {
      _backingList.Add(item);
    }

    public void Clear()
    {
      _backingList.Clear();
    }

    public bool Contains(ListItem item)
    {
      return _backingList.Contains(item);
    }

    public void CopyTo(ListItem[] array, int arrayIndex)
    {
      _backingList.CopyTo(array, arrayIndex);
    }

    public bool Remove(ListItem item)
    {
      return _backingList.Remove(item);
    }

    public AbstractProperty CountProperty
    {
      get { return _countProperty; }
    }

    public int Count
    {
      get { return (int)_countProperty.GetValue(); }
      private set { _countProperty.SetValue(value); }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    #endregion

    #region IList<ListItem> implementation

    public int IndexOf(ListItem item)
    {
      return _backingList.IndexOf(item);
    }

    public void Insert(int index, ListItem item)
    {
      _backingList.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
      _backingList.RemoveAt(index);
    }

    public ListItem this[int index]
    {
      get { return _backingList[index]; }
      set { _backingList[index] = value; }
    }

    #endregion
  }
}
