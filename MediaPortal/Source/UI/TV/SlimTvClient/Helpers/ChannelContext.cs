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

using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Plugins.SlimTv.Client.Messaging;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// Helper class to store channel groups and channels in a common place for all models.
  /// </summary>
  public class ChannelContext
  {
    protected static readonly object _syncObj = new object();
    protected static ChannelContext _channelContext;

    #region Static instance

    /// <summary>
    /// Gets the current <see cref="ChannelContext"/> from the <see cref="ServiceRegistration"/>. This allows all models to access one common group and channel lists.
    /// </summary>
    public static ChannelContext Instance
    {
      get
      {
        lock (_syncObj)
        {
          return _channelContext ?? (_channelContext = new ChannelContext());
        }
      }
    }

    #endregion

    public NavigationList<IChannelGroup> ChannelGroups { get; internal set; }
    public NavigationList<IChannel> Channels { get; internal set; }

    public ChannelContext()
    {
      ChannelGroups = new NavigationList<IChannelGroup>();
      Channels = new NavigationList<IChannel>();
      ChannelGroups.OnCurrentChanged += ReloadChannels;
      InitChannelGroups();
    }

    private void InitChannelGroups()
    {
      IList<IChannelGroup> channelGroups;
      var tvHandler = ServiceRegistration.Get<ITvHandler>(false);
      if (tvHandler != null && tvHandler.ChannelAndGroupInfo.GetChannelGroups(out channelGroups))
      {
        ChannelGroups.Clear();
        ChannelGroups.AddRange(channelGroups);
        ChannelGroups.FireListChanged();

        int selectedChannelGroupId = tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId;
        if (tvHandler.ChannelAndGroupInfo != null && selectedChannelGroupId != 0)
          ChannelGroups.MoveTo(group => group.ChannelGroupId == selectedChannelGroupId);

        ChannelGroups.FireCurrentChanged(-1);
      }
    }

    /// <summary>
    /// Reload all channels if channel group is changed.
    /// </summary>
    /// <param name="oldindex">Index of previous selected entry</param>
    /// <param name="newindex">Index of current selected entry</param>
    private void ReloadChannels(int oldindex, int newindex)
    {
      IList<IChannel> channels;
      var tvHandler = ServiceRegistration.Get<ITvHandler>(false);
      if (tvHandler != null && tvHandler.ChannelAndGroupInfo.GetChannels(ChannelGroups.Current, out channels))
      {
        Channels.Clear();
        Channels.AddRange(channels);
        Channels.FireListChanged();
        // Check if the current channel is part of new group and select it
        int selectedChannelId = tvHandler.ChannelAndGroupInfo.SelectedChannelId;
        if (tvHandler.ChannelAndGroupInfo != null && selectedChannelId != 0)
          Channels.MoveTo(channel => channel.ChannelId == selectedChannelId);
      }
      // Notify listeners about group change
      SlimTvClientMessaging.SendSlimTvClientMessage(SlimTvClientMessaging.MessageType.GroupChanged);
    }
  }

  /// <summary>
  /// <see cref="NavigationList{T}"/> provides navigation features for moving inside a <see cref="List{T}"/> and exposing <see cref="Current"/> item.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class NavigationList<T> : List<T>
  {
    public delegate void CurrentChangedEvent(int oldIndex, int newIndex);
    public CurrentChangedEvent OnCurrentChanged;
    public EventHandler OnListChanged;

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

    public void MoveNext()
    {
      if (Count == 0)
        return;
      int oldIndex = CurrentIndex;
      _current++;
      if (_current >= Count)
        _current = 0;
      FireCurrentChanged(oldIndex);
    }

    public void MovePrevious()
    {
      if (Count == 0)
        return;
      int oldIndex = CurrentIndex;
      _current--;
      if (_current < 0)
        _current = Count - 1;
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
        return true;
      }
      FireCurrentChanged(oldIndex);
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
