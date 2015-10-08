using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.MAS.Music
{
  public class WebMusicArtistBasic : WebObject, ITitleSortable, IArtwork
  {
    public WebMusicArtistBasic()
    {
      Artwork = new List<WebArtwork>();
      HasAlbums = true; //default
    }

    public string Id { get; set; }
    public string Title { get; set; }
    public bool HasAlbums { get; set; }
    public IList<WebArtwork> Artwork { get; set; }

    public override string ToString()
    {
      return Title;
    }
  }
}