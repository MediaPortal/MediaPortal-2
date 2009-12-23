#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *  Copyright (C) 2007-2009 Team MediaPortal
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