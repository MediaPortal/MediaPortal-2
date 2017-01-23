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
  //      "idAlbum": "2110240",
  //      "idArtist": "111279",
  //      "idLabel": null,
  //      "strAlbum": "Metallica",
  //      "strArtist": "Metallica",
  //      "intYearReleased": "1991",
  //      "strStyle": "Metal",
  //      "strGenre": "Thrash Metal",
  //      "strLabel": null,
  //      "strReleaseFormat": "Album",
  //      "intSales": "30000000",
  //      "strAlbumThumb": "http://www.theaudiodb.com/images/media/album/thumb/vxtttu1347748172.jpg",
  //      "strAlbumThumbBack": null,
  //      "strAlbumCDart": "http://www.theaudiodb.com/images/media/album/cdart/metallica-4edaa38433179.png",
  //      "strAlbumSpine": null,
  //      "strDescriptionEN": "Metallica (also known as The Black Album) is the eponymously-titled...",
  //      "strDescriptionDE": null,
  //      "strDescriptionFR": null,
  //      "strDescriptionCN": null,
  //      "strDescriptionIT": null,
  //      "strDescriptionJP": null,
  //      "strDescriptionRU": null,
  //      "strDescriptionES": null,
  //      "strDescriptionPT": null,
  //      "strDescriptionSE": null,
  //      "strDescriptionNL": null,
  //      "strDescriptionHU": null,
  //      "strDescriptionNO": null,
  //      "strDescriptionIL": null,
  //      "strDescriptionPL": null,
  //      "intLoved": null,
  //      "intScore": "10",
  //      "intScoreVotes": "2",
  //      "strReview": "Any attempt to move away from a tried and tested formula is often met with resistance by some fans who never want their idols to change. Smarter than your average heavy metal band, the more complex turn-on-a dime twists of their previous albums, Master Of Puppets and 1989�s And Justice For All, were trimmed back in favour of a more honed-down delivery. \n\nThough the band didn�t always see eye to eye with Bob Rock (who had previously cut his teeth engineering for the likes of Bon Jovi before producing Motley Crue�s Dr.Feelgood), the tensions between the two camps resulted in an album bursting at the seams with alternative ideas.\n\nSure enough, accusations that they had sold out came from the rump of hardcore fans within seconds of their fifth album being released in 1991. Several years later thousands of fans signed an online petition calling on the band to sever its links with Bob Rock such was their conviction that their beloved Metallica had strayed from the straight and narrow.\n\nYet his involvement gained them mass sales (number one on both sides of the Atlantic) and earned them the Grammy they�d missed out on, having lost out to Jethro Tull�s Catfish Rising the previous year. With millions of new fans going on to discover their back catalogue, Metallica moved from cult metal gods to bona fide rock stars, straddling the airwaves with the psycho-dramatics of �Enter Sandman�, whose terse motifs served notice that things were changing. \n\nThe spaghetti western set dressing of �The Unforgiven�, �Nothing Else Matters� with its sensitive lyrics and string section embellishments, as well as the widescreen dynamics of �My Friend Of Misery� demonstrated how keen they were to move things on. In �The God That Failed�, vocalist, rhythm guitarist and principle writer, James Hetfield deals unflinchingly with parental loss and the contradictions of faith in a mature and considered manner. \n\nThe confidence exuding from almost every track isn�t due to a clich�d, puffed-up HM swagger but a result of literate and articulate artists breaking free of generic expectation.",
  //      "strMood": "Angry",
  //      "strTheme": null,
  //      "strSpeed": "Medium",
  //      "strLocation": null,
  //      "strMusicBrainzID": "e8f70201-8899-3f0c-9e07-5d6495bc8046",
  //      "strMusicBrainzArtistID": "65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab",
  //      "strItunesID": null,
  //      "strAmazonID": null,
  //      "strLocked": "unlocked"
  //    }
  [DataContract]
  public class AudioDbAlbum
  {
    [DataMember(Name = "idAlbum")]
    public long AlbumId { get; set; }

    [DataMember(Name = "idArtist")]
    public long? ArtistId { get; set; }

    [DataMember(Name = "idLabel")]
    public long? LabelId { get; set; }

    [DataMember(Name = "strAlbum")]
    public string Album { get; set; }

    [DataMember(Name = "strArtist")]
    public string Artist { get; set; }

    [DataMember(Name = "strLabel")]
    public string Label { get; set; }

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

    [DataMember(Name = "intScore")]
    public double? Rating { get; set; }

    [DataMember(Name = "intScoreVotes")]
    public int? RatingCount { get; set; }

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

    public bool SetLanguage(string language)
    {
      if (!string.IsNullOrEmpty(language) && AudioDbApiV1.AvailableLanguageMap.ContainsKey(language.ToLowerInvariant()))
        language = AudioDbApiV1.AvailableLanguageMap[language.ToLowerInvariant()];
      else
        language = AudioDbApiV1.DefaultLanguage;

      PropertyInfo description = GetType().GetProperty("Description" + language.ToUpperInvariant());
      if(description != null)
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
