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
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Device descriptor class for all UPnP standard data types.
  /// </summary>
  public class DvStandardDataType : DvDataType
  {
    protected UPnPStandardDataType _type;

    public DvStandardDataType(UPnPStandardDataType type)
    {
      _type = type;
    }

    public override string SoapSerializeValue(object value, bool forceSimpleValue)
    {
      return _type.SoapSerializeValue(value);
    }

    public override object SoapDeserializeValue(XmlElement enclosingElement, bool isSimpleValue)
    {
      XmlText text = (XmlText) enclosingElement.SelectSingleNode("text()");
      string serializedValue = text == null ? string.Empty : text.Data;
      return _type.SoapDeserializeValue(serializedValue);
    }

    public override bool IsAssignableFrom(Type type)
    {
      return _type.DotNetType.IsAssignableFrom(type);
    }

    public double GetNumericValue(object val)
    {
      return _type.GetNumericValue(val);
    }

    public double GetNumericDelta(object value1, object value2)
    {
      return GetNumericValue(value1) - GetNumericValue(value2);
    }

    #region Description generation

    internal override void AddSCDPDescriptionForStandardDataType(StringBuilder result,
        IDictionary<string, string> dataTypeSchemas2NSPrefix)
    {
      result.Append(
          "<dataType>");
      result.Append(_type.UPnPTypeName);
      result.Append("</dataType>");
    }

    #endregion
  }
}
