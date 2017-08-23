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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Xml.Serialization;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.Network;

namespace MediaPortal.Common.General
{
  /// <summary>
  /// Identifier for a computer in an IP network. Contains its host name or IP address.
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

    public const string LOCALHOST = "localhost";
    public const string LOOPBACK_IPv4_ADDRESS = "127.0.0.1";
    public const string LOOPBACK_IPv6_ADDRESS = "[::1]";

    #endregion

    #region Protected fields

    protected string _address;
    protected string _hostName;
    protected string[] _aliases;

    protected static string LOCAL_HOST_DNS_NAME;
    protected readonly string[] EMPTY_ALIAS_LIST = new string[0];
    protected static ICollection<string> _failedLookups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    static SystemName()
    {
      LOCAL_HOST_DNS_NAME = Dns.GetHostName();
    }

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new system name for the specified host.
    /// </summary>
    /// <param name="hostName">A DNS host name or IP address.
    /// In case of an IPv6 address, the address can also be given enclosed by <c>[</c> and <c>]</c> brackets.</param>
    public SystemName(string hostName)
    {
      _address = GetCanonicalForm(hostName);
      InitializeAliases(_address);
    }

    protected void InitializeAliases(string hostName)
    {
      if (_failedLookups.Contains(hostName))
        return;

      List<string> aliases = new List<string>();
      try
      {
        IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
        _hostName = hostEntry.HostName;
        aliases.AddRange(hostEntry.Aliases.Select(GetCanonicalForm));
        aliases.AddRange(hostEntry.AddressList.Select(NetworkUtils.IPAddrToString));
      }
      catch (SocketException e) // Could occur if the nameserver doesn't answer, for example
      {
        ServiceRegistration.Get<ILogger>().Warn("SystemName: Could not retrieve host alias/address list from DNS (hostName: {0})", hostName);
        _failedLookups.Add(hostName);
        aliases.Add(hostName); // Will be used to compare in IsAddressOrAlias
      }
      _aliases = aliases.ToArray();
    }

    #endregion

    /// <summary>
    /// Returns the host name or ip address of the specified system.
    /// </summary>
    [XmlIgnore]
    public string Address
    {
      get { return _address; }
    }

    /// <summary>
    /// Returns the name of the local computer.
    /// </summary>
    public static string LocalHostName
    {
      get { return LOCAL_HOST_DNS_NAME; }
    }

    /// <summary>
    /// Returns a new <see cref="SystemName"/> specifying the loopback adapter.
    /// </summary>
    /// <returns>Loopback adapter address.</returns>
    public static SystemName Loopback()
    {
      return new SystemName(LOCALHOST);
    }

    public static SystemName GetLocalSystemName()
    {
      return new SystemName(LOCAL_HOST_DNS_NAME);
    }

    public string HostName
    {
      get { return _hostName; }
    }

    public bool IsAddressOrAlias(string address)
    {
      var cnAddr = GetCanonicalForm(address);
      return _aliases.Any(a => string.Equals(a, cnAddr, StringComparison.InvariantCultureIgnoreCase));
    }

    public bool IsLocalSystem()
    {
      // localhost, 127.0.0.1, [::1]
      if (string.Equals(_address, LOCALHOST, StringComparison.InvariantCultureIgnoreCase) ||
          _address == LOOPBACK_IPv4_ADDRESS || _address == LOOPBACK_IPv6_ADDRESS)
        return true;
      if (string.Equals(_address, LOCAL_HOST_DNS_NAME, StringComparison.InvariantCultureIgnoreCase))
        return true;
      return IsALocalAddress();
    }

    protected string GetCanonicalForm(string address)
    {
      address = address.ToLowerInvariant();
      if (address.StartsWith("[") && address.EndsWith("]"))
        address = address.Substring(1, address.Length-2);
      IPAddress ipAddress;
      if (IPAddress.TryParse(address, out ipAddress))
        address = NetworkUtils.IPAddrToString(ipAddress);
      return address;
    }

    protected bool IsALocalAddress()
    {
      IPAddress thisAddress;
      if (_address == null)
        return false;
      if (!IPAddress.TryParse(_address, out thisAddress))
        return false;
      foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
      { // For each network interface adapter
        IPInterfaceProperties properties = adapter.GetIPProperties();
        foreach (UnicastIPAddressInformation info in properties.UnicastAddresses)
        { // For each address of that adapter
          IPAddress address = info.Address;
          if (address.AddressFamily == AddressFamily.InterNetworkV6)
          {
            // IPAddress.ToString() adds the scope id; remove it
            string addrStr = NetworkUtils.IPAddrToString(address);
            int i = addrStr.IndexOf('%');
            if (i >= 0) addrStr = addrStr.Substring(0, i);
            address = IPAddress.Parse(addrStr);
          }
          if (address.Equals(thisAddress))
            return true;
        }
      }
      return false;
    }

    public static bool operator==(SystemName a, SystemName b)
    {
      bool s2null = ReferenceEquals(b, null);
      if (ReferenceEquals(a, null))
        return s2null;
      if (s2null)
        return false;
      return a.Equals(b);
    }

    public static bool operator!=(SystemName first, SystemName second)
    {
      return !(first == second);
    }

    public bool Equals(SystemName obj)
    {
      if (obj == null)
        return false;
      if (IsLocalSystem() && obj.IsLocalSystem())
        return true;
      return IsAddressOrAlias(obj._address);
    }

    public override bool Equals(object obj)
    {
      if (!(obj is SystemName))
        return false;
      return Equals((SystemName) obj);
    }

    public override int GetHashCode()
    {
      return _address.GetHashCode();
    }

    public override string ToString()
    {
      return _address;
    }

    #region Additional members for the XML serialization

    internal SystemName() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Address")]
    public string XML_Address
    {
      get { return _address; }
      set
      {
        _address = value;
        InitializeAliases(value);
      }
    }

    #endregion
  }
}
