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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data
{
//  {
//  "images": [
//    {
//      "types": [
//        "Front"
//      ],
//      "front": true,
//      "back": false,
//      "edit": 20239360,
//      "image": "http://coverartarchive.org/release/2f32ab2d-05cf-4e80-a93f-f1fd5e4c98b1/2884943713.jpg",
//      "comment": "",
//      "approved": true,
//      "thumbnails": {
//        "large": "http://coverartarchive.org/release/2f32ab2d-05cf-4e80-a93f-f1fd5e4c98b1/2884943713-500.jpg",
//        "small": "http://coverartarchive.org/release/2f32ab2d-05cf-4e80-a93f-f1fd5e4c98b1/2884943713-250.jpg"
//      },
//      "id": "2884943713"
//    }
//  ],
//  "release": "http://musicbrainz.org/release/2f32ab2d-05cf-4e80-a93f-f1fd5e4c98b1"
//}
  [DataContract]
  public class TrackImageCollection
  {
    [DataMember(Name = "images")]
    public List<TrackImage> Images { get; set; }

    [DataMember(Name = "release")]
    public string ReleaseUrl { get; set; }
  }
}
