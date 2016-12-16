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
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// Placeholder for all extended data types at control point side, for which the application didn't provide an
  /// implementation.
  /// </summary>
  public class ExtendedDataTypeDummy : UPnPExtendedDataType
  {
    public ExtendedDataTypeDummy(string schemaURI, string dataTypeName) : base(schemaURI, dataTypeName) { }

    public override bool SupportsStringEquivalent
    {
      get { return false; }
    }

    public override bool IsNullable
    {
      get { return true; }
    }

    public override bool IsAssignableFrom(Type type)
    {
      return false;
    }

    protected override void DoSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      throw new IllegalCallException("Dummy extended data type '{0}' cannot serialize values", _dataTypeName);
    }

    protected override object DoDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      throw new IllegalCallException("Dummy extended data type '{0}' cannot deserialize values", _dataTypeName);
    }
  }
}
