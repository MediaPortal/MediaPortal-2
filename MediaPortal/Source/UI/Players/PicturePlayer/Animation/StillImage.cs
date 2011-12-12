using System.Drawing;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.Players.Picture.Animation
{
  /// <summary>
  /// StillImage animation class provides static images only.
  /// </summary>
  public class StillImage: IPictureAnimator
  {
    #region IPictureAnimator Member

    public RectangleF Animate(Texture currentImage, SizeF maxUV)
    {
      // Simply return the max available rect.
      return new RectangleF(PointF.Empty, maxUV);
    }

    public void Reset()
    { }

    #endregion
  }
}
