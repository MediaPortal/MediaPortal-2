using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.MAS.Music
{
  public class WebMusicTrackDetailed : WebMusicTrackBasic
  {
    public WebMusicTrackDetailed()
    {
      Artists = new List<WebMusicArtistBasic>();
      AlbumArtistObject = new WebMusicArtistBasic();
    }

    public IList<WebMusicArtistBasic> Artists { get; set; }
    public WebMusicArtistBasic AlbumArtistObject { get; set; }
  }
}