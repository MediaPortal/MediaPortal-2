using System.Xml;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// Device descriptor class for all UPnP extended data types. Must be derived to create concrete extended data types.
  /// All abstract methods must be implemented by subclasses.
  /// </summary>
  public abstract class CpExtendedDataType : CpDataType
  {
    protected string _schemaURI;
    protected string _dataTypeName;

    protected CpExtendedDataType(string schemaURI, string dataTypeName)
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
  }
}
