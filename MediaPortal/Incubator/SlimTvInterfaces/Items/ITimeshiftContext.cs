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

using System;

namespace MediaPortal.Plugins.SlimTv.Interfaces.Items
{
  /// <summary>
  /// ITimeshiftContext represents a state of timeshifting in progress. The timeshift context changes 
  /// on zapping or on new programs to start.
  /// </summary>
  public interface ITimeshiftContext
  {
    /// <summary>
    /// Gets or Sets the Channel.
    /// </summary>
    IChannel Channel { get; set; }

    /// <summary>
    /// Gets or Sets the current Program.
    /// </summary>
    IProgram Program { get; set; }

    /// <summary>
    /// Gets or Sets the time when the channel was tuned or a new program started.
    /// </summary>
    DateTime TuneInTime { get; set; }

    /// <summary>
    /// Gets or Sets the duration of timeshift.
    /// </summary>
    TimeSpan TimeshiftDuration { get; set; }
  }
}
