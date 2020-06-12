using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.TheGamesDb.Data.Platforms
{
  [DataContract]
  public class Platform
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "alias")]
    public string Alias { get; set; }

    [DataMember(Name = "icon")]
    public string Icon { get; set; }

    [DataMember(Name = "console")]
    public string Console { get; set; }

    [DataMember(Name = "controller")]
    public string Controller { get; set; }

    [DataMember(Name = "developer")]
    public string Developer { get; set; }

    [DataMember(Name = "manufacturer")]
    public string Manufacturer { get; set; }

    [DataMember(Name = "media")]
    public string media { get; set; }

    [DataMember(Name = "cpu")]
    public string Cpu { get; set; }

    [DataMember(Name = "memory")]
    public string Memory { get; set; }

    [DataMember(Name = "graphics")]
    public string Graphics { get; set; }

    [DataMember(Name = "sound")]
    public string Sound { get; set; }

    [DataMember(Name = "maxcontrollers")]
    public string MaxControllers { get; set; }

    [DataMember(Name = "display")]
    public string Display { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "youtube")]
    public string Youtube { get; set; }
  }
}
