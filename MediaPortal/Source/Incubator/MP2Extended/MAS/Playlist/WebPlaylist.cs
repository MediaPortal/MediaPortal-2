using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.MAS.Playlist
{
  public class WebPlaylist : WebMediaItem
  {
    public int ItemCount { get; set; }

    public override WebMediaType Type
    {
      get { return WebMediaType.Playlist; }
    }
  }
}