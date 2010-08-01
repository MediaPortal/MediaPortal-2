#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Represents a group of objects, identified by a group name.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class ValueGroup
  {
    protected string _groupName;
    protected long _numItemsInGroup;

    // We could use some cache for this instance, if we would have one...
    [ThreadStatic]
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized
    
    public ValueGroup(string groupName, long numItemsInGroup)
    {
      _groupName = groupName;
      _numItemsInGroup = numItemsInGroup;
    }

    [XmlIgnore]
    public string GroupName
    {
      get { return _groupName; }
    }

    [XmlIgnore]
    public long NumItemsInGroup
    {
      get { return _numItemsInGroup; }
      set { _numItemsInGroup = value; }
    }

    /// <summary>
    /// Serializes this value group instance to XML.
    /// </summary>
    /// <returns>String containing an XML fragment with this instance's data.</returns>
    public string Serialize()
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      lock (xs)
      {
        StringBuilder sb = new StringBuilder(); // Will contain the data, formatted as XML
        using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings {OmitXmlDeclaration = true}))
          xs.Serialize(writer, this);
        return sb.ToString();
      }
    }

    /// <summary>
    /// Serializes this value group instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      lock (xs)
        xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a value group instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized value group instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static ValueGroup Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      lock (xs)
        using (StringReader reader = new StringReader(str))
          return xs.Deserialize(reader) as ValueGroup;
    }

    /// <summary>
    /// Deserializes a value group instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized value group instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static ValueGroup Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      lock (xs)
        return xs.Deserialize(reader) as ValueGroup;
    }

    #region Additional members for the XML serialization

    internal ValueGroup() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      if (_xmlSerializer == null)
        _xmlSerializer = new XmlSerializer(typeof(ValueGroup));
      return _xmlSerializer;
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Name")]
    public string XML_GroupName
    {
      get { return _groupName; }
      set { _groupName = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("NumItems")]
    public long XML_NumItems
    {
      get { return _numItemsInGroup; }
      set { _numItemsInGroup = value; }
    }

    #endregion
  }
}