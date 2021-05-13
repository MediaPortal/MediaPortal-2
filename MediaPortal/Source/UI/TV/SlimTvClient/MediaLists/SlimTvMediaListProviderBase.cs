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

using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.MediaLists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.UserManagement;
using MediaPortal.UI.ContentLists;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public abstract class SlimTvMediaListProviderBase : IContentListProvider
  {
    protected MediaType? _mediaType;
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

    public abstract Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason, ICollection<object> updatedObjects);

    protected bool ShouldUpdate(UpdateReason updateReason, ICollection<object> updatedObjects)
    {
      if (updateReason.HasFlag(UpdateReason.Forced) || updateReason.HasFlag(UpdateReason.UserChanged))
        return true;

      bool update = false;
      if (updateReason.HasFlag(UpdateReason.PlaybackComplete))
      {
        if (updatedObjects?.Count > 0)
        {
          foreach (MediaItem item in updatedObjects)
          {
            if (!item.GetPlayData(out string mimeType, out string title))
              continue;
            if (_mediaType == null && (mimeType == LiveTvMediaItem.MIME_TYPE_TV || mimeType == LiveTvMediaItem.MIME_TYPE_RADIO))
            {
              update = true;
              break;
            }
            else if (_mediaType == MediaType.TV && mimeType == LiveTvMediaItem.MIME_TYPE_TV)
            {
              update = true;
              break;
            }
            else if (_mediaType == MediaType.Radio && mimeType == LiveTvMediaItem.MIME_TYPE_RADIO)
            {
              update = true;
              break;
            }
          }
        }
        else
        {
          update = true;
        }
      }

      return update;
    }
    
    protected ListItem CreateChannelItem(IChannel channel)
    {
      ChannelProgramListItem item = new ChannelProgramListItem(channel, null)
      {
        Command = new AsyncMethodDelegateCommand(() => SlimTvModelBase.TuneChannel(channel)),
      };
      item.AdditionalProperties["CHANNEL"] = channel;
      return item;
    }

    protected bool TryInitTvHandler()
    {
      if (_tvHandler != null)
        return true;
      ITvProvider provider = ServiceRegistration.Get<ITvProvider>(false);
      if (provider == null)
        return false;
      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      tvHandler.Initialize();
      if (tvHandler.ChannelAndGroupInfo == null)
        return false;
      _tvHandler = tvHandler;
      return true;
    }

    protected async Task<IList<IChannel>> GetUserChannelList(int maxItems, string userDataKey, bool fillList = false)
    {
      IList<IChannel> userChannels = new List<IChannel>();

      if (!TryInitTvHandler())
        return userChannels;

      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement == null || !userProfileDataManagement.IsValidUser)
        return userChannels;

      Guid userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      var userResult = await userProfileDataManagement.UserProfileDataManagement.GetUserAdditionalDataListAsync(userProfile, userDataKey, true, SortDirection.Descending);
      if (!userResult.Success)
        return userChannels;

      IEnumerable<Tuple<int, string>> channelList = userResult.Result;

      //Add favorite channels first
      var channelAndGroupInfo = _tvHandler.ChannelAndGroupInfo;
      foreach (int channelId in channelList.Select(c => c.Item1))
      {
        var result = await channelAndGroupInfo.GetChannelAsync(channelId);
        if (result.Success && (result.Result.MediaType == _mediaType || _mediaType == null))
          userChannels.Add(result.Result);
        if (userChannels.Count >= maxItems)
          break;
      }

      //Add any remaining channels
      if (fillList && userChannels.Count < maxItems)
      {
        foreach (int channelId in ChannelContext.Instance.Channels.Where(c => c.MediaType == _mediaType || _mediaType == null).Select(c => c.ChannelId).Except(channelList.Select(c => c.Item1)))
        {
          var result = await channelAndGroupInfo.GetChannelAsync(channelId);
          if (result.Success)
            userChannels.Add(result.Result);
          if (userChannels.Count >= maxItems)
            break;
        }
      }
      return userChannels;
    }
  }
}
