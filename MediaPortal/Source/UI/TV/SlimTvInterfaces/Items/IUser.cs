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

namespace MediaPortal.Plugins.SlimTv.Interfaces.Items
{
  public enum SlimTvStoppedReason
  {
    UnknownReason = 0,
    RecordingStarted = 1,
    KickedByAdmin = 2,
    HeartBeatTimeOut = 3,
    OwnerChangedTS = 4,
  }

  public enum SlimTvChannelState
  {
    nottunable = 0,
    tunable = 1,
    timeshifting = 2,
    recording = 3,
  }
  
  /// <summary>
  /// ICard represents a card.
  /// </summary>
  public interface IUser
  {
    int CardId { get; set; }
    Dictionary<int, SlimTvChannelState> ChannelStates { get; set; }
    int FailedCardId { get; set; }
    DateTime HeartBeat { get; set; }
    object History { get; set; }
    int IdChannel { get; set; }
    bool IsAdmin { get; set; }
    string Name { get; set; }
    int? Priority { get; set; }
    int SubChannel { get; set; }
    SlimTvStoppedReason TvStoppedReason { get; set; }
  }
}
