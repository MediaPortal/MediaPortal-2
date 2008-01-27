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

    public override void Measure(System.Drawing.SizeF availableSize)
    {
      _desiredSize = new System.Drawing.SizeF((float)Width, (float)Height);
      if (Width <= 0)
        _desiredSize.Width = (float)availableSize.Width - (float)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = (float)availableSize.Height - (float)(Margin.Y + Margin.Z);

      System.Drawing.RectangleF rect = new System.Drawing.RectangleF(0, 0, 0, 0);
      if (Children.Count > 0)
      {
        UIElement child = Children[0];
        if (child.IsVisible)
        {
          child.Measure(_desiredSize);
          rect = System.Drawing.RectangleF.Union(rect, new System.Drawing.RectangleF(new System.Drawing.PointF((float)child.Position.X * SkinContext.Zoom.Width, (float)child.Position.Y * SkinContext.Zoom.Height), new System.Drawing.SizeF((float)child.DesiredSize.Width, (float)child.DesiredSize.Height)));
        }
      }
      if (Width > 0) rect.Width = (float)Width;
      if (Height > 0) rect.Height = (float)Height;
      _desiredSize = rect.Size;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _desiredSize.Width += (float)(Margin.X + Margin.W);
      _desiredSize.Height += (float)(Margin.Y + Margin.Z);
      _originalSize = _desiredSize;

      _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
    }
    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      System.Drawing.RectangleF layoutRect = new System.Drawing.RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X);
      layoutRect.Y += (float)(Margin.Y);
      layoutRect.Width -= (float)(Margin.X + Margin.W);
      layoutRect.Height -= (float)(Margin.Y + Margin.Z);
      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (Children.Count > 0)
      {
        UIElement child = Children[0];
        if (child.IsVisible)
        {
          System.Drawing.PointF p = new System.Drawing.PointF((float)(child.Position.X * SkinContext.Zoom.Width + this.ActualPosition.X), (float)(child.Position.Y * SkinContext.Zoom.Height + this.ActualPosition.Y));
          double widthPerCell = ActualWidth - (child.Position.X * SkinContext.Zoom.Width - this.ActualPosition.X);
          double heightPerCell = ActualHeight - (child.Position.Y * SkinContext.Zoom.Height - this.ActualPosition.Y);

          child.Arrange(new System.Drawing.RectangleF(p, child.DesiredSize));
        }
      }
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      IsArrangeValid = true;
      InitializeBindings();
      InitializeTriggers();
      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      }
      _isLayoutInvalid = false;
    }
    public override void Reset()
    {
      base.Reset();

      foreach (UIElement element in Children)
      {
        element.Reset();
      }
    }
  }
}
