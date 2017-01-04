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

using System.Collections.Generic;
using System.Text;
using System.Xml;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv.GENA
{
  public class GENAMessageBuilder
  {
    public static string BuildEventNotificationMessage(IEnumerable<DvStateVariable> variables, bool forceSimpleValue)
    {
      StringBuilder result = new StringBuilder(1000);
      using (StringWriterWithEncoding stringWriter = new StringWriterWithEncoding(result, UPnPConsts.UTF8_NO_BOM))
      using (XmlWriter writer = XmlWriter.Create(stringWriter, UPnPConfiguration.DEFAULT_XML_WRITER_SETTINGS))
      {
        writer.WriteStartDocument();
        writer.WriteStartElement("e", "propertyset", UPnPConsts.NS_UPNP_EVENT);
        writer.WriteAttributeString("xmlns", "xsi", null, UPnPConsts.NS_XSI);
        foreach (DvStateVariable variable in variables)
        {
          writer.WriteStartElement("property", UPnPConsts.NS_UPNP_EVENT);
          writer.WriteStartElement(variable.Name);
          variable.DataType.SoapSerializeValue(variable.Value, forceSimpleValue, writer);
          writer.WriteEndElement(); // variable.Name
          writer.WriteEndElement(); // property
        }
        writer.WriteEndElement(); // propertyset
        writer.WriteEndDocument();
        writer.Close();
      }
      return result.ToString();
    }
  }
}
