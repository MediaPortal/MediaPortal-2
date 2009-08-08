using System;
using System.Xml;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// Abstract descriptor class for a data type for the client (control point) side, will be inherited for standard data
  /// types (<see cref="CpStandardDataType"/>) and extended data types (<see cref="CpExtendedDataType"/>).
  /// </summary>
  /// <remarks>
  /// Parts of this class are intentionally parallel to the implementation in <see cref="UPnP.Infrastructure.Dv.DeviceTree.DvDataType"/>.
  /// </remarks>
  public abstract class CpDataType
  {
    /// <summary>
    /// Serializes the given <paramref name="value"/> in the serialization strategy specified by this UPnP data type. The
    /// serialized value will be XML text.
    /// </summary>
    /// <remarks>
    /// The returned string is a serialized XML node which contains either the serialized value directly, encoded as
    /// string (for simple data types and for UPnP 1.0 complex data types, if <paramref name="forceSimpleValue"/> is set to
    /// <c>true</c>), or which contains a serialized XML element containing the structure as specified by the schema type
    /// of this data type for extended UPnP 1.1 data types.
    /// </remarks>
    /// <param name="value">Value to be serialized.</param>
    /// <param name="forceSimpleValue">If set to <c>true</c>, also extended datatypes will be serialized using their
    /// "string equivalent".</param>
    /// <returns>SOAP serialization for the given <paramref name="value"/>.</returns>
    public abstract string SoapSerializeValue(object value, bool forceSimpleValue);

    /// <summary>
    /// Deserializes the given SOAP <paramref name="enclosingElement"/> to an object of this UPnP data type.
    /// </summary>
    /// <param name="enclosingElement">SOAP representation of an object of this UPnP data type.</param>
    /// <param name="isSimpleValue">If set to <c>true</c>, for extended data types, the value should be deserialized from its
    /// string-equivalent, i.e. the XML text content of the <paramref name="enclosingElement"/> should be used,
    /// else the value should be deserialized from the extended representation of this data type.</param>
    /// <returns>Value which was deserialized.</returns>
    public abstract object SoapDeserializeValue(XmlElement enclosingElement, bool isSimpleValue);

    /// <summary>
    /// Returns the information if an object of the given type can be assigned to a variable of this UPnP data type.
    /// </summary>
    /// <param name="type">Type which will be checked if objects of that type can be assigned to a variable of this
    /// UPnP data type.</param>
    /// <returns><c>true</c>, if an object of the specified <paramref name="type"/> can be assigned to a variable of this
    /// UPnP data type.</returns>
    public abstract bool IsAssignableFrom(Type type);

    /// <summary>
    /// Checks  if the given <paramref name="value"/> is of a type that is assignable to this type.
    /// </summary>
    /// <param name="value">Value to check.</param>
    /// <returns><c>true</c>, if the given <paramref name="value"/> is of a type that is assignable to this
    /// data type, else <c>false</c>.</returns>
    public bool IsValueAssignable(object value)
    {
      Type actualType = value == null ? null : value.GetType();
      return actualType == null || IsAssignableFrom(actualType);
    }

    internal static CpDataType CreateDataType(XmlElement dataTypeElement, DataTypeResolverDlgt dataTypeResolver)
    {
      string standardDataType = ParserHelper.SelectText(dataTypeElement, "text()");
      string extendedDataType = dataTypeElement.GetAttribute("type");
      if (string.IsNullOrEmpty(extendedDataType))
      { // Standard data type
        UPnPStandardDataType type = UPnPStandardDataType.ParseStandardType(standardDataType);
        if (type == null)
          throw new ArgumentException(string.Format("Invalid UPnP standard data type name '{0}'", standardDataType));
        return new CpStandardDataType(type);
      }
      else
      { // Extended data type
        if (standardDataType != "string")
          throw new ArgumentException("UPnP extended data types need to yield a standard data type of 'string'");
        string schemaURI;
        string dataTypeName;
        if (!ParserHelper.TryParseDataTypeReference(extendedDataType, dataTypeElement, out schemaURI, out dataTypeName))
          throw new ArgumentException(string.Format("Unable to parse namespace URI of extended data type '{0}'", extendedDataType));
        CpExtendedDataType result;
        if (dataTypeResolver != null && dataTypeResolver(schemaURI + ":" + dataTypeName, out result))
          return result;
        return new CpExtendedDataTypeDummy(schemaURI, dataTypeName);
      }
    }
  }
}
