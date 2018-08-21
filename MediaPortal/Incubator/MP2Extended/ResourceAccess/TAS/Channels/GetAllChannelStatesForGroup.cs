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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Channels
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "userName", Type = typeof(string), Nullable = false)]
  internal class GetAllChannelStatesForGroup
  {
    public async Task<IList<WebChannelState>> ProcessAsync(IOwinContext context, int groupId, string userName)
    {

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetAllChannelStatesForGroup: ITvProvider not found");

      if (userName == String.Empty)
        throw new BadRequestException("GetAllChannelStatesForGroup: userName is null");

      List<WebChannelState> output = new List<WebChannelState>();
      IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
      IChannelGroup channelGroup = new ChannelGroup { ChannelGroupId = groupId };
      var channels = await channelAndGroupInfo.GetChannelsAsync(channelGroup);
      if (!channels.Success)
        throw new BadRequestException(string.Format("GetAllChannelStatesForGroup: Couldn't get channels for group: {0}", groupId));

      foreach (var channel in channels.Result)
      {
        output.Add(new WebChannelState
        {
          ChannelId = channel.ChannelId,
          State = ChannelState.Tunable // TODO: implement in SlimTv
        });
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
