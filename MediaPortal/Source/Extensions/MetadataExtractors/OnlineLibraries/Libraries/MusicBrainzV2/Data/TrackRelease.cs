#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
   [DataContract]
  public class TrackRelease
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "release-group")]
    public TrackReleaseGroup ReleaseGroup { get; set; }
    
    [DataMember(Name = "date")]
    public string Date { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }

    [DataMember(Name = "barcode")]
    public string Barcode { get; set; }

    [DataMember(Name = "track-count")]
    public int TrackCount { get; set; }

    [DataMember(Name = "media")]
    public IList<TrackMedia> Media { get; set; }

    [DataMember(Name = "artist-credit")]
    public IList<TrackArtistCredit> Artists { get; set; }

    public override string ToString()
    {
      return string.Format("Id: {0}, Title: {1}, Status: {2}, Date: {3}, Country: {4}, TrackCount: {5}, Media: [{6}]", Id, Title, Status, Date, Country, TrackCount, string.Join(",", Media));
    }
  }
}
