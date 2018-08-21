﻿#region Copyright (C) 2007-2017 Team MediaPortal

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
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "startTime", Type = typeof(DateTime), Nullable = false)]
  [ApiFunctionParam(Name = "endTime", Type = typeof(DateTime), Nullable = false)]
  internal class GetProgramsBasicForGroup : BaseProgramBasic
  {
    public async Task<IList<WebChannelPrograms<WebProgramBasic>>> ProcessAsync(IOwinContext context, int groupId, DateTime startTime, DateTime endTime)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetProgramsBasicForGroup: ITvProvider not found");

      var programs = await TVAccess.GetGroupProgramsAsync(context, startTime, endTime, groupId);
      if (programs.Count == 0)
        Logger.Warn("GetProgramsDetailedForGroup: Couldn't get Now/Next Info for channel with Id: {0}", groupId);

      List<WebChannelPrograms<WebProgramBasic>> output = new List<WebChannelPrograms<WebProgramBasic>>();
      foreach (var program in programs)
      {
        if (output.FindIndex(x => x.ChannelId == program.ChannelId) == -1)
          output.Add(new WebChannelPrograms<WebProgramBasic>
          {
            ChannelId = program.ChannelId,
            Programs = programs.Select(y => ProgramBasic(y)).Where(x => x.ChannelId == program.ChannelId).ToList()
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
