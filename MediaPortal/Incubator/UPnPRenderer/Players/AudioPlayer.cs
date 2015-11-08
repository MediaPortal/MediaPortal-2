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
      string sourceFilterName = PlayerHelpers.GetSourceFilterName(_resourceAccessor.ResourcePathName);
      if (string.IsNullOrEmpty(sourceFilterName))
      {
        base.AddSourceFilter();
      }
      else
      {
        PlayerHelpers.AddSourceFilter(sourceFilterName, _resourceAccessor, _graphBuilder);
      }
    }

    public override string Name
    {
      get { return "UPnPRenderer Audio Player"; }
    }
  }
}
