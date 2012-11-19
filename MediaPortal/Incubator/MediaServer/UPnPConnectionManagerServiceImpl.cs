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
using System.Linq;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Extensions.MediaServer
{
  public class UPnPConnectionManagerServiceImpl : DvService
  {
    public UPnPConnectionManagerServiceImpl()
      : base(
        UPnPMediaServerDevice.CONNECTION_MANAGER_SERVICE_TYPE,
        UPnPMediaServerDevice.CONNECTION_MANAGER_SERVICE_TYPE_VERSION,
        UPnPMediaServerDevice.CONNECTION_MANAGER_SERVICE_ID)
    {
      // Used for a boolean value
      DvStateVariable SourceProtocolInfo = new DvStateVariable("SourceProtocolInfo",
                                                               new DvStandardDataType(
                                                                 UPnPStandardDataType.String))
                                             {
                                               SendEvents = false,
                                             };
      AddStateVariable(SourceProtocolInfo);

      // Used for a boolean value
      DvStateVariable SinkProtocolInfo = new DvStateVariable("SinkProtocolInfo",
                                                             new DvStandardDataType(
                                                               UPnPStandardDataType.String))
                                           {
                                             SendEvents = false,
                                           };
      AddStateVariable(SinkProtocolInfo);

      // Used for a boolean value
      DvStateVariable CurrentConnectionIDs = new DvStateVariable("CurrentConnectionIDs",
                                                                 new DvStandardDataType(
                                                                   UPnPStandardDataType.String))
                                               {
                                                 SendEvents = false,
                                               };
      AddStateVariable(CurrentConnectionIDs);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_ConnectionStatus = new DvStateVariable("A_ARG_TYPE_ConnectionStatus",
                                                                        new DvStandardDataType(
                                                                          UPnPStandardDataType.String))
                                                      {
                                                        SendEvents = false,
                                                        AllowedValueList =
                                                          new List<string>()
                                                            {
                                                              "OK",
                                                              "ContentFormatMismatch",
                                                              "InsufficientBandwidth",
                                                              "UnreliableChannel",
                                                              "Unknown"
                                                            }
                                                      };
      AddStateVariable(A_ARG_TYPE_ConnectionStatus);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_ConnectionManager = new DvStateVariable("A_ARG_TYPE_ConnectionManager",
                                                                         new DvStandardDataType(
                                                                           UPnPStandardDataType.String))
                                                       {
                                                         SendEvents = false,
                                                       };
      AddStateVariable(A_ARG_TYPE_ConnectionManager);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_Direction = new DvStateVariable("A_ARG_TYPE_Direction",
                                                                 new DvStandardDataType(
                                                                   UPnPStandardDataType.String))
                                               {
                                                 SendEvents = false,
                                                 AllowedValueList = new List<string>() {"Output", "Input"}
                                               };
      AddStateVariable(A_ARG_TYPE_Direction);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_ProtocolInfo = new DvStateVariable("A_ARG_TYPE_ProtocolInfo",
                                                                    new DvStandardDataType(
                                                                      UPnPStandardDataType.String))
                                                  {
                                                    SendEvents = false,
                                                  };
      AddStateVariable(A_ARG_TYPE_ProtocolInfo);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_ConnectionID = new DvStateVariable("A_ARG_TYPE_ConnectionID",
                                                                    new DvStandardDataType(
                                                                      UPnPStandardDataType.I4))
                                                  {
                                                    SendEvents = false,
                                                  };
      AddStateVariable(A_ARG_TYPE_ConnectionID);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_AVTransportID = new DvStateVariable("A_ARG_TYPE_AVTransportID",
                                                                     new DvStandardDataType(
                                                                       UPnPStandardDataType.I4))
                                                   {
                                                     SendEvents = false,
                                                   };
      AddStateVariable(A_ARG_TYPE_AVTransportID);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_RcsID = new DvStateVariable("A_ARG_TYPE_RcsID",
                                                             new DvStandardDataType(
                                                               UPnPStandardDataType.I4))
                                           {
                                             SendEvents = false,
                                           };
      AddStateVariable(A_ARG_TYPE_RcsID);


      DvAction getProtocolInfoAction = new DvAction("GetProtocolInfo", OnGetProtocolInfo,
                                                    new DvArgument[]
                                                      {
                                                      },
                                                    new DvArgument[]
                                                      {
                                                        new DvArgument("Source",
                                                                       SourceProtocolInfo,
                                                                       ArgumentDirection.Out),
                                                        new DvArgument("Sink",
                                                                       SinkProtocolInfo,
                                                                       ArgumentDirection.Out),
                                                      });
      AddAction(getProtocolInfoAction);

      DvAction getCurrentConnectionIDsAction = new DvAction("GetCurrentConnectionIDs", OnGetCurrentConnectionIDs,
                                                            new DvArgument[]
                                                              {
                                                              },
                                                            new DvArgument[]
                                                              {
                                                                new DvArgument("ConnectionIDs",
                                                                               CurrentConnectionIDs,
                                                                               ArgumentDirection.Out),
                                                              });
      AddAction(getCurrentConnectionIDsAction);

      DvAction getCurrentConnectionInfoAction = new DvAction("GetCurrentConnectionInfo", OnGetCurrentConnectionInfo,
                                                             new DvArgument[]
                                                               {
                                                                 new DvArgument("ConnectionID",
                                                                                A_ARG_TYPE_ConnectionID,
                                                                                ArgumentDirection.In),
                                                               },
                                                             new DvArgument[]
                                                               {
                                                                 new DvArgument("RcsID",
                                                                                A_ARG_TYPE_RcsID,
                                                                                ArgumentDirection.Out),
                                                                 new DvArgument("AVTransportID",
                                                                                A_ARG_TYPE_AVTransportID,
                                                                                ArgumentDirection.Out),
                                                                 new DvArgument("ProtocolInfo",
                                                                                A_ARG_TYPE_ProtocolInfo,
                                                                                ArgumentDirection.Out),
                                                                 new DvArgument("PeerConnectionManager",
                                                                                A_ARG_TYPE_ConnectionManager,
                                                                                ArgumentDirection.Out),
                                                                 new DvArgument("PeerConnectionID",
                                                                                A_ARG_TYPE_ConnectionID,
                                                                                ArgumentDirection.Out),
                                                                 new DvArgument("Direction",
                                                                                A_ARG_TYPE_Direction,
                                                                                ArgumentDirection.Out),
                                                                 new DvArgument("Status",
                                                                                A_ARG_TYPE_ConnectionStatus,
                                                                                ArgumentDirection.Out),
                                                               });
      AddAction(getCurrentConnectionInfoAction);
    }

    private static UPnPError OnGetProtocolInfo(DvAction action, IList<object> inParams, out IList<object> outParams,
                                               CallContext context)
    {
      string source = "";
      string sink = "";

      source =
        "http-get:*:audio/L16:*,http-get:*:audio/wav:*,http-get:*:audio/mpeg:*,http-get:*:audio/x-ms-wma:*,http-get:*:audio/L8:*,http-get:*:video/avi:*,http-get:*:video/mpeg:*,http-get:*:video/x-ms-wmv:*,http-get:*:video/x-ms-asf:*,http-get:*:video/x-ms-dvr:*,http-get:*:image/bmp:*,http-get:*:image/gif:*,http-get:*:image/jpeg:*,http-get:*:image/png:*,http-get:*:image/tiff:*,http-get:*:image/x-ycbcr-yuv420:*";
      outParams = new List<object>() {source, sink};
      return null;
    }

    private static UPnPError OnGetCurrentConnectionIDs(DvAction action, IList<object> inParams,
                                                       out IList<object> outParams, CallContext context)
    {
      outParams = null;
      return null;
    }

    private static UPnPError OnGetCurrentConnectionInfo(DvAction action, IList<object> inParams,
                                                        out IList<object> outParams, CallContext context)
    {
      outParams = null;
      return null;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}