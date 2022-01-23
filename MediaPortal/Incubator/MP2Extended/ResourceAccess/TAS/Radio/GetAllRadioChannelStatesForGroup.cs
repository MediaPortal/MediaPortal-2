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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Radio
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(string), Nullable = true)]
  internal class GetAllRadioChannelStatesForGroup : BaseChannelBasic
  {
    public static async Task<IList<WebChannelState>> ProcessAsync(IOwinContext context, string groupId, string userName)
    {
      List<WebChannelState> output = new List<WebChannelState>();

      if (groupId == null)
        throw new BadRequestException("GetAllRadioChannelStatesForGroup: groupId is null");
      if (userName == null)
        throw new BadRequestException("GetAllRadioChannelStatesForGroup: userName is null");

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetAllRadioChannelStatesForGroup: ITvProvider not found");

      var channels = await TVAccess.GetGroupChannelsAsync(context, groupId != null ? int.Parse(groupId) : (int?)null);
      output.AddRange(channels.Where(x => x.MediaType == MediaType.Radio).Select(channel => new WebChannelState
      {
        ChannelId = channel.ChannelId,
        State = ChannelState.Tunable, // TODO: implement in SlimTv
      }));

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
