///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(IsReference = false, Name = "program", Namespace = "")]
  public class Program
  {
    [DataMember(Name = "program_id", EmitDefaultValue = true, IsRequired = false, Order = 0)]
    public string Id { get; set; }

    [DataMember(Name = "name", EmitDefaultValue = true, IsRequired = false, Order = 1)]
    public string Title { get; set; }

    [DataMember(Name = "short_desc", EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public string ShortDesc { get; set; }

    [DataMember(Name = "actors", EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public string Actors { get; set; }

    [DataMember(Name = "directors", EmitDefaultValue = false, IsRequired = false, Order = 4)]
    public string Directors { get; set; }

    [DataMember(Name = "subname", EmitDefaultValue = false, IsRequired = false, Order = 5)]
    public string Subname { get; set; }

    [DataMember(Name = "producers", EmitDefaultValue = false, IsRequired = false, Order = 6)]
    public string Producers { get; set; }

    [DataMember(Name = "guests", EmitDefaultValue = false, IsRequired = false, Order = 7)]
    public string Guests { get; set; }

    [DataMember(Name = "categories", EmitDefaultValue = false, IsRequired = false, Order = 8)]
    public string Keywords { get; set; }

    [DataMember(Name = "image", EmitDefaultValue = false, IsRequired = false, Order = 9)]
    public string Image { get; set; }

    [DataMember(Name = "start_time", EmitDefaultValue = false, IsRequired = false, Order = 10)]
    public long StartTime { get; set; }

    [DataMember(Name = "duration", EmitDefaultValue = false, IsRequired = false, Order = 11)]
    public int Duration { get; set; }

    [DataMember(Name = "year", EmitDefaultValue = false, IsRequired = false, Order = 12)]
    public int Year { get; set; }

    [DataMember(Name = "language", EmitDefaultValue = false, IsRequired = false, Order = 13)]
    public string Language { get; set; }

    [DataMember(Name = "writers", EmitDefaultValue = false, IsRequired = false, Order = 14)]
    public string Writers { get; set; }

    [DataMember(Name = "episode_num", EmitDefaultValue = false, IsRequired = false, Order = 15)]
    public int EpisodeNum { get; set; }

    [DataMember(Name = "season_num", EmitDefaultValue = false, IsRequired = false, Order = 16)]
    public int SeasonNum { get; set; }

    [DataMember(Name = "stars_num", EmitDefaultValue = false, IsRequired = false, Order = 17)]
    public int StarsNum { get; set; }

    [DataMember(Name = "starsmax_num", EmitDefaultValue = false, IsRequired = false, Order = 18)]
    public int StarsMaxNum { get; set; }

    [DataMember(Name = "hdtv", EmitDefaultValue = false, IsRequired = false, Order = 19)]
    public bool IsHdtv { get; set; }

    [DataMember(Name = "premiere", EmitDefaultValue = false, IsRequired = false, Order = 20)]
    public bool IsPremiere { get; set; }

    [DataMember(Name = "repeat", EmitDefaultValue = false, IsRequired = false, Order = 21)]
    public bool IsRepeat { get; set; }

    [DataMember(Name = "cat_action", EmitDefaultValue = false, IsRequired = false, Order = 22)]
    public bool IsAction { get; set; }

    [DataMember(Name = "cat_comedy", EmitDefaultValue = false, IsRequired = false, Order = 23)]
    public bool IsComedy { get; set; }

    [DataMember(Name = "cat_documentary", EmitDefaultValue = false, IsRequired = false, Order = 24)]
    public bool IsDocumentary { get; set; }

    [DataMember(Name = "cat_drama", EmitDefaultValue = false, IsRequired = false, Order = 25)]
    public bool IsDrama { get; set; }

    [DataMember(Name = "cat_educational", EmitDefaultValue = false, IsRequired = false, Order = 26)]
    public bool IsEducational { get; set; }

    [DataMember(Name = "cat_horror", EmitDefaultValue = false, IsRequired = false, Order = 27)]
    public bool IsHorror { get; set; }

    [DataMember(Name = "cat_kids", EmitDefaultValue = false, IsRequired = false, Order = 28)]
    public bool IsKids { get; set; }

    [DataMember(Name = "cat_movie", EmitDefaultValue = false, IsRequired = false, Order = 29)]
    public bool IsMovie { get; set; }

    [DataMember(Name = "cat_music", EmitDefaultValue = false, IsRequired = false, Order = 30)]
    public bool IsMusic { get; set; }

    [DataMember(Name = "cat_news", EmitDefaultValue = false, IsRequired = false, Order = 31)]
    public bool IsNews { get; set; }

    [DataMember(Name = "cat_reality", EmitDefaultValue = false, IsRequired = false, Order = 32)]
    public bool IsReality { get; set; }

    [DataMember(Name = "cat_romance", EmitDefaultValue = false, IsRequired = false, Order = 33)]
    public bool IsRomance { get; set; }

    [DataMember(Name = "cat_scifi", EmitDefaultValue = false, IsRequired = false, Order = 34)]
    public bool IsScifi { get; set; }

    [DataMember(Name = "cat_serial", EmitDefaultValue = false, IsRequired = false, Order = 35)]
    public bool IsSerial { get; set; }

    [DataMember(Name = "cat_soap", EmitDefaultValue = false, IsRequired = false, Order = 36)]
    public bool IsSoap { get; set; }

    [DataMember(Name = "cat_special", EmitDefaultValue = false, IsRequired = false, Order = 37)]
    public bool IsSpecial { get; set; }

    [DataMember(Name = "cat_sports", EmitDefaultValue = false, IsRequired = false, Order = 38)]
    public bool IsSports { get; set; }

    [DataMember(Name = "cat_thriller", EmitDefaultValue = false, IsRequired = false, Order = 39)]
    public bool IsThriller { get; set; }

    [DataMember(Name = "cat_adult", EmitDefaultValue = false, IsRequired = false, Order = 40)]
    public bool IsAdult { get; set; }

    [DataMember(Name = "is_series", EmitDefaultValue = false, IsRequired = false, Order = 41)]
    public bool IsSeries { get; set; }

    [DataMember(Name = "is_record", EmitDefaultValue = false, IsRequired = false, Order = 42)]
    public bool IsRecord { get; set; }

    [DataMember(Name = "is_repeat_record", EmitDefaultValue = false, IsRequired = false, Order = 43)]
    public bool IsRepeatRecord { get; set; }
  }

  [CollectionDataContract(Name = "dvblink_epg", Namespace = "")]
  public class Programs : List<Program>
  {
  }
}
