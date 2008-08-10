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
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Presentation.Localisation;

using MediaPortal.Media.MetaData;

namespace Components.Services.MetaDataMapper
{
  public class MetadataMappingProvider : IMetadataMappingProvider
  {
    Dictionary<string, IMetaDataMappingCollection> _mappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataMappingProvider"/> class.
    /// </summary>
    public MetadataMappingProvider()
    {
      _mappings = new Dictionary<string, IMetaDataMappingCollection>();
    }
    #region IMetadataMappingProvider Members

    /// <summary>
    /// Gets the metadata mapping for the specific mapping name
    /// </summary>
    /// <param name="mappingName"></param>
    /// <returns></returns>
    public IMetaDataMappingCollection Get(string mappingName)
    {
      if (Contains(mappingName))
      {
        return _mappings[mappingName];
      }
      return null;
    }

    /// <summary>
    /// Adds a new mapping
    /// </summary>
    /// <param name="name">The name for the mapping.</param>
    /// <param name="mapping">The mapping.</param>
    public void Add(string name, IMetaDataMappingCollection mapping)
    {
      _mappings[name] = mapping;
    }

    /// <summary>
    /// Determines whether the provider contains the mapping with the name specified
    /// </summary>
    /// <param name="mappingName">Name of the mapping.</param>
    /// <returns>
    /// 	<c>true</c> if provider contains the specified mapping ; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(string mappingName)
    {
      if (String.IsNullOrEmpty(mappingName)) return false;
      if (!_mappings.ContainsKey(mappingName))
      {
        if (LoadMappingDefinition(mappingName)) return true;
        return false;
      }
      return true;
    }

    /// <summary>
    /// Loads the mapping definition from disk
    /// </summary>
    /// <param name="mappingName">Name of the mapping.</param>
    bool LoadMappingDefinition(string mappingName)
    {
      IMetaDataFormatterCollection serviceFormatters = ServiceScope.Get<IMetaDataFormatterCollection>();

      string fileName = String.Format(@"ViewMapping\{0}.xml", mappingName);
      if (!File.Exists(fileName)) return false;
      XmlTextReader reader = new XmlTextReader(fileName);
      XmlDocument doc = new XmlDocument();
      doc.Load(reader);
      XmlNodeList maps = doc.SelectNodes("/mapping/map");
      IMetaDataMappingCollection collection = new MetaDataMappingCollection();
      foreach (XmlNode map in maps)
      {
        MetadataMapping mapping = new MetadataMapping();
        mapping.LocalizedName = new StringId(getValue(map, "name"));
        XmlNodeList items = map.SelectNodes("item");
        foreach (XmlNode nodeItem in items)
        {
          MetadataMappingItem item = new MetadataMappingItem();
          item.Formatting = getValue(nodeItem, "formatas");
          item.MetaDataField = getValue(nodeItem, "metadata");
          item.SkinLabel = getValue(nodeItem, "skinlabel");
          string formatter = getValue(nodeItem, "format");
          if (serviceFormatters.Contains(formatter))
          {
            item.Formatter = serviceFormatters.Get(formatter);
          }
          mapping.Items.Add(item);
        }
        collection.Add(mapping);
      }
      this.Add(mappingName, collection);

      return true;
    }

    string getValue(XmlNode node, string attributeName)
    {
      XmlNode attrib = node.Attributes.GetNamedItem(attributeName);
      if (attrib == null)
      {
        return "";
      }
      string name = attrib.Value;
      if (name == null)
      {
        return "";
      }
      return name;
    }
    #endregion
  }
}
