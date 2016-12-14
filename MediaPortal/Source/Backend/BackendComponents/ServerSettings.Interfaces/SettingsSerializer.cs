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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MediaPortal.Plugins.ServerSettings
{
  /// <summary>
  /// SettingsSerializer provides helper methods for XML (de-)serialization and Type lookups. Using an own <see cref="CustomAssemblyResolver"/> callers
  /// can provide custom methods to lookup types (i.e. if containing assembly was not yet loaded).
  /// </summary>
  public class SettingsSerializer
  {
    /// <summary>
    /// Custom assembly resolver, if set it will be used for settings type lookups only.
    /// </summary>
    public static ResolveEventHandler CustomAssemblyResolver;

    /// <summary>
    /// Tries to get the <see cref="Type"/> from the given <paramref name="settingsTypeName"/>.
    /// </summary>
    /// <param name="settingsTypeName">Assembly qualified type name.</param>
    /// <returns></returns>
    public static Type GetSettingsType(string settingsTypeName)
    {
      ResolveEventHandler customAssemblyResolver = CustomAssemblyResolver;
      try
      {
        if (customAssemblyResolver != null)
          AppDomain.CurrentDomain.AssemblyResolve += customAssemblyResolver;

        return Type.GetType(settingsTypeName);
      }
      finally 
      {
        if (customAssemblyResolver != null)
          AppDomain.CurrentDomain.AssemblyResolve -= customAssemblyResolver;
      }
    }

    /// <summary>
    /// Deserializes an <paramref name="settings"/> into the given <paramref name="settingsTypeName"/>.
    /// </summary>
    /// <param name="settingsTypeName">Assembly qualified type name.</param>
    /// <param name="settings">XML serialized settings object.</param>
    /// <returns></returns>
    public static object Deserialize(string settingsTypeName, string settings)
    {
      Type settingsType = GetSettingsType(settingsTypeName);
      if (settingsType == null)
        return null;

      XmlSerializer xmlSerializer = new XmlSerializer(settingsType);
      return xmlSerializer.Deserialize(new StringReader(settings));
    }

    /// <summary>
    /// Serializes a given <paramref name="settingsObject"/> into XML.
    /// </summary>
    /// <param name="settingsObject">Object to serialize.</param>
    /// <returns>XML representation.</returns>
    public static string Serialize(object settingsObject)
    {
      StringBuilder serialized = new StringBuilder();
      XmlSerializer xmlSerializer = new XmlSerializer(settingsObject.GetType());
      using (XmlWriter writer = XmlWriter.Create(serialized, new XmlWriterSettings { OmitXmlDeclaration = true }))
        xmlSerializer.Serialize(writer, settingsObject);
      return serialized.ToString();
    }
  }
}
