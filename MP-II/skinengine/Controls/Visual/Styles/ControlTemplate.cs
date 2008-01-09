using System;
using System.Collections.Generic;
using System.Text;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Panels;
namespace SkinEngine.Controls.Visuals.Styles
{
  public class ControlTemplate : Canvas
  {
    public ControlTemplate()
    {
    }
    public ControlTemplate(ControlTemplate t)
      : base(t)
    {
    }

    public override object Clone()
    {
      return new ControlTemplate(this);
    }

    /// <summary>
    /// Gets or sets the type of the target (not used here, but required for real xaml)
    /// </summary>
    /// <value>The type of the target.</value>
    public string TargetType
    {
      get
      {
        return "";
      }
      set
      {
      }
    }

    public override void Measure(System.Drawing.Size availableSize)
    {
      _desiredSize = new System.Drawing.Size((int)Width, (int)Height);
      if (Width <= 0)
        _desiredSize.Width = (int)availableSize.Width - (int)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = (int)availableSize.Height - (int)(Margin.Y + Margin.Z);

      System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, 0, 0);
      if (Children.Count > 0)
      {
        UIElement child = Children[0];
        if (child.IsVisible)
        {
          child.Measure(_desiredSize);
          rect = System.Drawing.Rectangle.Union(rect, new System.Drawing.Rectangle(new System.Drawing.Point((int)child.Position.X, (int)child.Position.Y), new System.Drawing.Size((int)child.DesiredSize.Width, (int)child.DesiredSize.Height)));
        }
      }
      if (Width > 0) rect.Width = (int)Width;
      if (Height > 0) rect.Height = (int)Height;
      _desiredSize = rect.Size;

      _desiredSize.Width += (int)(Margin.X + Margin.W);
      _desiredSize.Height += (int)(Margin.Y + Margin.Z);
      _originalSize = _desiredSize;

      _availableSize = new System.Drawing.Size(availableSize.Width, availableSize.Height);
    }
    public override void Arrange(System.Drawing.Rectangle finalRect)
    {
      _finalRect = new System.Drawing.Rectangle(finalRect.Location, finalRect.Size);
      System.Drawing.Rectangle layoutRect = new System.Drawing.Rectangle(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (int)(Margin.X);
      layoutRect.Y += (int)(Margin.Y);
      layoutRect.Width -= (int)(Margin.X + Margin.W);
      layoutRect.Height -= (int)(Margin.Y + Margin.Z);
      ActualPosition = new Microsoft.DirectX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      if (Children.Count > 0)
      {
        UIElement child = Children[0];
        if (child.IsVisible)
        {
          System.Drawing.Point p = new System.Drawing.Point((int)(child.Position.X + this.ActualPosition.X), (int)(child.Position.Y + this.ActualPosition.Y));
          double widthPerCell = ActualWidth - (child.Position.X - this.ActualPosition.X);
          double heightPerCell = ActualHeight - (child.Position.Y - this.ActualPosition.Y);

          child.Arrange(new System.Drawing.Rectangle(p, child.DesiredSize));
        }
      }
      base.PerformLayout();
      if (!IsArrangeValid)
      {
        IsArrangeValid = true;
        InitializeBindings();
        InitializeTriggers();
      }
    }
  }
}
