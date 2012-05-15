using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  //{
  //  "aspect_ratio": 1.78,
  //  "file_path": "/mOTtuakUTb1qY6jG6lzMfjdhLwc.jpg",
  //  "height": 1080,
  //  "iso_639_1": null,
  //  "width": 1920
  //}
  [DataContract]
  public class ImageCollection
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "backdrops")]
    public List<MovieImage> Backdrops { get; set; }

    [DataMember(Name = "covers")]
    public List<MovieImage> Covers { get; set; }

    [DataMember(Name = "posters")]
    public List<MovieImage> Posters { get; set; }

    public void SetMovieIds()
    {
      if (Covers != null) Covers.ForEach(c => c.MovieId = Id);
      if (Backdrops != null) Backdrops.ForEach(c => c.MovieId = Id);
      if (Posters != null) Posters.ForEach(c => c.MovieId = Id);
    }
  }
}
