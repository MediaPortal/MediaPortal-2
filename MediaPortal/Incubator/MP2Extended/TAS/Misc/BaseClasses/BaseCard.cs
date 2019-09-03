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

using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.TAS.Misc.BaseClasses
{
  class BaseCard
  {
    internal static WebCard Card(ICard card)
    {
      return new WebCard
      {
        Id = card.CardId,
        Name = card.Name,
        Priority = card.Priority,
        PreloadCard = card.PreloadCard,
        CAM = card.HasCam,
        CamType = (int)card.CamType,
        DecryptLimit = card.DecryptLimit,
        DevicePath = card.DevicePath,
        Enabled = card.Enabled,
        GrabEPG = card.EpgIsGrabbing,
        //LastEpgGrab = card,
        RecordingFolder = card.RecordingFolder,
        RecordingFormat = card.RecordingFormat,
        TimeShiftFolder = card.TimeshiftFolder,
        SupportSubChannels = card.SupportSubChannels
      };
    }
  }
}
