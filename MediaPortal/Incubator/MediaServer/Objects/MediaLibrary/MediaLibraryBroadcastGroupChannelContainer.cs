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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryBroadcastGroupChannelContainer : BasicContainer
  {
    public MediaLibraryBroadcastGroupChannelContainer(string id, string title, EndPointSettings client)
      : base(id, client)
    {
      Title = title;
    }

    public IList<IChannel> GetItems(string sortCriteria)
    {
      if (ServiceRegistration.IsRegistered<ITvProvider>())
      {
        IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
        IChannelGroup group = new ChannelGroup() { ChannelGroupId = GroupId };
        var res = channelAndGroupInfo.GetChannelsAsync(group).Result;
        if (res.Success)
        {
          return res.Result.OrderBy(c => c.Name).ToList();
        }
      }
      return null;
    }

    public override void Initialise(string sortCriteria, uint? offset = null, uint? count = null)
    {
      base.Initialise(sortCriteria, offset, count);

      IList<IChannel> items = GetItems(sortCriteria);
      foreach (var item in items.OrderBy(c => c.Name))
      {
        string title = item.Name;
        string key = "CHANNEL_CONTAINER_" + item.ChannelId;

        MediaLibraryBroadcastChannelContainer container = new MediaLibraryBroadcastChannelContainer(key, title, Client);
        container.ChannelId = item.ChannelId;

        Add(container);
      }
    }

    public int GroupId { get; set; }
  }
}
