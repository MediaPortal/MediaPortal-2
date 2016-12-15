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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1.Data
{
  //  {
  //  "artists": [
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
  //      "strBiographySE": null,,
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
  //  ]
  //}
  [DataContract]
  public class AudioDbArtists
  {
    [DataMember(Name = "artists")]
    public List<AudioDbArtist> Artists { get; set; }
  }
}
