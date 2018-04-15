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
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.UPnPRenderer.UPnP
{

  public delegate void PlayEventHandler();
  public delegate void PauseEventHandler();
  public delegate void StopEventHandler();
  public delegate void SeekEventHandler();
  public delegate void SetAVTransportURIEventHandler(OnEventSetAVTransportURIEventArgs e);

  public class UPnPAVTransportServiceImpl : DvService
  {

    public UPnPAVTransportServiceImpl()
      : base(
        UPnPDevice.AV_TRANSPORT_SERVICE_TYPE,
        UPnPDevice.AV_TRANSPORT_SERVICE_TYPE_VERSION,
        UPnPDevice.AV_TRANSPORT_SERVICE_ID)
    {

      #region DvStateVariables

      // Used for a boolean value
      DvStateVariable AbsoluteCounterPosition = new DvStateVariable("AbsoluteCounterPosition", new DvStandardDataType(UPnPStandardDataType.I4))
      {
        SendEvents = false,
        Value = 2147483647,
      };
      AddStateVariable(AbsoluteCounterPosition);

      // Used for a boolean value
      DvStateVariable AbsoluteTimePosition = new DvStateVariable("AbsoluteTimePosition", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "NOT_IMPLEMENTED",
      };
      AddStateVariable(AbsoluteTimePosition);

      // Used for a boolean value
      DvStateVariable AVTransportURI = new DvStateVariable("AVTransportURI", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
      };
      AddStateVariable(AVTransportURI);

      // Used for a boolean value
      DvStateVariable AVTransportURIMetaData = new DvStateVariable("AVTransportURIMetaData", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
      };
      AddStateVariable(AVTransportURIMetaData);

      // Used for a boolean value
      DvStateVariable CurrentMediaDurtaion = new DvStateVariable("CurrentMediaDuration", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "00:00:00",
      };
      AddStateVariable(CurrentMediaDurtaion);

      // Used for a boolean value
      DvStateVariable CurrentPlayMode = new DvStateVariable("CurrentPlayMode", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "NORMAL",
        AllowedValueList = new List<string>
        {
          "NORMAL",
          "REPEAT_ALL",
          "INTRO"
        }
      };
      AddStateVariable(CurrentPlayMode);

      // Used for a boolean value
      DvStateVariable CurrentRecordQualityMode = new DvStateVariable("CurrentRecordQualityMode", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "NOT_IMPLEMENT",
        AllowedValueList = new List<string>
        {
          "0:EP",
          "1:LP",
          "2:SP",
          "0:BASIC",
          "1:MEDIUM",
          "2:HIGH",
          "NOT_IMPLEMENTED",
          "vendor-defined"
        }
      };
      AddStateVariable(CurrentRecordQualityMode);

      // Used for a boolean value
      DvStateVariable CurrentTrack = new DvStateVariable("CurrentTrack", new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = false,
        Value = (UInt32)0,
        // TODO Add allowed Range
        // AllowedValueRange = new DvAllowedValueRange()
      };
      AddStateVariable(CurrentTrack);

      // Used for a boolean value
      DvStateVariable CurrentTrackDurtion = new DvStateVariable("CurrentTrackDuration", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "00:00:00",
      };
      AddStateVariable(CurrentTrackDurtion);

      // Used for a boolean value
      DvStateVariable CurrentTrackMetaData = new DvStateVariable("CurrentTrackMetaData", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
      };
      AddStateVariable(CurrentTrackMetaData);

      // Used for a boolean value
      DvStateVariable CurrentTrackURI = new DvStateVariable("CurrentTrackURI", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
      };
      AddStateVariable(CurrentTrackURI);

      // Used for a boolean value
      DvStateVariable CurrentTransportActions = new DvStateVariable("CurrentTransportActions", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
      };
      AddStateVariable(CurrentTransportActions);

      // Used for a boolean value
      DvStateVariable LastChange = new DvStateVariable("LastChange", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = true,
        Value = "<Event xmlns=\"urn:schemas-upnp-org:metadata-1-0/AVT/\"></Event>"
      };
      AddStateVariable(LastChange);

      // Used for a boolean value
      DvStateVariable NextAVTransportURI = new DvStateVariable("NextAVTransportURI", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
      };
      AddStateVariable(NextAVTransportURI);

      // Used for a boolean value
      DvStateVariable NextAVTransportURIMetaData = new DvStateVariable("NextAVTransportURIMetaData", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
      };
      AddStateVariable(NextAVTransportURIMetaData);

      // Used for a boolean value
      DvStateVariable NumberOfTracks = new DvStateVariable("NumberOfTracks", new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = false,
        Value = (UInt32)0,
        // TODO Add valueRange
      };
      AddStateVariable(NumberOfTracks);

      // Used for a boolean value
      DvStateVariable PlaybackStorageMedium = new DvStateVariable("PlaybackStorageMedium", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "NONE",
        AllowedValueList = new List<string>
        {
          "UNKNOWN",
          "DV",
          "MINI-DV",
          "VHS",
          "W-VHS",
          "S-VHS",
          "D-VHS",
          "VHSC",
          "VIDEO8",
          "HI8",
          "CD-ROM",
          "CD-DA",
          "CD-R",
          "CD-RW",
          "VIDEO-CD",
          "SACD",
          "MD-AUDIO",
          "MD-PICTURE",
          "DVD-ROM",
          "DVD-VIDEO",
          "DVD-R",
          "DVD+RW",
          "DVD-RW",
          "DVD-RAM",
          "DVD-AUDIO",
          "DAT",
          "LD",
          "HDD",
          "MICRO-MV",
          "NETWORK",
          "NONE",
          "NOT_IMPLEMENTED",
          "vendor-defined"
        }
      };
      AddStateVariable(PlaybackStorageMedium);

      // Used for a boolean value
      DvStateVariable PossiblePlaybackStorageMedia = new DvStateVariable("PossiblePlaybackStorgrageMedia", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = String.Join(",", PlaybackStorageMedium.AllowedValueList),
      };
      AddStateVariable(PossiblePlaybackStorageMedia);

      // Used for a boolean value
      DvStateVariable PossibleRecordQualityModes = new DvStateVariable("PossibleRecordQualityModes", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "NOT_IMPLEMENT",
      };
      AddStateVariable(PossibleRecordQualityModes);


      // Used for a boolean value
      DvStateVariable RecordStorageMedium = new DvStateVariable("RecordStorageMedium", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "NONE",
        AllowedValueList = new List<string>
        {
          "UNKNOWN",
          "DV",
          "MINI-DV",
          "VHS",
          "W-VHS",
          "S-VHS",
          "D-VHS",
          "VHSC",
          "VIDEO8",
          "HI8",
          "CD-ROM",
          "CD-DA",
          "CD-R",
          "CD-RW",
          "VIDEO-CD",
          "SACD",
          "MD-AUDIO",
          "MD-PICTURE",
          "DVD-ROM",
          "DVD-VIDEO",
          "DVD-R",
          "DVD+RW",
          "DVD-RW",
          "DVD-RAM",
          "DVD-AUDIO",
          "DAT",
          "LD",
          "HDD",
          "MICRO-MV",
          "NETWORK",
          "NONE",
          "NOT_IMPLEMENTED",
          "vendor-defined"
        }
      };
      AddStateVariable(RecordStorageMedium);

      // Used for a boolean value
      DvStateVariable PossibleRecordStorageMedia = new DvStateVariable("PossibleRecordStorageMedia", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = String.Join(",", RecordStorageMedium.AllowedValueList),
      };
      AddStateVariable(PossibleRecordStorageMedia);

      // Used for a boolean value
      DvStateVariable RecordMediumWriteStatus = new DvStateVariable("RecordMediumWriteStatus", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "NOT_IMPLEMENT",
        AllowedValueList = new List<string>
        {
          "WRITABLE",
          "PROTECTED",
          "NOT_WRITABLE",
          "UNKNOWN",
          "NOT_IMPLEMENTED"
        }
      };
      AddStateVariable(RecordMediumWriteStatus);

      // Used for a boolean value
      DvStateVariable RelativeCounterPosition = new DvStateVariable("RelativeCounterPosition", new DvStandardDataType(UPnPStandardDataType.I4))
      {
        SendEvents = false,
        Value = 2147483647,
      };
      AddStateVariable(RelativeCounterPosition);

      // Used for a boolean value
      DvStateVariable RelativeTimePosition = new DvStateVariable("RelativeTimePosition", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "NOT_IMPLEMENTED",
      };
      AddStateVariable(RelativeTimePosition);

      // Used for a boolean value
      DvStateVariable TransportPlaySpeed = new DvStateVariable("TransportPlaySpeed", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "1",
        AllowedValueList = new List<string>
        {
          "1",
          "vendor-defined"
        }
      };
      AddStateVariable(TransportPlaySpeed);

      // Used for a boolean value
      DvStateVariable TransportState = new DvStateVariable("TransportState", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "NO_MEDIA_PRESENT",
        AllowedValueList = new List<string>
        {
          "STOPPED",
          "PAUSED_PLAYBACK",
          "PAUSED_RECORDING",
          "PLAYING",
          "RECORDING",
          "TRANSITIONING",
          "NO_MEDIA_PRESENT"
        }
      };
      AddStateVariable(TransportState);

      // Used for a boolean value 
      DvStateVariable TransportStatus = new DvStateVariable("TransportStatus", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        Value = "OK",
        AllowedValueList = new List<string>
        {
          "OK",
          "ERROR_OCCURRED",
          "vendor-defined"
        }
      };
      AddStateVariable(TransportStatus);

      #endregion DvStateVariables

      #region A_ARG_TYPE

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_InstanceID = new DvStateVariable("A_ARG_TYPE_InstanceID", new DvStandardDataType(UPnPStandardDataType.Ui4)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_InstanceID);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_SeekMode = new DvStateVariable("A_ARG_TYPE_SeekMode", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        AllowedValueList = new List<string>
        {
          //"ABS_TIME",
          "REL_TIME", // we only support REL_TIME => checked in "onAction
          //"ABS_COUNT",
          //"REL_COUNT",
          //"TRACK_NR",
          //"CHANNEL_FREQ",
          //"TAPE-INDEX",
          //"FRAME"
        }
      };
      AddStateVariable(A_ARG_TYPE_SeekMode);

      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_SeekTarget = new DvStateVariable("A_ARG_TYPE_SeekTarget", new DvStandardDataType(UPnPStandardDataType.String)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_SeekTarget);

      #endregion A_ARG_TYPE;

      #region Actions

      DvAction getCurrentTransportActionsction = new DvAction("GetCurrentTransportActions", OnGetCurrentTransportActions,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
          new DvArgument("Sink",
            CurrentTransportActions,
            ArgumentDirection.Out),
        });
      AddAction(getCurrentTransportActionsction);

      DvAction getDeviceCapabilitiesAction = new DvAction("GetDeviceCapabilities", OnGetDeviceCapabilities,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
          new DvArgument("PlayMedia",
            PossiblePlaybackStorageMedia,
            ArgumentDirection.Out),
          new DvArgument("RecMedia",
            PossibleRecordStorageMedia,
            ArgumentDirection.Out),
          new DvArgument("RecQualityModes",
            PossibleRecordQualityModes,
            ArgumentDirection.Out),
        });
      AddAction(getDeviceCapabilitiesAction);

      DvAction getMediaInfoAction = new DvAction("GetMediaInfo", OnGetMediaInfo,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
          new DvArgument("NrTracks",
            NumberOfTracks,
            ArgumentDirection.Out),
          new DvArgument("MediaDuration",
            CurrentMediaDurtaion,
            ArgumentDirection.Out),
          new DvArgument("CurrentURI",
            AVTransportURI,
            ArgumentDirection.Out),
          new DvArgument("CurrentURIMetaData",
            AVTransportURIMetaData,
            ArgumentDirection.Out),
          new DvArgument("NextURI",
            NextAVTransportURI,
            ArgumentDirection.Out),
          new DvArgument("NextURIMetaData",
            NextAVTransportURIMetaData,
            ArgumentDirection.Out),
          new DvArgument("PlayMedium",
            PlaybackStorageMedium,
            ArgumentDirection.Out),
          new DvArgument("RecordMedium",
            RecordStorageMedium,
            ArgumentDirection.Out),
          new DvArgument("WriteStatus",
            RecordMediumWriteStatus,
            ArgumentDirection.Out),
        });
      AddAction(getMediaInfoAction);

      DvAction getPositionInfoAction = new DvAction("GetPositionInfo", OnGetPositionInfo,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
          new DvArgument("Track",
            CurrentTrack,
            ArgumentDirection.Out),
          new DvArgument("TrackDuration",
            CurrentTrackDurtion,
            ArgumentDirection.Out),
          new DvArgument("TrackMetaData",
            CurrentTrackMetaData,
            ArgumentDirection.Out),
          new DvArgument("TrackURI",
            CurrentTrackURI,
            ArgumentDirection.Out),
          new DvArgument("RelTime",
            RelativeTimePosition,
            ArgumentDirection.Out),
          new DvArgument("AbsTime",
            AbsoluteTimePosition,
            ArgumentDirection.Out),
          new DvArgument("RelCount",
            RelativeCounterPosition,
            ArgumentDirection.Out),
          new DvArgument("AbsCount",
            AbsoluteCounterPosition,
            ArgumentDirection.Out),
        });
      AddAction(getPositionInfoAction);

      DvAction getTransportInfoAction = new DvAction("GetTransportInfo", OnGetTransportInfo,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
          new DvArgument("CurrentTransportState",
            TransportState,
            ArgumentDirection.Out),
          new DvArgument("CurrentTransportStatus",
            TransportStatus,
            ArgumentDirection.Out),
          new DvArgument("CurrentSpeed",
            TransportPlaySpeed,
            ArgumentDirection.Out),
        });
      AddAction(getTransportInfoAction);

      DvAction getTransportSettingsAction = new DvAction("GetTransportSettings", OnGetTransportSettings,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
          new DvArgument("PlayMode",
            CurrentPlayMode,
            ArgumentDirection.Out),
          new DvArgument("RecQualityMode",
            CurrentRecordQualityMode,
            ArgumentDirection.Out),
        });
      AddAction(getTransportSettingsAction);

      DvAction nextAction = new DvAction("Next", OnNext,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
        });
      AddAction(nextAction);

      DvAction pauseAction = new DvAction("Pause", OnPause,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
        });
      AddAction(pauseAction);

      DvAction playAction = new DvAction("Play", OnPlay,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
          new DvArgument("Speed",
            TransportPlaySpeed,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
          
        });
      AddAction(playAction);

      DvAction previousAction = new DvAction("Previous", OnPrevious,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
        });
      AddAction(previousAction);

      DvAction seekAction = new DvAction("Seek", OnSeek,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
          new DvArgument("Unit",
            A_ARG_TYPE_SeekMode,
            ArgumentDirection.In),
          new DvArgument("Target",
            A_ARG_TYPE_SeekTarget,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
        });
      AddAction(seekAction);

      DvAction setAVTransportURIAction = new DvAction("SetAVTransportURI", OnSetAVTransportURI,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
          new DvArgument("CurrentURI",
            AVTransportURI,
            ArgumentDirection.In),
          new DvArgument("CurrentURIMetaData",
            AVTransportURIMetaData,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
        });
      AddAction(setAVTransportURIAction);

      DvAction setNextAVTransportURIAction = new DvAction("SetNextAVTransportURI", OnSetNextAVTransportURI,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
          new DvArgument("NextURI",
            NextAVTransportURI,
            ArgumentDirection.In),
          new DvArgument("NextURIMetaData",
            NextAVTransportURIMetaData,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
        });
      AddAction(setNextAVTransportURIAction);

      DvAction setPlayModeAction = new DvAction("SetPlayMode", OnSetPlayMode,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
          new DvArgument("NewPlayMode",
            CurrentPlayMode,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
        });
      AddAction(setPlayModeAction);

      DvAction stopction = new DvAction("Stop", OnStop,
        new DvArgument[]
        {
          new DvArgument("InstanceID",
            A_ARG_TYPE_InstanceID,
            ArgumentDirection.In),
        },
        new DvArgument[]
        {
        });
      AddAction(stopction);

      #endregion Actions
    }

    #region OnAction

    private static UPnPError OnGetCurrentTransportActions(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      foreach (var inArgument in action.InArguments)
      {
        TraceLogger.WriteLine("In Argument: " + inArgument.Name);
        switch (inArgument.Name)
        {
          case "InstanceID":
            inArgument.RelatedStateVar.Value = inParams[0];
            break;
        }
      }
      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      return null;
    }

    private static UPnPError OnGetDeviceCapabilities(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      TraceLogger.WriteLine(action.OutArguments[0].RelatedStateVar.DefaultValue);
      return null;
    }

    private static UPnPError OnGetMediaInfo(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      foreach (var outArgument in action.OutArguments)
      {
        TraceLogger.WriteLine("- " + outArgument.Name + " - " + outArgument.RelatedStateVar.Value);
      }
      return null;
    }

    private static UPnPError OnGetPositionInfo(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      return null;
    }

    private static UPnPError OnGetTransportInfo(DvAction action, IList<object> inParams,
      out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      TraceLogger.WriteLine("OUTPUT:");
      foreach (var outParam in outParams)
      {
        TraceLogger.WriteLine(outParam);
      }
      return null;
    }

    // just returns the vars
    private static UPnPError OnGetTransportSettings(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      return null;
    }

    // not implemented
    private static UPnPError OnNext(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      return null;
    }

    private static UPnPError OnPause(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      if (action.ParentService.StateVariables["TransportState"].Value.ToString() == "PLAYING")
      {
        TraceLogger.WriteLine("TransPortState is PLAYING");
        ChangeStateVariable("TransportState", "PAUSED_PLAYBACK", action);
        OnEventPause(); // FireEfent
      }
      else
      {
        TraceLogger.WriteLine("TransportState is not Playing");
      }

      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      return null;
    }

    private static UPnPError OnPlay(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);

      ChangeStateVariables(new List<string>
      {
        "TransportState",
        "TransportPlaySpeed"

      }, new List<object>
      {
        "PLAYING",
        inParams[1]
      }, action);

      OnEventPlay(); // FireEfent

      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      return null;
    }

    // not implemented
    private static UPnPError OnPrevious(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      return null;
    }

    private static UPnPError OnSeek(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      ChangeStateVariables(new List<string>
      {
        "A_ARG_TYPE_SeekMode",
        "A_ARG_TYPE_SeekTarget"

      }, new List<object>
      {
        inParams[1],
        inParams[2]
      }, action);


      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();

      if (inParams[1].ToString() == "REL_TIME")  // we only support "REL_TIME"
        OnEventSeek(); // FireEfent
      else
        return new UPnPError(710, "The specified seek mode is not supported by the device.");

      return null;
    }

    private static UPnPError OnSetAVTransportURI(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);

      ChangeStateVariables(new List<string>
      {
        "AVTransportURI",
        "AVTransportURIMetaData",
        "PlaybackStorageMedium"
      }, new List<object>
      {
        inParams[1],
        inParams[2],
        "NETWORK"   // we always play from a network resource
      }, action);

      // From the Documentation:
      // If the current transport state is “NO MEDIA PRESENT” the transport state changes to “STOPPED”.
      // In all other cases, this action does not change the transport state of the specified instance.
      if (action.ParentService.StateVariables["TransportState"].Value.ToString() == "NO_MEDIA_PRESENT")
      {
        TraceLogger.WriteLine("Change State to STOPPED");
        ChangeStateVariables(new List<string>
        {
          "TransportState"
        }, new List<object>
        {
          "STOPPED"
        }, action);
      }

      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();

      OnEventSetAVTransportURIEventArgs eventArgs = new OnEventSetAVTransportURIEventArgs
      {
        CurrentURI = inParams[1] as string,
        CurrentURIMetaData = inParams[2] as string
      };

      OnEventSetAVTransportURI(eventArgs);

      TraceLogger.WriteLine("OnSetAVTransportURI RETURN");
      return null;
    }

    // not implemented
    private static UPnPError OnSetNextAVTransportURI(DvAction action, IList<object> inParams,
      out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      return null;
    }

    // not really implemented
    private static UPnPError OnSetPlayMode(DvAction action, IList<object> inParams,
      out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);
      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      return null;
    }

    private static UPnPError OnStop(DvAction action, IList<object> inParams,
      out IList<object> outParams, CallContext context)
    {
      TraceLogger.DebugLogParams(inParams);

      ChangeStateVariables(new List<string>
      {
        "TransportState"
      }, new List<object>
      {
        "STOPPED"
      }, action);

      OnEventStop(); // FireEfent

      outParams = action.OutArguments.Select(outArgument => outArgument.RelatedStateVar.Value).ToList();
      return null;
    }

    #endregion OnAction

    private static string LastChangeXML(List<string> varNames, List<string> stateValues, IDictionary<string, DvStateVariable> stateVariables)
    {
      XNamespace aw = "urn:schemas-upnp-org:metadata-1-0/AVT/";
      XElement Event = new XElement(aw + "Event");

      XElement InstanceID = new XElement(aw + "InstanceID");
      InstanceID.SetAttributeValue("val", "0");

      for (int i = 0; i < varNames.Count; i++)
      {
        var StateVariableElement = new XElement(aw + varNames[i]);
        StateVariableElement.SetAttributeValue("val", stateValues[i]);
        InstanceID.Add(StateVariableElement);
      }


      Event.Add(InstanceID);

      XDocument srcTree = new XDocument(
        Event
        );
      //  TraceLogger.WriteLine(srcTree.ToString());
      stateVariables["LastChange"].Value = srcTree.ToString();

      return srcTree.ToString();
    }

    #region Events

    public static event PlayEventHandler Play;
    public static event PauseEventHandler Pause;
    public static event StopEventHandler Stop;
    public static event SeekEventHandler Seek;
    public static event SetAVTransportURIEventHandler SetAVTransportURI;

    // Invoke the Play event; called whenever list changes:
    protected static void OnEventPlay()
    {
      if (Play != null)
        Play();
    }

    protected static void OnEventPause()
    {
      if (Pause != null)
        Pause();
    }

    protected static void OnEventStop()
    {
      if (Stop != null)
        Stop();
    }

    protected static void OnEventSeek()
    {
      TraceLogger.WriteLine("OnEventSeek()");
      if (Seek != null)
      {
        TraceLogger.WriteLine("Call Seek()");
        Seek();
      }
    }

    protected static void OnEventSetAVTransportURI(OnEventSetAVTransportURIEventArgs e)
    {
      if (SetAVTransportURI != null)
        SetAVTransportURI(e);
    }

    #endregion Events

    #region ChangeStateVariablesFromOutside

    public void ChangeStateVariable(string name, string value)
    {
      StateVariables[name].Value = value;
      LastChangeXML(new List<string>
      {
        name
      },
      new List<string>
      {
        value
      }, StateVariables);
    }

    public static void ChangeStateVariable(string name, string value, DvAction action)
    {
      action.ParentService.StateVariables[name].Value = value;
      LastChangeXML(new List<string>
      {
        name
      },
      new List<string>
      {
        value
      }, action.ParentService.StateVariables);
    }

    public void ChangeStateVariables(List<string> varNames, IList<object> inParams)
    {
      List<string> changedValues = new List<string>();

      for (int i = 0; i < varNames.Count; i++)
      {
        TraceLogger.WriteLine("Name: " + varNames[i] + " Value: " + inParams[i]);
        StateVariables[varNames[i]].Value = inParams[i];
        changedValues.Add(inParams[i].ToString());
      }

      LastChangeXML(varNames, changedValues, StateVariables);
    }

    private static void ChangeStateVariables(List<string> varNames, IList<object> inParams, DvAction action)
    {
      List<string> changedValues = new List<string>();

      for (int i = 0; i < varNames.Count; i++)
      {
        TraceLogger.WriteLine("Name: " + varNames[i] + " Value: " + inParams[i]);
        action.ParentService.StateVariables[varNames[i]].Value = inParams[i];
        changedValues.Add(inParams[i].ToString());
      }

      LastChangeXML(varNames, changedValues, action.ParentService.StateVariables);
    }

    #endregion ChangeStateVariablesFromOutside

    /*internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }*/
  }

  public class OnEventSetAVTransportURIEventArgs : EventArgs
  {
    public string CurrentURI { get; set; }
    public string CurrentURIMetaData { get; set; }
  }
}
