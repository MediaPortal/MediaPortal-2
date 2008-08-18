#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 *  Code modified from SharpDevelop AddIn code
 *  Thanks goes to: Mike Krüger
 */

#endregion

using System;
using System.Collections.Generic;
using System.Xml;
using MediaPortal.Core.PluginManager.PluginSpace;

namespace MediaPortal.Services.PluginManager.PluginDetails
{
  /// <summary>
  /// Stores information about the manifest of a plugin.
  /// </summary>
  internal class PluginManifest
  {
    #region Variables
    List<PluginReference> _dependencies = new List<PluginReference>();
    List<PluginReference> _conflicts = new List<PluginReference>();
    Dictionary<string, Version> _identities = new Dictionary<string, Version>();
    Version _version;
    string _identity;
    #endregion

    #region Properties
    public string Identity
    {
      get { return _identity; }
    }

    public Version Version
    {
      get { return _version; }
    }

    public Dictionary<string, Version> Identities
    {
      get { return _identities; }
    }

    public List<PluginReference> Dependencies
    {
      get { return _dependencies; }
    }

    public List<PluginReference> Conflicts
    {
      get { return _conflicts; }
    }
    #endregion

    #region Public Methods
    public void ReadManifestSection(XmlReader reader, string hintPath)
    {
      if (reader.AttributeCount != 0)
      {
        throw new PluginLoadException("Manifest node cannot have attributes.");
      }
      if (reader.IsEmptyElement)
      {
        throw new PluginLoadException("Manifest node cannot be empty.");
      }
      while (reader.Read())
      {
        switch (reader.NodeType)
        {
          case XmlNodeType.EndElement:
            if (reader.LocalName == "Manifest")
            {
              return;
            }
            break;
          case XmlNodeType.Element:
            string nodeName = reader.LocalName;
            PluginProperties properties = PluginProperties.ReadFromAttributes(reader);
            switch (nodeName)
            {
              case "Identity":
                AddIdentity(properties["name"], properties["version"], hintPath);
                break;
              case "Dependency":
                _dependencies.Add(PluginReference.Create(properties, hintPath));
                break;
              case "Conflict":
                _conflicts.Add(PluginReference.Create(properties, hintPath));
                break;
              default:
                throw new PluginLoadException("Unknown node in Manifest section:" + nodeName);
            }
            break;
        }
      }
    }
    #endregion

    #region Private Methods
    private void AddIdentity(string name, string version, string hintPath)
    {
      if (name.Length == 0)
        throw new PluginLoadException("Identity needs a name");
      foreach (char c in name)
      {
        if (!char.IsLetterOrDigit(c) && c != '.' && c != '_')
        {
          throw new PluginLoadException("Identity name contains invalid character: '" + c + "'");
        }
      }
      Version v = PluginReference.ParseVersion(version, hintPath);
      if (_version == null)
      {
        _version = v;
      }
      if (_identity == null)
      {
        _identity = name;
      }
      //identities.Add(name, v);
    }
    #endregion
  }
}
