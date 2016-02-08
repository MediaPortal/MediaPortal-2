using System.Runtime.Serialization;
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.MAS.General
{
  [KnownType(typeof(WebArtworkDetailed))]
  public class WebArtwork
  {
    public WebFileType Type { get; set; }
    public string Id { get; set; }
    public int Rating { get; set; }
    public string Filetype { get; set; }
    public int Offset { get; set; }
  }

  public class WebArtworkDetailed : WebArtwork
  {
    public string Path { get; set; }
  }
}