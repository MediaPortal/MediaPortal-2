#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.Commands;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.MediaLists;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public class SlimTvLastWatchedChannelMediaListProvider : IMediaListProvider
  {
    public SlimTvLastWatchedChannelMediaListProvider()
    {
      AllItems = new ItemsList();
    }

    public ItemsList AllItems { get; private set; }

    public bool UpdateItems(int maxItems)
    {
      var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory == null)
        return false;

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      }

      AllItems.Clear();
      IEnumerable<Tuple<int, string>> channelList;
      if (userProfile.HasValue && userProfileDataManagement.UserProfileDataManagement.GetUserAdditionalDataList(userProfile.Value, UserDataKeysKnown.KEY_CHANNEL_PLAY_DATE, out channelList))
      {
        int count = 0;
        IOrderedEnumerable<Tuple<int, string>> sortedList = channelList.OrderByDescending(c => DateTime.Parse(c.Item2)); //Order by play date
        foreach (var channelData in sortedList)
        {
          IChannel channel = ChannelContext.Instance.Channels.FirstOrDefault(c => c.ChannelId == channelData.Item1);
          if (channel != null)
          {
            count++;
            ChannelProgramListItem item = new ChannelProgramListItem(channel, null)
            {
              Command = new MethodDelegateCommand(() => SlimTvModelBase.TuneChannel(channel)),
            };
            item.AdditionalProperties["CHANNEL"] = channel;
            AllItems.Add(item);
          }
          if (count >= maxItems)
            break;
        }
      }
      AllItems.FireChange();

      return true;
    }
  }
}
