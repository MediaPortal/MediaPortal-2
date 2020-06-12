using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.TheGamesDb.Data
{
  /*
        {
          "id": 1,
          "game_title": "Halo: Combat Evolved",
          "release_date": "2001-11-15",
          "platform": 1,
          "players": 1,
          "overview": "In Halo's twenty-sixth century setting, the player assumes the role of the Master Chief, a cybernetically enhanced super-soldier. The player is accompanied by Cortana, an artificial intelligence who occupies the Master Chief's neural interface. Players battle various aliens on foot and in vehicles as they attempt to uncover the secrets of the eponymous Halo, a ring-shaped artificial planet.",
          "last_updated": "2018-07-11 21:05:01",
          "rating": "M - Mature",
          "coop": "No",
          "youtube": "dR3Hm8scbEw",
          "os": "98SE/ME/2000/XP",
          "processor": "733mhz",
          "ram": "128 MB",
          "hdd": "1.2GB",
          "video": "32 MB / 3D T&L capable",
          "sound": null,
          "developers": [
            1389
          ],
          "genres": [
            8
          ],
          "publishers": [
            1
          ],
          "alternates": null
        }
  */

  [DataContract]
  public class Game
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "game_title")]
    public string GameTitle { get; set; }

    [DataMember(Name = "release_date")]
    public string ReleaseDate { get; set; }

    [DataMember(Name = "platform")]
    public int Platform { get; set; }

    [DataMember(Name = "players")]
    public int? Players { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "last_updated")]
    public string LastUpdated { get; set; }

    [DataMember(Name = "rating")]
    public string Rating { get; set; }

    [DataMember(Name = "coop")]
    public string Coop { get; set; }

    [DataMember(Name = "youtube")]
    public string Youtube { get; set; }

    [DataMember(Name = "os")]
    public string OS { get; set; }

    [DataMember(Name = "processor")]
    public string Processor { get; set; }

    [DataMember(Name = "ram")]
    public string Ram { get; set; }

    [DataMember(Name = "hdd")]
    public string Hdd { get; set; }

    [DataMember(Name = "video")]
    public string Video { get; set; }

    [DataMember(Name = "sound")]
    public string Sound { get; set; }

    [DataMember(Name = "developers")]
    public int[] Developers { get; set; }

    [DataMember(Name = "genres")]
    public int[] Genres { get; set; }

    [DataMember(Name = "publishers")]
    public int[] Publishers { get; set; }

    [DataMember(Name = "alternates")]
    public string[] Alternates { get; set; }
  }
}
