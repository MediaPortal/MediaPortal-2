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

using System.Xml;

namespace UPnP.Infrastructure.Utils
{
  public static class SoapHelper
  {
    public static void WriteSoapEnvelopeStart(XmlWriter writer, bool addXSINamespace)
    {
      writer.WriteStartDocument();
      writer.WriteStartElement("s", "Envelope", UPnPConsts.NS_SOAP_ENVELOPE);
      if (addXSINamespace)
        writer.WriteAttributeString("xmlns", "xsi", null, UPnPConsts.NS_XSI);
      writer.WriteAttributeString("s", "encodingStyle", null, UPnPConsts.NS_SOAP_ENCODING);
      writer.WriteStartElement("Body", UPnPConsts.NS_SOAP_ENVELOPE);
    }

    public static void WriteSoapEnvelopeEndAndClose(XmlWriter writer)
    {
      writer.WriteEndElement(); // s:Body
      writer.WriteEndElement(); // s:Envelope
      writer.Close();
    }

    public static void WriteNull(XmlWriter writer)
    {
      writer.WriteStartAttribute("null", UPnPConsts.NS_XSI);
      writer.WriteValue(true);
      writer.WriteEndAttribute();
    }

    public static bool IsNull(XmlReader reader)
    {
      bool result = reader.MoveToAttribute("null", UPnPConsts.NS_XSI) && reader.ReadContentAsBoolean();
      reader.MoveToElement();
      return result;
    }

    public static bool ReadNull(XmlReader reader)
    {
      if (IsNull(reader))
      {
        if (reader.IsEmptyElement)
          reader.ReadStartElement();
        else
        {
          reader.ReadStartElement();
          reader.ReadEndElement();
        }
        return true;
      }
      return false;
    }

    public static bool ReadEmptyStartElement(XmlReader reader)
    {
      bool result = reader.IsEmptyElement;
      reader.ReadStartElement();
      return result;
    }

    public static bool ReadEmptyStartElement(XmlReader reader, string name)
    {
      bool result = reader.IsEmptyElement;
      reader.ReadStartElement(name);
      return result;
    }

    public static bool ReadEmptyStartElement(XmlReader reader, string localName, string ns)
    {
      bool result = reader.IsEmptyElement;
      reader.ReadStartElement(localName, ns);
      return result;
    }
  }
}