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
      Rectangle rect = new Rectangle(0, 0, 0, 0);
      foreach (UIElement child in Children)
      {
        child.Measure(availableSize);
        rect = Rectangle.Union(rect, new Rectangle(new Point((int)child.Position.X, (int)child.Position.Y), new Size((int)child.DesiredSize.Width, (int)child.DesiredSize.Height)));
      }
      _desiredSize = rect.Size;
    }

    public override void Arrange(Rectangle finalRect)
    {
      this.ActualPosition = new Microsoft.DirectX.Vector3(finalRect.X, finalRect.Y, 1.0f);
      this.ActualWidth = finalRect.Width;
      this.ActualHeight = finalRect.Height;
      foreach (UIElement child in Children)
      {
        child.Arrange(new Rectangle(new Point((int)(child.Position.X + this.ActualPosition.X),
                                               (int)(child.Position.Y + this.ActualPosition.Y)),
                                               child.DesiredSize));
      }
      base.PerformLayout();
    }
  }
}
