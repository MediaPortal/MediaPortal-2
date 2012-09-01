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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces
{
  /// <summary>
  /// ITimeshiftControl defines all actions and properties for TV providers timeshift features.
  /// </summary>
  public interface ITimeshiftControl
  {
    /// <summary>
    /// Starts timeshifting a channel an returns the created MediaItem.
    /// </summary>
    /// <param name="slotIndex">Slot Index for Playback (0=Primary, 1=PiP).</param>
    /// <param name="channel">Channel.</param>
    /// <param name="timeshiftMediaItem">Returns the created MediaItem.</param>
    /// <returns>True if succeeded.</returns>
    bool StartTimeshift(int slotIndex, IChannel channel, out MediaItem timeshiftMediaItem);

    /// <summary>
    /// Stops the active timeshifting.
    /// </summary>
    /// <param name="slotIndex">Slot Index to stop (0=Primary, 1=PiP).</param>
    /// <returns>True if succeeded.</returns>
    bool StopTimeshift(int slotIndex);

    /// <summary>
    /// Gets the currently timeshifting channel.
    /// </summary>
    IChannel GetChannel(int slotIndex);
    //TODO: define methods for error and message handling
  }
}
