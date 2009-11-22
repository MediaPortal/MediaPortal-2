#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *  Copyright (C) 2007-2009 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
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
