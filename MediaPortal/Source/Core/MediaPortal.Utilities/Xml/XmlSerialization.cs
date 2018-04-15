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

namespace MediaPortal.Utilities.Xml
{
  // This class contains by design some methods which are also in the UPnP library's SOAP helper class(es).
  public class XmlSerialization
  {
    /// <summary>
    /// XML namespace for the XSI scheme.
    /// </summary>
    public const string NS_XSI = "http://www.w3.org/2001/XMLSchema-instance";

    public static void WriteNull(XmlWriter writer)
    {
      writer.WriteStartAttribute("null", NS_XSI);
      writer.WriteValue(true);
      writer.WriteEndAttribute();
    }

    public static bool IsNull(XmlReader reader)
    {
      bool result = reader.MoveToAttribute("null", NS_XSI) && reader.ReadContentAsBoolean();
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
  }
}