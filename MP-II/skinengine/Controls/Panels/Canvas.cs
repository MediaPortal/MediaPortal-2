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
      foreach (UIElement child in Children)
      {
        child.Arrange(new Rectangle( new Point((int)child.ActualPosition.X,(int)child.ActualPosition.Y), child.DesiredSize));
      }
      base.PerformLayout();
    }
  }
}
