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

using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces
{
  /// <summary>
  /// ITimeshiftControl extents <see cref="ITimeshiftControlAsync"/> with methods for server side handling for multiple
  /// clients. Each client needs to be uniquely identified by its userName.
  /// </summary>
  public interface ITimeshiftControlEx : ITimeshiftControlAsync
  {
    /// <summary>
    /// Starts timeshifting a channel an returns the created MediaItem.
    /// </summary>
    /// <param name="userName">Unique name that identifies one TV client.</param>
    /// <param name="slotIndex">Slot Index for Playback (0=Primary, 1=PiP).</param>
    /// <param name="channel">Channel.</param>
    /// <returns>True if succeeded.</returns>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if programs could be found.
    /// <see cref="AsyncResult{T}.Result"/> returns the created MediaItem.
    /// </returns>
    Task<AsyncResult<MediaItem>> StartTimeshiftAsync(string userName, int slotIndex, IChannel channel);

    /// <summary>
    /// Stops the active timeshifting.
    /// </summary>
    /// <param name="userName">Unique name that identifies one TV client.</param>
    /// <param name="slotIndex">Slot Index to stop (0=Primary, 1=PiP).</param>
    /// <returns>True if succeeded.</returns>
    Task<bool> StopTimeshiftAsync(string userName, int slotIndex);
  }
}
