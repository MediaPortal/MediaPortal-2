#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Linq;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// <see cref="NavigationList{T}"/> provides navigation features for moving inside a <see cref="IReadOnlyList{T}"/> and exposing <see cref="Current"/> item.
  /// </summary>
  /// <remarks>
  /// Individual methods/properties of this class are thread safe including enumerating the underlying items, however it is not guaranteed that
  /// a thread sees a consistent view of the current navigation across different calls to methods and properties.
  /// For example, a thread may replace the underlying items whilst another thread is enumerating the previous items, in which case the
  /// <see cref="CurrentIndex"/> and <see cref="Current"/> properties may not be valid for the previous enumeration. Similar issues exist when
  /// accessing the <see cref="Count"/> and <see cref="this[int]"/> indexer, etc.
  /// The <see cref="GetItemsWithCurrent(out int)"/> method is provided so that a thread can get an immutable snapshot of the current items and current
  /// index that is guaranteed to be valid.
  /// </remarks>
  /// <typeparam name="T"></typeparam>
  public class NavigationList<T> : IReadOnlyList<T>
  {
    #region Protected classes

    /// <summary>
    /// Internal class for storing the current items and navigation position.
    /// </summary>
    protected class Navigation
    {
      public Navigation(IEnumerable<T> items)
      {
        Items = (items != null ? new List<T>(items) : new List<T>()).AsReadOnly();
        if (Items.Count == 0)
          CurrentIndex = -1;
      }

      public IReadOnlyList<T> Items { get; }
      public int CurrentIndex { get; set; }
    }

    #endregion

    public delegate void CurrentChangedEvent(int oldIndex, int newIndex);
    public CurrentChangedEvent OnCurrentChanged;
    public EventHandler OnListChanged;

    protected Navigation _navigation = new Navigation(null);

    /// <summary>
    /// Gets the item at the <see cref="CurrentIndex"/> or <c>default</c> if empty.
    /// </summary>
    public T Current
    {
      get
      {
        Navigation current = _navigation;
        return current.Items.Count > 0 ? current.Items[current.CurrentIndex] : default; 
      }
    }

    /// <summary>
    /// Gets or sets the index of the current item.
    /// </summary>
    public int CurrentIndex
    {      
      get { return _navigation.CurrentIndex; }
      set
      {
        Navigation current = _navigation;
        if (!IsValidIndex(value, current.Items.Count))
          return;
        int oldIndex = current.CurrentIndex;
        current.CurrentIndex = value;
        FireCurrentChanged(oldIndex);
      }
    }

    /// <summary>
    /// Sets the items to navigate, replacing the existing items.
    /// </summary>
    /// <param name="items">The items to navigate.</param>
    /// <param name="currentIndex">The initial value of <see cref="CurrentIndex"/>.</param>
    /// <returns><c>true</c> if the items were changed; else <c>false</c>.</returns>
    public bool SetItems(IEnumerable<T> items, int currentIndex)
    {
      Navigation navigation = new Navigation(items);
      bool hasChanged = navigation.Items.Count > 0 || _navigation.Items.Count > 0;
      if (IsValidIndex(currentIndex, navigation.Items.Count))
        navigation.CurrentIndex = currentIndex;
      _navigation = navigation;
      return hasChanged;
    }

    /// <summary>
    /// Sets the items to navigate, replacing the existing items.
    /// </summary>
    /// <param name="items">The items to navigate.</param>
    /// <param name="predicate">The predicate to use to set the intital value of <see cref="CurrentIndex"/>.</param>
    /// <returns><c>true</c> if the items were changed; else <c>false</c>.</returns>
    public bool SetItems(IEnumerable<T> items, Predicate<T> predicate)
    {
      return SetItems(items, IndexOf(items, predicate));
    }

    /// <summary>
    /// Clears the items to navigate.
    /// </summary>
    public void ClearItems()
    {
      SetItems(null, -1);
    }

    /// <summary>
    /// Gets a snapshot of the items and current index.
    /// </summary>
    /// <param name="currentIndex">The index of the current item in the returned items, or <c>-1</c> if items is empty.</param>
    /// <returns>The items being navigated.</returns>
    public IReadOnlyList<T> GetItemsWithCurrent(out int currentIndex)
    {
      Navigation current = _navigation;
      currentIndex = current.Items.Count > 0 ? current.CurrentIndex : -1;
      return current.Items;
    }

    /// <summary>
    /// Increments <see cref="CurrentIndex"/>, wraps to the beginning of the items if at the end.
    /// </summary>
    public void MoveNext()
    {
      Move(1);
    }

    /// <summary>
    /// Decrements <see cref="CurrentIndex"/>, wraps to the end of the items if at the beginning.
    /// </summary>
    public void MovePrevious()
    {
      Move(-1);
    }

    protected void Move(int direction)
    {
      Navigation current = _navigation;
      if (current.Items.Count == 0)
        return;
      int oldIndex = current.CurrentIndex;
      current.CurrentIndex = (oldIndex + direction) % current.Items.Count;
      FireCurrentChanged(oldIndex);
    }

    /// <summary>
    /// Tries to move the current index to the index of the first item that matches a condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns><c>true</c> if the current index was changed; else <c>false</c>.</returns>
    public bool MoveTo(Predicate<T> predicate)
    {
      Navigation current = _navigation;
      int index = IndexOf(current.Items, predicate);
      if(index < 0)
        return false;
      int oldIndex = current.CurrentIndex;
      current.CurrentIndex = index;
      FireCurrentChanged(oldIndex);
      return true;
    }

    /// <summary>
    /// Notifies listeners that the <see cref="CurrentIndex"/> has changed.
    /// </summary>
    /// <param name="oldIndex">The previous current index.</param>
    public void FireCurrentChanged(int oldIndex)
    {
      var currentIndex = CurrentIndex;
      if (OnCurrentChanged != null && oldIndex != currentIndex)
        OnCurrentChanged(oldIndex, currentIndex);
    }

    /// <summary>
    /// Notifies listeners that the list's items have changed, also fires the <see cref="OnCurrentChanged"/> event.
    /// </summary>
    public void FireListChanged()
    {
      if (OnListChanged != null)
        OnListChanged(this, EventArgs.Empty);

      FireCurrentChanged(-1);
    }

    protected bool IsValidIndex(int index, int count)
    {
      return index >= 0 && index < count;
    }

    protected int IndexOf(IEnumerable<T> items, Predicate<T> predicate)
    {
      if (items == null || predicate == null)
        return -1;

      int index = 0;
      foreach (var item in items)
        if (predicate(item))
          return index;
        else
          index++;

      return -1;
    }

    #region IReadonlyList<T>

    /// <summary>
    /// Gets the element at the specified index or default if the index is out of range.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>The element at the specified index; or default if the index is out of range.</returns>
    public T this[int index]
    {
      get 
      {
        Navigation current = _navigation;
        return IsValidIndex(index, current.Items.Count) ? current.Items[index] : default;
      }
    }

    public int Count
    {
      get { return _navigation.Items.Count; }
    }

    public IEnumerator<T> GetEnumerator()
    {
      return _navigation.Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return ((IEnumerable)_navigation.Items).GetEnumerator();
    }

    #endregion
  }
}
