using MediaPortal.UI.SkinEngine.Controls.Visuals;
using SharpDX;

namespace MediaPortal.UiComponents.WMCSkin.Controls
{
  public class SubItemsContentPresenter : ScrollContentPresenter
  {
    public override void BringIntoView(UIElement element, RectangleF elementBounds)
    {
      if (AutoCentering != ScrollAutoCenteringEnum.None)
      {
        //disable auto centering if using mouse, prevents items from scrolling
        FrameworkElement frameworkElement = element as FrameworkElement;
        if (frameworkElement != null && frameworkElement.IsMouseOver)
          return;
      }
      base.BringIntoView(element, elementBounds);
    }
  }
}
