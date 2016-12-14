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
  //      "idArtist": "111239",
  //      "strArtist": "Coldplay",
  //      "strArtistAlternate": "",
  //      "strLabel": "Parlophone",
  //      "idLabel": "45114",
  //      "intFormedYear": "1996",
  //      "intBornYear": null,
  //      "intDiedYear": null,
  //      "strDisbanded": null,
  //      "strStyle": "Rock/Pop",
  //      "strGenre": "Alternative Rock",
  //      "strMood": "Happy",
  //      "strWebsite": "www.coldplay.com",
  //      "strFacebook": "www.facebook.com/coldplay",
  //      "strTwitter": "www.twitter.com/coldplay",
  //      "strBiographyEN": "Coldplay are a British alternative rock band formed in 1996 by lead vocalist Chris Martin and...",
  //      "strBiographyDE": null,
  //      "strBiographyFR": null,
  //      "strBiographyCN": null,
  //      "strBiographyIT": null,
  //      "strBiographyJP": null,
  //      "strBiographyRU": null,
  //      "strBiographyES": null,
  //      "strBiographyPT": null,
  //      "strBiographySE": null,
  //      "strBiographyNL": null,
  //      "strBiographyHU": null,
  //      "strBiographyNO": null,
  //      "strBiographyIL": null,
  //      "strBiographyPL": null,
  //      "strGender": "Male",
  //      "intMembers": "4",
  //      "strCountry": "London, England",
  //      "strCountryCode": "GB",
  //      "strArtistThumb": "http://www.theaudiodb.com/images/media/artist/thumb/uxrqxy1347913147.jpg",
  //      "strArtistLogo": "http://www.theaudiodb.com/images/media/artist/logo/urspuv1434553994.png",
  //      "strArtistFanart": "http://media.theaudiodb.com/images/media/artist/fanart/spvryu1347980801.jpg",
  //      "strArtistFanart2": "http://media.theaudiodb.com/images/media/artist/fanart/uupyxx1342640221.jpg",
  //      "strArtistFanart3": "http://media.theaudiodb.com/images/media/artist/fanart/qstpsp1342640238.jpg",
  //      "strArtistBanner": "http://www.theaudiodb.com/images/media/artist/banner/xuypqw1386331010.jpg",
  //      "strMusicBrainzID": "cc197bad-dc9c-440d-a5b5-d52ba2e14234",
  //      "strLastFMChart": "http://www.last.fm/music/Coldplay/+charts?rangetype=6month",
  //      "strLocked": "unlocked"
  //    }
  [DataContract]
  public class AudioDbArtist
  {
    [DataMember(Name = "idArtist")]
    public long ArtistId { get; set; }

    [DataMember(Name = "strArtist")]
    public string Artist { get; set; }

    [DataMember(Name = "strArtistAlternate")]
    public string ArtistAlternate { get; set; }

    [DataMember(Name = "idLabel")]
    public int? Label { get; set; }

    [DataMember(Name = "intFormedYear")]
    public int? FormedYear { get; set; }

    [DataMember(Name = "intBornYear")]
    public int? BornYear { get; set; }

    [DataMember(Name = "intDiedYear")]
    public int? DiedYear { get; set; }

    [DataMember(Name = "strDisbanded")]
    public string Disbanded { get; set; }

    [DataMember(Name = "strGenre")]
    public string Genre { get; set; }

    [DataMember(Name = "strSubGenre")]
    public string SubGenre { get; set; }

    [DataMember(Name = "strWebsite")]
    public string Website { get; set; }

    [DataMember(Name = "strFacebook")]
    public string Facebook { get; set; }

    [DataMember(Name = "strTwitter")]
    public string Twitter { get; set; }

    [DataMember(Name = "strBiographyEN")]
    public string BiographyEN { get; set; }

    [DataMember(Name = "strBiographyDE")]
    public string BiographyDE { get; set; }

    [DataMember(Name = "strBiographyFR")]
    public string BiographyFR { get; set; }

    [DataMember(Name = "strBiographyCN")]
    public string BiographyCN { get; set; }

    [DataMember(Name = "strBiographyIT")]
    public string BiographyIT { get; set; }

    [DataMember(Name = "strBiographyJP")]
    public string BiographyJP { get; set; }

    [DataMember(Name = "strBiographyRU")]
    public string BiographyRU { get; set; }

    [DataMember(Name = "strBiographyES")]
    public string BiographyES { get; set; }

    [DataMember(Name = "strBiographyPT")]
    public string BiographyPT { get; set; }

    [DataMember(Name = "strBiographySE")]
    public string BiographySE { get; set; }

    [DataMember(Name = "strBiographyNL")]
    public string BiographyNL { get; set; }

    [DataMember(Name = "strBiographyHU")]
    public string BiographyHU { get; set; }

    [DataMember(Name = "strBiographyNO")]
    public string BiographyNO { get; set; }

    [DataMember(Name = "strBiographyIL")]
    public string BiographyIL { get; set; }

    [DataMember(Name = "strBiographyPL")]
    public string BiographyPL { get; set; }

    [DataMember(Name = "strGender")]
    public string Gender { get; set; }

    [DataMember(Name = "intMembers")]
    public int? Members { get; set; }

    [DataMember(Name = "strCountry")]
    public string Country { get; set; }

    [DataMember(Name = "strCountryCode")]
    public string CountryCode { get; set; }

    [DataMember(Name = "strArtistThumb")]
    public string ArtistThumb { get; set; }

    [DataMember(Name = "strArtistLogo")]
    public string ArtistLogo { get; set; }

    [DataMember(Name = "strArtistFanart")]
    public string ArtistFanart { get; set; }

    [DataMember(Name = "strArtistFanart2")]
    public string ArtistFanart2 { get; set; }

    [DataMember(Name = "strArtistFanart3")]
    public string ArtistFanart3 { get; set; }

    [DataMember(Name = "strArtistBanner")]
    public string ArtistBanner { get; set; }

    [DataMember(Name = "strMusicBrainzID")]
    public string MusicBrainzID { get; set; }

    [DataMember(Name = "strLastFMChart")]
    public string LastFMChart { get; set; }

    [DataMember(Name = "strLocked")]
    public string Locked { get; set; }

    public string Biography { get; set; }

    public bool SetLanguage(string language)
    {
      if (!string.IsNullOrEmpty(language) && AudioDbApiV1.AvailableLanguageMap.ContainsKey(language.ToLowerInvariant()))
        language = AudioDbApiV1.AvailableLanguageMap[language.ToLowerInvariant()];
      else
        language = AudioDbApiV1.DefaultLanguage;

      PropertyInfo biography = GetType().GetProperty("Biography" + language.ToUpperInvariant());
      if (biography != null)
      {
        Biography = (string)biography.GetValue(this);
        return true;
      }
      else
      {
        Biography = BiographyEN;
      }
      return false;
    }
  }
}
