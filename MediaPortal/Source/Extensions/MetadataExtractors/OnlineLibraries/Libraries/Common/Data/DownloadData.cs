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
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Common.Data
{
  public class DownloadData
  {
    public string FanArtMediaType;
    public string ShortLanguage;
    public string MediaItemId;
    public string Name;
    public Dictionary<string, string> FanArtId = new Dictionary<string, string>();

    public string Serialize()
    {
      StringBuilder builder = new StringBuilder();
      builder.Append(FanArtMediaType ?? "").Append('|');
      builder.Append(ShortLanguage ?? "").Append('|');
      builder.Append(MediaItemId ?? "").Append('|');
      builder.Append(Name ?? "").Append('|');
      foreach (KeyValuePair<string, string> pair in FanArtId)
      {
        builder.Append(pair.Key).Append(":").Append(pair.Value).Append(',');
      }
      string result = builder.ToString();
      return result.TrimEnd(',');
    }

    public bool Deserialize(string val)
    {
      FanArtId.Clear();
      string[] tokens = val.Split('|');
      if (tokens.Length != 5)
        return false;
      FanArtMediaType = tokens[0];
      ShortLanguage = tokens[1];
      MediaItemId = tokens[2];
      Name = tokens[3];
      tokens = tokens[4].Split(new char[] { ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
      for (int i = 0; i < tokens.Length; i += 2)
      {
        string key = tokens[i];
        string value = tokens[i + 1];
        FanArtId[tokens[i]] = tokens[i + 1];
      }
      return true;
    }
  }
}
