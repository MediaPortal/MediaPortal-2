#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
  //{
  //  "back": false,
  //  "darkened": false,
  //  "front": true,
  //  "count": 1,
  //  "artwork": true
  //}
  [DataContract]
  public class TrackCoverArt
  {
    [DataMember(Name = "back")]
    public bool Back { get; set; }

    [DataMember(Name = "darkened")]
    public bool Darkened { get; set; }

    [DataMember(Name = "front")]
    public bool Front { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }

    [DataMember(Name = "artwork")]
    public bool Artwork { get; set; }
  }
}
