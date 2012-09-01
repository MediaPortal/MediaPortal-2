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
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces
{
  /// <summary>
  /// ITvHandler defines all actions and properties for the TvHandler.
  /// </summary>
  public interface ITvHandler: IDisposable
  {
    /// <summary>
    /// Initializes internal structures. This has to be called before any other methods can be used.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Exposes the ITimeshiftControl interface of active TvProvider.
    /// </summary>
    ITimeshiftControl TimeshiftControl { get; }

    /// <summary>
    /// Exposes the IChannelAndGroupInfo interface of active TvProvider.
    /// </summary>
    IChannelAndGroupInfo ChannelAndGroupInfo { get; }

    /// <summary>
    /// Exposes the IProgramInfo interface of active TvProvider.
    /// </summary>
    IProgramInfo ProgramInfo { get; }

    /// <summary>
    /// Exposes the IScheduleControl interface of active TvProvider.
    /// </summary>
    IScheduleControl ScheduleControl { get; }

    /// <summary>
    /// Uses the <see cref="TimeshiftControl"/> to start timeshifting and the playback of
    /// the created MediaItem.
    /// </summary>
    /// <param name="slotIndex">Slot Index for Playback (0=Primary, 1=PiP).</param>
    /// <param name="channel">Channel.</param>
    /// <returns>True if succeeded.</returns>
    bool StartTimeshift(int slotIndex, IChannel channel);

    /// <summary>
    /// Stops the active Timeshift.
    /// </summary>
    /// <param name="slotIndex">Slot Index to stop (0=Primary, 1=PiP).</param>
    /// <returns>True if succeeded.</returns>
    bool StopTimeshift(int slotIndex);

    /// <summary>
    /// Disposes an open Slot. Usually a StopTimeshift is called, except a ResourceAccessor was
    /// changed due to a card change. Then timeshift continues.
    /// </summary>
    /// <param name="slotIndex">Slot Index to Dispose.</param>
    /// <returns>True if succeeded.</returns>
    bool DisposeSlot(int slotIndex);

    /// <summary>
    /// Gets a value how many slots are currently used for timeshifting (0..2).
    /// </summary>
    int NumberOfActiveSlots { get; }

    /// <summary>
    /// Gets the active channel from the slot.
    /// </summary>
    /// <param name="slotIndex">Slot Index (0=Primary, 1=PiP).</param>
    /// <returns>Channel</returns>
    IChannel GetChannel(int slotIndex);
  }
}
