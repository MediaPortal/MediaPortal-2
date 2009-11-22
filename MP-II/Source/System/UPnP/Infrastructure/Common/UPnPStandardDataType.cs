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

using System;
using System.Collections.Generic;
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
        case "i1":
        case "i2":
        case "i4":
        case "int":
          writer.WriteString(value.ToString());
          break;
        case "r4":
        case "r8":
        case "number":
        case "float":
          writer.WriteString(((Double) value).ToString("E"));
          break;
        case "fixed.14.4":
          writer.WriteString(((Double) value).ToString("0.##############E+0"));
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
      {
        if (reader.IsEmptyElement)
          reader.ReadStartElement();
        else
        {
          reader.ReadStartElement();
          reader.ReadEndElement();
        }
        result = null;
      }
      else
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
