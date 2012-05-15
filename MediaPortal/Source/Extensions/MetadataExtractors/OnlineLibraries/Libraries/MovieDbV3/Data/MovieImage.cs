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
  public class MovieImage
  {
    // Not filled by API!
    public int MovieId { get; set; }

    [DataMember(Name = "aspect_ratio")]
    public float AspectRatio { get; set; }

    [DataMember(Name = "file_path")]
    public string FilePath { get; set; }

    [DataMember(Name = "height")]
    public int Height { get; set; }

    [DataMember(Name = "width")]
    public int Width { get; set; }

    [DataMember(Name = "iso_639_1")]
    public string Language { get; set; }
    
    public override string ToString()
    {
      return FilePath;
    }
  }
}
