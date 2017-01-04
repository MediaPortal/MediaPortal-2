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

using UPnP.Infrastructure.Dv.DeviceTree;

namespace UPnP.Infrastructure.Dv.SSDP
{
  /// <summary>
  /// Callback interface for the <see cref="DeviceTreeNotificationProducer"/>.
  /// </summary>
  /// <remarks>
  /// Implementations of this interface will produce a special kind of notify message for each of the objects in the
  /// device tree.
  /// </remarks>
  internal interface ISSDPDiscoveryMessageSender
  {
    /// <summary>
    /// Callback method which will be called for each of the NT/USN combinations for each device and service
    /// as specified in the UPnP architecture document.
    /// </summary>
    /// <param name="NT">NT parameter for NOTIFY messages, ST for search response.</param>
    /// <param name="USN">USN parameter.</param>
    /// <param name="rootDevice">Root device for that the message should be sent.</param>
    void SendMessage(string NT, string USN, DvDevice rootDevice);
  }
}
