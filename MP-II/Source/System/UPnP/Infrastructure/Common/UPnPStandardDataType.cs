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
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Common
{
  /// <summary>
  /// Generic class which implements serializing and deserializing functionality for UPnP standard data types,
  /// and providing the set of all UPnP standard data types.
  /// </summary>
  public class UPnPStandardDataType
  {
    protected static IDictionary<string, UPnPStandardDataType> TYPES =
        new Dictionary<string, UPnPStandardDataType>();

    public static UPnPStandardDataType Ui1 = new UPnPStandardDataType("ui1", typeof(Byte));
    public static UPnPStandardDataType Ui2 = new UPnPStandardDataType("ui2", typeof(UInt16));
    public static UPnPStandardDataType Ui4 = new UPnPStandardDataType("ui4", typeof(UInt32));
    public static UPnPStandardDataType I1 = new UPnPStandardDataType("i1", typeof(SByte));
    public static UPnPStandardDataType I2 = new UPnPStandardDataType("i2", typeof(Int16));
    public static UPnPStandardDataType I4 = new UPnPStandardDataType("i4", typeof(Int32));
    public static UPnPStandardDataType Int = new UPnPStandardDataType("int", typeof(int));
    public static UPnPStandardDataType R4 = new UPnPStandardDataType("r4", typeof(Single));
    public static UPnPStandardDataType R8 = new UPnPStandardDataType("r8", typeof(Double));
    public static UPnPStandardDataType Number = new UPnPStandardDataType("number", typeof(double));
    public static UPnPStandardDataType Fixed144 = new UPnPStandardDataType("fixed.14.4", typeof(double));
    public static UPnPStandardDataType Float = new UPnPStandardDataType("float", typeof(float));
    public static UPnPStandardDataType Char = new UPnPStandardDataType("char", typeof(Char));
    public static UPnPStandardDataType String = new UPnPStandardDataType("string", typeof(string));
    public static UPnPStandardDataType Date = new UPnPStandardDataType("date", typeof(DateTime));
    public static UPnPStandardDataType DateTime = new UPnPStandardDataType("dateTime", typeof(DateTime));
    public static UPnPStandardDataType DateTimeTZ = new UPnPStandardDataType("dateTime.tz", typeof(DateTime));
    public static UPnPStandardDataType Time = new UPnPStandardDataType("time", typeof(DateTime));
    public static UPnPStandardDataType TimeTZ = new UPnPStandardDataType("time.tz", typeof(DateTime));
    public static UPnPStandardDataType Boolean = new UPnPStandardDataType("boolean", typeof(bool));
    public static UPnPStandardDataType BinBase64 = new UPnPStandardDataType("bin.base64", typeof(byte[]));
    public static UPnPStandardDataType BinHex = new UPnPStandardDataType("bin.hex", typeof(byte[]));
    public static UPnPStandardDataType Uri = new UPnPStandardDataType("uri", typeof(Uri));
    public static UPnPStandardDataType Uuid = new UPnPStandardDataType("uuid", typeof(string));

    protected string _upnpTypeName;
    protected Type _dotNetType;

    private UPnPStandardDataType(string upnpTypeName, Type dotNetType)
    {
      _upnpTypeName = upnpTypeName;
      _dotNetType = dotNetType;
      TYPES.Add(_upnpTypeName, this);
    }

    public static UPnPStandardDataType ParseStandardType(string typeStr)
    {
      foreach (UPnPStandardDataType type in TYPES.Values)
      {
        if (type.UPnPTypeName == typeStr)
          return type;
      }
      return null;
    }

    public string UPnPTypeName
    {
      get { return _upnpTypeName; }
    }

    public Type DotNetType
    {
      get { return _dotNetType; }
    }

    public string SoapSerializeValue(object value)
    {
      switch (_upnpTypeName)
      {
        case "ui1":
        case "ui2":
        case "ui4":
        case "i1":
        case "i2":
        case "i4":
        case "int":
          return value.ToString();
        case "r4":
        case "r8":
        case "number":
        case "float":
          return ((Double) value).ToString("E");
        case "fixed.14.4":
          return ((Double) value).ToString("0.##############E+0");
        case "char":
        case "string":
        case "uuid":
          return EncodingUtils.XMLEscape(value.ToString());
        case "date":
          return ((DateTime) value).ToString("yyyy-MM-dd");
        case "dateTime":
          return ((DateTime) value).ToString("s");
        case "dateTime.tz":
          return ((DateTime) value).ToUniversalTime().ToString("u");
        case "time":
          return ((DateTime) value).ToString("T");
        case "time.tz":
          return ((DateTime) value).ToUniversalTime().ToString("hh:mm:ss");
        case "boolean":
          return ((bool) value) ? "1" : "0";
        case "bin.base64":
          return Convert.ToBase64String((byte[]) value);
        case "bin.hex":
          return EncodingUtils.ToHexString((byte[]) value);
        case "uri":
          return EncodingUtils.XMLEscape(((Uri) value).AbsoluteUri);
        default:
          throw new NotImplementedException(string.Format("UPnP standard data type '{0}' is not implemented", _upnpTypeName));
      }
    }

    public object SoapDeserializeValue(string serializedValue)
    {
      switch (_upnpTypeName)
      {
        case "ui1":
          return Byte.Parse(serializedValue);
        case "ui2":
          return UInt16.Parse(serializedValue);
        case "ui4":
          return UInt32.Parse(serializedValue);
        case "i1":
          return SByte.Parse(serializedValue);
        case "i2":
          return Int16.Parse(serializedValue);
        case "i4":
          return Int32.Parse(serializedValue);
        case "int":
          return int.Parse(serializedValue);
        case "r4":
          return Single.Parse(serializedValue);
        case "r8":
          return Double.Parse(serializedValue);
        case "number":
          return double.Parse(serializedValue);
        case "fixed.14.4":
          return double.Parse(serializedValue);
        case "float":
          return float.Parse(serializedValue);
        case "char":
          if (serializedValue.Length != 1)
            throw new ArgumentException(string.Format("String '{0}' is not compatible with char", serializedValue));
          return serializedValue[0];
        case "string":
        case "uuid":
          return EncodingUtils.XMLUnescape(serializedValue);
        case "date":
          return System.DateTime.Parse(serializedValue);
        case "dateTime":
          return System.DateTime.Parse(serializedValue);
        case "dateTime.tz":
          return System.DateTime.Parse(serializedValue);
        case "time":
          return System.DateTime.Parse(serializedValue);
        case "time.tz":
          return System.DateTime.Parse(serializedValue);
        case "boolean":
          return bool.Parse(serializedValue);
        case "bin.base64":
          return Convert.FromBase64String(serializedValue);
        case "bin.hex":
          return EncodingUtils.FromHexString(serializedValue);
        case "uri":
          return new Uri(EncodingUtils.XMLUnescape(System.Web.HttpUtility.UrlDecode(serializedValue)));
        default:
          throw new NotImplementedException(string.Format("UPnP standard data type '{0}' is not implemented", _upnpTypeName));
      }
    }

    public double GetNumericValue(object val)
    {
      switch (_upnpTypeName)
      {
        case "ui1":
        case "ui2":
        case "ui4":
        case "i1":
        case "i2":
        case "i4":
        case "int":
        case "r4":
        case "r8":
        case "number":
        case "fixed.14.4":
        case "float":
          return Convert.ToDouble(val);
        default:
          return 0;
      }
    }
  }
}
