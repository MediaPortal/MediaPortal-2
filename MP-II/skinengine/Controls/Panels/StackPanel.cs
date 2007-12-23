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
      float totalHeight = 0.0f;
      Size childSize = availableSize;
      foreach (UIElement child in Children)
      {
        child.Measure(childSize);
        childSize.Height -= child.DesiredSize.Height;
        totalHeight += child.DesiredSize.Height;
      }
      _desiredSize = new Size((int)availableSize.Width, (int)totalHeight);
    }

    public override void Arrange(Rectangle finalRect)
    {
      float totalHeight = 0;
      foreach (UIElement child in Children)
      {
        Point location = new Point((int)(this.ActualPosition.X), (int)(this.ActualPosition.Y + totalHeight));
        Size size = new Size(child.DesiredSize.Width, child.DesiredSize.Height);

        //align horizontally 
        if (AlignmentX == AlignmentX.Center)
        {
          location.X += (int)((finalRect.Width - child.DesiredSize.Width) / 2);
        }
        else if (AlignmentX == AlignmentX.Right)
        {
          location.X = finalRect.Right - child.DesiredSize.Width;
        }

        if (AlignmentY == AlignmentY.Center)
        {
          location.Y += (int)((finalRect.Height - DesiredSize.Height) / 2);
        }
        else if (AlignmentY == AlignmentY.Bottom)
        {
          location.Y += (int)(finalRect.Height - DesiredSize.Height);
        }

        child.Arrange(new Rectangle(location, size));
        totalHeight += child.DesiredSize.Height;
      }
    }
  }
}
