using System;
using System.Collections.Generic;
using MediaPortal.Common.General;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  /// <summary>
  /// <see cref="NavigationList{T}"/> provides navigation features for moving inside a <see cref="List{T}"/> and exposing <see cref="Current"/> item.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class NavigationList<T> : List<T>, IObservable
  {
    public delegate void CurrentChangedEvent(int oldIndex, int newIndex);
    public CurrentChangedEvent OnCurrentChanged;
    public EventHandler OnListChanged;
    protected WeakEventMulticastDelegate _objectChanged = new WeakEventMulticastDelegate();

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
      _objectChanged.Fire(new object[] { this });
    }

    private int _current;

    public T Current
    {
      get { return Count > 0 && _current < Count ? this[_current] : default(T); }
    }

    public int CurrentIndex
    {
      get { return Count > 0 ? _current : -1; }
      set
      {
        if (Count == 0 || value < 0 || value >= Count)
          return;
        int oldIndex = CurrentIndex;
        _current = value;
        FireCurrentChanged(oldIndex);
      }
    }

    public T GetAt(int index)
    {
      if (Count == 0 || index < 0)
        return default(T);

      //if (index < 0)
      //  index += Count;
      return this[index % Count];
    }

    public void MoveNext()
    {
      MoveNext(1);
    }

    public void MoveNext(int count)
    {
      if (Count == 0)
        return;
      int oldIndex = CurrentIndex;
      _current += count;
      if (_current >= Count)
        _current = _current % Count;
      FireCurrentChanged(oldIndex);
    }

    public void MovePrevious()
    {
      MovePrevious(1);
    }

    public void MovePrevious(int count)
    {
      if (Count == 0)
        return;
      int oldIndex = CurrentIndex;
      _current -= count;
      if (_current < 0)
        _current = Count + (_current % Count);
      FireCurrentChanged(oldIndex);
    }

    public void SetIndex(int index)
    {
      if (Count == 0 || index < 0 || index >= Count)
        return;
      int oldIndex = CurrentIndex;
      _current = index;
      FireCurrentChanged(oldIndex);
    }

    public bool MoveTo(Predicate<T> condition)
    {
      int oldIndex = CurrentIndex;
      for (int index = 0; index < Count; index++)
      {
        T item = this[index];
        if (!condition.Invoke(item))
          continue;
        _current = index;
        if (_current != oldIndex)
          FireCurrentChanged(oldIndex);
        return true;
      }
      return false;
    }

    public void FireCurrentChanged(int oldIndex)
    {
      var currentIndex = CurrentIndex;
      if (OnCurrentChanged != null && oldIndex != currentIndex)
        OnCurrentChanged(oldIndex, currentIndex);
    }

    public void FireListChanged()
    {
      if (OnListChanged != null)
        OnListChanged(this, EventArgs.Empty);
    }
  }
}