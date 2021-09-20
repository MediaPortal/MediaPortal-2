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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "channelId", Type = typeof(string), Nullable = false)]
  internal class GetNowNextWebProgramDetailedForChannel : BaseProgramDetailed
  {
    public static async Task<IList<WebProgramDetailed>> ProcessAsync(IOwinContext context, string channelId)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetNowNextWebProgramDetailedForChannel: ITvProvider not found");

      var programs = await TVAccess.GetChannelNowNextProgramAsync(context, int.Parse(channelId));
      if (programs == null)
        throw new NotFoundException(string.Format("GetNowNextWebProgramDetailedForChannel: Couldn't get Now/Next Info for channel with Id: {0}", channelId));

      List<WebProgramDetailed> output = new List<WebProgramDetailed>
      {
        ProgramDetailed(programs[0]),
        ProgramDetailed(programs[1])
      };

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
