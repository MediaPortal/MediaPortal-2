#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
  /// IRecording represents a recording.
  /// </summary>
  public interface IRecording
  {
    /// <summary>
    /// Gets or Sets the Recording ID.
    /// </summary>
    int RecordingId { get; set; }

    /// <summary>
    /// Gets or Sets the Channel ID where this program is on.
    /// </summary>
    int? ChannelId { get; set; }

    /// <summary>
    /// Gets or Sets the Schedule ID.
    /// </summary>
    int? ScheduleId { get; set; }

    /// <summary>
    /// Gets or Sets whether it is a manual recording or not.
    /// </summary>
    bool IsManual { get; set; }

    /// <summary>
    /// Gets or Sets the Title.
    /// </summary>
    String Title { get; set; }

    /// <summary>
    /// Gets or Sets the Long Description.
    /// </summary>
    String Description { get; set; }

    /// <summary>
    /// Gets or Sets the Genre.
    /// </summary>
    String Genre { get; set; }

    /// <summary>
    /// Gets or Sets the Start time.
    /// </summary>
    DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or Sets the End time.
    /// </summary>
    DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the keep method.
    /// </summary>
    KeepMethodType KeepMethod { get; set; }

    /// <summary>
    /// Gets or sets the "keep until" date.
    /// </summary>
    DateTime? KeepDate { get; set; }
  }
}
