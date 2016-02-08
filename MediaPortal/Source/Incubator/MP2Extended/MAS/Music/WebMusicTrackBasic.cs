using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.MAS.Music
{
  public class WebMusicTrackBasic : WebMediaItem, IYearSortable, IGenreSortable, IRatingSortable, IMusicTrackNumberSortable
  {
    public WebMusicTrackBasic()
    {
      ArtistId = new List<string>();
      Genres = new List<string>();
    }

    public string AlbumArtist { get; set; }
    public string AlbumArtistId { get; set; }
    public IList<string> Artist { get; set; }
    public IList<string> ArtistId { get; set; }
    public string Album { get; set; }
    public string AlbumId { get; set; }
    public int DiscNumber { get; set; }
    public int TrackNumber { get; set; }
    public int Year { get; set; }
    public int Duration { get; set; }
    public float Rating { get; set; }
    public IList<string> Genres { get; set; }

    public override WebMediaType Type
    {
      get { return WebMediaType.MusicTrack; }
    }

    public override string ToString()
    {
      return Title;
    }
  }
}