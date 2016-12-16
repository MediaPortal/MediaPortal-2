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
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Utilities;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Contains playlist identification data and a list of media items.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class PlaylistContents : PlaylistBase
  {
    protected readonly IList<MediaItem> _itemList = new List<MediaItem>();

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    public PlaylistContents(Guid playlistId, string name, string playlistType) : base(playlistId, name, playlistType) { }

    public PlaylistContents(Guid playlistId, string name, string playlistType, IEnumerable<MediaItem> mediaItems) :
        base(playlistId, name, playlistType)
    {
      CollectionUtils.AddAll(_itemList, mediaItems);
    }

    [XmlIgnore]
    public override int NumItems
    {
      get { return _itemList.Count; }
    }

    [XmlIgnore]
    public IList<MediaItem> ItemList
    {
      get { return _itemList; }
    }

    public virtual void SetItems(IEnumerable<MediaItem> itemList)
    {
      _itemList.Clear();
      CollectionUtils.AddAll(_itemList, itemList);
    }

    /// <summary>
    /// Extracts this playlist's raw data (i.e. its name, id and media item ids) and returns a
    /// <see cref="PlaylistRawData"/> instance for it.
    /// </summary>
    public PlaylistRawData GetRawData()
    {
      return new PlaylistRawData(_playlistId, _name, _playlistType, _itemList.Select(mediaItem => mediaItem.MediaItemId));
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
    public static PlaylistContents Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as PlaylistContents;
    }

    /// <summary>
    /// Deserializes a playlist instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized share descriptor instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static PlaylistContents Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as PlaylistContents;
    }

    #region Additional members for the XML serialization

    internal PlaylistContents() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(PlaylistContents)));
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("MediaItems")]
    [XmlArrayItem("Item")]
    public MediaItem[] XML_ItemList
    {
      get
      {
        MediaItem[] result = new MediaItem[_itemList.Count];
        _itemList.CopyTo(result, 0);
        return result;
      }
      set { SetItems(value); }
    }

    #endregion
  }
}