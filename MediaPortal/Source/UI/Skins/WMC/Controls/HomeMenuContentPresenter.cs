using MediaPortal.UI.SkinEngine.Controls.Visuals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.WMCSkin.Models;

namespace MediaPortal.UiComponents.WMCSkin.Controls
{
  public class HomeMenuContentPresenter : AnimatedScrollContentPresenter
  {
    public override void BringIntoView(UIElement element, RectangleF elementBounds)
    {
      if (IsSelectedItem(element))
        base.BringIntoView(element, elementBounds);
    }

    protected bool IsSelectedItem(UIElement element)
    {
      var lvi = element.FindParentOfType<ListViewItem>();
      if (lvi != null)
      {
        var item = lvi.Context as ListItem;
        return item != null && item.Selected;
      }
      return false;
    }
  }
}
