using MediaPortal.UI.SkinEngine.Controls.Visuals;
using SharpDX;

namespace MediaPortal.UiComponents.WMCSkin.Controls
{
  public class SubItemsContentPresenter : AnimatedScrollContentPresenter
  {
    public override void BringIntoView(UIElement element, RectangleF elementBounds)
    {
      //disable auto centering if using mouse, prevents items from scrolling
      if (AutoCentering != ScrollAutoCenteringEnum.None && IsMouseOverElement(element))
        return;
      base.BringIntoView(element, elementBounds);
    }

    protected bool IsMouseOverElement(UIElement element)
    {
      FrameworkElement frameworkElement = element as FrameworkElement;
      return frameworkElement != null && frameworkElement.IsMouseOver;
    }
  }
}
