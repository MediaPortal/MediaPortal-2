#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
    protected static readonly object _initSyncObj = new object();
    protected static ChannelContext _channelContext;

    private UserMessageHandler _userMessageHandler;
    protected bool _isChannelGroupsInitialized = false;
    protected bool _isTvChannelGroupsSet = false;
    protected bool _isRadioChannelGroupsSet = false;
    protected NavigationList<IChannelGroup> _channelTvGroups;
    protected NavigationList<IChannelGroup> _channelRadioGroups;
    protected NavigationList<IChannel> _channelsTv;
    protected NavigationList<IChannel> _channelsRadio;

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

    public NavigationList<IChannelGroup> ChannelGroups => TvChannelGroups;

    public NavigationList<IChannelGroup> TvChannelGroups
    {
      get
      {
        PrepareChannelGroups(_channelTvGroups, false, ref _isTvChannelGroupsSet);
        return _channelTvGroups;
      }
    }

    public NavigationList<IChannelGroup> RadioChannelGroups
    {
      get
      {
        PrepareChannelGroups(_channelRadioGroups, true, ref _isRadioChannelGroupsSet);
        return _channelRadioGroups;
      }
    }

    public NavigationList<IChannel> Channels => TvChannels;

    public NavigationList<IChannel> TvChannels
    {
      get
      {
        PrepareChannelGroups(_channelTvGroups, false, ref _isTvChannelGroupsSet);
        return _channelsTv;
      }
    }

    public NavigationList<IChannel> RadioChannels
    {
      get
      {
        PrepareChannelGroups(_channelRadioGroups, true, ref _isRadioChannelGroupsSet);
        return _channelsRadio;
      }
    }

    public ChannelContext()
    {
      _channelsTv = new NavigationList<IChannel>();
      _channelsRadio = new NavigationList<IChannel>();

      _channelTvGroups = new NavigationList<IChannelGroup>();
      _channelTvGroups.OnCurrentChanged += ReloadTvChannels;
      _channelRadioGroups = new NavigationList<IChannelGroup>();
      _channelRadioGroups.OnCurrentChanged += ReloadRadioChannels;

      _userMessageHandler = new UserMessageHandler();
      _userMessageHandler.RequestRestrictions += OnRegisterRestrictions;
      _userMessageHandler.UserChanged += OnUserChanged;
      InitChannelGroups().Wait();
    }

    private void PrepareChannelGroups(NavigationList<IChannelGroup> groupList, bool isRadio, ref bool isGroupSet)
    {
      if (!_isChannelGroupsInitialized)
      {
        lock(_initSyncObj)
          InitChannelGroups().Wait();
      }

      if (!isGroupSet)
      {
        lock (_initSyncObj)
          InitSelectedGroup(groupList, isRadio, ref isGroupSet);
      }
    }

    public async Task InitChannelGroups()
    {
      if (_isChannelGroupsInitialized)
        return;

      var tvHandler = ServiceRegistration.Get<ITvHandler>(false);
      if (tvHandler != null && tvHandler.ChannelAndGroupInfo != null)
      {
        _isTvChannelGroupsSet = false;
        _isRadioChannelGroupsSet = false;

        _channelTvGroups.Clear();
        _channelRadioGroups.Clear();
        var result = await tvHandler.ChannelAndGroupInfo.GetChannelGroupsAsync();

        //Reset initialized statue on failure so we can retry later 
        _isChannelGroupsInitialized = result.Success && result.Result != null && result.Result.Count > 0;
        if (!_isChannelGroupsInitialized)
          return;

        var channelGroups = result.Result;
        RegisterRestrictions(channelGroups);

        InitGroup(_channelsTv, _channelTvGroups, FilterGroups(channelGroups, MediaType.TV));
        InitGroup(_channelsRadio, _channelRadioGroups, FilterGroups(channelGroups, MediaType.Radio));
      }
    }

    private void InitGroup(NavigationList<IChannel> channelList, NavigationList<IChannelGroup> groupList, IList<IChannelGroup> serverGroups)
    {
      channelList?.Clear();

      groupList.AddRange(serverGroups);
      groupList.FireListChanged();
    }

    private void InitSelectedGroup(NavigationList<IChannelGroup> groupList, bool isRadio, ref bool isGroupSet)
    {
      if (isGroupSet)
        return;

      var tvHandler = ServiceRegistration.Get<ITvHandler>(false);
      var selectedChannelGroupId = (isRadio ? tvHandler?.ChannelAndGroupInfo?.SelectedRadioChannelGroupId : tvHandler?.ChannelAndGroupInfo?.SelectedChannelGroupId);
      if (selectedChannelGroupId.HasValue)
      {
        isGroupSet = groupList.MoveTo(group => group.ChannelGroupId == selectedChannelGroupId);
        if (!isGroupSet)
        {
          groupList.CurrentIndex = 0;
          isGroupSet = groupList.CurrentIndex == 0;
        }
      }

      if (isGroupSet)
        groupList.FireCurrentChanged(-1);
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
    private static IList<IChannelGroup> FilterGroups(IList<IChannelGroup> channelGroups, MediaType? type = null)
    {
      IList<IChannelGroup> filteredGroups = new List<IChannelGroup>();
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      bool hideAllChannelsGroup = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>().HideAllChannelsGroup;
      foreach (IChannelGroup channelGroup in channelGroups.Where(g => !hideAllChannelsGroup || g.Name != "All Channels"))
      {
        IUserRestriction restriction = channelGroup as IUserRestriction;
        if (restriction != null && !userManagement.CheckUserAccess(restriction))
          continue;

        //if (channelGroup.Name == "All Channels")
        //  filteredGroups.Add(channelGroup);
        if (!type.HasValue)
          filteredGroups.Add(channelGroup);
        else if (channelGroup.MediaType == type)
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
    private async void ReloadTvChannels(int oldindex, int newindex)
    {
      await ReloadChannelsAsync(_channelsTv, _channelTvGroups, MediaType.TV, oldindex, newindex);
    }
    private async void ReloadRadioChannels(int oldindex, int newindex)
    {
      await ReloadChannelsAsync(_channelsRadio, _channelRadioGroups, MediaType.Radio, oldindex, newindex);
    }

    private async Task ReloadChannelsAsync(NavigationList<IChannel> channelList, NavigationList<IChannelGroup> groupList, MediaType? type, int oldindex, int newindex)
    {
      if (groupList?.Current == null)
        return;

      SlimTvClientSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
      var tvHandler = ServiceRegistration.Get<ITvHandler>(false);
      if (tvHandler != null)
      {
        if (type == MediaType.Radio)
          tvHandler.ChannelAndGroupInfo.SelectedRadioChannelGroupId = groupList.Current.ChannelGroupId;
        else
          tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId = groupList.Current.ChannelGroupId;

        int? selectedChannelId;
        if (type == MediaType.Radio)
          selectedChannelId = tvHandler.ChannelAndGroupInfo?.SelectedRadioChannelId;
        else
          selectedChannelId = tvHandler.ChannelAndGroupInfo?.SelectedChannelId;

        IList<IChannel> channels = null;
        var result = await tvHandler.ChannelAndGroupInfo.GetChannelsAsync(groupList.Current);
        if (!result.Success)
          return;

        channels = result.Result;
        if (type == MediaType.TV)
          channels = channels.Where(c => c.MediaType != MediaType.Radio).ToList();
        else if (type == MediaType.Radio)
          channels = channels.Where(c => c.MediaType != MediaType.TV).ToList();

        channelList.ClearAndReset();
        channelList.AddRange(channels);
        channelList.FireListChanged();
        // Check user zapping setting for channel index vs. number preference
        if (settings.ZapByChannelIndex)
        {
          for (int i = 0; i < channelList.Count; i++)
            channelList[i].ChannelNumber = i + 1;
        }

        // Check if the current channel is part of new group and select it
        if (selectedChannelId.HasValue)
          channelList.MoveTo(channel => channel.ChannelId == selectedChannelId);
      }
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

    private int _current;

    public void ClearAndReset()
    {
      Clear();
      _current = 0;
    }

    public T Current
    {
      get { return _current >= 0 && Count > 0 && _current < Count ? this[_current] : default(T); }
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
