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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Client.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Services.UserManagement;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// Helper class to store channel groups and channels in a common place for all models.
  /// </summary>
  public class ChannelContext : IDisposable
  {
    protected static readonly object _syncObj = new object();
    protected static ChannelContext _channelContext;

    private UserMessageHandler _userMessageHandler;
    protected bool _isChannelGroupsInitialized = false;
    protected NavigationList<IChannelGroup> _channelGroups;

    protected readonly object _channelSyncObj = new object(); 

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

    public NavigationList<IChannelGroup> ChannelGroups
    {
      get
      {
        if (!_isChannelGroupsInitialized)
          InitChannelGroups().Wait();
        return _channelGroups;
      }
    }

    public NavigationList<IChannel> Channels { get; internal set; }

    public ChannelContext()
    {
      Channels = new NavigationList<IChannel>();
      _channelGroups = new NavigationList<IChannelGroup>();
      _channelGroups.OnCurrentChanged += ReloadChannels;
      _userMessageHandler = new UserMessageHandler();
      _userMessageHandler.RequestRestrictions += OnRegisterRestrictions;
      _userMessageHandler.UserChanged += OnUserChanged;
      InitChannelGroups().Wait();
    }

    public async Task InitChannelGroups()
    {
      var tvHandler = ServiceRegistration.Get<ITvHandler>(false);
      if (tvHandler != null && tvHandler.ChannelAndGroupInfo != null)
      {
        _channelGroups.Clear();
        var result = await tvHandler.ChannelAndGroupInfo.GetChannelGroupsAsync();

        //Reset initialized statue on failure so we can retry later 
        _isChannelGroupsInitialized = result.Success && result.Result != null && result.Result.Count > 0;
        if (!_isChannelGroupsInitialized)
          return;

        var channelGroups = result.Result;

        RegisterRestrictions(channelGroups);

        Channels?.Clear();
        _channelGroups.AddRange(FilterGroups(channelGroups));
        _channelGroups.FireListChanged();

        int selectedChannelGroupId = tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId;
        if (tvHandler.ChannelAndGroupInfo != null && selectedChannelGroupId != 0)
          _channelGroups.MoveTo(group => group.ChannelGroupId == selectedChannelGroupId);

        _channelGroups.FireCurrentChanged(-1);
      }
    }

    public static bool IsSameChannel(IChannel channel1, IChannel channel2)
    {
      if (channel1 == channel2)
        return true;

      if (channel1 != null && channel2 != null)
        return channel1.ChannelId == channel2.ChannelId && channel1.MediaType == channel2.MediaType;
      return false;
    }

    /// <summary>
    /// Registers known channel groups so it can be used to restrict users.
    /// </summary>
    /// <param name="channelGroups"></param>
    private void RegisterRestrictions(IList<IChannelGroup> channelGroups)
    {
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      foreach (IUserRestriction channelGroup in channelGroups.OfType<IUserRestriction>())
        userManagement.RegisterRestrictionGroup(channelGroup.RestrictionGroup);
    }

    /// <summary>
    /// Applies a filter to channel groups. This will be used to remove "All Channels" group if needed or to apply user restrictions.
    /// </summary>
    /// <param name="channelGroups">Groups</param>
    /// <returns>Filtered groups</returns>
    private static IList<IChannelGroup> FilterGroups(IList<IChannelGroup> channelGroups)
    {
      IList<IChannelGroup> filteredGroups = new List<IChannelGroup>();
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      bool hideAllChannelsGroup = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>().HideAllChannelsGroup;
      foreach (IChannelGroup channelGroup in channelGroups.Where(g => !hideAllChannelsGroup || g.Name != "All Channels"))
      {
        IUserRestriction restriction = channelGroup as IUserRestriction;
        if (restriction != null && !userManagement.CheckUserAccess(restriction))
          continue;
        filteredGroups.Add(channelGroup);
      }
      return filteredGroups;
    }

    private void OnUserChanged(object sender, EventArgs e)
    {
      InitChannelGroups().Wait();
    }

    private void OnRegisterRestrictions(object sender, EventArgs e)
    {
      InitChannelGroups().Wait();
    }

    /// <summary>
    /// Reload all channels if channel group is changed.
    /// </summary>
    /// <param name="oldindex">Index of previous selected entry</param>
    /// <param name="newindex">Index of current selected entry</param>
    private async void ReloadChannels(int oldindex, int newindex)
    {
      var tvHandler = ServiceRegistration.Get<ITvHandler>(false);
      if (tvHandler == null)
        return;

      IChannelGroup currentGroup;
      lock (_channelSyncObj)
        currentGroup = _channelGroups.Current;

      var result = await tvHandler.ChannelAndGroupInfo.GetChannelsAsync(currentGroup);
      if (!result.Success)
        return;

      IList<IChannel> channels = result.Result;

      // Check user zapping setting for channel index vs. number preferance
      SlimTvClientSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
      if (settings.ZapByChannelIndex)
      {
        for (int i = 0; i < channels.Count; i++)
          channels[i].ChannelNumber = i + 1;
      }

      int oldChannelIndex = -1;
      bool channelIndexChanged = false;
      lock (_channelSyncObj)
      {
        if (_channelGroups.Current != currentGroup)
          return;
        Channels.ClearAndReset();
        Channels.AddRange(channels);
        // Check if the current channel is part of new group and select it
        int selectedChannelId = tvHandler.ChannelAndGroupInfo.SelectedChannelId;
        if (tvHandler.ChannelAndGroupInfo != null && selectedChannelId != 0)
          channelIndexChanged = Channels.MoveTo(channel => channel.ChannelId == selectedChannelId, false, out oldChannelIndex);
      }
      Channels.FireListChanged();
      if (channelIndexChanged)
        Channels.FireCurrentChanged(oldChannelIndex);
    }

    public void Dispose()
    {
      _userMessageHandler?.Dispose();
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

    private readonly object _syncObj = new object();
    private int _current;

    public void ClearAndReset(IEnumerable<T> newItems = null)
    {
      lock (_syncObj)
      {
        Clear();
        _current = 0;
        if (newItems != null)
          AddRange(newItems);
      }
    }

    public T Current
    {
      get
      {
        lock (_syncObj)
          return Count > 0 && _current < Count ? this[_current] : default(T);
      }
    }

    public int CurrentIndex
    {
      get
      {
        lock (_syncObj)
          return Count > 0 ? _current : -1;
      }
      set
      {
        int oldIndex;
        lock (_syncObj)
        {
          if (Count == 0 || value < 0 || value >= Count)
            return;
          oldIndex = CurrentIndex;
          _current = value;
        }
        FireCurrentChanged(oldIndex);
      }
    }

    public void MoveNext()
    {
      int oldIndex;
      lock (_syncObj)
      {
        if (Count == 0)
          return;
        oldIndex = CurrentIndex;
        _current++;
        if (_current >= Count)
          _current = 0;
      }
      FireCurrentChanged(oldIndex);
    }

    public void MovePrevious()
    {
      int oldIndex;
      lock (_syncObj)
      {
        if (Count == 0)
          return;
        oldIndex = CurrentIndex;
        _current--;
        if (_current < 0)
          _current = Count - 1;
      }
      FireCurrentChanged(oldIndex);
    }

    public void SetIndex(int index)
    {
      int oldIndex;
      lock (_syncObj)
      {
        if (Count == 0 || index < 0 || index >= Count)
          return;
        oldIndex = CurrentIndex;
        _current = index;
      }
      FireCurrentChanged(oldIndex);
    }

    public bool MoveTo(Predicate<T> condition)
    {
      return MoveTo(condition, true, out _);
    }

    public bool MoveTo(Predicate<T> condition, bool fireChange, out int oldIndex)
    {
      bool isCurrentChanged = false;
      lock (_syncObj)
      {
        oldIndex = CurrentIndex;
        for (int index = 0; index < Count; index++)
        {
          T item = this[index];
          if (!condition.Invoke(item))
            continue;
          _current = index;
          isCurrentChanged = true;
          break;
        }
      }
      if (isCurrentChanged && fireChange)
        FireCurrentChanged(oldIndex);
      return isCurrentChanged;
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
