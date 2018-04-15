#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Contains some playlist information data: Name, id, type and number of items.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class PlaylistInformationData : PlaylistBase
  {
    protected int _numItems;

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    public PlaylistInformationData(Guid playlistId, string name, string playlistType, int numItems) :
        base(playlistId, name, playlistType)
    {
      _numItems = numItems;
    }

    [XmlIgnore]
    public override int NumItems
    {
      get { return _numItems; }
    }

    /// <summary>
    /// Serializes this playlist instance to XML.
    /// </summary>
    /// <returns>String containing an XML fragment with this instance's data.</returns>
    public string Serialize()
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      StringBuilder sb = new StringBuilder(); // Will contain the data, formatted as XML
      using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings {OmitXmlDeclaration = true}))
        xs.Serialize(writer, this);
      return sb.ToString();
    }

    /// <summary>
    /// Serializes this playlist instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a playlist instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized share descriptor instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static PlaylistInformationData Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as PlaylistInformationData;
    }

    /// <summary>
    /// Deserializes a playlist instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized share descriptor instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static PlaylistInformationData Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as PlaylistInformationData;
    }

    #region Additional members for the XML serialization

    internal PlaylistInformationData() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(PlaylistInformationData)));
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("NumItems")]
    public int XML_NumItems
    {
      get { return _numItems; }
      set { _numItems = value; }
    }

    #endregion
  }
}