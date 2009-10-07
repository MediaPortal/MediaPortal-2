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
    protected XmlElement _value;

    public ExtendedDataTypeDummy(string schemaURI, string dataTypeName) : base(schemaURI, dataTypeName) { }

    public override bool SupportsStringEquivalent
    {
      get { return false; }
    }

    public override string SoapSerializeValue(object value, bool forceSimpleValue)
    {
      throw new IllegalCallException("Dummy extended data type cannot serialize values");
    }

    public override object SoapDeserializeValue(XmlElement enclosingElement, bool isSimpleValue)
    {
      throw new IllegalCallException("Dummy extended data type cannot deserialize values");
    }

    public override bool IsAssignableFrom(Type type)
    {
      return false;
    }
  }
}
