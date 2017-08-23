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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Utilities;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Contains playlist identification data and a list of media item ids. This information is enough to persist a playlist.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class PlaylistRawData : PlaylistBase
  {
    protected readonly IList<Guid> _mediaItemIds = new List<Guid>();

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    public PlaylistRawData(Guid playlistId, string name, string playlistType) : base(playlistId, name, playlistType) { }

    public PlaylistRawData(Guid playlistId, string name, string playlistType, IEnumerable<Guid> mediaItemIds) :
        base(playlistId, name, playlistType)
    {
      CollectionUtils.AddAll(_mediaItemIds, mediaItemIds);
    }

    [XmlIgnore]
    public override int NumItems
    {
      get { return _mediaItemIds.Count; }
    }

    [XmlIgnore]
    public IList<Guid> MediaItemIds
    {
      get { return _mediaItemIds; }
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
    public static PlaylistRawData Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as PlaylistRawData;
    }

    /// <summary>
    /// Deserializes a playlist instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized share descriptor instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static PlaylistRawData Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as PlaylistRawData;
    }

    #region Additional members for the XML serialization

    internal PlaylistRawData() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(PlaylistRawData)));
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("MediaItemIds")]
    [XmlArrayItem("Id")]
    public Guid[] XML_MediaItemIds
    {
      get
      {
        Guid[] result = new Guid[_mediaItemIds.Count];
        _mediaItemIds.CopyTo(result, 0);
        return result;
      }
      set
      {
        _mediaItemIds.Clear();
        CollectionUtils.AddAll(_mediaItemIds, value);
      }
    }

    #endregion
  }
}