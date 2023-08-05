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

using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Client.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Services.UserManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// Helper class to store channel groups and channels in a common place for all models.
  /// </summary>
  public class ChannelContext : IDisposable
  {
    protected static Lazy<ChannelContext> _instance = new Lazy<ChannelContext>(true);

    protected IReadOnlyList<IChannelGroup> _groups;
    protected ChannelNavigation _tvNavigation;
    protected ChannelNavigation _radioNavigation;

    private UserMessageHandler _userMessageHandler;

    protected readonly object _syncObj = new object();

    #region Static instance

    public static ChannelContext Instance
    {
      get { return _instance.Value; } 
    }

    #endregion

    public ChannelContext()
    {
      _groups = new List<IChannelGroup>().AsReadOnly();
      _tvNavigation = new ChannelNavigation(MediaType.TV);
      _tvNavigation.ChannelGroups.OnCurrentChanged += async (o, n) => await UpdateChannels(_tvNavigation); 
      _radioNavigation = new ChannelNavigation(MediaType.Radio);
      _radioNavigation.ChannelGroups.OnCurrentChanged += async (o, n) => await UpdateChannels(_radioNavigation);

      _userMessageHandler = new UserMessageHandler();
      _userMessageHandler.RequestRestrictions += OnRegisterRestrictions;
      _userMessageHandler.UserChanged += OnUserChanged;
    }

    /// <summary>
    /// Gets all channel groups.
    /// </summary>
    public IReadOnlyList<IChannelGroup> ChannelGroups
    {
      get
      {
        InitChannelGroups(false).Wait();
        return _groups;
      }
    }

    /// <summary>
    /// Gets the current Tv <see cref="ChannelNavigation"/>. This allows all models to access one common group and channel lists.
    /// </summary>
    public ChannelNavigation Tv
    {
      get
      {
        InitChannelGroups(false).Wait();
        return _tvNavigation;
      }
    }

    /// <summary>
    /// Gets the current radio <see cref="ChannelNavigation"/>. This allows all models to access one common group and channel lists.
    /// </summary>
    public ChannelNavigation Radio
    {
      get
      {
        InitChannelGroups(false).Wait();
        return _radioNavigation;
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

    public async Task InitChannelGroups(bool force)
    {
      if (!force && _groups.Count > 0)
        return;

      var groupInfo = ServiceRegistration.Get<ITvHandler>(false)?.ChannelAndGroupInfo;
      IList<IChannelGroup> channelGroups = await GetChannelGroups(groupInfo) ?? new List<IChannelGroup>()
        ;
      bool tvChanged;
      bool radioChanged;
      lock (_syncObj)
      {
        _groups = new ReadOnlyCollection<IChannelGroup>(channelGroups);
        tvChanged = _tvNavigation.ChannelGroups.SetItems(channelGroups.Where(g => g.MediaType != MediaType.Radio), g => g.ChannelGroupId == groupInfo?.SelectedChannelGroupId);
        radioChanged = _radioNavigation.ChannelGroups.SetItems(channelGroups.Where(g => g.MediaType == MediaType.Radio), g => g.ChannelGroupId == groupInfo?.SelectedRadioChannelGroupId);
      }
      if (tvChanged)
        _tvNavigation.ChannelGroups.FireListChanged();
      if (radioChanged)
        _radioNavigation.ChannelGroups.FireListChanged();
    }

    protected async Task UpdateChannels(ChannelNavigation channelNavigation)
    {
      var channelInfo = ServiceRegistration.Get<ITvHandler>(false)?.ChannelAndGroupInfo;
      IChannelGroup group = channelNavigation.ChannelGroups.Current;

      IList<IChannel> channels = await GetChannels(channelInfo, group) ?? new List<IChannel>();

      bool channelsChanged;
      lock(_syncObj)
      {
        if (group != channelNavigation.ChannelGroups.Current)
          return;
        int? currentChannel = channelNavigation.MediaType == MediaType.Radio ? channelInfo?.SelectedRadioChannelId : channelInfo?.SelectedChannelId;
        channelsChanged = channelNavigation.Channels.SetItems(channels, c => c.ChannelId == currentChannel);
      }
      if(channelsChanged)
        channelNavigation.Channels.FireListChanged();
    }
    
    private static async Task<IList<IChannelGroup>> GetChannelGroups(IChannelAndGroupInfoAsync groupInfo)
    {
      if (groupInfo == null)
        return null;

      var groupResult = await groupInfo.GetChannelGroupsAsync();
      if (!groupResult.Success || groupResult.Result == null || groupResult.Result.Count == 0)
        return null;
      IList<IChannelGroup> groups = new List<IChannelGroup>(groupResult.Result);
      RegisterRestrictions(groups);
      return FilterGroups(groups);
    }

    private static async Task<IList<IChannel>> GetChannels(IChannelAndGroupInfoAsync channelInfo, IChannelGroup channelGroup)
    {
      if (channelInfo == null || channelGroup == null)
        return null;

      var channelResult = await channelInfo.GetChannelsAsync(channelGroup);
      if (!channelResult.Success || channelResult.Result == null || channelResult.Result.Count == 0)
        return null;

      IList<IChannel> channels = new List<IChannel>(channelResult.Result);
      // Check user zapping setting for channel index vs. number preferance
      SlimTvClientSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
      if (settings.ZapByChannelIndex)
      {
        for (int i = 0; i < channels.Count; i++)
          channels[i].ChannelNumber = i + 1;
      }
      return channels;
    }

    /// <summary>
    /// Registers known channel groups so it can be used to restrict users.
    /// </summary>
    /// <param name="channelGroups"></param>
    private static void RegisterRestrictions(IList<IChannelGroup> channelGroups)
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
      List<IChannelGroup> filteredGroups = new List<IChannelGroup>();
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
      InitChannelGroups(true).Wait();
    }

    private void OnRegisterRestrictions(object sender, EventArgs e)
    {
      InitChannelGroups(true).Wait();
    }

    public void Dispose()
    {
      _userMessageHandler?.Dispose();
    }
  }

  public class ChannelNavigation
  {
    protected NavigationList<IChannelGroup> _channelGroups;
    protected NavigationList<IChannel> _channels;
    protected MediaType _mediaType;

    public ChannelNavigation(MediaType mediaType)
    {
      _mediaType = mediaType;
      _channelGroups = new NavigationList<IChannelGroup>();
      _channels = new NavigationList<IChannel>();
    }

    public MediaType MediaType
    {
      get { return _mediaType; }
    }

    public NavigationList<IChannelGroup> ChannelGroups
    {
      get { return _channelGroups; }
    }

    public NavigationList<IChannel> Channels
    {
      get { return _channels; }
    }
  }
}
