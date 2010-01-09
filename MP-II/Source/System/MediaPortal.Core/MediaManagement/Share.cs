#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.SystemResolver;

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
    protected string _systemId;
    protected ResourcePath _baseResourcePath;
    protected string _name;
    protected HashSet<string> _mediaCategories;

    // We could use some cache for this instance, if we would have one...
    [ThreadStatic]
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    #endregion

    /// <summary>
    /// Creates a new share descriptor with the specified values.
    /// </summary>
    /// <param name="shareId">Id of the share. For the same share (i.e. the same resource path on the same
    /// system), the id should be perserverd, i.e. the id should not be re-created
    /// but stored persistently. This helps other components to use the id as fixed identifier for the share.</param>
    /// <param name="systemId">Specifies the system on that the <paramref name="baseResourcePath"/> can be
    /// evaluated.</param>
    /// <param name="baseResourcePath">Description of the resource provider chain for the share's base directory.</param>
    /// <param name="name">Name of the share. This name will be shown at the GUI. The string might be
    /// localized using a "[[Section-Name].[String-Name]]" syntax, for example "[Media.MyMusic]".</param>
    /// <param name="mediaCategories">Categories of media in this share. If set, the categories describe
    /// the desired contents of this share. If set to <c>null</c>, the share has no explicit media categories,
    /// i.e. it is a general share.</param>
    public Share(Guid shareId, string systemId, ResourcePath baseResourcePath, string name,
        IEnumerable<string> mediaCategories)
    {
      if (baseResourcePath == null)
        throw new ArgumentException("Share base resource path must not null");
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException("Share name must not be empty or null");

      _shareId = shareId;
      _systemId = systemId;
      _baseResourcePath = baseResourcePath;
      _name = name;
      _mediaCategories = mediaCategories == null ? new HashSet<string>() : new HashSet<string>(mediaCategories);
    }

    /// <summary>
    /// Creates a new share. This will create a new share id and call the constructor with it.
    /// </summary>
    /// <param name="systemId">Specifies the system on that the media provider with the specified
    /// <paramref name="baseResourcePath"/> will be evaluated.</param>
    /// <param name="baseResourcePath">Description of the resource provider chain for the share's base directory.</param>
    /// <param name="name">Name of the share. This name will be shown at the GUI. The string might be
    /// localized using a "[[Section-Name].[String-Name]]" syntax, for example "[Media.MyMusic]".</param>
    /// <param name="mediaCategories">Media content categories of this share. If set, the category
    /// describes the desired contents of this share. If set to <c>null</c>, this share has no explicit
    /// media categories, i.e. it is a general share.</param>
    /// <returns>Created <see cref="Share"/> with a new share id.</returns>
    public static Share CreateNewShare(string systemId, ResourcePath baseResourcePath, string name,
        IEnumerable<string> mediaCategories)
    {
      return new Share(Guid.NewGuid(), systemId, baseResourcePath, name, mediaCategories);
    }

    /// <summary>
    /// Creates a new local share. This will create a new share ID and call the constructor with it.
    /// </summary>
    /// <param name="baseResourcePath">Description of the resource provider chain for the share's base directory.</param>
    /// <param name="name">Name of the share. This name will be shown at the GUI. The string might be
    /// localized using a "[[Section-Name].[String-Name]]" syntax, for example "[Media.MyMusic]".</param>
    /// <param name="mediaCategories">Media content categories of this share. If set, the category
    /// describes the desired contents of this share. If set to <c>null</c>, this share has no explicit
    /// media categories, i.e. it is a general share.</param>
    /// <returns>Created <see cref="Share"/> with a new share id.</returns>
    public static Share CreateNewLocalShare(ResourcePath baseResourcePath, string name, IEnumerable<string> mediaCategories)
    {
      ISystemResolver systemResolver = ServiceScope.Get<ISystemResolver>();
      return CreateNewShare(systemResolver.LocalSystemId, baseResourcePath, name, mediaCategories);
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
    /// Returns the UUID of the system where this share is located.
    /// </summary>
    [XmlIgnore]
    public string SystemId
    {
      get { return _systemId; }
      set { _systemId = value; }
    }

    /// <summary>
    /// Returns the resource path describing the resource provider chain for the share's base directory.
    /// </summary>
    [XmlIgnore]
    public ResourcePath BaseResourcePath
    {
      get { return _baseResourcePath; }
      set { _baseResourcePath = value; }
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
        using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings {OmitXmlDeclaration = true}))
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
      return _shareId == other._shareId;
    }

    public override int GetHashCode()
    {
      return _shareId.GetHashCode();
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
    [XmlElement("SystemId", IsNullable = false)]
    public string XML_SystemId
    {
      get { return _systemId; }
      set { _systemId = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("BaseResourcePath", IsNullable = false)]
    public string XML_ResourcePath
    {
      get { return _baseResourcePath.Serialize(); }
      set { _baseResourcePath = ResourcePath.Deserialize(value); }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Name", IsNullable = false)]
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
