#region Copyright (C) 2007-2010 Team MediaPortal

/* 
 *  Copyright (C) 2007-2010 Team MediaPortal
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
      using (XmlWriter writer = XmlWriter.Create(new StringWriterWithEncoding(result, Encoding.UTF8), Configuration.DEFAULT_XML_WRITER_SETTINGS))
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
