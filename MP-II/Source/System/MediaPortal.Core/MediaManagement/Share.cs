#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Core.General;
using MediaPortal.Utilities.Xml;

namespace MediaPortal.Core.MediaManagement
{
  public enum DefaultMediaCategory
  {
    Audio,
    AudioStream,
    Video,
    VideoStream,
    Image,
    RemovableDisc,
    TvData
  }

  /// <summary>
  /// Holds all configuration data of a share. A share descriptor globally describes a share
  /// in an MP-II system.
  /// A share basically is a directory of a provider, which gets assigned a special name (the share name).
  /// Some user interaction at the GUI level will use the share as a means to simplify the work with
  /// media provider paths (for example the automatic import).
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class Share
  {
    #region Protected fields

    protected Guid _shareId;
    protected SystemName _nativeSystemName;
    protected Guid _mediaProviderId;
    protected string _path;
    protected string _name;
    protected HashSet<string> _mediaCategories;

    // We could use some cache for this instance, if we would have one...
    [ThreadStatic]
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    #endregion

    /// <summary>
    /// Creates a new share descriptor with the specified values.
    /// </summary>
    /// <param name="shareId">Id of the share. For the same share (i.e. the same media provider on the same
    /// system with the same path), the id should be perserverd, i.e. the id should not be re-created
    /// but stored persistently. This helps other components to use the id as fixed identifier for the share.</param>
    /// <param name="systemName">Specifies the system on that the media provider with the specified
    /// <paramref name="mediaProviderId"/> is located.</param>
    /// <param name="mediaProviderId">Id of the media provider which provides the file system for the
    /// share.</param>
    /// <param name="path">Path at the media provider with the specified <paramref name="mediaProviderId"/>
    /// where the share should have its root directory.</param>
    /// <param name="name">Name of the share. This name will be shown at the GUI. The string might be
    /// localized.</param>
    /// <param name="mediaCategories">Categories of media in this share. If set, the categories describe
    /// the desired contents of this share. If set to <c>null</c>, the share has no explicit media categories,
    /// i.e. it is a general share.</param>
    public Share(Guid shareId, SystemName systemName,
        Guid mediaProviderId, string path, string name,
        IEnumerable<string> mediaCategories)
    {
      _shareId = shareId;
      _nativeSystemName = systemName;
      _mediaProviderId = mediaProviderId;
      _path = path;
      _name = name;
      _mediaCategories = mediaCategories == null ? new HashSet<string>() : new HashSet<string>(mediaCategories);
    }

    /// <summary>
    /// Creates a new share. This will create a new share id.
    /// </summary>
    /// <param name="systemName">Specifies the system on that the media provider with the specified
    /// <paramref name="mediaProviderId"/> is located.</param>
    /// <param name="mediaProviderId">Id of the media provider which provides the file system for the
    /// share.</param>
    /// <param name="path">Path at the media provider with the specified <paramref name="mediaProviderId"/>
    /// where the share should have its root directory.</param>
    /// <param name="name">Name of the share. This name will be shown at the GUI. The string might be
    /// localized.</param>
    /// <param name="mediaCategories">Media content categories of this share. If set, the category
    /// describes the desired contents of this share. If set to <c>null</c>, this share has no explicit
    /// media categories, i.e. it is a general share.</param>
    /// <returns>Created <see cref="Share"/> with a new share id.</returns>
    public static Share CreateNewShare(SystemName systemName,
        Guid mediaProviderId, string path, string name,
        IEnumerable<string> mediaCategories)
    {
      return new Share(Guid.NewGuid(), systemName, mediaProviderId, path, name, mediaCategories);
    }

    /// <summary>
    /// Returns the globally unique id of this share.
    /// </summary>
    [XmlIgnore]
    public Guid ShareId
    {
      get { return _shareId; }
    }

    /// <summary>
    /// Returns the system name where this share is located.
    /// </summary>
    [XmlIgnore]
    public SystemName NativeSystem
    {
      get { return _nativeSystemName; }
      set { _nativeSystemName = value; }
    }

    /// <summary>
    /// Returns the id of the media provider this share is based on.
    /// </summary>
    [XmlIgnore]
    public Guid MediaProviderId
    {
      get { return _mediaProviderId; }
      set { _mediaProviderId = value; }
    }

    /// <summary>
    /// Returns the path used for the media provider (specified by <see cref="MediaProviderId"/>) for this share.
    /// </summary>
    [XmlIgnore]
    public string Path
    {
      get { return _path; }
      set { _path = value; }
    }

    /// <summary>
    /// Returns the name of this share.
    /// </summary>
    [XmlIgnore]
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// Returns the media contents categories of this share. The media categories can be used for a filtering
    /// of shares or for the GUI to add default metadata extractors for the specified categories.
    /// </summary>
    [XmlIgnore]
    public ICollection<string> MediaCategories
    {
      get { return _mediaCategories; }
    }

    /// <summary>
    /// Serializes this share descriptor instance to XML.
    /// </summary>
    /// <returns>String containing an XML fragment with this instance's data.</returns>
    public string Serialize()
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      lock (xs)
      {
        StringBuilder sb = new StringBuilder(); // Will contain the data, formatted as XML
        using (XmlWriter writer = new XmlInnerElementWriter(sb))
          xs.Serialize(writer, this);
        return sb.ToString();
      }
    }

    /// <summary>
    /// Serializes this share descriptor instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      lock (xs)
        xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a share descriptor instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized share descriptor instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static Share Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      lock (xs)
        using (StringReader reader = new StringReader(str))
          return xs.Deserialize(reader) as Share;
    }

    /// <summary>
    /// Deserializes a share descriptor instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized share descriptor instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static Share Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      lock (xs)
        return xs.Deserialize(reader) as Share;
    }

    public override bool Equals(object obj)
    {
      if (!(obj is Share))
        return false;
      Share other = (Share) obj;
      return ShareId == other.ShareId;
    }

    public override int GetHashCode()
    {
      return ShareId.GetHashCode();
    }

    #region Additional members for the XML serialization

    internal Share() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      if (_xmlSerializer == null)
        _xmlSerializer = new XmlSerializer(typeof(Share));
      return _xmlSerializer;
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Id")]
    public Guid XML_Id
    {
      get { return _shareId; }
      set { _shareId = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("NativeSystem")]
    public SystemName XML_NativeSystem
    {
      get { return _nativeSystemName; }
      set { _nativeSystemName = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("MediaProviderId")]
    public Guid XML_MediaProviderId
    {
      get { return _mediaProviderId; }
      set { _mediaProviderId = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Path")]
    public string XML_Path
    {
      get { return _path; }
      set { _path = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Name")]
    public string XML_Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("MediaCategories")]
    public HashSet<string> XML_MediaCategories
    {
      get { return _mediaCategories; }
      set { _mediaCategories = value; }
    }

    #endregion
  }
}
