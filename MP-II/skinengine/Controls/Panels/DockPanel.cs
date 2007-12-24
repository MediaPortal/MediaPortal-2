using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SkinEngine.Controls.Visuals;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Panels
{
  public class DockPanel : Panel
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

      float offsetTop = 0.0f;
      float offsetBottom = 0.0f;
      foreach (UIElement child in Children)
      {
        if (child.Dock == Dock.Top)
        {
          Point location = new Point((int)(this.ActualPosition.X), (int)(this.ActualPosition.Y + offsetTop));
          child.Arrange(new Rectangle(location, new Size((int)this.ActualWidth, (int)child.DesiredSize.Height)));
          offsetTop += child.DesiredSize.Height;
        }
        else if (child.Dock == Dock.Bottom)
        {
          Point location = new Point((int)(this.ActualPosition.X), (int)(this.ActualPosition.Y + ActualHeight - offsetBottom - child.DesiredSize.Height));
          child.Arrange(new Rectangle(location, new Size((int)this.ActualWidth, (int)child.DesiredSize.Height)));
          offsetBottom += child.DesiredSize.Height;
        }
      }
      float height = (float)(ActualHeight - (offsetBottom + offsetTop));
      float offsetLeft = 0.0f;
      float offsetRight = 0.0f;
      foreach (UIElement child in Children)
      {
        if (child.Dock == Dock.Left)
        {
          Point location = new Point((int)(this.ActualPosition.X + offsetLeft), (int)(this.ActualPosition.Y + offsetTop));
          child.Arrange(new Rectangle(location, new Size((int)child.DesiredSize.Width, (int)height)));
          offsetLeft += child.DesiredSize.Width;
        }
        else if (child.Dock == Dock.Right)
        {
          Point location = new Point((int)(this.ActualPosition.X + ActualWidth - offsetRight - child.DesiredSize.Width), (int)(this.ActualPosition.Y + offsetTop));
          child.Arrange(new Rectangle(location, new Size((int)child.DesiredSize.Width, (int)height)));
          offsetRight += child.DesiredSize.Width;
        }
      }

      foreach (UIElement child in Children)
      {
        if (child.Dock == Dock.Center)
        {
          float width = (float)(ActualWidth - (offsetLeft + offsetRight));
          Point location = new Point((int)(this.ActualPosition.X + offsetLeft), (int)(this.ActualPosition.Y + offsetTop));
          child.Arrange(new Rectangle(location, new Size((int)width, (int)height)));
          offsetLeft += child.DesiredSize.Width;
        }
      }
      base.PerformLayout();
      base.Arrange(finalRect);
    }
  }
}
