#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Net;
using System.Xml.Serialization;

namespace MediaPortal.Core.General
{
  /// <summary>
  /// Address of a computer in an IP network.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class SystemName
  {
    #region Protected fields

    protected IPAddress _ipAddress;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new computer address with the specified ip address.
    /// </summary>
    /// <param name="ipAddress">IP address to use.</param>
    public SystemName(IPAddress ipAddress)
    {
      _ipAddress = ipAddress;
    }

    #endregion

    /// <summary>
    /// Returns the ip address of the specified system.
    /// </summary>
    [XmlIgnore]
    public IPAddress IPAddress
    {
      get { return _ipAddress; }
    }

    /// <summary>
    /// Returns a new <see cref="SystemName"/> specifying the loopback adapter.
    /// </summary>
    /// <returns>Loopback adapter address.</returns>
    public static SystemName Loopback()
    {
      return new SystemName(System.Net.IPAddress.IPv6Loopback);
    }

    /// <summary>
    /// Returns a collection of system names for all local network cards.
    /// </summary>
    /// <returns>Collection of all local names.</returns>
    public static ICollection<SystemName> GetLocalNames()
    {
      string strHostName = Dns.GetHostName();
      ICollection<SystemName> result = new List<SystemName>();
      foreach (IPAddress ipAddress in Dns.GetHostAddresses(strHostName))
        result.Add(new SystemName(ipAddress));
      return result;
    }

    #region Additional members for the XML serialization

    internal SystemName() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("IPAddressBytes")]
    public byte[] XML_IPAddress
    {
      get { return _ipAddress.GetAddressBytes(); }
      set { _ipAddress = new IPAddress(value); }
    }

    #endregion
  }
}
