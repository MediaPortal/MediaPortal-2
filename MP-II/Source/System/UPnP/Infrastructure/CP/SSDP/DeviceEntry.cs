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

using System.Collections.Generic;

namespace UPnP.Infrastructure.CP.SSDP
{
  /// <summary>
  /// Contains SSDP advertisement data for a the advertisements of a special device.
  /// The entries are lazily initialized, as the SSDP protocol doesn't provide a strict order of advertisement messages of
  /// the single entries.
  /// </summary>
  public class DeviceEntry
  {
    protected string _deviceType;
    protected int _deviceTypeVersion;
    protected string _uuid; // UUID of the device
    protected ICollection<string> _services = new HashSet<string>(); // Types and versions of the services

    /// <summary>
    /// Creates a new <see cref="DeviceEntry"/> instance.
    /// </summary>
    /// <param name="uuid">UUID of the device which gets represented by the new instance.</param>
    public DeviceEntry(string uuid)
    {
      _uuid = uuid;
    }

    /// <summary>
    /// UUID of the device which gets represented by this instance.
    /// </summary>
    public string UUID
    {
      get { return _uuid; }
    }

    /// <summary>
    /// Gets or sets the type of the represented device.
    /// </summary>
    public string DeviceType
    {
      get { return _deviceType; }
      internal set { _deviceType = value; }
    }

    /// <summary>
    /// Gets or sets the type version of the represented device.
    /// </summary>
    public int DeviceTypeVersion
    {
      get { return _deviceTypeVersion; }
      internal set { _deviceTypeVersion = value; }
    }

    /// <summary>
    /// Returns the device type URN with version, in the format "urn:schemas-upnp-org:device:[device-type]:[version]" or
    /// "urn:domain-name:device:[device-type]:[version]".
    /// </summary>
    public string DeviceTypeVersion_URN
    {
      get { return "urn:" + _deviceType + ":" + _deviceTypeVersion; }
    }

    /// <summary>
    /// Returns a collection of service type URNs with version, in the format
    /// "urn:schemas-upnp-org:service:[service-type]:[version]" or "urn:domain-name:service:[service-type]:[version]".
    /// </summary>
    public ICollection<string> Services
    {
      get { return _services; }
    }
  }
}
