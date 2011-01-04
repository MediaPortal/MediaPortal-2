#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTvClient.Interfaces
{
  /// <summary>
  /// ITvHandler defines all actions and properties for the TvHandler.
  /// </summary>
  public interface ITvHandler: IDisposable
  {
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
    /// Uses the <see cref="TimeshiftControl"/> to start timeshifting and the playback of
    /// the created MediaItem.
    /// </summary>
    /// <param name="channel">Channel.</param>
    /// <returns>True if succeeded.</returns>
    bool StartTimeshift(IChannel channel);

    /// <summary>
    /// Stops the active Timeshift.
    /// </summary>
    /// <returns>True if succeeded.</returns>
    bool StopTimeshift();
  }
}
