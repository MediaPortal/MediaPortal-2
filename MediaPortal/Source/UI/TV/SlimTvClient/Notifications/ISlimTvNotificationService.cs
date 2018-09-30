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

using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using System;

namespace MediaPortal.Plugins.SlimTv.Client.Notifications
{
  /// <summary>
  /// Interface for a service to monitor the TV server state and show notifications for TV server events.
  /// </summary>
  public interface ISlimTvNotificationService
  {
    /// <summary>
    /// Current Tv server state.
    /// </summary>
    TvServerState CurrentTvServerState { get; }

    /// <summary>
    /// Shows the notification using the super layer specified in the <paramref name="notification"/>. 
    /// </summary>
    /// <param name="notification">The notification to show.</param>
    /// <param name="duration">Duration to show the given notification.</param>
    void ShowNotification(ISlimTvNotification notification, TimeSpan duration);
  }
}
