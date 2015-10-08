using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.Music;

namespace MediaPortal.Plugins.MP2Extended.MAS.Playlist
{
  public class WebPlaylistItem : WebMediaItem
  {
    public WebPlaylistItem()
    {
    }

    public WebPlaylistItem(WebMusicTrackBasic track)
    {
      this.Id = track.Id;
      this.PID = track.PID;
      this.Title = track.Title;
      this.Type = track.Type;
      this.Duration = track.Duration;
      this.DateAdded = track.DateAdded;
      this.Path = track.Path;
    }

    public int Duration { get; set; }
  }
}