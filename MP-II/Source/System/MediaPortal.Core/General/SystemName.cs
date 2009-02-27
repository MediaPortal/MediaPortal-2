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

using System;
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
    #region Consts

    public const string LOCALHOST_NAME = "localhost";

    #endregion

    #region Protected fields

    protected string _hostName;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new system name for the specified host.
    /// </summary>
    /// <param name="hostName">The DNS host name to use for this <see cref="SystemName"/>.</param>
    public SystemName(string hostName)
    {
      _hostName = hostName.ToLowerInvariant();
    }

    #endregion

    /// <summary>
    /// Returns the ip address of the specified system.
    /// </summary>
    [XmlIgnore]
    public string HostName
    {
      get { return _hostName; }
    }

    /// <summary>
    /// Returns the name of the local computer.
    /// </summary>
    public static string LocalHostName
    {
      get { return Dns.GetHostName(); }
    }

    /// <summary>
    /// Returns a new <see cref="SystemName"/> specifying the loopback adapter.
    /// </summary>
    /// <returns>Loopback adapter address.</returns>
    public static SystemName Loopback()
    {
      return new SystemName(LOCALHOST_NAME);
    }

    public static SystemName GetLocalSystemName()
    {
      return new SystemName(Dns.GetHostName());
    }

    public bool IsLocalSystem()
    {
      return string.Equals(_hostName, LocalHostName, StringComparison.InvariantCultureIgnoreCase) ||
          string.Equals(_hostName, LOCALHOST_NAME, StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool operator==(SystemName first, SystemName second)
    {
      return string.Equals(first.HostName, second.HostName, StringComparison.InvariantCultureIgnoreCase) ||
          (first.IsLocalSystem() && second.IsLocalSystem());
    }

    public static bool operator!=(SystemName first, SystemName second)
    {
      return !(first == second);
    }

    public bool Equals(SystemName obj)
    {
      return obj._hostName == _hostName;
    }

    public override bool Equals(object obj)
    {
      if (!(obj is SystemName))
        return false;
      return Equals((SystemName)obj);
    }

    public override int GetHashCode()
    {
      return _hostName.GetHashCode();
    }

    #region Additional members for the XML serialization

    internal SystemName() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("HostName")]
    public string XML_HostName
    {
      get { return _hostName; }
      set { _hostName = value; }
    }

    #endregion
  }
}
