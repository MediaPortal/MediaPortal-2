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
using System.Collections.Generic;
using System.Drawing;
using MediaPortal.SkinEngine.Controls.Visuals.Templates;
using SlimDX;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class ContentPresenter : FrameworkElement
  {
    #region Private fields

    private Property _contentProperty;
    private Property _contentTemplateProperty;

    #endregion

    #region Ctor

    public ContentPresenter()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _contentProperty = new Property(typeof(FrameworkElement), null);
      _contentTemplateProperty = new Property(typeof(DataTemplate), null);
    }

    void Attach()
    {
      _contentProperty.Attach(OnContentChanged);
    }

    void Detach()
    {
      _contentProperty.Detach(OnContentChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ContentPresenter p = (ContentPresenter) source;
      Content = copyManager.GetCopy(p.Content);
      ContentTemplate = copyManager.GetCopy(p.ContentTemplate);
      Attach();
    }

    #endregion

    void OnContentChanged(Property property)
    {
      if (Content == null)
        return;
      Content.VisualParent = this;
      Content.SetScreen(Screen);
    }

    public Property ContentProperty
    {
      get { return _contentProperty; }
    }

    public FrameworkElement Content
    {
      get { return (FrameworkElement) _contentProperty.GetValue(); }
      set { _contentProperty.SetValue(value); }
    }

    public Property ContentTemplateProperty
    {
      get { return _contentTemplateProperty; }
    }

    public DataTemplate ContentTemplate
    {
      get { return _contentTemplateProperty.GetValue() as DataTemplate; }
      set { _contentTemplateProperty.SetValue(value); }
    }

    public override void Measure(ref SizeF totalSize)
    {
      SizeF childSize = new SizeF(0, 0);

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      // Do we have a child
      if (Content != null)
      {
        // Measure the child
        Content.Measure(ref childSize);
      }

      _desiredSize = new SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);

      if (Double.IsNaN(Width))
        _desiredSize.Width = childSize.Width;

      if (Double.IsNaN(Height))
        _desiredSize.Height = childSize.Height;

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }

      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      totalSize = _desiredSize;
      AddMargin(ref totalSize);
      //Trace.WriteLine(String.Format("ContentPresenter.Measure :{0} returns {1}x{2}", this.Name, (int)totalSize.Width, (int)totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("ContentPresenter.Arrange :{0} X {1},Y {2} W {4}xH {5}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));
  
      ComputeInnerRectangle(ref finalRect);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      if (Content != null)
      {
        PointF position = new PointF(finalRect.X, finalRect.Y);
        SizeF availableSize = new SizeF(finalRect.Width, finalRect.Height);
        ArrangeChild(Content, ref position, ref availableSize);
        Content.Arrange(finalRect);
      }

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      Initialize();
      InitializeTriggers();
      IsInvalidLayout = false;
    }

    protected void ArrangeChild(FrameworkElement child, ref PointF p, double widthPerCell, double heightPerCell)
    {
      if (VisualParent == null) return;

      if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center)
      {
        p.X += (float)((widthPerCell - child.DesiredSize.Width) / 2);
      }
      else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
      {
        p.X += (float)(widthPerCell - child.DesiredSize.Width);
      }
      if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
      {
        p.Y += (float)((heightPerCell - child.DesiredSize.Height) / 2);
      }
      else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
      {
        p.Y += (float)(heightPerCell - child.DesiredSize.Height);
      }
    }

    public override void DoRender()
    {
      base.DoRender();
      if (Content != null)
      {
        SkinContext.AddOpacity(Opacity);
        Content.DoRender();
        SkinContext.RemoveOpacity();
      }
    }

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      if (Content != null)
      {
        Content.BuildRenderTree();
      }
    }

    public override void DestroyRenderTree()
    {
      if (Content != null)
      {
        Content.DestroyRenderTree();
      }
    }

    public override void OnMouseMove(float x, float y)
    {
      if (Content != null)
      {
        Content.OnMouseMove(x, y);
      }
      base.OnMouseMove(x, y);
    }

    public override void FireUIEvent(UIEvent eventType, UIElement source)
    {
      if (Content != null)
        Content.FireUIEvent(eventType,  source);
    }

    public override void OnKeyPressed(ref MediaPortal.Control.InputManager.Key key)
    {
      if (Content != null)
      {
        Content.OnKeyPressed(ref key);
      }
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      if (Content != null)
        childrenOut.Add(Content);
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (Content != null)
      {
        Content.Deallocate();
      }
    }

    public override void Allocate()
    {
      base.Allocate();
      if (Content != null)
      {
        Content.Allocate();
      }
    }
  }
}
