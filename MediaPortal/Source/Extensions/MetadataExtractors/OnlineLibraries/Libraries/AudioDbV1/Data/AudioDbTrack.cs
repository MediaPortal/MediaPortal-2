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
using System.Reflection;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1.Data
{
  //    {
  //      "idTrack": "32730340",
  //      "idAlbum": "2110240",
  //      "idArtist": "111279",
  //      "idLyric": "331605",
  //      "idIMVDB": null,
  //      "strTrack": "Nothing Else Matters",
  //      "strAlbum": "Metallica",
  //      "strArtist": "Metallica",
  //      "strArtistAlternate": null,
  //      "intCD": null,
  //      "intDuration": "388760",
  //      "strGenre": "Thrash Metal",
  //      "strMood": null,
  //      "strStyle": null,
  //      "strTheme": null,
  //      "strDescriptionEN": "\"Nothing Else Matters\" is a rock single by the American heavy metal band Metallica.",
  //      "strTrackThumb": "http://www.theaudiodb.com/images/media/track/thumb/xwxsrv1379922617.jpg",
  //      "strTrackLyrics": "",
  //      "strMusicVid": "http://www.youtube.com/watch?v=tAGnKpE4NCI",
  //      "strMusicVidDirector": "Adam Dubin",
  //      "strMusicVidCompany": "",
  //      "strMusicVidScreen1": null,
  //      "strMusicVidScreen2": null,
  //      "strMusicVidScreen3": null,
  //      "intMusicVidViews": "11826333",
  //      "intMusicVidLikes": "57685",
  //      "intMusicVidDislikes": "2018",
  //      "intMusicVidFavorites": null,
  //      "intMusicVidComments": null,
  //      "intTrackNumber": "8",
  //      "intLoved": "1",
  //      "intScore": null,
  //      "intScoreVotes": null,
  //      "strMusicBrainzID": "2dacc772-bff6-4347-a586-8bff3a7d7c79",
  //      "strMusicBrainzAlbumID": "e8f70201-8899-3f0c-9e07-5d6495bc8046",
  //      "strMusicBrainzArtistID": "65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab",
  //      "strLocked": "unlocked",
  //      "strDebugTime": "0 Secconds"
  //    }
  [DataContract]
  public class AudioDbTrack
  {
    [DataMember(Name = "idTrack")]
    public long TrackID { get; set; }

    [DataMember(Name = "idAlbum")]
    public long? AlbumID { get; set; }

    [DataMember(Name = "idLyric")]
    public long? LyricID { get; set; }

    [DataMember(Name = "idIMVDB")]
    public long? MvDbID { get; set; }

    [DataMember(Name = "idArtist")]
    public long? ArtistID { get; set; }

    [DataMember(Name = "strTrack")]
    public string Track { get; set; }

    [DataMember(Name = "strAlbum")]
    public string Album { get; set; }

    [DataMember(Name = "strArtist")]
    public string Artist { get; set; }

    [DataMember(Name = "intCD")]
    public int? CD { get; set; }

    [DataMember(Name = "intDuration")]
    public int? Duration { get; set; }

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

    [DataMember(Name = "intScore")]
    public double? Rating { get; set; }

    [DataMember(Name = "intScoreVotes")]
    public int? RatingCount { get; set; }

    [DataMember(Name = "strMusicBrainzID")]
    public string MusicBrainzID { get; set; }

    [DataMember(Name = "strMusicBrainzAlbumID")]
    public string MusicBrainzAlbumID { get; set; }

    [DataMember(Name = "strMusicBrainzArtistID")]
    public string MusicBrainzArtistID { get; set; }

    [DataMember(Name = "strLocked")]
    public string Locked { get; set; }

    public string Description { get; set; }

    public bool SetLanguage(string language)
    {
      if (!string.IsNullOrEmpty(language) && AudioDbApiV1.AvailableLanguageMap.ContainsKey(language.ToLowerInvariant()))
        language = AudioDbApiV1.AvailableLanguageMap[language.ToLowerInvariant()];
      else
        language = AudioDbApiV1.DefaultLanguage; 

      PropertyInfo description = GetType().GetProperty("Description" + language.ToUpperInvariant());
      if (description != null)
      {
        Description = (string)description.GetValue(this);
        return true;
      }
      else
      {
        Description = DescriptionEN;
      }
      return false;
    }
  }
}
