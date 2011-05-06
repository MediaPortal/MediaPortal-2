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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Holds all metadata for a the media provider specified by the <see cref="MediaProviderId"/>
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class MediaProviderMetadata
  {
    #region Protected fields

    protected Guid _mediaProviderId;
    protected string _name;

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    #endregion

    public MediaProviderMetadata(Guid mediaProviderId, string name)
    {
      _mediaProviderId = mediaProviderId;
      _name = name;
    }

    /// <summary>
    /// GUID which uniquely identifies the media provider.
    /// </summary>
    [XmlIgnore]
    public Guid MediaProviderId
    {
      get { return _mediaProviderId; }
    }

    /// <summary>
    /// Returns the name of the media provider.
    /// </summary>
    [XmlIgnore]
    public string Name
    {
      get { return _name; }
    }

    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    public static MediaProviderMetadata Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as MediaProviderMetadata;
    }

    #region Additional members for the XML serialization

    internal MediaProviderMetadata() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(MediaProviderMetadata)));
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Id", IsNullable = false)]
    public string XML_Id
    {
      get { return MarshallingHelper.SerializeGuid(_mediaProviderId); }
      set { _mediaProviderId = MarshallingHelper.DeserializeGuid(value); }
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

   #endregion
  }
}
