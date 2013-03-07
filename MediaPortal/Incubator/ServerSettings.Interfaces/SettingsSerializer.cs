#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
  public class SettingsSerializer
  {
    public static object Deserialize(string settingsTypeName, string settings)
    {
      Type settingsType = Type.GetType(settingsTypeName);
      if (settingsType == null)
        return null;

      XmlSerializer xmlSerializer = new XmlSerializer(settingsType);
      return xmlSerializer.Deserialize(new StringReader(settings));
    }

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
