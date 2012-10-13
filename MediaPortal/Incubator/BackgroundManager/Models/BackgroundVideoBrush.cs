using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Brushes;
using MediaPortal.UI.SkinEngine.Players;

namespace MediaPortal.UiComponents.BackgroundManager.Models
{
  public class BackgroundVideoBrush: VideoBrush
  {
    protected AbstractProperty _videoPlayerProperty;

    public AbstractProperty VideoPlayerProperty
    {
      get { return _videoPlayerProperty; }
    }

    public ISlimDXVideoPlayer VideoPlayer
    {
      get { return (ISlimDXVideoPlayer) _videoPlayerProperty.GetValue(); }
      set { _videoPlayerProperty.SetValue(value); }
    }

    public BackgroundVideoBrush ()
    {
      _videoPlayerProperty = new SProperty(typeof(ISlimDXVideoPlayer), null);
    }

    public override void DeepCopy(Utilities.DeepCopy.IDeepCopyable source, Utilities.DeepCopy.ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      BackgroundVideoBrush b = (BackgroundVideoBrush) source;
      VideoPlayer = b.VideoPlayer;
    }

    protected override bool GetPlayer(out ISlimDXVideoPlayer player)
    {
      player = VideoPlayer;
      return player != null;
    }
  }
}
