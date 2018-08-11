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

using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using MediaPortal.Common.Settings;

namespace MediaPortal.Extensions.MediaServer.Interfaces.Settings
{
  public class ProfileLinkSettings
  {
    #region Variables

    public static Dictionary<string, string> Profiles = new Dictionary<string, string>();

    #endregion

    public ProfileLinkSettings()
    {
      Links = new List<ProfileLink>();
    }

    [Setting(SettingScope.Global)]
    public List<ProfileLink> Links { get; set; }

    #region Additional members for the XML serialization

    /// <summary>
    /// Workaround property to enable automatic serialization because the <see cref="Dictionary{TKey,TValue}"/> cannot be serialized.
    /// </summary>
    [XmlAttribute("Profiles")]
    public DictionaryEntry[] XML_Profiles
    {
      get
      {
        DictionaryEntry[] entries = new DictionaryEntry[Profiles.Count];
        int count = 0;
        foreach (var entry in Profiles)
          entries[count++] = new DictionaryEntry(entry.Key, entry.Value);
        return entries;
      }
      set
      {
        if (Profiles.Count == 0)
        {
          foreach (DictionaryEntry entry in value)
            Profiles.Add((string)entry.Key, (string)entry.Value);
        }
      }
    }

    #endregion
  }

  public class ProfileLink
  {
    public string ClientName { get; set; }
    public string Profile { get; set; }
    public string DefaultUserProfile { get; set; }
  }
}
