using System;
using System.Collections.Generic;
using System.Text;
using SkinEngine.Controls.Visuals;
using System.Drawing;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals;

namespace SkinEngine.Controls.Panels
{
  public class StackPanel : Panel
  {
    Property _orientationProperty;
    public StackPanel()
    {
      _orientationProperty = new Property(Orientation.Vertical);
    }

    public Property OrientationProperty
    {
      get
      {
        return _orientationProperty;
      }
      set
      {
        _orientationProperty = value;
      }
    }

    public Orientation Orientation
    {
      get
      {
        return (Orientation)_orientationProperty.GetValue();
      }
      set
      {
        _orientationProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public override void Measure(System.Drawing.Size availableSize)
    {
      float totalHeight = 0.0f;
      float totalWidth = 0.0f;
      Size childSize = availableSize;
      foreach (UIElement child in Children)
      {
        child.Measure(childSize);
        if (Orientation == Orientation.Vertical)
        {
          childSize.Height -= child.DesiredSize.Height;
          totalHeight += child.DesiredSize.Height;
          if (child.DesiredSize.Width > totalWidth)
            totalWidth = child.DesiredSize.Width;
        }
        else
        {
          childSize.Width -= child.DesiredSize.Width;
          totalWidth += child.DesiredSize.Width;

          if (child.DesiredSize.Height > totalHeight)
            totalHeight = child.DesiredSize.Height;
        }

      }
      _desiredSize = new Size((int)totalWidth, (int)totalHeight);
    }

    public override void Arrange(Rectangle finalRect)
    {
      this.ActualPosition = new Microsoft.DirectX.Vector3(finalRect.X, finalRect.Y, 1.0f);
      this.ActualWidth = finalRect.Width;
      this.ActualHeight = finalRect.Height;
      switch (Orientation)
      {
        case Orientation.Vertical:
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

              //align vertically 
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
          break;

        case Orientation.Horizontal:
          {
            float totalWidth = 0;
            foreach (UIElement child in Children)
            {
              Point location = new Point((int)(this.ActualPosition.X + totalWidth), (int)(this.ActualPosition.Y));
              Size size = new Size(child.DesiredSize.Width, child.DesiredSize.Height);

              //align horizontally 
              if (DesiredSize.Width < finalRect.Width)
              {
                if (AlignmentX == AlignmentX.Center)
                {
                  location.X += (int)((finalRect.Width - DesiredSize.Width) / 2);
                }
                else if (AlignmentX == AlignmentX.Right)
                {
                  location.X += finalRect.Right - DesiredSize.Width;
                }
              }
              //align vertically 
              if (AlignmentY == AlignmentY.Center)
              {
                location.Y += (int)((finalRect.Height - child.DesiredSize.Height) / 2);
              }
              else if (AlignmentY == AlignmentY.Bottom)
              {
                location.Y += (int)(finalRect.Height - child.DesiredSize.Height);
              }

              child.Arrange(new Rectangle(location, size));
              totalWidth += child.DesiredSize.Width;
            }
          }
          break;
      }
    }
  }
}
