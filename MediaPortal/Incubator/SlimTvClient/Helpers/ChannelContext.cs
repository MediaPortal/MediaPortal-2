#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// Helper class to store channel groups and channels in a common place for all models.
  /// </summary>
  public class ChannelContext
  {
    public readonly NavigationList<IChannelGroup> ChannelGroups = new NavigationList<IChannelGroup>();
    public readonly NavigationList<IChannel> Channels = new NavigationList<IChannel>();
  }

  /// <summary>
  /// <see cref="NavigationList{T}"/> provides navigation features for moving inside a <see cref="List{T}"/> and exposing <see cref="Current"/> item.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class NavigationList<T> : List<T>
  {
    private int _current;

    public T Current
    {
      get { return Count > 0 ? this[_current] : default(T); }
    }

    public int CurrentIndex
    {
      get { return Count > 0 ? _current : -1; }
      set
      {
        if (Count == 0 || value < 0 || value >= Count)
          return;
        _current = value;
      }
    }

    public void MoveNext()
    {
      if (Count == 0)
        return;
      _current++;
      if (_current >= Count)
        _current = 0;
    }

    public void MovePrevious()
    {
      if (Count == 0)
        return;
      _current--;
      if (_current < 0)
        _current = Count - 1;
    }

    public void SetIndex(int index)
    {
      if (Count == 0 || index < 0 || index >= Count)
        return;
      _current = index;
    }

    public bool MoveTo(Predicate<T> condition)
    {
      for (int index = 0; index < Count; index++)
      {
        T item = this[index];
        if (!condition.Invoke(item))
          continue;
        _current = index;
        return true;
      }
      return false;
    }
  }
}
