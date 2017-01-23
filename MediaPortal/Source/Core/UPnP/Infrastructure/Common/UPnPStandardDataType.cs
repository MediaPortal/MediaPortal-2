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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
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

    /// <summary>
    /// <see cref="Byte"/>.
    /// </summary>
    public static UPnPStandardDataType Ui1 = new UPnPStandardDataType("ui1", typeof(Byte));

    /// <summary>
    /// <see cref="UInt16"/>.
    /// </summary>
    public static UPnPStandardDataType Ui2 = new UPnPStandardDataType("ui2", typeof(UInt16));

    /// <summary>
    /// <see cref="UInt32"/>.
    /// </summary>
    public static UPnPStandardDataType Ui4 = new UPnPStandardDataType("ui4", typeof(UInt32));

    /// <summary>
    /// <see cref="UInt64"/>.
    /// </summary>
    public static UPnPStandardDataType Ui8 = new UPnPStandardDataType("ui8", typeof(UInt64));

    /// <summary>
    /// <see cref="SByte"/>.
    /// </summary>
    public static UPnPStandardDataType I1 = new UPnPStandardDataType("i1", typeof(SByte));

    /// <summary>
    /// <see cref="Int16"/>.
    /// </summary>
    public static UPnPStandardDataType I2 = new UPnPStandardDataType("i2", typeof(Int16));

    /// <summary>
    /// <see cref="Int32"/>.
    /// </summary>
    public static UPnPStandardDataType I4 = new UPnPStandardDataType("i4", typeof(Int32));

    /// <summary>
    /// <see cref="Int64"/>.
    /// </summary>
    public static UPnPStandardDataType I8 = new UPnPStandardDataType("i8", typeof(Int64));

    /// <summary>
    /// <see cref="int"/>.
    /// </summary>
    public static UPnPStandardDataType Int = new UPnPStandardDataType("int", typeof(int));

    /// <summary>
    /// <see cref="Single"/>.
    /// </summary>
    public static UPnPStandardDataType R4 = new UPnPStandardDataType("r4", typeof(Single));

    /// <summary>
    /// <see cref="Double"/>.
    /// </summary>
    public static UPnPStandardDataType R8 = new UPnPStandardDataType("r8", typeof(Double));

    /// <summary>
    /// <see cref="double"/>.
    /// </summary>
    public static UPnPStandardDataType Number = new UPnPStandardDataType("number", typeof(double));

    /// <summary>
    /// <see cref="double"/>.
    /// </summary>
    public static UPnPStandardDataType Fixed144 = new UPnPStandardDataType("fixed.14.4", typeof(double));

    /// <summary>
    /// <see cref="float"/>.
    /// </summary>
    public static UPnPStandardDataType Float = new UPnPStandardDataType("float", typeof(float));

    /// <summary>
    /// <see cref="Char"/>.
    /// </summary>
    public static UPnPStandardDataType Char = new UPnPStandardDataType("char", typeof(Char));

    /// <summary>
    /// <see cref="string"/>.
    /// </summary>
    public static UPnPStandardDataType String = new UPnPStandardDataType("string", typeof(string));

    /// <summary>
    /// <see cref="DateTime"/>.
    /// </summary>
    public static UPnPStandardDataType Date = new UPnPStandardDataType("date", typeof(DateTime));

    /// <summary>
    /// <see cref="DateTime"/>.
    /// </summary>
    public static UPnPStandardDataType DateTime = new UPnPStandardDataType("dateTime", typeof(DateTime));

    /// <summary>
    /// <see cref="DateTime"/>.
    /// </summary>
    public static UPnPStandardDataType DateTimeTZ = new UPnPStandardDataType("dateTime.tz", typeof(DateTime));

    /// <summary>
    /// <see cref="DateTime"/>.
    /// </summary>
    public static UPnPStandardDataType Time = new UPnPStandardDataType("time", typeof(DateTime));

    /// <summary>
    /// <see cref="DateTime"/>.
    /// </summary>
    public static UPnPStandardDataType TimeTZ = new UPnPStandardDataType("time.tz", typeof(DateTime));

    /// <summary>
    /// <see cref="Boolean"/>.
    /// </summary>
    public static UPnPStandardDataType Boolean = new UPnPStandardDataType("boolean", typeof(Boolean));

    /// <summary>
    /// <c>byte[]</c>.
    /// </summary>
    public static UPnPStandardDataType BinBase64 = new UPnPStandardDataType("bin.base64", typeof(byte[]));

    /// <summary>
    /// <c>byte[]</c>.
    /// </summary>
    public static UPnPStandardDataType BinHex = new UPnPStandardDataType("bin.hex", typeof(byte[]));

    /// <summary>
    /// <see cref="Uri"/>.
    /// </summary>
    public static UPnPStandardDataType Uri = new UPnPStandardDataType("uri", typeof(Uri));

    /// <summary>
    /// <see cref="string"/>.
    /// </summary>
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
      return TYPES.Values.FirstOrDefault(type => type.UPnPTypeName == typeStr);
    }

    public string UPnPTypeName
    {
      get { return _upnpTypeName; }
    }

    public Type DotNetType
    {
      get { return _dotNetType; }
    }

    /// <summary>
    /// Serializes the given <paramref name="value"/> as contents of the current element in the
    /// given XML <paramref name="writer"/> in the formatting rules given by this data type.
    /// </summary>
    /// <param name="value">Value to serialize. The value must be of this data type.</param>
    /// <param name="writer">XML writer where the value will be serialized to. The value will be serialized as
    /// contents of the writer's current element.
    /// The writer's position is the start of the parent element, the result should go. The caller will write the end
    /// element tag.</param>
    public void SoapSerializeValue(object value, XmlWriter writer)
    {
      if (value == null)
      {
        SoapHelper.WriteNull(writer);
        return;
      }
      switch (_upnpTypeName)
      {
        case "ui1":
        case "ui2":
        case "ui4":
        case "ui8":
        case "i1":
        case "i2":
        case "i4":
        case "i8":
        case "int":
          writer.WriteString(value.ToString());
          break;
        case "r4":
        case "r8":
        case "number":
        case "float":
          writer.WriteString(((Double) value).ToString("E", CultureInfo.InvariantCulture));
          break;
        case "fixed.14.4":
          writer.WriteString(((Double) value).ToString("0.##############E+0", CultureInfo.InvariantCulture));
          break;
        case "char":
        case "string":
        case "uuid":
          writer.WriteString(value.ToString());
          break;
        case "date":
          writer.WriteString(((DateTime) value).ToString("yyyy-MM-dd"));
          break;
        case "dateTime":
          writer.WriteString(((DateTime) value).ToString("s"));
          break;
        case "dateTime.tz":
          writer.WriteString(((DateTime) value).ToUniversalTime().ToString("u"));
          break;
        case "time":
          writer.WriteString(((DateTime) value).ToString("T"));
          break;
        case "time.tz":
          writer.WriteString(((DateTime) value).ToUniversalTime().ToString("hh:mm:ss"));
          break;
        case "boolean":
          writer.WriteValue((bool) value);
          break;
        case "bin.base64":
          writer.WriteValue(value);
          break;
        case "bin.hex":
          writer.WriteString(EncodingUtils.ToHexString((byte[]) value));
          break;
        case "uri":
          writer.WriteString(((Uri) value).AbsoluteUri);
          break;
        default:
          throw new NotImplementedException(string.Format("UPnP standard data type '{0}' is not implemented", _upnpTypeName));
      }
    }

    /// <summary>
    /// Deserializes the contents of the given XML <paramref name="reader"/>'s current element in the formatting rules
    /// given by this data type.
    /// </summary>
    /// <param name="reader">XML reader whose current element's value will be deserialized.
    /// The reader's position is the start of the parent element, the result should go. After this method returns, the reader
    /// must have read the end element.</param>
    /// <returns>Deserialized object of this data type (may be <c>null</c>).</returns>
    public object SoapDeserializeValue(XmlReader reader)
    {
      object result;
      if (SoapHelper.ReadNull(reader))
        result = null;
      else
      {
        switch (_upnpTypeName)
        {
          case "ui1":
          case "ui2":
          case "ui4":
          case "ui8":
          case "i1":
          case "i2":
          case "i4":
          case "i8":
          case "int":
          case "r4":
          case "r8":
          case "number":
          case "fixed.14.4":
          case "float":
          case "char":
            result = reader.ReadElementContentAs(_dotNetType, null);
            break;
          case "string":
          case "uuid":
            result = reader.ReadElementContentAsString();
            break;
          case "date":
            result = System.DateTime.ParseExact(reader.ReadElementContentAsString(), "yyyy-MM-dd", null);
            break;
          case "dateTime":
            result = System.DateTime.ParseExact(reader.ReadElementContentAsString(), "s", null);
            break;
          case "dateTime.tz":
            result = System.DateTime.ParseExact(reader.ReadElementContentAsString(), "u", null).ToLocalTime();
            break;
          case "time":
            result = System.DateTime.ParseExact(reader.ReadElementContentAsString(), "T", null);
            break;
          case "time.tz":
            result = System.DateTime.ParseExact(reader.ReadElementContentAsString(), "hh:mm:ss", null).ToLocalTime();
            break;
          case "boolean":
            result = reader.ReadElementContentAs(_dotNetType, null);
            break;
          case "bin.base64":
            result = reader.ReadElementContentAs(_dotNetType, null);
            break;
          case "bin.hex":
            result = EncodingUtils.FromHexString(reader.ReadElementContentAsString());
            break;
          case "uri":
            result = new Uri(reader.ReadElementContentAsString());
            break;
          default:
            throw new NotImplementedException(string.Format("UPnP standard data type '{0}' is not implemented", _upnpTypeName));
        }
        // Reader will already have read the end element in the methods ReadElementContentXXX
      }
      return result;
    }

    public double GetNumericValue(object val)
    {
      switch (_upnpTypeName)
      {
        case "ui1":
        case "ui2":
        case "ui4":
        case "ui8":
        case "i1":
        case "i2":
        case "i4":
        case "i8":
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
