using System;
using System.Collections.Generic;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Misc;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetActiveCards
  {
    public IList<WebVirtualCard> Process()
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetActiveCards: ITvProvider not found");

      ITunerInfo tunerInfo = ServiceRegistration.Get<ITvProvider>() as ITunerInfo;

      if (tunerInfo == null)
        throw new BadRequestException("GetActiveCards: ITunerInfo not present");

      List<IVirtualCard> cards;
      tunerInfo.GetActiveVirtualCards(out cards);

      List<WebVirtualCard> output = new List<WebVirtualCard>();

      foreach (var card in cards)
      {
        output.Add(new WebVirtualCard
        {
          BitRateMode = (int)card.BitRateMode,
          ChannelName = card.ChannelName,
          Device = card.Device,
          Enabled = card.Enabled,
          GetTimeshiftStoppedReason = (int)card.GetTimeshiftStoppedReason,
          GrabTeletext = card.GrabTeletext,
          HasTeletext = card.HasTeletext,
          Id = card.Id,
          ChannelId = card.ChannelId,
          IsGrabbingEpg = card.IsGrabbingEpg,
          IsRecording = card.IsRecording,
          IsScanning = card.IsScanning,
          IsScrambled = card.IsScrambled,
          IsTimeShifting = card.IsTimeShifting,
          IsTunerLocked = card.IsTunerLocked,
          MaxChannel = card.MaxChannel,
          MinChannel = card.MinChannel,
          Name = card.Name,
          QualityType = (int)card.QualityType,
          RecordingFileName = card.RecordingFileName,
          RecordingFolder = card.RecordingFolder,
          RecordingFormat = card.RecordingFormat,
          RecordingScheduleId = card.RecordingScheduleId,
          RecordingStarted = card.RecordingStarted != DateTime.MinValue ? card.RecordingStarted : new DateTime(2000, 1, 1),
          RemoteServer = card.RemoteServer,
          RTSPUrl = card.RTSPUrl,
          SignalLevel = card.SignalLevel,
          SignalQuality = card.SignalQuality,
          TimeShiftFileName = card.TimeShiftFileName,
          TimeShiftFolder = card.TimeShiftFolder,
          TimeShiftStarted = card.TimeShiftStarted != DateTime.MinValue ? card.TimeShiftStarted : new DateTime(2000, 1, 1),
          Type = (WebCardType)card.Type,
          User = card.User != null ? new WebUser
          {
            ChannelId = card.User.IdChannel,
            Name = card.User.Name,
            CardId = card.User.CardId,
            HeartBeat = card.User.HeartBeat,
            IsAdmin = card.User.IsAdmin,
            SubChannel = card.User.SubChannel,
            TvStoppedReason = (int)card.User.TvStoppedReason
          } : null
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