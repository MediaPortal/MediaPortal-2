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

using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  public abstract class SlimTvScheduleRuleManagementBase : SlimTvModelBase
  {
    #region Fields

    protected Dictionary<int, IChannel> _channels = new Dictionary<int, IChannel>();
    protected Dictionary<int, IChannelGroup> _channelGroups = new Dictionary<int, IChannelGroup>();
    protected bool _loadChannels = true;

    #endregion

    #region GUI properties and methods

    public void RecordMenu()
    {
      ListItem item = SlimTvExtScheduleModel.CurrentItem;
      if(item != null)
        ShowActions(item);
    }

    #endregion

    protected async Task LoadChannels()
    {
      if (!_loadChannels)
        return;

      _channels.Clear();
      var channelsResult = await _tvHandler.ChannelAndGroupInfo.GetChannelsAsync();
      if (channelsResult.Success)
      {
        foreach (var channel in channelsResult.Result)
          _channels[channel.ChannelId] = channel;
      }

      _channelGroups.Clear();
      var channelGroupsResult = await _tvHandler.ChannelAndGroupInfo.GetChannelGroupsAsync();
      if (channelGroupsResult.Success)
      {
        foreach (var channelGroup in channelGroupsResult.Result)
          _channelGroups[channelGroup.ChannelGroupId] = channelGroup;
      }

      _loadChannels = false;
    }

    protected abstract void ShowActions(ListItem item);
  }
}
