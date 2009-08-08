using System.Collections.Generic;
using System.Text;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Device descriptor class for all UPnP extended data types. Must be derived to create concrete extended data types.
  /// </summary>
  public abstract class DvExtendedDataType : DvDataType
  {
    protected string _schemaURI;
    protected string _dataTypeName;

    protected DvExtendedDataType(string schemaURI, string dataTypeName)
    {
      _schemaURI = schemaURI;
      _dataTypeName = dataTypeName;
    }

    /// <summary>
    /// Returns the URI which denotes the XML schema containing a description of this extended data type.
    /// </summary>
    public string SchemaURI
    {
      get { return _schemaURI; }
    }

    /// <summary>
    /// The extended data type name in the schema of the specified <see cref="SchemaURI"/>.
    /// </summary>
    public string DataTypeName
    {
      get { return _dataTypeName; }
    }

    /// <summary>
    /// Returns <c>true</c> if this extended data can serialize to and deserialize from the "string-equivalent" form of values.
    /// </summary>
    public abstract bool SupportsStringEquivalent { get; }

    internal override void AddSCDPDescriptionForStandardDataType(StringBuilder result,
        IDictionary<string, string> dataTypeSchemas2NSPrefix)
    {
      result.Append(
          "<dataType type=\"");
      result.Append(dataTypeSchemas2NSPrefix[_schemaURI]);
      result.Append(_dataTypeName);
      result.Append("\">string</dataType>");
    }
  }
}
