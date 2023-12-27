using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Webradio.OnlineLibraries.MusicBrainz
{
  [DataContract]
  public class Alias
  {

    [DataMember(Name = "sort-name")]
    public string SortName { get; set; }

    [DataMember(Name = "type-id")]
    public string TypeId { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "locale")]
    public string Locale { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "primary")]
    public object Primary { get; set; }

    [DataMember(Name = "begin-date")]
    public string BeginDate { get; set; }

    [DataMember(Name = "end-date")]
    public string EndDate { get; set; }
  }

  [DataContract]
  public class Artist
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "sort-name")]
    public string SortName { get; set; }

    [DataMember(Name = "disambiguation")]
    public string Disambiguation { get; set; }

    [DataMember(Name = "aliases")]
    public IList<Alias> Aliases { get; set; }
  }

  [DataContract]
  public class ArtistCredit
  {

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "artist")]
    public Artist Artist { get; set; }
  }

  [DataContract]
  public class ReleaseGroup
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "type-id")]
    public string TypeId { get; set; }

    [DataMember(Name = "primary-type-id")]
    public string PrimaryTypeId { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "primary-type")]
    public string PrimaryType { get; set; }
  }

  [DataContract]
  public class Area
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "sort-name")]
    public string SortName { get; set; }

    [DataMember(Name = "iso-3166-1-codes")]
    public IList<string> Iso31661Codes { get; set; }
  }

  [DataContract]
  public class ReleaseEvent
  {

    [DataMember(Name = "date")]
    public string Date { get; set; }

    [DataMember(Name = "area")]
    public Area Area { get; set; }
  }

  [DataContract]
  public class Track
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "number")]
    public string Number { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "length")]
    public int? Length { get; set; }
  }

  [DataContract]
  public class Medium
  {

    [DataMember(Name = "position")]
    public int Position { get; set; }

    [DataMember(Name = "format")]
    public string Format { get; set; }

    [DataMember(Name = "track")]
    public IList<Track> Track { get; set; }

    [DataMember(Name = "track-count")]
    public int TrackCount { get; set; }

    [DataMember(Name = "track-offset")]
    public int TrackOffset { get; set; }
  }

  [DataContract]
  public class Release
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "status-id")]
    public string StatusId { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "disambiguation")]
    public string Disambiguation { get; set; }

    [DataMember(Name = "artist-credit")]
    public IList<ArtistCredit> ArtistCredit { get; set; }

    [DataMember(Name = "release-group")]
    public ReleaseGroup ReleaseGroup { get; set; }

    [DataMember(Name = "date")]
    public string Date { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }

    [DataMember(Name = "release-events")]
    public IList<ReleaseEvent> ReleaseEvents { get; set; }

    [DataMember(Name = "track-count")]
    public int TrackCount { get; set; }

    [DataMember(Name = "media")]
    public IList<Medium> Media { get; set; }
  }

  [DataContract]
  public class Recording
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "score")]
    public int Score { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "length")]
    public int Length { get; set; }

    [DataMember(Name = "video")]
    public object Video { get; set; }

    [DataMember(Name = "artist-credit")]
    public IList<ArtistCredit> ArtistCredit { get; set; }

    [DataMember(Name = "first-release-date")]
    public string FirstReleaseDate { get; set; }

    [DataMember(Name = "releases")]
    public IList<Release> Releases { get; set; }

    [DataMember(Name = "isrcs")]
    public IList<string> Isrcs { get; set; }
  }

  [DataContract]
  public class MbRecording
  {

    [DataMember(Name = "created")]
    public DateTime Created { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }

    [DataMember(Name = "offset")]
    public int Offset { get; set; }

    [DataMember(Name = "recordings")]
    public IList<Recording> Recordings { get; set; }
  }
}
