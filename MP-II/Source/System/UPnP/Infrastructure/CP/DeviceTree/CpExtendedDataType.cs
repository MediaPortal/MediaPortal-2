using System;
using System.Xml;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// Descriptor class for all UPnP extended data types at client side.
  /// </summary>
  public class CpExtendedDataType : CpDataType
  {
    protected UPnPExtendedDataType _dataType;

    public CpExtendedDataType(UPnPExtendedDataType dataType)
    {
      _dataType = dataType;
    }

    /// <summary>
    /// Returns the URI which denotes the XML schema containing a description of this extended data type.
    /// </summary>
    public string SchemaURI
    {
      get { return _dataType.SchemaURI; }
    }

    /// <summary>
    /// The extended data type name in the schema of the specified <see cref="SchemaURI"/>.
    /// </summary>
    public string DataTypeName
    {
      get { return _dataType.DataTypeName; }
    }

    /// <summary>
    /// Returns <c>true</c> if this extended data type can serialize to and deserialize from the "string-equivalent"
    /// form of values.
    /// </summary>
    public bool SupportsStringEquivalent
    {
      get { return _dataType.SupportsStringEquivalent; }
    }

    #region Base overrides

    public override string SoapSerializeValue(object value, bool forceSimpleValue)
    {
      return _dataType.SoapSerializeValue(value, forceSimpleValue);
    }

    public override object SoapDeserializeValue(XmlElement enclosingElement, bool isSimpleValue)
    {
      return _dataType.SoapDeserializeValue(enclosingElement, isSimpleValue);
    }

    public override bool IsAssignableFrom(Type type)
    {
      return _dataType.IsAssignableFrom(type);
    }

    #endregion
  }
}
