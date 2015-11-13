using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UPnPRenderer.Players
{
  public class UPnPRendererAudioPlayer : BaseDXPlayer, IAudioPlayer
  {
    public const string MIMETYPE = "upnpaudio/upnprenderer";
    public const string DUMMY_FILE = "UPnPRenderer://localhost/UPnPRendererAudio.upnp";

    protected override void AddSourceFilter()
    {
      PlayerHelpers.AddSourceFilterOverride(base.AddSourceFilter, _resourceAccessor, _graphBuilder);
    }

    public override string Name
    {
      get { return "UPnPRenderer Audio Player"; }
    }
  }
}
