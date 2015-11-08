using MediaPortal.UI.Players.Video;

namespace MediaPortal.UPnPRenderer.Players
{
  public class UPnPRendererVideoPlayer : VideoPlayer
  {
    public const string MIMETYPE = "upnpvideo/upnprenderer";

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
      get { return "UPnPRenderer Video Player"; }
    }
  }
}
