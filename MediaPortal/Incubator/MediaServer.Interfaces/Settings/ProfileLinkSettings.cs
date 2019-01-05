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
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using MediaPortal.Common.Settings;
using MediaPortal.Utilities.UPnP;

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

    [XmlAttribute("Profiles")]
    public string XML_Profiles
    {
      get
      {
        List<Tuple<string, string>> convert = new List<Tuple<string, string>>();
        foreach (var key in Profiles)
        {
          convert.Add(new Tuple<string, string>(key.Key.ToString(), key.Value));
        }
        if (convert.Count > 0)
          return MarshallingHelper.SerializeTuple2EnumerationToCsv(convert);
        return null;
      }
      set
      {
        IEnumerable<Tuple<string, string>> convert = MarshallingHelper.ParseCsvTuple2Collection(value);
        if (convert == null)
          return;

        if (Profiles.Count == 0)
        {
          foreach (var entry in convert)
            Profiles.Add(entry.Item1, entry.Item2);
        }
      }
    }

    #endregion
  }

  public class ProfileLink
  {
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public string Profile { get; set; }
    public string DefaultUserProfile { get; set; }
  }
}
