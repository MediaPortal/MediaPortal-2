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
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Common.Localization;
using MediaPortal.Utilities;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryBroadcastGroupContainer : BasicContainer
  {
    public MediaLibraryBroadcastGroupContainer(string id, EndPointSettings client)
      : base(id, client)
    {
    }

    public IList<IChannelGroup> GetItems()
    {
      if (ServiceRegistration.IsRegistered<ITvProvider>())
      {
        IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

        IList<IChannelGroup> channelGroups = new List<IChannelGroup>();
        channelAndGroupInfo.GetChannelGroups(out channelGroups);
        return channelGroups;
      }
      return null;
    }

    public override void Initialise()
    {
      const string RES_TV = "[MediaServer.TV]";
      const string RES_RADIO = "[MediaServer.Radio]";

      ILocalization language = ServiceRegistration.Get<ILocalization>();

      IList<IChannelGroup> items = GetItems();

      foreach (IChannelGroup item in items)
      {
        string title = item.Name;
        if (item.MediaType == MediaType.TV) title += string.Format(" ({0})", StringUtils.TrimToNull(language.ToString(RES_TV)) ?? "TV");
        else if (item.MediaType == MediaType.Radio) title += string.Format(" ({0})", StringUtils.TrimToNull(language.ToString(RES_RADIO)) ?? "Radio");
        string key = "BROADCAST_GROUP_" + item.ChannelGroupId;

        MediaLibraryBroadcastGroupChannelContainer container = new MediaLibraryBroadcastGroupChannelContainer(key, title, Client);
        container.GroupId = item.ChannelGroupId;
        container.GroupMediaType = item.MediaType;

        Add(container);
      }
      Sort();
    }
  }
}
