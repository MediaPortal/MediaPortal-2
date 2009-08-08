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
