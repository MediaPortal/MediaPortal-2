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

using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.UPnP;
using MediaPortal.Plugins.SlimTv.UPnP.DataTypes;
using MediaPortal.Plugins.SlimTv.UPnP.Items;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Plugins.SlimTv.Service.UPnP
{
  public class SlimTvServiceImpl : DvService
  {
    public SlimTvServiceImpl()
      : base(Consts.SLIMTV_SERVICE_TYPE, Consts.SLIMTV_SERVICE_TYPE_VERSION, Consts.SLIMTV_SERVICE_ID)
    {
      #region DvStateVariable definitions

      DvStateVariable A_ARG_TYPE_SlotIndex = new DvStateVariable("A_ARG_TYPE_SlotIndex", new DvStandardDataType(UPnPStandardDataType.Int)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_SlotIndex);

      DvStateVariable A_ARG_TYPE_ChannelId = new DvStateVariable("A_ARG_TYPE_ChannelId", new DvStandardDataType(UPnPStandardDataType.Int)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_ChannelId);

      DvStateVariable A_ARG_TYPE_ChannelGroupId = new DvStateVariable("A_ARG_TYPE_ChannelGroupId", new DvStandardDataType(UPnPStandardDataType.Int)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_ChannelGroupId);

      DvStateVariable A_ARG_TYPE_Bool = new DvStateVariable("A_ARG_TYPE_Bool", new DvStandardDataType(UPnPStandardDataType.Boolean)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_Bool);

      DvStateVariable A_ARG_TYPE_DateTime = new DvStateVariable("A_ARG_TYPE_DateTime", new DvStandardDataType(UPnPStandardDataType.DateTime)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_DateTime);

      DvStateVariable A_ARG_TYPE_TimeshiftSource = new DvStateVariable("A_ARG_TYPE_TimeshiftSource", new DvStandardDataType(UPnPStandardDataType.String)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_TimeshiftSource);

      DvStateVariable A_ARG_TYPE_ChannelGroups = new DvStateVariable("A_ARG_TYPE_ChannelGroups", new DvExtendedDataType(UPnPDtChannelGroupList.Instance)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_ChannelGroups);

      DvStateVariable A_ARG_TYPE_Channels = new DvStateVariable("A_ARG_TYPE_Channels", new DvExtendedDataType(UPnPDtChannelList.Instance)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_Channels);

      DvStateVariable A_ARG_TYPE_Programs = new DvStateVariable("A_ARG_TYPE_Programs", new DvExtendedDataType(UPnPDtProgramList.Instance)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_Programs);

      #endregion

      #region ITimeshiftControl actions

      DvAction startTimeshift = new DvAction(Consts.ACTION_START_TIMESHIFT, OnStartTimeshift,
                                   new[]
                                     {
                                       new DvArgument("SlotIndex", A_ARG_TYPE_SlotIndex, ArgumentDirection.In),
                                       new DvArgument("ChannelId", A_ARG_TYPE_ChannelId, ArgumentDirection.In),
                                     },
                                   new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("TimeshiftSource", A_ARG_TYPE_TimeshiftSource, ArgumentDirection.Out, false)
                                     });
      AddAction(startTimeshift);

      DvAction stopTimeshift = new DvAction(Consts.ACTION_STOP_TIMESHIFT, OnStopTimeshift,
                                   new[]
                                     {
                                       new DvArgument("SlotIndex", A_ARG_TYPE_SlotIndex, ArgumentDirection.In)
                                     },
                                   new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
                                     });
      AddAction(stopTimeshift);

      #endregion

      #region IChannelAndGroupInfo actions

      DvAction getChannelGroups = new DvAction(Consts.ACTION_GET_CHANNELGROUPS, OnGetChannelGroups,
                                   new DvArgument[0],
                                   new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("ChannelGroups", A_ARG_TYPE_ChannelGroups, ArgumentDirection.Out, false)
                                     });
      AddAction(getChannelGroups);

      DvAction getChannels = new DvAction(Consts.ACTION_GET_CHANNELS, OnGetChannels,
                                   new[]
                                     {
                                       new DvArgument("ChannelGroupId", A_ARG_TYPE_ChannelGroupId, ArgumentDirection.In)
                                     },
                                   new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Channels", A_ARG_TYPE_Channels, ArgumentDirection.Out, false)
                                     });
      AddAction(getChannels);

      DvAction getPrograms = new DvAction(Consts.ACTION_GET_PROGRAMS, OnGetPrograms,
                             new[]
                                     {
                                       new DvArgument("ChannelId", A_ARG_TYPE_ChannelId, ArgumentDirection.In),
                                       new DvArgument("TimeFrom", A_ARG_TYPE_DateTime, ArgumentDirection.In),
                                       new DvArgument("TimeTo", A_ARG_TYPE_DateTime, ArgumentDirection.In)
                                     },
                             new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Programs", A_ARG_TYPE_Programs, ArgumentDirection.Out, false)
                                     });
      AddAction(getPrograms);

      #endregion
    }

    private UPnPError OnStartTimeshift(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      //ITimeshiftControl timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControl;
      //if (timeshiftControl == null)
      //  return new UPnPError(500, "ITimeshiftControl service not available");

      int slotIndex = (int) inParams[0];
      int channelId = (int) inParams[1];

      // TODO:
      string timeshiftSource = "C:\\temp\\timeshift.ts";
      outParams = new List<object> { true, timeshiftSource };
      return null;
    }

    private UPnPError OnStopTimeshift(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      //ITimeshiftControl timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControl;
      //if (timeshiftControl == null)
      //  return new UPnPError(500, "ITimeshiftControl service not available");

      int slotIndex = (int) inParams[0];

      // TODO:
      outParams = new List<object> { true };
      return null;
    }

    private UPnPError OnGetChannelGroups(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      // TODO:
      List<ChannelGroup> groups = new List<ChannelGroup> { new ChannelGroup { ChannelGroupId = 1, Name = "Native group 1" } };
      outParams = new List<object> { true, groups };
      return null;
    }

    private UPnPError OnGetChannels(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      // TODO:
      List<Channel> channels = new List<Channel> { new Channel { ChannelId = 1, Name = "Native channel 1" } };
      outParams = new List<object> { true, channels };
      return null;
    }

    private UPnPError OnGetPrograms(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      // TODO:
      int channelId = (int) inParams[0];
      DateTime timeFrom = (DateTime) inParams[1];
      DateTime timeTo = (DateTime) inParams[2];

      List<Program> programs = new List<Program>
        {
          new Program { ChannelId = 1, Title = "Program 1", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1)},
          new Program { ChannelId = 1, Title = "Program 2", StartTime = DateTime.Now.AddHours(1), EndTime = DateTime.Now.AddHours(2)}
        };
      outParams = new List<object> { true, programs };
      return null;
    }
  }
}
