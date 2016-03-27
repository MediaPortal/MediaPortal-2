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

using System.Reflection;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1.Data
{
  [DataContract]
  public class AudioDbAlbum
  {
    [DataMember(Name = "idAlbum")]
    public string AlbumId { get; set; }

    [DataMember(Name = "idArtist")]
    public string ArtistId { get; set; }

    [DataMember(Name = "strAlbum")]
    public string Album { get; set; }

    [DataMember(Name = "strArtist")]
    public string Artist { get; set; }

    [DataMember(Name = "intYearReleased")]
    public int? Year { get; set; }

    [DataMember(Name = "strGenre")]
    public string Genre { get; set; }

    [DataMember(Name = "strSubGenre")]
    public string SubGenre { get; set; }

    [DataMember(Name = "strReleaseFormat")]
    public string ReleaseFormat { get; set; }

    [DataMember(Name = "intSales")]
    public int? Sales { get; set; }

    [DataMember(Name = "strAlbumThumb")]
    public string AlbumThumb { get; set; }

    [DataMember(Name = "strAlbumThumbBack")]
    public string AlbumThumbBack { get; set; }

    [DataMember(Name = "strAlbumCDart")]
    public string AlbumCDart { get; set; }

    [DataMember(Name = "strAlbumSpine")]
    public string AlbumSpine { get; set; }

    [DataMember(Name = "strDescriptionEN")]
    public string DescriptionEN { get; set; }

    [DataMember(Name = "strDescriptionDE")]
    public string DescriptionDE { get; set; }

    [DataMember(Name = "strDescriptionFR")]
    public string DescriptionFR { get; set; }

    [DataMember(Name = "strDescriptionCN")]
    public string DescriptionCN { get; set; }

    [DataMember(Name = "strDescriptionIT")]
    public string DescriptionIT { get; set; }

    [DataMember(Name = "strDescriptionJP")]
    public string DescriptionJP { get; set; }

    [DataMember(Name = "strDescriptionRU")]
    public string DescriptionRU { get; set; }

    [DataMember(Name = "strDescriptionES")]
    public string DescriptionES { get; set; }

    [DataMember(Name = "strDescriptionPT")]
    public string DescriptionPT { get; set; }

    [DataMember(Name = "strDescriptionSE")]
    public string DescriptionSE { get; set; }

    [DataMember(Name = "strDescriptionNL")]
    public string DescriptionNL { get; set; }

    [DataMember(Name = "strDescriptionHU")]
    public string DescriptionHU { get; set; }

    [DataMember(Name = "strDescriptionNO")]
    public string DescriptionNO { get; set; }

    [DataMember(Name = "strDescriptionIL")]
    public string DescriptionIL { get; set; }

    [DataMember(Name = "strDescriptionPL")]
    public string DescriptionPL { get; set; }

    [DataMember(Name = "strReview")]
    public string Review { get; set; }

    [DataMember(Name = "strMood")]
    public string Mood { get; set; }

    [DataMember(Name = "strTheme")]
    public string Theme { get; set; }

    [DataMember(Name = "strSpeed")]
    public string Speed { get; set; }

    [DataMember(Name = "strLocation")]
    public string Location { get; set; }

    [DataMember(Name = "strMusicBrainzID")]
    public string MusicBrainzID { get; set; }

    [DataMember(Name = "strMusicBrainzArtistID")]
    public string MusicBrainzArtistID { get; set; }

    [DataMember(Name = "strItunesID")]
    public string ItunesID { get; set; }

    [DataMember(Name = "strAmazonID")]
    public string AmazonID { get; set; }

    [DataMember(Name = "strLocked")]
    public string Locked { get; set; }

    public string Description { get; set; }

    public void SetLanguage(string language)
    {
      PropertyInfo description = GetType().GetProperty("Description" + language.ToUpperInvariant());
      if(description != null)
      {
        Description = (string)description.GetValue(this);
      }
      if(description == null || string.IsNullOrEmpty(Description))
      {
        Description = DescriptionEN;
      }
    }
  }
}
