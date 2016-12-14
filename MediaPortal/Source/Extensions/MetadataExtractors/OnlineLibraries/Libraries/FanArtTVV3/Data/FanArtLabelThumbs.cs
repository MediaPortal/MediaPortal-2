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
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data
{
  //  {
  //  "name": "Profile Records",
  //  "id": "e832b688-546b-45e3-83e5-9f8db5dcde1d",
  //  "musiclabel": [
  //    {
  //      "id": "120425",
  //      "url": "http://assets.fanart.tv/labels/e832b688-546b-45e3-83e5-9f8db5dcde1d/musiclabel/profile-records-53633ee474306.png",
  //      "colour": "colour",
  //      "likes": "0"
  //    },
  //    {
  //      "id": "120426",
  //      "url": "http://assets.fanart.tv/labels/e832b688-546b-45e3-83e5-9f8db5dcde1d/musiclabel/profile-records-53633ef28bf22.png",
  //      "colour": "white",
  //      "likes": "0"
  //    }
  //  ]
  //}
  [DataContract]
  public class FanArtLabelThumbs
  {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "mbid_id")]
    public string MusicBrainzID { get; set; }

    [DataMember(Name = "musiclabel")]
    public List<FanArtLabelThumb> LabelLogos { get; set; }
  }
}
