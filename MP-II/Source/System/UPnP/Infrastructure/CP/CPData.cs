#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
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

using UPnP.Infrastructure.CP.SSDP;

namespace UPnP.Infrastructure.CP
{
  /// <summary>
  /// Contains data shared throughout the control point system.
  /// </summary>
  public class CPData
  {
    protected object _syncObj = new object();
    protected uint _httpPort = 0;
    protected SSDPClientController _ssdpClientController = null;

    /// <summary>
    /// Synchronization object for the UPnP control point system.
    /// </summary>
    public object SyncObj
    {
      get { return _syncObj; }
    }

    /// <summary>
    /// Gets or sets the HTTP listening port for used for event messages.
    /// </summary>
    public uint HttpPort
    {
      get { return _httpPort; }
      internal set { _httpPort = value; }
    }

    /// <summary>
    /// Gets or sets the SSDP controller of the UPnP client.
    /// </summary>
    public SSDPClientController SSDPController
    {
      get { return _ssdpClientController; }
      internal set { _ssdpClientController = value; }
    }
  }
}
