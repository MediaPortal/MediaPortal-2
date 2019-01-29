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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Misc;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetActiveCards
  {
    public static async Task<IList<WebVirtualCard>> ProcessAsync(IOwinContext context)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetActiveCards: ITvProvider not found");

      var cards = await TVAccess.GetVirtualCardsAsync(context);

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
