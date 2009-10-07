#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
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
    /// Serializes the given <paramref name="value"/> in the serialization strategy specified by this UPnP data type. The
    /// serialized value will be an XML string.
    /// </summary>
    /// <remarks>
    /// The returned string is a serialized XML node which contains either the serialized value directly, encoded as
    /// string (for simple data types and for UPnP 1.0 complex data types, if <paramref name="forceSimpleValue"/> is set to
    /// <c>true</c>), or which contains a serialized XML element containing the structure as specified by the schema type
    /// of this data type for extended UPnP 1.1 data types.
    /// </remarks>
    /// <param name="value">Value to be serialized.</param>
    /// <param name="forceSimpleValue">If set to <c>true</c>, the resulting value mustn't be an XML element but must
    /// be encoded in its "string equivalent".</param>
    /// <returns>SOAP serialization for the given <paramref name="value"/>.</returns>
    public abstract string SoapSerializeValue(object value, bool forceSimpleValue);

    /// <summary>
    /// Deserializes the contents of the given SOAP <paramref name="enclosingElement"/> to an object of this UPnP data type.
    /// </summary>
    /// <param name="enclosingElement">SOAP representation of an object of this UPnP data type.</param>
    /// <param name="isSimpleValue">If set to <c>true</c>, for extended data types, the value should be deserialized from its
    /// string-equivalent, i.e. the XML text content of the <paramref name="enclosingElement"/> should be expected,
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
  }
}
