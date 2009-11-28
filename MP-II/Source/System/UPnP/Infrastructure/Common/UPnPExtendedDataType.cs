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
using System.Xml;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Common
{
  /// <summary>
  /// Base class for all UPnP extended data types. Must be derived to create concrete extended data types.
  /// </summary>
  public abstract class UPnPExtendedDataType
  {
    protected string _schemaURI;
    protected string _dataTypeName;

    protected UPnPExtendedDataType(string schemaURI, string dataTypeName)
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
    /// Returns <c>true</c> if this extended data type can serialize to and deserialize from the "string-equivalent"
    /// form of values.
    /// </summary>
    public abstract bool SupportsStringEquivalent { get; }

    /// <summary>
    /// Returns the information if an object of the given type can be assigned to a variable of this UPnP data type.
    /// </summary>
    /// <param name="type">Type which will be checked if objects of that type can be assigned to a variable of this
    /// UPnP data type.</param>
    /// <returns><c>true</c>, if an object of the specified <paramref name="type"/> can be assigned to a variable of this
    /// UPnP data type.</returns>
    public abstract bool IsAssignableFrom(Type type);

    /// <summary>
    /// Returns the information if this data type can transport <c>null</c>-values.
    /// </summary>
    public abstract bool IsNullable { get; }

    /// <summary>
    /// Serializes the given <paramref name="value"/> as contents of the <paramref name="writer"/>'s current element
    /// in the serialization strategy specified by this UPnP data type.
    /// </summary>
    /// <remarks>
    /// The value should be either written as string contents of the current element (for simple data types and for
    /// UPnP 1.0 complex data types, if <paramref name="forceSimpleValue"/> is set to <c>true</c>),
    /// or as an XML sub element containing the structure as specified by the schema type of this data type for extended
    /// UPnP 1.1 data types.
    /// <c>null</c> values are serialized by the caller if <see cref="IsNullable"/> is set to <c>true</c>,
    /// and don't need to be handled in this method.
    /// </remarks>
    /// <param name="value">Value to be serialized.</param>
    /// <param name="forceSimpleValue">If set to <c>true</c>, the value must be encoded in its "string equivalent" and
    /// written as string contents of the <paramref name="writer"/>'s current element.</param>
    /// <param name="writer">XML writer where the value will be serialized to. The value will be serialized as
    /// contents of the writer's current element.
    /// The writer's position is the start of the parent element, the result should go. The caller will write the end
    /// element tag.</param>
    protected abstract void DoSerializeValue(object value, bool forceSimpleValue, XmlWriter writer);

    /// <summary>
    /// Deserializes the contents of the <paramref name="reader"/>'s current XML element to an object of this UPnP data type.
    /// </summary>
    /// <remarks>
    /// <c>null</c> values are deserialized by the caller if <see cref="IsNullable"/> is set to <c>true</c>,
    /// and don't need to be handled in this method.
    /// </remarks>
    /// <param name="reader">XML reader to read the value from.</param>
    /// <param name="isSimpleValue">If set to <c>true</c>, for extended data types, the value should be deserialized
    /// from its string-equivalent, i.e. the XML text content of the given XML element should be evaluated,
    /// else the value should be deserialized from the extended representation of this data type.</param>
    /// <returns>Value which was deserialized.</returns>
    protected abstract object DoDeserializeValue(XmlReader reader, bool isSimpleValue);

    /// <summary>
    /// Serializes the given <paramref name="value"/> as contents of the <paramref name="writer"/>'s current element.
    /// </summary>
    /// <remarks>
    /// The value will be either encoded as string (for simple data types and for UPnP 1.0 complex data types,
    /// if <paramref name="forceSimpleValue"/> is set to <c>true</c>), as an XML sub element containing the structure
    /// as specified by the schema type of this data type for extended UPnP 1.1 data types.
    /// </remarks>
    /// <param name="value">Value to be serialized.</param>
    /// <param name="forceSimpleValue">If set to <c>true</c>, the resulting value is encoded in its
    /// "string equivalent".</param>
    /// <param name="writer">XML writer where the value will be serialized to. The value will be serialized as
    /// contents of the writer's current element.
    /// The writer's position is the start of the parent element, the result should go. The caller will write the end
    /// element tag.</param>
    public void SoapSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      if (value == null)
      {
        if (!IsNullable)
          throw new InvalidDataException("{0}: Invalid null value", GetType().Name);
        SoapWriteNull(writer);
        return;
      }
      if (!IsAssignableFrom(value.GetType()))
        throw new InvalidDataException("{0} cannot serialize values of type {1}", GetType().Name, value.GetType().Name);
      DoSerializeValue(value, forceSimpleValue, writer);
    }

    /// <summary>
    /// Deserializes the contents of the <paramref name="reader"/>'s current XML element to an object of this
    /// UPnP data type.
    /// </summary>
    /// <param name="reader">XML reader to read the value from.</param>
    /// <param name="isSimpleValue">If set to <c>true</c>, for extended data types, the value will be deserialized from its
    /// string-equivalent, i.e. the XML text content of the given XML element should be evaluated,
    /// else the value will be deserialized from the extended representation of this data type.</param>
    /// <returns>Value which was deserialized.</returns>
    public object SoapDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      if (SoapReadNull(reader))
        if (IsNullable)
          return null;
        else
          throw new InvalidDataException("{0}: Invalid null value", GetType().Name);
      return DoDeserializeValue(reader, isSimpleValue);
    }

    /// <summary>
    /// Helper method to be used within the <see cref="SoapSerializeValue"/> method to write a <c>null</c> value
    /// into a SOAP serialization.
    /// </summary>
    /// <param name="writer">XML writer which is positioned at the starting tag of the XML element enclosing the value.</param>
    protected void SoapWriteNull(XmlWriter writer)
    {
      SoapHelper.WriteNull(writer);
    }

    /// <summary>
    /// Helper method to be used within the <see cref="SoapDeserializeValue"/> method to check if the serialization is
    /// a <c>null</c> value.
    /// The reader will read the end element tag after it encountered a <c>null</c> value.
    /// </summary>
    /// <param name="reader">XML reader which is positioned at the starting tag of the XML element enclosing the value.</param>
    protected bool SoapReadNull(XmlReader reader)
    {
      return SoapHelper.ReadNull(reader);
    }
  }
}
