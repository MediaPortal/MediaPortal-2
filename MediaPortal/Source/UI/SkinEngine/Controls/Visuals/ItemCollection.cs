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
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public delegate void ItemCollectionChangedDlgt(ItemCollection collection);

  public class ItemCollection : ICollection<object>, IDisposable, IAddChild<object>, ISynchronizable
  {
    protected IList<object> _elements = new List<object>();
    protected object _syncObj = new object();

    public ItemCollectionChangedDlgt CollectionChanged;

    public void Dispose()
    {
      CollectionChanged = null;
      Clear();
    }

    public object SyncRoot
    {
      get { return _syncObj; }
    }

    public object this[int index]
    {
      get { return _elements[index]; }
      set
      {
        bool changed;
        lock (_syncObj)
        {
          changed = value != _elements[index];
          if (changed)
            _elements[index] = value;
        }
        if (changed)
          FireCollectionChanged();
      }
    }

    protected void FireCollectionChanged()
    {
      ItemCollectionChangedDlgt dlgt = CollectionChanged;
      if (dlgt != null)
        dlgt(this);
    }

    public void AddAll<T>(IEnumerable<T> elements)
    {
      lock (_syncObj)
        CollectionUtils.AddAll(_elements, elements);
      FireCollectionChanged();
    }

    /// <summary>
    /// Removes and returns all elements of this item collection without disposing them.
    /// </summary>
    /// <returns></returns>
    public IList<object> ExtractElements()
    {
      IList<object> result;
      lock (_syncObj)
      {
        result = _elements;
        _elements = new List<object>();
      }
      FireCollectionChanged();
      return result;
    }

    /// <summary>
    /// Sets the <see cref="ListViewItem.ItemIndex"/> for all contained elements.
    /// </summary>
    public void SetItemIndexes()
    {
      lock (_syncObj)
        for (int i = 0; i < _elements.Count; i++)
        {
          ListViewItem lvi = _elements[i] as ListViewItem;
          if (lvi != null)
            lvi.ItemIndex = i;
        }
    }

    #region ICollection implementation

    public void Add(object element)
    {
      lock (_syncObj)
        _elements.Add(element);
      FireCollectionChanged();
    }

    public void CopyTo(object[] array, int arrayIndex)
    {
      lock (_syncObj)
        _elements.CopyTo(array, arrayIndex);
    }

    public bool Remove(object element)
    {
      bool result;
      lock (_syncObj)
      {
        result = _elements.Remove(element);
        MPF.TryCleanupAndDispose(element);
      }
      FireCollectionChanged();
      return result;
    }

    public void Clear()
    {
      lock (_syncObj)
      {
        foreach (object element in _elements)
          MPF.TryCleanupAndDispose(element);
        _elements.Clear();
      }
      FireCollectionChanged();
    }

    public bool Contains(object item)
    {
      lock (_syncObj)
        return _elements.Contains(item);
    }

    public int Count
    {
      get { return _elements.Count; }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    #endregion

    #region IEnumerable<UIElement> implementation

    public IEnumerator<object> GetEnumerator()
    {
      return _elements.GetEnumerator();
    }

    #endregion

    #region IEnumerable implementation

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _elements.GetEnumerator();
    }

    #endregion

    #region IAddChild<T> implementation

    public void AddChild(object child)
    {
      lock (_syncObj)
        _elements.Add(child);
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("{0}: Count={1}", typeof(ItemCollection).Name, Count);
    }

    #endregion
  }
}
