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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Data transfer object to transfer a <see cref="MediaCategory"/> object.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class MediaCategory_DTO
  {
    #region Protected fields

    protected string _name;
    protected HashSet<string> _parentCategories;

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    #endregion

    public MediaCategory_DTO(MediaCategory mediaCategory)
    {
      _name = mediaCategory.CategoryName;
      _parentCategories = new HashSet<string>(mediaCategory.ParentCategories.Select(category => category.CategoryName));
    }

    [XmlIgnore]
    public string CategoryName
    {
      get { return _name; }
    }

    /// <summary>
    /// Returns the <see cref="MediaCategory"/> object to this data transfer object.
    /// </summary>
    public MediaCategory GetMediaCategory(IDictionary<string, MediaCategory_DTO> allCategories)
    {
      ICollection<MediaCategory> parentCategories = new List<MediaCategory>();
      foreach (string parentCategoryName in _parentCategories)
      {
        MediaCategory_DTO parentCategoryDto;
        if (!allCategories.TryGetValue(parentCategoryName, out parentCategoryDto))
          continue;
        parentCategories.Add(parentCategoryDto.GetMediaCategory(allCategories));
      }
      return new MediaCategory(_name, parentCategories);
    }

    public override int GetHashCode()
    {
      return _name.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      MediaCategory_DTO other = obj as MediaCategory_DTO;
      return other != null && _name.Equals(other._name);
    }

    public override string ToString()
    {
      return _name;
    }

    /// <summary>
    /// Serializes this media category instance to XML.
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
    /// Serializes this media category instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a media category instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized media category instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static MediaCategory_DTO Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as MediaCategory_DTO;
    }

    /// <summary>
    /// Deserializes a media category instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized media category instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static MediaCategory_DTO Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as MediaCategory_DTO;
    }

    #region Additional members for the XML serialization

    internal MediaCategory_DTO() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(MediaCategory_DTO)));
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
    [XmlElement("ParentCategories")]
    public HashSet<string> XML_ParentCategories
    {
      get { return _parentCategories; }
      set { _parentCategories = value; }
    }

    #endregion
  }
}
