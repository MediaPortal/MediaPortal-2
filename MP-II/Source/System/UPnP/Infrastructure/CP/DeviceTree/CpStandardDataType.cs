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
using System.Xml;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// Device descriptor class for all UPnP standard data types.
  /// </summary>
  public class CpStandardDataType : CpDataType
  {
    protected UPnPStandardDataType _type;

    public CpStandardDataType(UPnPStandardDataType type)
    {
      _type = type;
    }

    public UPnPStandardDataType Type
    {
      get { return _type; }
    }

    public override string SoapSerializeValue(object value, bool forceSimpleValue)
    {
      return _type.SoapSerializeValue(value);
    }

    public override object SoapDeserializeValue(XmlElement enclosingElement, bool isSimpleValue)
    {
      string serializedValue = ParserHelper.SelectText(enclosingElement, "text()");
      return _type.SoapDeserializeValue(serializedValue);
    }

    public override bool IsAssignableFrom(Type type)
    {
      return _type.DotNetType.IsAssignableFrom(type);
    }
  }
}
