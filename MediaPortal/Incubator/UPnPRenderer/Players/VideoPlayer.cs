using MediaPortal.UI.Players.Video;

namespace MediaPortal.UPnPRenderer.Players
{
  public class UPnPRendererVideoPlayer : VideoPlayer
  {
    public const string MIMETYPE = "upnpvideo/upnprenderer";

    protected override void AddSourceFilter()
    {
      PlayerHelpers.AddSourceFilterOverride(base.AddSourceFilter, _resourceAccessor, _graphBuilder);
    }

    public override string Name
    {
      get { return "UPnPRenderer Video Player"; }
    }
  }
}
