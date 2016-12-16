#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Net.NetworkInformation;

namespace MediaPortal.Utilities.Network
{
  /// <summary>
  /// Active class which provides the information about the availability of a network connection.
  /// When the network availability changes, that change is automatically reflected by the property
  /// <see cref="IsNetworkConnected"/>.
  /// </summary>
  public static class NetworkConnectionTracker
  {
    private static volatile bool _isNetworkAvailable;

    static NetworkConnectionTracker()
    {
      Install();
      _isNetworkAvailable = NetworkUtils.IsNetworkAvailable(null, false);
    }

    /// <summary>
    /// Installs the network change handlers in the system. This makes the property <see cref="IsNetworkConnected"/>
    /// updated automatically.
    /// </summary>
    public static void Install()
    {
      NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
      NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
    }

    /// <summary>
    /// Uninstalls the network change handlers from the system.
    /// </summary>
    public static void Uninstall()
    {
      NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
      NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
    }

    private static void OnNetworkAddressChanged(object sender, EventArgs e)
    {
      _isNetworkAvailable = NetworkUtils.IsNetworkAvailable(null, false);
    }

    private static void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
    {
      _isNetworkAvailable = NetworkUtils.IsNetworkAvailable(null, false);
    }

    /// <summary>
    /// Indicates whether any network connection is currently available. This property uses a cache which is
    /// automatically updated when change handlers are installed (<see cref="Install"/>).
    /// </summary>
    /// <remarks>
    /// Connections with virtual network cards are NOT filtered out.
    /// </remarks>
    /// <value>
    /// <c>true</c> if a network connection is available; otherwise, <c>false</c>. The microsoft loopback
    /// adapter is not considered.
    /// </value>
    public static bool IsNetworkConnected
    {
      get { return _isNetworkAvailable; }
    }
  }
}