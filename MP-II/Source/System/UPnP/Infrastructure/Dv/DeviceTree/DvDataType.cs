#region Copyright (C) 2007-2010 Team MediaPortal

/* 
 *  Copyright (C) 2007-2010 Team MediaPortal
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

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Abstract device descriptor class for a data type, will be inherited for standard data types (<see cref="DvStandardDataType"/>) and
  /// extended data types (<see cref="DvExtendedDataType"/>).
  /// </summary>
  public abstract class DvDataType
  {
    /// <summary>
    /// Serializes the given <paramref name="value"/> as the value of the <paramref name="writer"/>s current element
    /// in the serialization strategy specified by this UPnP data type.
    /// </summary>
    /// <remarks>
    /// The value is either encoded as string (for simple data types and for UPnP 1.0 complex data types,
    /// if <paramref name="forceSimpleValue"/> is set to <c>true</c>), or as an XML element containing the structure
    /// as specified by the schema type of this data type for extended UPnP 1.1 data types.
    /// </remarks>
    /// <param name="value">Value to be serialized.</param>
    /// <param name="forceSimpleValue">If set to <c>true</c>, also extended datatypes will be serialized using their
    /// "string equivalent".</param>
    /// <param name="writer">Xml writer here the value will be serialized to.
    /// The writer's position is the start of the parent element, the result should go. After this method returns, the writer
    /// must have read the end element.</param>
    /// <returns>SOAP serialization for the given <paramref name="value"/>. May be <c>null</c> for serializations of
    /// <c>null</c> values. In this case, an attriute <i>xsi:null="true"</i> must be added in the enclosing SOAP element.</returns>
    public abstract void SoapSerializeValue(object value, bool forceSimpleValue, XmlWriter writer);

    /// <summary>
    /// Deserializes the contents of the current element of the given <paramref name="reader"/>
    /// to an object of this UPnP data type.
    /// </summary>
    /// <param name="reader">XML reader whose current element's value will be deserialized.
    /// The writer's position is the start of the parent element, the result should go. After this method returns, the writer
    /// must have read the end element.</param>
    /// <param name="isSimpleValue">If set to <c>true</c>, for extended data types, the value should be deserialized from its
    /// string-equivalent, i.e. the XML text content of the given XML element should be examined,
    /// else the value should be deserialized from the extended representation of this data type.</param>
    /// <returns>Value which was deserialized.</returns>
    public abstract object SoapDeserializeValue(XmlReader reader, bool isSimpleValue);

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

    #region Description generation

    /// <summary>
    /// Generates the (UPnP 1.0) SCPD description which will be written in the state variable description as child into the
    /// element &lt;dataType/&gt;.
    /// </summary>
    /// <param name="writer">XML writer to write the datatype description fragment to.</param>
    internal abstract void AddSCPDDescriptionForStandardDataType(XmlWriter writer);

    #endregion
  }
}
