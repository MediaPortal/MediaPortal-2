using System;
using System.Xml;
using MediaPortal.Utilities.Exceptions;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// Placeholder for all extended data types at control point side, for which the application didn't provide an
  /// implementation.
  /// </summary>
  public class CpExtendedDataTypeDummy : CpExtendedDataType
  {
    protected XmlElement _value;

    public CpExtendedDataTypeDummy(string schemaURI, string dataTypeName) : base(schemaURI, dataTypeName) { }

    /// <summary>
    /// Returns <c>true</c> if this extended data can serialize to and deserialize from the "string-equivalent" form of values.
    /// </summary>
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
