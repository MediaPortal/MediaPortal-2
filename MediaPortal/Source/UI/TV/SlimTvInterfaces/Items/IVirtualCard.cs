#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.ComponentModel;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces.Items
{
  public enum SlimTvCardType
  {
    Analog = 0,
    DvbS = 1,
    DvbT = 2,
    DvbC = 3,
    Atsc = 4,
    RadioWebStream = 5,
    DvbIP = 6,
    Unknown = 7,
  }

  /// <summary>
  /// IVirtualCard represents a virtual card a user or recording is on.
  /// </summary>
  public interface IVirtualCard
  {
    int BitRateMode { get; set; }
    string ChannelName { get; set; }
    string Device { get; set; }
    bool Enabled { get; set; }
    int GetTimeshiftStoppedReason { get; set; }
    bool GrabTeletext { get; set; }
    bool HasTeletext { get; set; }
    int Id { get; set; }
    int ChannelId { get; set; }
    bool IsGrabbingEpg { get; set; }
    bool IsRecording { get; set; }
    bool IsScanning { get; set; }
    bool IsScrambled { get; set; }
    bool IsTimeShifting { get; set; }
    bool IsTunerLocked { get; set; }
    int MaxChannel { get; set; }
    int MinChannel { get; set; }
    string Name { get; set; }
    int QualityType { get; set; }
    string RecordingFileName { get; set; }
    string RecordingFolder { get; set; }
    int RecordingFormat { get; set; }
    int RecordingScheduleId { get; set; }
    DateTime RecordingStarted { get; set; }
    string RemoteServer { get; set; }
    string RTSPUrl { get; set; }
    int SignalLevel { get; set; }
    int SignalQuality { get; set; }
    string TimeShiftFileName { get; set; }
    string TimeShiftFolder { get; set; }
    DateTime TimeShiftStarted { get; set; }
    SlimTvCardType Type { get; set; }
    IUser User { get; set; }
  }
}
