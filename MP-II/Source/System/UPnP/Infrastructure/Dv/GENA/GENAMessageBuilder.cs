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

using System.Collections.Generic;
using System.Text;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace UPnP.Infrastructure.Dv.GENA
{
  public class GENAMessageBuilder
  {
    public static string BuildEventNotificationMessage(IEnumerable<DvStateVariable> variables, bool forceSimpleValue)
    {
      StringBuilder result = new StringBuilder(
          "<?xml version=\"1.0\"?>" +
          "<e:propertyset xmlns:e=\"urn:schemas-upnp-org:event-1-0\">", 1000);
      foreach (DvStateVariable variable in variables)
      {
        result.Append(
            "<e:property>" +
              "<");
        result.Append(variable.Name);
        result.Append(">");
        result.Append(variable.DataType.SoapSerializeValue(variable.Value, forceSimpleValue));
        result.Append("</");
        result.Append(variable.Name);
        result.Append(">" +
            "</e:property>");
      }
      result.Append(
          "</e:propertyset>");
      return result.ToString();
    }
  }
}
