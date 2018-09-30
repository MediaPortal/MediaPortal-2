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
using System.Threading.Tasks;
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
    /// Exposes the ITimeshiftControlAsync interface of active TvProvider.
    /// </summary>
    ITimeshiftControlAsync TimeshiftControl { get; }

    /// <summary>
    /// Exposes the IChannelAndGroupInfoAsync interface of active TvProvider.
    /// </summary>
    IChannelAndGroupInfoAsync ChannelAndGroupInfo { get; }

    /// <summary>
    /// Exposes the IProgramInfoAsync interface of active TvProvider.
    /// </summary>
    IProgramInfoAsync ProgramInfo { get; }

    /// <summary>
    /// Exposes the IScheduleControlAsync interface of active TvProvider.
    /// </summary>
    IScheduleControlAsync ScheduleControl { get; }

    /// <summary>
    /// Uses the <see cref="TimeshiftControl"/> to start timeshifting and the playback of
    /// the created MediaItem.
    /// </summary>
    /// <param name="slotIndex">Slot Index for Playback (0=Primary, 1=PiP).</param>
    /// <param name="channel">Channel.</param>
    /// <returns>True if succeeded.</returns>
    Task<bool> StartTimeshiftAsync(int slotIndex, IChannel channel);

    /// <summary>
    /// Stops the active Timeshift.
    /// </summary>
    /// <param name="slotIndex">Slot Index to stop (0=Primary, 1=PiP).</param>
    /// <returns>True if succeeded.</returns>
    Task<bool> StopTimeshiftAsync(int slotIndex);

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

    /// <summary>
    /// Gets the current recording for the given <paramref name="program"/> and starts playback if possible.
    /// </summary>
    /// <param name="program">Program that is currently recording.</param>
    /// <returns>True if succeeded.</returns>
    Task<bool> WatchRecordingFromBeginningAsync(IProgram program);
  }
}
