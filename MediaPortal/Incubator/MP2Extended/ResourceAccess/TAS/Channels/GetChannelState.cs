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
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Channels
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "channelId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "userName", Type = typeof(string), Nullable = false)]
  internal class GetChannelState
  {
    public static async Task<WebChannelState> ProcessAsync(IOwinContext context, string channelId, string userName)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetChannelState: ITvProvider not found");

      if (channelId == null)
        throw new BadRequestException("GetChannelState: channelId is null");
      if (userName == null)
        throw new BadRequestException("GetChannelState: userName is null");

      IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
      var channel = await channelAndGroupInfo.GetChannelAsync(int.Parse(channelId));
      if (!channel.Success)
        throw new BadRequestException(string.Format("GetChannelState: Couldn't get channel with id: {0}", channelId));

      WebChannelState webChannelState = new WebChannelState
      {
        ChannelId = channel.Result.ChannelId,
        State = ChannelState.Tunable // TODO: implement in SlimTv
      };

      return webChannelState;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
