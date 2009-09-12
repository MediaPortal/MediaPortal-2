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
