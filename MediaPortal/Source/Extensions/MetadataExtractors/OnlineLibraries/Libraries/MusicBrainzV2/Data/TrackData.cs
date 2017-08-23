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
  //            {
  //              "title": "New Life",
  //              "artist-credit": [
  //                {
  //                  "artist": {
  //                    "disambiguation": "",
  //                    "name": "Depeche Mode",
  //                    "id": "8538e728-ca0b-4321-b7e5-cff6565dd4c0",
  //                    "sort-name": "Depeche Mode"
  //                  },
  //                  "name": "Depeche Mode",
  //                  "joinphrase": ""
  //                }
  //              ],
  //              "id": "40a141c2-08f2-36b9-9ccd-d75f47c847b1",
  //              "number": "A",
  //              "length": 223000
  //            }
  [DataContract]
  public class TrackData
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "number")]
    public string Number { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "length")]
    public long? Length { get; set; }

    [DataMember(Name = "artist-credit")]
    public List<TrackArtistCredit> Artists { get; set; }

    [DataMember(Name = "recording")]
    public TrackRecording Recording { get; set; }

    public override string ToString()
    {
      return string.Format("Id: {0}, Number: {1}, Title: {2}, Length: {3}", Id, Number, Title, Length);
    }
  }
}
