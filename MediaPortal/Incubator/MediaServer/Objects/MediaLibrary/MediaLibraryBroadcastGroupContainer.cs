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
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Common.Localization;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryBroadcastGroupContainer : BasicContainer
  {
    public MediaLibraryBroadcastGroupContainer(string id, EndPointSettings client)
      : base(id, client)
    {
    }

    public IList<IChannelGroup> GetItems(string sortCriteria)
    {
      if (ServiceRegistration.IsRegistered<ITvProvider>())
      {
        IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;

        IList<IChannelGroup> channelGroups = new List<IChannelGroup>();
        var result = channelAndGroupInfo.GetChannelGroupsAsync().Result;
        if (result.Success)
        {
          result.Result = FilterGroups(result.Result);
          return result.Result.OrderBy(g => g.Name).ToList();
        }
      }
      return null;
    }

    public override void Initialise(string sortCriteria, uint? offset = null, uint? count = null)
    {
      base.Initialise(sortCriteria, offset, count);

      ILocalization language = ServiceRegistration.Get<ILocalization>();
      IList<IChannelGroup> items = GetItems(sortCriteria);

      foreach (IChannelGroup item in items.OrderBy(g => g.Name))
      {
        string title = item.Name;
        string key = "BROADCAST_GROUP_" + item.ChannelGroupId;

        MediaLibraryBroadcastGroupChannelContainer container = new MediaLibraryBroadcastGroupChannelContainer(key, title, Client);
        container.GroupId = item.ChannelGroupId;

        Add(container);
      }
    }
  }
}
