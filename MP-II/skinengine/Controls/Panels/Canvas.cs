using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SkinEngine.Controls.Visuals;

namespace SkinEngine.Controls.Panels
{
  public class Canvas : Panel
  {
    public override void Measure(Size availableSize)
    {
      foreach (UIElement child in Children)
      {
        child.Measure(availableSize);
      }
      _desiredSize = availableSize;
    }

    public override void Arrange(Rectangle finalRect)
    {
      this.ActualPosition = new Microsoft.DirectX.Vector3(finalRect.X, finalRect.Y, 1.0f);
      this.ActualWidth = finalRect.Width;
      this.ActualHeight = finalRect.Height;
      foreach (UIElement child in Children)
      {
        child.Arrange(new Rectangle(new Point((int)(child.ActualPosition.X + this.ActualPosition.X),
                                               (int)(child.ActualPosition.Y + this.ActualPosition.Y)),
                                               child.DesiredSize));
      }
      base.PerformLayout();
    }
  }
}
