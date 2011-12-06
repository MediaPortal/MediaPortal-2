#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Utilities.UPnP;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Holds all metadata for a the resource provider specified by the <see cref="ResourceProviderId"/>
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class ResourceProviderMetadata
  {
    #region Protected fields

    protected Guid _resourceProviderId;
    protected string _name;
    protected string _description;
    protected bool _transientMedia;

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    #endregion

    public ResourceProviderMetadata(Guid resourceProviderId, string name, string description, bool transientMedia)
    {
      _resourceProviderId = resourceProviderId;
      _name = name;
      _description = description;
      _transientMedia = transientMedia;
    }

    /// <summary>
    /// GUID which uniquely identifies the resource provider.
    /// </summary>
    [XmlIgnore]
    public Guid ResourceProviderId
    {
      get { return _resourceProviderId; }
    }

    /// <summary>
    /// Returns the name of the resource provider.
    /// </summary>
    [XmlIgnore]
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Returns the description of the resource provider.
    /// </summary>
    [XmlIgnore]
    public string Description
    {
      get { return _description; }
    }

    /// <summary>
    /// Returns the information if this resource provider provides access to transient resources like removable media or temporary
    /// available resources.
    /// </summary>
    [XmlIgnore]
    public bool TransientMedia
    {
      get { return _transientMedia; }
    }

    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    public static ResourceProviderMetadata Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as ResourceProviderMetadata;
    }

    #region Additional members for the XML serialization

    internal ResourceProviderMetadata() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(ResourceProviderMetadata)));
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Id")]
    public string XML_Id
    {
      get { return MarshallingHelper.SerializeGuid(_resourceProviderId); }
      set { _resourceProviderId = MarshallingHelper.DeserializeGuid(value); }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Name")]
    public string XML_Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Description")]
    public string XML_Description
    {
      get { return _description; }
      set { _description = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("TransientMedia")]
    public bool XML_TransientMedia
    {
      get { return _transientMedia; }
      set { _transientMedia = value; }
    }

   #endregion
  }
}
