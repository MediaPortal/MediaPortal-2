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
using MediaPortal.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses
{
  class BaseChannelDetailed : BaseProgramDetailed
  {
    internal static WebChannelDetailed ChannelDetailed(IChannel channel)
    {
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;

      var programs = programInfo.GetNowNextProgramAsync(channel).Result;
      WebChannelBasic webChannelBasic = BaseChannelBasic.ChannelBasic(channel);

      WebChannelDetailed webChannelDetailed = new WebChannelDetailed
      {
        // From Basic
        Id = webChannelBasic.Id,
        IsRadio = webChannelBasic.IsRadio,
        IsTv = webChannelBasic.IsTv,
        Title = webChannelBasic.Title,

        CurrentProgram = ProgramDetailed(programs.Result[0]),
        NextProgram = ProgramDetailed(programs.Result[1]),
        EpgHasGaps = channel.EpgHasGaps,
        ExternalId = channel.ExternalId,
        GrabEpg = channel.GrapEpg,
        GroupNames = channel.GroupNames,
        LastGrabTime = channel.LastGrabTime ?? DateTime.Now,
        TimesWatched = channel.TimesWatched,
        TotalTimeWatched = channel.TotalTimeWatched ?? DateTime.Now,
        VisibleInGuide = channel.VisibleInGuide,
      };

      return webChannelDetailed;
    }
  }
}
