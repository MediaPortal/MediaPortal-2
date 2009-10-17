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

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.XPath;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.CP.DeviceTree;

namespace UPnP.Infrastructure.CP.GENA
{
  public class GENAHandler
  {
    public static HttpStatusCode HandleEventNotification(Stream stream, Encoding streamEncoding, CpService service,
        UPnPVersion upnpVersion)
    {
      try
      {
        // Parse XML document
        XPathDocument doc = new XPathDocument(new StreamReader(stream, streamEncoding));
        XPathNavigator nav = doc.CreateNavigator();
        nav.MoveToChild(XPathNodeType.Element);
        if (nav.LocalName != "propertyset" || nav.NamespaceURI != "urn:schemas-upnp-org:event-1-0")
          return HttpStatusCode.BadRequest;
        XPathNodeIterator propertyIt = nav.SelectChildren("property", "urn:schemas-upnp-org:event-1-0");
        while (propertyIt.MoveNext())
        {
          XPathNavigator variableNav = propertyIt.Current.Clone();
          if (variableNav.MoveToChild(XPathNodeType.Element))
            HandleVariableChangeNotification(variableNav, service, upnpVersion);
        }
        return HttpStatusCode.OK;
      }
      catch (Exception)
      {
        return HttpStatusCode.BadRequest;
      }
    }

    protected static void HandleVariableChangeNotification(XPathNavigator variableNav, CpService service, UPnPVersion upnpVersion)
    {
      string variableName = variableNav.LocalName;
      CpStateVariable stateVariable;
      if (!service.StateVariables.TryGetValue(variableName, out stateVariable))
        // We don't know that variable. This is no error case, as the service might have
        // only a subset of state variable templates defined
        return;
      object value = stateVariable.DataType.SoapDeserializeValue(variableNav, upnpVersion.VerMin == 0);
      stateVariable.Value = value;
    }
  }
}
