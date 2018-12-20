#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UiComponents.Media.MediaLists;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public class BaseLastWatchedChannelMediaListProvider : SlimTvMediaListProviderBase
  {
    protected ICollection<IChannel> _currentChannels = new List<IChannel>();

    public override async Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason)
    {
      if (!updateReason.HasFlag(UpdateReason.Forced) && !updateReason.HasFlag(UpdateReason.PlaybackComplete))
        return true;
      
      IList<IChannel> channels = await GetUserChannelList(maxItems, UserDataKeysKnown.KEY_CHANNEL_PLAY_DATE);
      lock (_allItems.SyncRoot)
      {
        if (_currentChannels.Select(c => c.ChannelId).SequenceEqual(channels.Select(c => c.ChannelId)))
          return true;
        _currentChannels = channels;
        _allItems.Clear();
        foreach (IChannel channel in channels)
          _allItems.Add(CreateChannelItem(channel));
      }
      _allItems.FireChange();
      return true;
    }
  }
}
