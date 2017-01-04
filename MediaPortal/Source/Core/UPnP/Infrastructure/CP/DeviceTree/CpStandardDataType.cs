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

using System;
using System.Xml;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// Device descriptor class for all UPnP standard data types.
  /// </summary>
  public class CpStandardDataType : CpDataType
  {
    protected UPnPStandardDataType _dataType;

    public CpStandardDataType(UPnPStandardDataType dataType)
    {
      _dataType = dataType;
    }

    public UPnPStandardDataType DataType
    {
      get { return _dataType; }
    }

    public override void SoapSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      _dataType.SoapSerializeValue(value, writer);
    }

    public override object SoapDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      return _dataType.SoapDeserializeValue(reader);
    }

    public override bool IsAssignableFrom(Type type)
    {
      return _dataType.DotNetType.IsAssignableFrom(type);
    }
  }
}
