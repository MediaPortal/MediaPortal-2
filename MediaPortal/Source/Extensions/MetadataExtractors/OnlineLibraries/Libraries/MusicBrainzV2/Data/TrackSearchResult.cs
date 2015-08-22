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

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data
{
  [DataContract]
  public class ArtistSection
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "sort-name")]
    public string SortName { get; set; }

    public override string ToString()
    {
      return string.Format("Id: {0}, Name: {1}, SortName: {2}", Id, Name, SortName);
    }
  }

  [DataContract]
  public class ArtistCreditSection
  {
    [DataMember(Name = "artist")]
    public ArtistSection Artist { get; set; }

    public override string ToString()
    {
      return string.Format("Artist: {0}", Artist);
    }
  }

  [DataContract]
  public class TrackSection
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "number")]
    public string Number { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "length")]
    public int Length { get; set; }

    public override string ToString()
    {
      return string.Format("Id: {0}, Number: {1}, Title: {2}, Length: {3}", Id, Number, Title, Length);
    }
  }

  [DataContract]
  public class MediaSection
  {
    [DataMember(Name = "track")]
    public IList<TrackSection> Tracks { get; set; }

    public override string ToString()
    {
      return string.Format("Tracks: [{0}]", string.Join(",", Tracks));
    }
  }

  [DataContract]
  public class Release
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "media")]
    public IList<MediaSection> Media { get; set; }

    [DataMember(Name = "artist-credit")]
    public IList<ArtistCreditSection> Artists { get; set; }

    public override string ToString()
    {
      return string.Format("Id: {0}, Title: {1}, Media: [{2}]", Id, Title, string.Join(",", Media));
    }
  }

  [DataContract]
  public class TrackSearchResult
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "artist_id")]
    public string ArtistId { get; set; }

    [DataMember(Name = "artist_name")]
    public string ArtistName { get; set; }

    [DataMember(Name = "album_id")]
    public string AlbumId { get; set; }

    [DataMember(Name = "album_name")]
    public string AlbumName { get; set; }

    [DataMember(Name = "genre")]
    public string Genre { get; set; }

    [DataMember(Name = "release_date")]
    public DateTime? ReleaseDate { get; set; }

    [DataMember(Name = "track_num")]
    public int TrackNum { get; set; }

    [DataMember(Name = "album_artist_id")]
    public string AlbumArtistId { get; set; }

    [DataMember(Name = "album_artist_name")]
    public string AlbumArtistName { get; set; }

    [DataMember(Name = "artist-credit")]
    public IList<ArtistCreditSection> Artists
    {
      get { return null; }
      set
      {
        ArtistId = value[0].Artist.Id;
        ArtistName = value[0].Artist.Name;
      }
    }

    [DataMember(Name = "releases")]
    public IList<Release> Releases
    {
      get { return null; }
      set
      {
        //Console.WriteLine("Release:{0}", value[0]);
        AlbumId = value[0].Id;
        AlbumName = value[0].Title;

        if(value[0].Artists != null)
        {
          Console.WriteLine("Artists:[{0}]", value[0].Artists);
          AlbumArtistId = value[0].Artists[0].Artist.Id;
          AlbumArtistName = value[0].Artists[0].Artist.Name;
        }
      }
    }

    public override string ToString()
    {
      return string.Format("Id: {0}, Title: {1}, ArtistId: {2}, ArtistName: {3}, AlbumId: {4}, AlbumName: {5}, Genre: {6}, ReleaseDate: {7}, TrackNum: {8}, AlbumArtistId: {9}, AlbumArtistName: {10}", Id, Title, ArtistId, ArtistName, AlbumId, AlbumName, Genre, ReleaseDate, TrackNum, AlbumArtistId, AlbumArtistName);
    }
  }
}