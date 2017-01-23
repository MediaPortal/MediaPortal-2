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

using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data
{
  //  "artist-credit": [
  //    {
  //      "joinphrase": "",
  //      "artist": {
  //        "disambiguation": "",
  //        "sort-name": "Depeche Mode",
  //        "id": "8538e728-ca0b-4321-b7e5-cff6565dd4c0",
  //        "name": "Depeche Mode"
  //      },
  //      "name": "Depeche Mode"
  //    }
  //  ]
  [DataContract]
  public class TrackArtistCredit
  {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "artist")]
    public TrackBaseName Artist { get; set; }

    public override string ToString()
    {
      return string.Format("Artist: {0}", Name);
    }
  }
}
