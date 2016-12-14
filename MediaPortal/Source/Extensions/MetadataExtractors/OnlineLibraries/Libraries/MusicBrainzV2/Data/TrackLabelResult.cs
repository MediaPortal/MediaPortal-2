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
  //{
  //  "created": "2016-04-27T11:33:17.78Z",
  //  "count": 1,
  //  "offset": 0,
  //  "labels": [
  //    {
  //      "id": "843e81b8-7ae9-40f5-b84c-1569d461c74d",
  //      "type": "Original Production",
  //      "score": "100",
  //      "name": "Medley Records",
  //      "sort-name": "Medley Records",
  //      "country": "DK",
  //      "area": {
  //        "id": "4757b525-2a60-324a-b060-578765d2c993",
  //        "name": "Denmark",
  //        "sort-name": "Denmark"
  //      },
  //      "life-span": {
  //        "begin": "1978",
  //        "end": "1992",
  //        "ended": true
  //      }
  //    }
  //  ]
  //}
  [DataContract]
  public class TrackLabelResult
  {
    [DataMember(Name = "labels")]
    public List<TrackLabelSearchResult> Results { get; set; }
  }
}
