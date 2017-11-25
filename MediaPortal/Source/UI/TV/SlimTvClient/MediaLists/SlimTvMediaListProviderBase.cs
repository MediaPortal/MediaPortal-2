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
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.MediaLists;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public abstract class SlimTvMediaListProviderBase : IMediaListProvider
  {
    protected MediaType _mediaType = MediaType.TV;
    protected ITvHandler _tvHandler;
    protected ItemsList _allItems;

    public SlimTvMediaListProviderBase()
    {
      _allItems = new ItemsList();
    }

    public ItemsList AllItems
    {
      get { return _allItems; }
    }

    public abstract bool UpdateItems(int maxItems, UpdateReason updateReason);
    
    protected ListItem CreateChannelItem(IChannel channel)
    {
      ChannelProgramListItem item = new ChannelProgramListItem(channel, null)
      {
        Command = new MethodDelegateCommand(() => SlimTvModelBase.TuneChannel(channel)),
      };
      item.AdditionalProperties["CHANNEL"] = channel;
      return item;
    }

    protected bool TryInitTvHandler()
    {
      if (_tvHandler != null)
        return true;
      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      tvHandler.Initialize();
      if (tvHandler.ChannelAndGroupInfo == null)
        return false;
      _tvHandler = tvHandler;
      return true;
    }

    protected IList<IChannel> GetUserChannelList(int maxItems, string userDataKey)
    {
      IList<IChannel> userChannels = new List<IChannel>();

      if (!TryInitTvHandler())
        return userChannels;

      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement == null || !userProfileDataManagement.IsValidUser)
        return userChannels;

      Guid userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      IEnumerable<Tuple<int, string>> channelList;
      if (!userProfileDataManagement.UserProfileDataManagement.GetUserAdditionalDataList(userProfile, userDataKey,
        out channelList, true, SortDirection.Descending))
        return userChannels;

      foreach (int channelId in channelList.Select(c => c.Item1))
      {
        IChannel channel;
        if (_tvHandler.ChannelAndGroupInfo.GetChannel(channelId, out channel) && channel.MediaType == _mediaType)
          userChannels.Add(channel);
        if (userChannels.Count >= maxItems)
          break;
      }
      return userChannels;
    }
  }
}
