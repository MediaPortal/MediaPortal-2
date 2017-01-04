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

using System.Reflection;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1.Data
{
  //    {
  //      "idArtist": "111279",
  //      "idAlbum": "2110231",
  //      "idTrack": "32730241",
  //      "strTrack": "The View",
  //      "strTrackThumb": "http://www.theaudiodb.com/images/media/track/thumb/xswuyt1379925150.jpg",
  //      "strMusicVid": "http://www.youtube.com/watch?v=fJlU_9Vyvqs",
  //      "strDescriptionEN": "\"The View\" is the first (and only) single by American singer Lou Reed and American heavy metal band Metallica..."
  //    }
  [DataContract]
  public class AudioDbMvid
  {
    [DataMember(Name = "idArtist")]
    public long ArtistId { get; set; }

    [DataMember(Name = "idAlbum")]
    public long? AlbumId { get; set; }

    [DataMember(Name = "idTrack")]
    public int TrackId { get; set; }

    [DataMember(Name = "strDescriptionEn")]
    public string DescriptionEN { get; set; }

    [DataMember(Name = "strMusicVid")]
    public string MusicVid { get; set; }

    [DataMember(Name = "strTrack")]
    public string Track { get; set; }

    [DataMember(Name = "strTrackThumb")]
    public string TrackThumb { get; set; }

    public string Description { get; set; }

    public void SetLanguage(string language)
    {
      if (!string.IsNullOrEmpty(language) && AudioDbApiV1.AvailableLanguageMap.ContainsKey(language.ToLowerInvariant()))
        language = AudioDbApiV1.AvailableLanguageMap[language.ToLowerInvariant()];
      else
        language = AudioDbApiV1.DefaultLanguage;

      PropertyInfo description = GetType().GetProperty("Description" + language.ToUpperInvariant());
      if (description != null)
      {
        Description = (string)description.GetValue(this);
      }
      else
      {
        Description = DescriptionEN;
      }
    }
  }
}
