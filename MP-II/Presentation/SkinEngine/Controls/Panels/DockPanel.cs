#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Drawing;
using System.Diagnostics;
using MediaPortal.Presentation.DataObjects;
using Presentation.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.SkinManagement;

namespace Presentation.SkinEngine.Controls.Panels
{
  public class DockPanel : Panel
  {
    protected const string DOCK_ATTACHED_PROPERTY = "DockPanel.Dock";

    protected Property _lastChildFillProperty;

    #region Ctor

    public DockPanel()
    {
      Init();
    }

    protected void Init()
    {
      _lastChildFillProperty = new Property(typeof(bool), false);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      DockPanel p = source as DockPanel;
      LastChildFill = copyManager.GetCopy(p.LastChildFill);
    }

    #endregion

    public Property LastChildFillProperty
    {
      get { return _lastChildFillProperty; }
    }
    
    public bool LastChildFill
    {
      get { return (bool) _lastChildFillProperty.GetValue(); }
      set { _lastChildFillProperty.SetValue(value); }
    }

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(SizeF availableSize)
    {
      float marginWidth = (Margin.Left + Margin.Right) * SkinContext.Zoom.Width;
      float marginHeight = (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height;
      _desiredSize = new SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);
      if (Width == 0)
        _desiredSize.Width = availableSize.Width - marginWidth;
      if (Height == 0)
        _desiredSize.Height = availableSize.Height - marginHeight;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      SizeF size = new SizeF(_desiredSize.Width, _desiredSize.Height);
      SizeF sizeTop = new SizeF();
      SizeF sizeLeft = new SizeF();
      SizeF sizeCenter = new SizeF();
      int count = 0;
      foreach (UIElement child in Children)
      {
        count++;
        if (!child.IsVisible)
          continue;
        if (size.Width < 0)
          size.Width = 0;
        if (size.Height < 0)
          size.Height = 0;
        
        if (GetDock(child) == Dock.Top || GetDock(child) == Dock.Bottom)
        {
          if (count == Children.Count && LastChildFill)
            child.Measure(size);
          else
            child.Measure(new SizeF(size.Width, 0));

          size.Height -= child.DesiredSize.Height;
          sizeTop.Height += child.DesiredSize.Height;
          if (child.DesiredSize.Width > sizeTop.Width)
            sizeTop.Width = child.DesiredSize.Width;
        }
        else if (GetDock(child) == Dock.Left || GetDock(child) == Dock.Right)
        {
          if (count == Children.Count && LastChildFill)
            child.Measure(size);
          else
            child.Measure(new SizeF(0, size.Height));

          size.Width -= child.DesiredSize.Width;
          sizeLeft.Width += child.DesiredSize.Width;
          if (child.DesiredSize.Height > sizeLeft.Height)
            sizeLeft.Height = child.DesiredSize.Height;
        }
        else if (GetDock(child) == Dock.Center)
        {
          child.Measure(size);

          size.Width -= child.DesiredSize.Width;
          size.Height -= child.DesiredSize.Height;
          
          if (child.DesiredSize.Width > sizeCenter.Width)
            sizeCenter.Width = child.DesiredSize.Width;
          if (child.DesiredSize.Height > sizeCenter.Height)
            sizeCenter.Height = child.DesiredSize.Height;
        }
      }

      if (availableSize.Width == 0)
      {
        _desiredSize.Width = sizeLeft.Width;
        float w = Math.Max(sizeTop.Width, sizeCenter.Width);
        if (w > sizeLeft.Width)
          _desiredSize.Width = w;
      }
      if (availableSize.Height == 0)
      {
        _desiredSize.Height = sizeTop.Height + Math.Max(sizeLeft.Height, sizeCenter.Height);
      }

      if (Width > 0) _desiredSize.Width = (float)Width * SkinContext.Zoom.Width;
      if (Height > 0) _desiredSize.Height = (float)Height * SkinContext.Zoom.Height;
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      _desiredSize.Width += marginWidth;
      _desiredSize.Height += marginHeight;

      base.Measure(availableSize);
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("DockPanel:arrange {0} {1},{2} {3}x{4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));

      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += Margin.Left * SkinContext.Zoom.Width;
      layoutRect.Y += Margin.Top * SkinContext.Zoom.Height;
      layoutRect.Width -= (Margin.Left + Margin.Right) * SkinContext.Zoom.Width;
      layoutRect.Height -= (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height;
      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      float offsetTop = 0.0f;
      float offsetLeft = 0.0f;
      float offsetRight = 0.0f;
      float offsetBottom = 0.0f;
      SizeF size = new SizeF(layoutRect.Width, layoutRect.Height);
      int count = 0;
      foreach (FrameworkElement child in Children)
      {
        count++;
        //Trace.WriteLine(String.Format("DockPanel:arrange {0} {1}", count, child.Name));

        if (!child.IsVisible)
          continue;
        if (GetDock(child) == Dock.Top)
        {

          PointF location = new PointF(offsetLeft, offsetTop);
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += ActualPosition.X;
          location.Y += ActualPosition.Y;

          PointF childOffset = ArrangeChild(child, size);
          location.X += childOffset.X;
          if (count == Children.Count && LastChildFill)
            child.Arrange(new RectangleF(location, new SizeF(child.DesiredSize.Width, size.Height)));
          else
            child.Arrange(new RectangleF(location, child.DesiredSize));

          offsetTop += child.DesiredSize.Height;
          size.Height -= child.DesiredSize.Height;
        }
        else if (GetDock(child) == Dock.Bottom)
        {
          PointF location = new PointF(offsetLeft, layoutRect.Height - (offsetBottom + child.DesiredSize.Height));
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += ActualPosition.X;
          location.Y += ActualPosition.Y;

          PointF childOffset = ArrangeChild(child, size);
          location.X += childOffset.X;
          if (count == Children.Count && LastChildFill)
            child.Arrange(new RectangleF(location, new SizeF(child.DesiredSize.Width, size.Height)));
          else
            child.Arrange(new RectangleF(location, child.DesiredSize));

          offsetBottom += child.DesiredSize.Height;
          size.Height -= child.DesiredSize.Height;
        }
        else if (GetDock(child) == Dock.Left)
        {
          PointF location = new PointF(offsetLeft, offsetTop);
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += ActualPosition.X;
          location.Y += ActualPosition.Y;

          PointF childOffset = ArrangeChild(child, size);
          location.Y += childOffset.Y;
          if (count == Children.Count && LastChildFill)
            child.Arrange(new RectangleF(location, new SizeF(size.Width, child.DesiredSize.Height)));
          else
            child.Arrange(new RectangleF(location, child.DesiredSize));

          offsetLeft += child.DesiredSize.Width;
          size.Width -= child.DesiredSize.Width;
        }
        else if (GetDock(child) == Dock.Right)
        {
          PointF location = new PointF(layoutRect.Width - (offsetRight + child.DesiredSize.Width), offsetTop);
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += ActualPosition.X;
          location.Y += ActualPosition.Y;

          PointF childOffset = ArrangeChild(child, size);
          location.Y += childOffset.Y;
          if (count == Children.Count && LastChildFill)
            child.Arrange(new RectangleF(location, new SizeF(size.Width, child.DesiredSize.Height)));
          else
            child.Arrange(new RectangleF(location, child.DesiredSize));

          offsetRight += child.DesiredSize.Width;
          size.Width -= child.DesiredSize.Width;
        }
        else
        {
          PointF location = new PointF(offsetLeft, offsetTop);
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += ActualPosition.X;
          location.Y += ActualPosition.Y;
          PointF childOffset = ArrangeChild(child, size);
          location.X += childOffset.X;
          location.Y += childOffset.Y;
          child.Arrange(new RectangleF(location, child.DesiredSize));

          // Do not remove child size from a border offset or from size - the child will
          // stay in the "empty space" without taking place from the border layouting variables
        }
      }

      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) continue;
        if (GetDock(child) == Dock.Center)
        {
          float width = (float)(ActualWidth - (offsetLeft + offsetRight));

          PointF location = new PointF(offsetLeft, offsetTop);
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += ActualPosition.X;
          location.Y += ActualPosition.Y;
          //ArrangeChild(child, ref location);
          child.Arrange(new RectangleF(location, child.DesiredSize));
          offsetLeft += child.DesiredSize.Width;
        }
      }
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
        if (Screen != null) Screen.Invalidate(this);
      }
      base.Arrange(layoutRect);
    }

    protected virtual PointF ArrangeChild(FrameworkElement child, SizeF s)
    {
      PointF result = new PointF(0, 0);
      if (VisualParent == null)
        return result;

      if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center)
      {
        if (s.Width > 0)
          result.X += (s.Width - child.DesiredSize.Width) / 2;
      }
      else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
      {
        if (s.Width > 0)
          result.X += s.Width - child.DesiredSize.Width;
      }
      if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
      {
        if (s.Height > 0)
          result.Y += (s.Height - child.DesiredSize.Height) / 2;
      }
      else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
      {
        if (s.Height > 0)
          result.Y += s.Height - child.DesiredSize.Height;
      }
      return result;
    }

    #region Attached properties

    /// <summary>
    /// Getter method for the attached property <c>Dock</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>Dock</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static Dock GetDock(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<Dock>(DOCK_ATTACHED_PROPERTY, Dock.Left);
    }

    /// <summary>
    /// Setter method for the attached property <c>Dock</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>Dock</c> property on the
    /// <paramref name="targetObject"/> to be set.</returns>
    public static void SetDock(DependencyObject targetObject, Dock value)
    {
      targetObject.SetAttachedPropertyValue<Dock>(DOCK_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>Dock</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>Dock</c> property.</returns>
    public static Property GetDockAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<Dock>(DOCK_ATTACHED_PROPERTY, Dock.Left);
    }

    #endregion
  }
}
