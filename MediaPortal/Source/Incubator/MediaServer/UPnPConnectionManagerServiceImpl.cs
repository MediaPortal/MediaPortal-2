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
using MediaPortal.Common.Logging;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Plugins.MediaServer
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
      DvStateVariable SourceProtocolInfo = new DvStateVariable("SourceProtocolInfo", new DvStandardDataType(UPnPStandardDataType.String))
                                             {
                                               SendEvents = false,
                                             };
      AddStateVariable(SourceProtocolInfo);

      // Used for a boolean value
      DvStateVariable SinkProtocolInfo = new DvStateVariable("SinkProtocolInfo", new DvStandardDataType(UPnPStandardDataType.String))
                                           {
                                             SendEvents = false,
                                           };
      AddStateVariable(SinkProtocolInfo);

      // Used for a boolean value
      DvStateVariable CurrentConnectionIDs = new DvStateVariable("CurrentConnectionIDs", new DvStandardDataType(UPnPStandardDataType.String))
                                               {
                                                 SendEvents = false,
                                               };
      AddStateVariable(CurrentConnectionIDs);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_ConnectionStatus = new DvStateVariable("A_ARG_TYPE_ConnectionStatus", new DvStandardDataType(UPnPStandardDataType.String))
                                                      {
                                                        SendEvents = false,
                                                        AllowedValueList =
                                                          new List<string>
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
      DvStateVariable A_ARG_TYPE_ConnectionManager = new DvStateVariable("A_ARG_TYPE_ConnectionManager", new DvStandardDataType(UPnPStandardDataType.String))
                                                       {
                                                         SendEvents = false,
                                                       };
      AddStateVariable(A_ARG_TYPE_ConnectionManager);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_Direction = new DvStateVariable("A_ARG_TYPE_Direction", new DvStandardDataType(UPnPStandardDataType.String))
                                               {
                                                 SendEvents = false,
                                                 AllowedValueList = new List<string> { "Output", "Input" }
                                               };
      AddStateVariable(A_ARG_TYPE_Direction);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_ProtocolInfo = new DvStateVariable("A_ARG_TYPE_ProtocolInfo", new DvStandardDataType(UPnPStandardDataType.String))
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
      DvStateVariable A_ARG_TYPE_AVTransportID = new DvStateVariable("A_ARG_TYPE_AVTransportID", new DvStandardDataType(UPnPStandardDataType.I4))
                                                   {
                                                     SendEvents = false,
                                                   };
      AddStateVariable(A_ARG_TYPE_AVTransportID);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_RcsID = new DvStateVariable("A_ARG_TYPE_RcsID", new DvStandardDataType(UPnPStandardDataType.I4))
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

    private static UPnPError OnGetProtocolInfo(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      string source = "";
      string sink = "";

      //      source =
      //        "http-get:*:audio/L16:*,http-get:*:audio/wav:*,http-get:*:audio/mpeg:*,http-get:*:audio/x-ms-wma:*,http-get:*:audio/L8:*,http-get:*:video/avi:*,http-get:*:video/mpeg:*,http-get:*:video/x-ms-wmv:*,http-get:*:video/x-ms-asf:*,http-get:*:video/x-ms-dvr:*,http-get:*:image/bmp:*,http-get:*:image/gif:*,http-get:*:image/jpeg:*,http-get:*:image/png:*,http-get:*:image/tiff:*,http-get:*:image/x-ycbcr-yuv420:*";
      source =
        "http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMABASE;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMAFULL;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_AAC_MULT5;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/avi:DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_24_AC3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_50_AC3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_60_AC3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_EU;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_24_AC3_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_50_AC3_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_60_AC3_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_EU_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_HD_24_AC3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_HD_50_AC3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_HD_60_AC3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_HD_EU_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/mpeg:DLNA.ORG_PN=MP3;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=44100;channels=1:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=44100;channels=2:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=48000;channels=1:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/L16;rate=48000;channels=2:DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/mp4:DLNA.ORG_PN=AAC_ISO;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/mp4:DLNA.ORG_PN=AAC_ISO_320;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/vnd.dlna.adts:DLNA.ORG_PN=AAC_ADTS;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/vnd.dlna.adts:DLNA.ORG_PN=AAC_ADTS_320;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/flac:DLNA.ORG_PN=FLAC;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:audio/ogg:DLNA.ORG_PN=OGG;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_SM;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_MED;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_LRG;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/png:DLNA.ORG_PN=PNG_LRG;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/png:DLNA.ORG_PN=PNG_TN;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:image/gif:DLNA.ORG_PN=GIF_LRG;DLNA.ORG_OP=00;DLNA.ORG_FLAGS=00D00000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG1;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_PAL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_NTSC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_EU;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_EU_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_EU_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_NA_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_KO_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_JP_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-matroska:DLNA.ORG_PN=MATROSKA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-flv:DLNA.ORG_PN=FLV;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-dvr:DLNA.ORG_PN=DVR_MS;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/wtv:DLNA.ORG_PN=WTV;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/ogg:DLNA.ORG_PN=OGV;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_MPEG1_L3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_AC3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_HD_720p_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_HD_1080i_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_HP_HD_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_LPCM;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_ASP_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_SP_L6_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_NDSD;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_ASP_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_ASP_AAC_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG4_P2_TS_ASP_AAC_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_ASP_AC3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_ASP_AC3_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG4_P2_TS_ASP_AC3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_ASP_MPEG1_L3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_ASP_MPEG1_L3_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG4_P2_TS_ASP_MPEG2_L2_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_ASP_MPEG2_L2;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_ASP_MPEG2_L2_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG4_P2_TS_ASP_MPEG1_L3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_MPEG1_L3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_MPEG1_L3_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_SD_MPEG1_L3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_MPEG1_L3;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_MPEG1_L3_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_HD_MPEG1_L3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_DTS_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_HD_DTS_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_HD_50_LPCM_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_BASE;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_FULL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVHIGH_FULL;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_PRO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVHIGH_PRO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L1_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L2_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L3_WMA;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=VC1_TS_AP_L1_AC3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=VC1_TS_AP_L2_AC3_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/mpeg:DLNA.ORG_PN=VC1_TS_HD_DTS_ISO;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=VC1_TS_HD_DTS_T;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_P2_3GPP_SP_L0B_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_P2_3GPP_SP_L0B_AMR;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_H263_3GPP_P0_L10_AMR;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_H263_MP4_P0_L10_AAC;DLNA.ORG_OP=11;DLNA.ORG_FLAGS=81500000000000000000000000000000";
      outParams = new List<object> { source, sink };
      return null;
    }

    private static UPnPError OnGetCurrentConnectionIDs(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = null;
      return null;
    }

    private static UPnPError OnGetCurrentConnectionInfo(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
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