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

  /// <summary>
  /// ICard represents a card.
  /// </summary>
  public interface IConflict
  {
    /// <summary>
    /// Gets the Conflict ID.
    /// </summary>    
    int ConflictId { get; }

    /// <summary>
    /// Gets the Card ID.
    /// </summary>    
    int CardId { get; }

    /// <summary>
    /// Gets the Channel ID.
    /// </summary>    
    int ChannelId { get; }

    /// <summary>
    /// Gets the Schedule's ID.
    /// </summary>
    int ScheduleId { get; }

    /// <summary>
    /// Gets the Program's start time.
    /// </summary>
    DateTime ProgramStartTime { get; }

    /// <summary>
    /// Gets the conflicting Schedule's ID.
    /// </summary>
    int ConflictingScheduleId { get; }
  }
}
