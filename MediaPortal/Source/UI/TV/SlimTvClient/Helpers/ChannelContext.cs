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
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// Helper class to store channel groups and channels in a common place for all models.
  /// </summary>
  public class ChannelContext : IDisposable
  {
    protected static Lazy<ChannelContext> _tvContext = new Lazy<ChannelContext>(() => new ChannelContext(MediaType.TV), true);
    protected static Lazy<ChannelContext> _radioContext = new Lazy<ChannelContext>(() => new ChannelContext(MediaType.Radio), true);
    protected static ChannelContext _channelContext;

    private UserMessageHandler _userMessageHandler;
    protected bool _isChannelGroupsInitialized = false;
    protected NavigationList<IChannelGroup> _channelGroups;

    protected MediaType _mediaType;
    protected readonly object _channelSyncObj = new object(); 

    #region Static instance

    /// <summary>
    /// Gets the current Tv <see cref="ChannelContext"/> from the <see cref="ServiceRegistration"/>. This allows all models to access one common group and channel lists.
    /// </summary>
    public static ChannelContext Tv
    {
      get { return _tvContext.Value; }
    }

    /// <summary>
    /// Gets the current radio <see cref="ChannelContext"/> from the <see cref="ServiceRegistration"/>. This allows all models to access one common group and channel lists.
    /// </summary>
    public static ChannelContext Radio
    {
      get { return _radioContext.Value; }
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

    public ChannelContext(MediaType mediaType)
    {
      _mediaType = mediaType;
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
        var result = await tvHandler.ChannelAndGroupInfo.GetChannelGroupsAsync();

        //Reset initialized statue on failure so we can retry later 
        _isChannelGroupsInitialized = result.Success && result.Result != null && result.Result.Count > 0;
        if (!_isChannelGroupsInitialized)
        {
          _channelGroups.SetItems(null, -1);
          return;
        }

        var channelGroups = result.Result.Where(g => g.MediaType == _mediaType).ToList();
        RegisterRestrictions(channelGroups);
        channelGroups = FilterGroups(channelGroups);
        int selectedGroupId = _mediaType == MediaType.TV ? tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId : tvHandler.ChannelAndGroupInfo.SelectedRadioChannelGroupId;
        int selectedIndex = channelGroups.FindIndex(g => g.ChannelGroupId == selectedGroupId);
        _channelGroups.SetItems(channelGroups, selectedIndex);
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
    private static List<IChannelGroup> FilterGroups(IList<IChannelGroup> channelGroups)
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
      if (tvHandler == null || tvHandler.ChannelAndGroupInfo == null)
        return;

      IChannelGroup currentGroup;
      lock (_channelSyncObj)
      {
        // Checked again before updating the channels list to ensure the
        // selected group hasn't been changed by another thread
        currentGroup = _channelGroups.Current;
        // Set the channels to empty whilst the new channels are loaded
        Channels.SetItems(null, -1, false);
      }
      Channels.FireListChanged();

      var result = await tvHandler.ChannelAndGroupInfo.GetChannelsAsync(currentGroup);
      if (!result.Success)
        return;

      List<IChannel> channels = result.Result.ToList();

      // Check user zapping setting for channel index vs. number preferance
      SlimTvClientSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
      if (settings.ZapByChannelIndex)
      {
        for (int i = 0; i < channels.Count; i++)
          channels[i].ChannelNumber = i + 1;
      }

      // Check if the current channel is part of new group and select it
      int selectedChannelId = _mediaType == MediaType.TV ? tvHandler.ChannelAndGroupInfo.SelectedChannelId : tvHandler.ChannelAndGroupInfo.SelectedRadioChannelId;
      int selectedIndex = channels.FindIndex(c => c.ChannelId == selectedChannelId);
      lock (_channelSyncObj)
      {
        // If another thread has changed the current group in the meantime
        // don't update the channels as they are not valid for the new group
        if (_channelGroups.Current != currentGroup)
          return;
        Channels.SetItems(channels, selectedIndex, false);
      }
      Channels.FireListChanged();
    }

    public void Dispose()
    {
      _userMessageHandler?.Dispose();
    }
  }
}
