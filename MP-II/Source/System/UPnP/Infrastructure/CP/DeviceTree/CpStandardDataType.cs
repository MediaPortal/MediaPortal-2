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
