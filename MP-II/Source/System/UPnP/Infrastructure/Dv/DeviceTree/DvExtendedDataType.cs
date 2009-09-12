using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Device descriptor class for all UPnP extended data types.
  /// </summary>
  public class DvExtendedDataType : DvDataType
  {
    protected UPnPExtendedDataType _dataType;

    public DvExtendedDataType(UPnPExtendedDataType dataType)
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

    internal override void AddSCDPDescriptionForStandardDataType(StringBuilder result,
        IDictionary<string, string> dataTypeSchemas2NSPrefix)
    {
      result.Append(
          "<dataType type=\"");
      result.Append(dataTypeSchemas2NSPrefix[_dataType.SchemaURI]);
      result.Append(_dataType.DataTypeName);
      result.Append("\">string</dataType>");
    }

    #endregion
  }
}
