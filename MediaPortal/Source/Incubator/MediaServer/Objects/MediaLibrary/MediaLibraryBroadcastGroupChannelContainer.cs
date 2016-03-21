#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryBroadcastGroupChannelContainer : BasicContainer
  {
    public MediaLibraryBroadcastGroupChannelContainer(string id, string title, EndPointSettings client)
      : base(id, client)
    {
      Title = title;
    }

    public IList<IChannel> GetItems()
    {
      if (ServiceRegistration.IsRegistered<ITvProvider>())
      {
        IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

        IChannelGroup group = new ChannelGroup() { ChannelGroupId = GroupId, MediaType = GroupMediaType };
        IList<IChannel> channels = new List<IChannel>();
        if (channelAndGroupInfo.GetChannels(group, out channels))
        {
          return channels;
        }
      }
      return null;
    }

    public override void Initialise()
    {
      IList<IChannel> items = GetItems();

      foreach (var item in items)
      {
        if (item.VisibleInGuide == false) continue;

        string title = item.Name;
        string key = "CHANNEL_CONTAINER_" + item.ChannelId;

        MediaLibraryBroadcastChannelContainer container = new MediaLibraryBroadcastChannelContainer(key, title, Client);
        container.ChannelId = item.ChannelId;

        Add(container);
      }
      Sort();
    }

    public int GroupId { get; set; }

    public MediaType GroupMediaType { get; set; }
  }
}
