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

using System;
using System.Xml;
using System.Xml.XPath;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.Utils
{
  public class ParserHelper
  {
    /// <summary>
    /// Given the <paramref name="userAgentStr"/> from an HTTP USER-AGENT header, this method extracts the UPnP version
    /// from the string.
    /// </summary>
    /// <param name="userAgentStr">USER-AGENT header entry of the form "OS/version UPnP/1.1 product/version".</param>
    /// <param name="minorVersion">Returns the minor version number in the specified <paramref name="userAgentStr"/>.</param>
    /// <returns><c>true</c>, if the user agent string could successfully be parsed and denotes a UPnP major version of 1.</returns>
    /// <exception cref="MediaPortal.Utilities.Exceptions.InvalidDataException">If the specified header value is malformed.</exception>
    public static bool ParseUserAgentUPnP1MinorVersion(string userAgentStr, out int minorVersion)
    {
      if (string.IsNullOrEmpty(userAgentStr))
      {
        minorVersion = 0;
        return false;
      }
      UPnPVersion ver;
      if (!UPnPVersion.TryParseFromUserAgent(userAgentStr, out ver))
      {
        if (UPnPConfiguration.LAX_USER_AGENT_PARSING)
        {
          // If a client sent a malformed USER-AGENT, we'll assume UPnP Version 1.0
          minorVersion = 0;
          return true;
        }
        throw new UnsupportedRequestException(string.Format("Unsupported USER-AGENT header entry '{0}'", userAgentStr));
      }
      minorVersion = 0;
      if (ver.VerMax != 1)
        return false;
      minorVersion = ver.VerMin;
      return true;
    }

    public static bool TryParseTypeVersion_URN(string typeVersionURN, out string type, out int version)
    {
      type = null;
      version = 0;
      if (string.IsNullOrEmpty(typeVersionURN))
        return false;
      int index = typeVersionURN.LastIndexOf(':');
      if (!typeVersionURN.StartsWith("urn:") || index == -1)
        return false;
      type = typeVersionURN.Substring("urn:".Length, index - "urn:".Length); // Type without "urn:" prefix and without version suffix
      string versionStr = typeVersionURN.Substring(index + 1); // Version suffix
      return int.TryParse(versionStr, out version); // We don't permit version numbers which aren't integers.
    }

    /// <summary>
    /// Parses a USN string of the form <c>uuid:device-UUID::remainder</c>. The remainder is most often a type+version URN of
    /// a service, but sometimes it is also used for device types or other information (see SSDP NOTIFY messages).
    /// </summary>
    /// <param name="usn">String of the form <c>uuid:device-UUID::remainder</c>.</param>
    /// <param name="deviceUUID">Returns the device UUID of the given <paramref name="usn"/> string.</param>
    /// <param name="remainingPart">Returns the remainder of the given <paramref name="usn"/> string (the part
    /// after the <c>::</c>).</param>
    /// <returns><c>true</c>, if the given string has a correct format and could be parsed, else <c>false</c>.</returns>
    public static bool TryParseUSN(string usn, out string deviceUUID, out string remainingPart)
    {
      deviceUUID = null;
      remainingPart = null;
      if (string.IsNullOrEmpty(usn))
        return false;
      int separatorIndex = usn.IndexOf("::");
      if (separatorIndex < 6 ||  // separatorIndex == -1 or separatorIndex not after "uuid:" prefix with at least one char UUID
          usn.Substring(0, 5) != "uuid:")
        return false;
      deviceUUID = usn.Substring(5, separatorIndex - 5);
      remainingPart = usn.Substring(separatorIndex + 2);
      return true;
    }

    public static bool TryParseDataTypeReference(string typeStr, XPathNavigator dataTypeElementNav,
        out string schemaURI, out string dataTypeName)
    {
      schemaURI = null;
      dataTypeName = null;
      int index = typeStr.LastIndexOf(':');
      if (index == -1)
        return false;
      string prefix = typeStr.Substring(0, index);
      dataTypeName = typeStr.Substring(index + 1);
      schemaURI = prefix.StartsWith("urn:") ? prefix : dataTypeElementNav.GetNamespace(prefix);
      return true;
    }

    /// <summary>
    /// Extracts the UUID from a string containing a UDN (of the form: "uuid:[uuid]").
    /// </summary>
    /// <param name="udn">UDN to break up.</param>
    /// <returns>UUID part of the given <paramref name="udn"/></returns>
    /// <exception cref="ArgumentException">If the given <paramref name="udn"/> doesn't start with "uuid:".</exception>
    public static string ExtractUUIDFromUDN(string udn)
    {
      if (!udn.StartsWith("uuid:"))
        throw new ArgumentException(string.Format("Invalid UDN '{0}'", udn));
      return udn.Substring("uuid:".Length);
    }

    /// <summary>
    /// Returns the text string result of the specified <paramref name="xPathExpr"/> referencing an
    /// XML text node.
    /// </summary>
    /// <param name="elementNav">XPath navigator pointing to an XML element to apply the XPath expression to.</param>
    /// <param name="xPathExpr">XPath expression which references an XML text node (i.e. must end with "text()").</param>
    /// <param name="nsmgr">Namespace resolver for the used namespace prefixes in the <paramref name="xPathExpr"/>.
    /// If set to <c>null</c>, no namespace resolver will be used.</param>
    /// <returns>Contents of the referenced XML text node.</returns>
    /// <exception cref="ArgumentException">If the given <paramref name="xPathExpr"/> doesn't reference an
    /// XML text node.</exception>
    public static string SelectText(XPathNavigator elementNav, string xPathExpr, IXmlNamespaceResolver nsmgr)
    {
      XPathNodeIterator it = elementNav.Select(xPathExpr, nsmgr);
      if (it.MoveNext())
        return it.Current.Value;
      throw new ArgumentException(string.Format("Error evaluating XPath expression '{0}'", xPathExpr));
    }
  }
}
