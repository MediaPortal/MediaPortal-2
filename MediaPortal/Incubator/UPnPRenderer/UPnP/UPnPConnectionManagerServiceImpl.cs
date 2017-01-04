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
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.UPnPRenderer.UPnP
{
  public class UPnPConnectionManagerServiceImpl : DvService
  {
    public UPnPConnectionManagerServiceImpl()
      : base(
        UPnPDevice.CONNECTION_MANAGER_SERVICE_TYPE,
        UPnPDevice.CONNECTION_MANAGER_SERVICE_TYPE_VERSION,
        UPnPDevice.CONNECTION_MANAGER_SERVICE_ID)
    {
      // Used for a boolean value
      DvStateVariable SourceProtocolInfo = new DvStateVariable("SourceProtocolInfo",
        new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
      };
      AddStateVariable(SourceProtocolInfo);
      // Used for a boolean value
      DvStateVariable SinkProtocolInfo = new DvStateVariable("SinkProtocolInfo",
        new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
      };
      AddStateVariable(SinkProtocolInfo);
      // Used for a boolean value
      DvStateVariable CurrentConnectionIDs = new DvStateVariable("CurrentConnectionIDs",
        new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = true,
        Value = "0" // we only support one connection => connectionID is always 0
      };
      AddStateVariable(CurrentConnectionIDs);
      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_ConnectionStatus = new DvStateVariable("A_ARG_TYPE_ConnectionStatus",
        new DvStandardDataType(UPnPStandardDataType.String))
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
      DvStateVariable A_ARG_TYPE_ConnectionManager = new DvStateVariable("A_ARG_TYPE_ConnectionManager",
        new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
      };
      AddStateVariable(A_ARG_TYPE_ConnectionManager);
      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_Direction = new DvStateVariable("A_ARG_TYPE_Direction",
        new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        AllowedValueList = new List<string> { "Output", "Input" }
      };
      AddStateVariable(A_ARG_TYPE_Direction);
      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_ProtocolInfo = new DvStateVariable("A_ARG_TYPE_ProtocolInfo",
        new DvStandardDataType(UPnPStandardDataType.String))
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
        new DvStandardDataType(UPnPStandardDataType.I4))
      {
        SendEvents = false,
      };
      AddStateVariable(A_ARG_TYPE_AVTransportID);
      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_RcsID = new DvStateVariable("A_ARG_TYPE_RcsID",
        new DvStandardDataType(UPnPStandardDataType.I4))
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
      //string sink = "http-get:*:audio/vnd.dlna.adts:DLNA.ORG_PN=AAC_ADTS,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVSPML_MP3,http-get:*:audio/vnd.dlna.adts:DLNA.ORG_PN=AAC_ADTS_192,http-get:*:audio/vnd.dlna.adts:DLNA.ORG_PN=AAC_ADTS_320,http-get:*:audio/mp4:DLNA.ORG_PN=AAC_ISO,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVSPML_BASE,http-get:*:audio/3gpp:DLNA.ORG_PN=AAC_ISO,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVSPLL_BASE,http-get:*:audio/mp4:DLNA.ORG_PN=AAC_ISO_192,http-get:*:audio/3gpp:DLNA.ORG_PN=AAC_ISO_192,http-get:*:audio/mp4:DLNA.ORG_PN=AAC_ISO_320,http-get:*:audio/3gpp:DLNA.ORG_PN=AAC_ISO_320,http-get:*:audio/vnd.dlna.adts:DLNA.ORG_PN=AAC_MULT5_ADTS,http-get:*:audio/mp4:DLNA.ORG_PN=AAC_MULT5_ISO,http-get:*:audio/3gpp:DLNA.ORG_PN=AAC_MULT5_ISO,http-get:*:video/3gpp:DLNA.ORG_PN=AVC_3GPP_BL_QCIF15_AAC,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_PRO,http-get:*:video/3gpp:DLNA.ORG_PN=AVC_3GPP_BL_QCIF15_HEAAC,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_CIF15_AAC,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_FULL,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_CIF15_AAC_350,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_CIF15_AAC_520,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_CIF15_HEAAC,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_CIF15_HEAAC_350,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_CIF30_AAC_940,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_CIF30_AAC_MULT5,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_CIF30_HEAAC_L2,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_CIF30_MPEG1_L3,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_L12_CIF15_HEAAC,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_L12_CIF15_HEAACv2,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_L12_CIF15_HEAACv2_350,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_L1B_QCIF15_HEAAC,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_L1B_QCIF15_HEAACv2,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_L2_CIF30_AAC,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_L31_HD_AAC,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_L32_HD_AAC,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_L3L_SD_AAC,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_L3L_SD_HEAAC,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_BL_L3_SD_AAC,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_AAC_LC,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_AAC_MULT5,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_HEAAC_L2,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_HEAAC_L4,http-get:*:video/mp4:DLNA.ORG_PN=AVC_MP4_MP_SD_MPEG1_L3,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF15_AAC,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVMED_BASE,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF15_AAC_540,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_BL_CIF15_AAC_540_ISO,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVHM_BASE,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF15_AAC_540_T,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_BL_CIF15_AAC_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF15_AAC_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF30_AAC_940,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_BL_CIF30_AAC_940_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF30_AAC_940_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF30_AAC_MULT5,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_BL_CIF30_AAC_MULT5_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF30_AAC_MULT5_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF30_AC3,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_BL_CIF30_AC3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF30_AC3_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF30_MPEG1_L3,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_BL_CIF30_MPEG1_L3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_BL_CIF30_MPEG1_L3_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_JP_AAC_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_AAC_MULT5,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_HD_AAC_MULT5_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_AAC_MULT5_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_AC3,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_HD_AC3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_AC3_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_MPEG1_L3,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_HD_MPEG1_L3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_HD_MPEG1_L3_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_AAC_MULT5,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_SD_AAC_MULT5_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_AAC_MULT5_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_AC3,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_SD_AC3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_AC3_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_MPEG1_L3,http-get:*:video/mpeg:DLNA.ORG_PN=AVC_TS_MP_SD_MPEG1_L3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=AVC_TS_MP_SD_MPEG1_L3_T,http-get:*:audio/mp4:DLNA.ORG_PN=HEAACv2_L2,http-get:*:audio/3gpp:DLNA.ORG_PN=HEAACv2_L2,http-get:*:audio/mp4:DLNA.ORG_PN=HEAACv2_L2_128,http-get:*:audio/3gpp:DLNA.ORG_PN=HEAACv2_L2_128,http-get:*:audio/mp4:DLNA.ORG_PN=HEAACv2_L2_320,http-get:*:audio/3gpp:DLNA.ORG_PN=HEAACv2_L2_320,http-get:*:audio/mp4:DLNA.ORG_PN=HEAACv2_L3,http-get:*:audio/3gpp:DLNA.ORG_PN=HEAACv2_L3,http-get:*:audio/mp4:DLNA.ORG_PN=HEAACv2_L4,http-get:*:audio/3gpp:DLNA.ORG_PN=HEAACv2_L4,http-get:*:audio/mp4:DLNA.ORG_PN=HEAACv2_MULT5,http-get:*:audio/3gpp:DLNA.ORG_PN=HEAACv2_MULT5,http-get:*:audio/mp4:DLNA.ORG_PN=HEAAC_L2_ISO,http-get:*:audio/3gpp:DLNA.ORG_PN=HEAAC_L2_ISO,http-get:*:audio/mp4:DLNA.ORG_PN=HEAAC_L2_ISO_128,http-get:*:audio/3gpp:DLNA.ORG_PN=HEAAC_L2_ISO_128,http-get:*:audio/mp4:DLNA.ORG_PN=HEAAC_L2_ISO_320,http-get:*:audio/3gpp:DLNA.ORG_PN=HEAAC_L2_ISO_320,http-get:*:audio/mp4:DLNA.ORG_PN=HEAAC_L3_ISO,http-get:*:audio/3gpp:DLNA.ORG_PN=HEAAC_L3_ISO,http-get:*:audio/mp4:DLNA.ORG_PN=HEAAC_MULT5_ISO,http-get:*:audio/3gpp:DLNA.ORG_PN=HEAAC_MULT5_ISO,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_LRG,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVHIGH_PRO,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_LRG_ICO,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_MED,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_SM,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_SM_ICO,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN,http-get:*:audio/L16:DLNA.ORG_PN=LPCM,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMVHIGH_FULL,http-get:*:audio/L16:DLNA.ORG_PN=LPCM_low,http-get:*:audio/mpeg:DLNA.ORG_PN=MP3,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMDRM_WMVSPML_MP3,http-get:*:audio/mpeg:DLNA.ORG_PN=MP3X,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG1,http-get:*:video/3gpp:DLNA.ORG_PN=MPEG4_P2_3GPP_SP_L0B_AAC,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_ASP_AAC,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_ASP_HEAAC,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_ASP_HEAAC_MULT5,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_SP_AAC,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_SP_HEAAC,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_SP_L2_AAC,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_SP_VGA_AAC,http-get:*:video/mp4:DLNA.ORG_PN=MPEG4_P2_MP4_SP_VGA_HEAAC,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_SP_AC3,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG4_P2_TS_SP_AC3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_SP_AC3_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_SP_MPEG1_L3,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG4_P2_TS_SP_MPEG1_L3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_SP_MPEG1_L3_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_SP_MPEG2_L2,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG4_P2_TS_SP_MPEG2_L2_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG4_P2_TS_SP_MPEG2_L2_T,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_NTSC,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_NTSC_XAC3,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_PAL,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_PAL_XAC3,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_HD_KO,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_HD_KO_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_HD_KO_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_HD_KO_XAC3,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_HD_KO_XAC3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_HD_KO_XAC3_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_HD_NA,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_HD_NA_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_HD_NA_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_HD_NA_XAC3,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_HD_NA_XAC3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_HD_NA_XAC3_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_JP_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_MP_LL_AAC,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_MP_LL_AAC_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_MP_LL_AAC_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_EU,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_EU_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_EU_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_KO_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO_XAC3,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_KO_XAC3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_KO_XAC3_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_NA_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA_T,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA_XAC3,http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_NA_XAC3_ISO,http-get:*:video/vnd.dlna.mpeg-tts:DLNA.ORG_PN=MPEG_TS_SD_NA_XAC3_T,http-get:*:image/png:DLNA.ORG_PN=PNG_LRG,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMDRM_WMVSPML_BASE,http-get:*:image/png:DLNA.ORG_PN=PNG_LRG_ICO,http-get:*:image/png:DLNA.ORG_PN=PNG_TN,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L1_WMA,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMDRM_WMVSPLL_BASE,http-get:*:video/x-ms-asf:DLNA.ORG_PN=VC1_ASF_AP_L2_WMA,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMABASE,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMDRM_WMVMED_PRO,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMAFULL,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMALSL,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMALSL_MULT5,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMAPRO,http-get:*:video/x-ms-asf:DLNA.ORG_PN=WMDRM_VC1_ASF_AP_L1_WMA,http-get:*:video/x-ms-asf:DLNA.ORG_PN=WMDRM_VC1_ASF_AP_L2_WMA,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMDRM_WMABASE,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMDRM_WMAFULL,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMDRM_WMALSL,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMDRM_WMALSL_MULT5,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMDRM_WMAPRO,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMDRM_WMVHIGH_FULL,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMDRM_WMVMED_FULL,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMDRM_WMVHIGH_PRO,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMDRM_WMVHM_BASE,http-get:*:video/x-ms-wmv:DLNA.ORG_PN=WMDRM_WMVMED_BASE,http-get:*:video/x-ms-wmv:*,http-get:*:audio/x-ms-wma:*,http-get:*:video/x-ms-asf:*,http-get:*:image/png:*,http-get:*:audio/mpeg:*,http-get:*:audio/L16:*,http-get:*:image/jpeg:*,http-get:*:video/mpeg:*,http-get:*:video/vnd.dlna.mpeg-tts:*,http-get:*:video/mp4:*,http-get:*:video/3gpp:*,http-get:*:audio/3gpp:*,http-get:*:audio/mp4:*,http-get:*:audio/vnd.dlna.adts:*,http-get:*:application/vnd.ms-search:*,http-get:*:application/vnd.ms-wpl:*,http-get:*:application/x-ms-wmd:*,http-get:*:application/x-ms-wmz:*,http-get:*:application/x-shockwave-flash:*,http-get:*:audio/3gpp2:*,http-get:*:audio/aiff:*,http-get:*:audio/basic:*,http-get:*:audio/l8:*,http-get:*:audio/mid:*,http-get:*:audio/wav:*,http-get:*:audio/x-mpegurl:*,http-get:*:audio/x-ms-wax:*,http-get:*:image/bmp:*,http-get:*:image/gif:*,http-get:*:image/vnd.ms-photo:*,http-get:*:video/3gpp2:*,http-get:*:video/avi:*,http-get:*:video/quicktime:*,http-get:*:video/x-ms-wm:*,http-get:*:video/x-ms-wmx:*,http-get:*:video/x-ms-wvx:*,http-get:*:video/x-msvideo:*,http-get:*:video/x-flv:*,http-get:*:video/x-ogm:*,http-get:*:audio/x-wavpack:*,rtsp-rtp-udp:*:audio/L16:*,rtsp-rtp-udp:*:audio/L8:*,rtsp-rtp-udp:*:audio/mpeg:*,rtsp-rtp-udp:*:audio/x-ms-wma:*,rtsp-rtp-udp:*:video/x-ms-wmv:*,rtsp-rtp-udp:*:audio/x-asf-pf:*";
      string sink = "http-get:*:audio/mpegurl:*," +
                    "http-get:*:audio/mp3:*," +
                    "http-get:*:audio/mpeg:*," +
                    "http-get:*:audio/x-ms-wma:*," +
                    "http-get:*:audio/wma:*," +
                    "http-get:*:audio/mpeg3:*," +
                    "http-get:*:video/x-ms-wmv:*," +
                    "http-get:*:video/x-ms-asf:*," +
                    "http-get:*:video/x-ms-avi:*," +
                    "http-get:*:video/mpeg:*," +
                    "http-get:*:image/*:*";
      outParams = new List<object> { source, sink };
      return null;
    }

    private static UPnPError OnGetCurrentConnectionIDs(DvAction action, IList<object> inParams,
      out IList<object> outParams, CallContext context)
    {
      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      return null;
    }

    private static UPnPError OnGetCurrentConnectionInfo(DvAction action, IList<object> inParams,
      out IList<object> outParams, CallContext context)
    {
      outParams = new List<object> { "0", "0", ":::", "/", "-1", "Input", "Unknown" };
      return null;
    }
  }
}
