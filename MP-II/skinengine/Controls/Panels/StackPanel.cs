using System;
using System.Collections.Generic;
using System.Text;
using SkinEngine.Controls.Visuals;
using System.Drawing;
namespace SkinEngine.Controls.Panels
{
  public class StackPanel : Panel
  {

    public override void Measure(System.Drawing.Size availableSize)
    {
      Size childSize = availableSize;
      foreach (UIElement child in Children)
      {
        child.Measure(childSize);
        childSize.Height -= child.DesiredSize.Height;
      }
      _desiredSize = availableSize;
    }

    public override void Arrange(Rectangle finalRect)
    {
      float totalHeight = 0;
      foreach (UIElement child in Children)
      {
        child.Arrange(new Rectangle((int)(this.ActualPosition.X), (int)(this.ActualPosition.Y+totalHeight), child.DesiredSize.Width, child.DesiredSize.Height));
        totalHeight += child.DesiredSize.Height;
      }
    }
  }
}
