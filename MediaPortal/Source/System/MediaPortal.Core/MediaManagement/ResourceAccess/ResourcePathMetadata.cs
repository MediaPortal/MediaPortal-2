#region Copyright (C) 2007-2010 Team MediaPortal

/* 
 *	Copyright (C) 2007-2010 Team MediaPortal
 *	http://www.team-mediaportal.com
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
using System.Xml.Serialization;

namespace MediaPortal.Core.MediaManagement.ResourceAccess
{
  /// <summary>
  /// Holds some metadata about a resource path in some system.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class ResourcePathMetadata
  {
    protected string _humanReadablePath;
    protected string _resourceName;
    protected ResourcePath _resourcePath;

    // We could use some cache for this instance, if we would have one...
    [ThreadStatic]
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    public ResourcePathMetadata() { }

    [XmlIgnore]
    public string HumanReadablePath
    {
      get { return _humanReadablePath; }
      set { _humanReadablePath = value; }
    }

    [XmlIgnore]
    public string ResourceName
    {
      get { return _resourceName; }
      set { _resourceName = value; }
    }

    [XmlIgnore]
    public ResourcePath ResourcePath
    {
      get { return _resourcePath; }
      set { _resourcePath = value; }
    }

    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      lock (xs)
        xs.Serialize(writer, this);
    }

    public static ResourcePathMetadata Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      lock (xs)
        return xs.Deserialize(reader) as ResourcePathMetadata;
    }

    #region Additional members for the XML serialization

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      if (_xmlSerializer == null)
        _xmlSerializer = new XmlSerializer(typeof(ResourcePathMetadata));
      return _xmlSerializer;
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("HumanReadablePath", IsNullable = false)]
    public string XML_HumanReadablePath
    {
      get { return _humanReadablePath; }
      set { _humanReadablePath = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("ResourceName", IsNullable = false)]
    public string XML_ResourceName
    {
      get { return _resourceName; }
      set { _resourceName = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("ResourcePath", IsNullable = false)]
    public string XML_ResourcePath
    {
      get { return _resourcePath.Serialize(); }
      set { _resourcePath = ResourcePath.Deserialize(value); }
    }

   #endregion
  }
}