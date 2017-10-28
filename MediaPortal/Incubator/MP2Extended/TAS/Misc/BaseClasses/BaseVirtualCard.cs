using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using System;

namespace MediaPortal.Plugins.MP2Extended.TAS.Misc.BaseClasses
{
  class BaseVirtualCard
  {
    internal static WebVirtualCard VirtualCard(IVirtualCard virtualCard)
    {
      return new WebVirtualCard
      {
        BitRateMode = (int)virtualCard.BitRateMode,
        ChannelName = virtualCard.ChannelName,
        Device = virtualCard.Device,
        Enabled = virtualCard.Enabled,
        GetTimeshiftStoppedReason = (int)virtualCard.GetTimeshiftStoppedReason,
        GrabTeletext = virtualCard.GrabTeletext,
        HasTeletext = virtualCard.HasTeletext,
        Id = virtualCard.Id,
        ChannelId = virtualCard.ChannelId,
        IsGrabbingEpg = virtualCard.IsGrabbingEpg,
        IsRecording = virtualCard.IsRecording,
        IsScanning = virtualCard.IsScanning,
        IsScrambled = virtualCard.IsScrambled,
        IsTimeShifting = virtualCard.IsTimeShifting,
        IsTunerLocked = virtualCard.IsTunerLocked,
        MaxChannel = virtualCard.MaxChannel,
        MinChannel = virtualCard.MinChannel,
        Name = virtualCard.Name,
        QualityType = (int)virtualCard.QualityType,
        RecordingFileName = virtualCard.RecordingFileName,
        RecordingFolder = virtualCard.RecordingFolder,
        RecordingFormat = virtualCard.RecordingFormat,
        RecordingScheduleId = virtualCard.RecordingScheduleId,
        RecordingStarted = virtualCard.RecordingStarted != DateTime.MinValue ? virtualCard.RecordingStarted : new DateTime(2000, 1, 1),
        RemoteServer = virtualCard.RemoteServer,
        RTSPUrl = virtualCard.RTSPUrl,
        SignalLevel = virtualCard.SignalLevel,
        SignalQuality = virtualCard.SignalQuality,
        TimeShiftFileName = virtualCard.TimeShiftFileName,
        TimeShiftFolder = virtualCard.TimeShiftFolder,
        TimeShiftStarted = virtualCard.TimeShiftStarted != DateTime.MinValue ? virtualCard.TimeShiftStarted : new DateTime(2000, 1, 1),
        Type = (WebCardType)virtualCard.Type,
        User = virtualCard.User != null ? BaseUser.User(virtualCard.User) : null
      };
    }
  }
}
