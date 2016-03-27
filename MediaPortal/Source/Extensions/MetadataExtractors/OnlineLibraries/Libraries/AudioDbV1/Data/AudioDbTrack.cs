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
using System.Reflection;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1.Data
{
  [DataContract]
  public class AudioDbTrack
  {
    [DataMember(Name = "idTrack")]
    public string TrackId { get; set; }

    [DataMember(Name = "idAlbum")]
    public string AlbumId { get; set; }

    [DataMember(Name = "idArtist")]
    public string ArtistId { get; set; }

    [DataMember(Name = "strTrack")]
    public string Track { get; set; }

    [DataMember(Name = "strAlbum")]
    public string Album { get; set; }

    [DataMember(Name = "strArtist")]
    public string Artist { get; set; }

    [DataMember(Name = "intCD")]
    public int? CD { get; set; }

    [DataMember(Name = "intDuration")]
    public int Duration { get; set; }

    [DataMember(Name = "strGenre")]
    public string Genre { get; set; }

    [DataMember(Name = "strDescriptionEN")]
    public string DescriptionEN { get; set; }

    [DataMember(Name = "strTrackThumb")]
    public string TrackThumb { get; set; }

    [DataMember(Name = "strTrackLyrics")]
    public string TrackLyrics { get; set; }

    [DataMember(Name = "strMusicVid")]
    public string MusicVid { get; set; }

    [DataMember(Name = "strMusicVidDirector")]
    public string MusicVidDirector { get; set; }

    [DataMember(Name = "strMusicVidCompany")]
    public string MusicVidCompany { get; set; }

    [DataMember(Name = "strMusicVidScreen1")]
    public string MusicVidScreen1 { get; set; }

    [DataMember(Name = "strMusicVidScreen2")]
    public string MusicVidScreen2 { get; set; }

    [DataMember(Name = "strMusicVidScreen3")]
    public string MusicVidScreen3 { get; set; }

    [DataMember(Name = "intTrackNumber")]
    public int TrackNumber { get; set; }

    [DataMember(Name = "intLoved")]
    public int? Loved { get; set; }

    [DataMember(Name = "strMusicBrainzID")]
    public string MusicBrainzID { get; set; }

    [DataMember(Name = "strMusicBrainzAlbumID")]
    public string MusicBrainzAlbumID { get; set; }

    [DataMember(Name = "strMusicBrainzArtistID")]
    public string MusicBrainzArtistID { get; set; }

    [DataMember(Name = "strLocked")]
    public string Locked { get; set; }

    public string Description { get; set; }

    public void SetLanguage(string language)
    {
      PropertyInfo description = GetType().GetProperty("Description" + language.ToUpperInvariant());
      if (description != null)
      {
        Description = (string)description.GetValue(this);
      }
      if (description == null || string.IsNullOrEmpty(Description))
      {
        Description = DescriptionEN;
      }
    }
  }
}
