using System;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  //  "backdrop_path": "/AkE7LQs2hPMG5tpWYcum847Knre.jpg",
  //  "id": 1891,
  //  "original_title": "Star Wars: Episode V - The Empire Strikes Back",
  //  "popularity": 8412.049,
  //  "poster_path": "/6u1fYtxG5eqjhtCPDx04pJphQRW.jpg",
  //  "release_date": "1980-05-21",
  //  "title": "Star Wars: Episode V - The Empire Strikes Back"
  [DataContract]
  public class MovieSearchResult
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "original_title")]
    public string OriginalTitle { get; set; }

    [DataMember(Name = "poster_path")]
    public string PosterPath { get; set; }

    [DataMember(Name = "backdrop_path")]
    public string BackdropPath { get; set; }

    [DataMember(Name = "release_date")]
    public DateTime ReleaseDate { get; set; }
  }
}