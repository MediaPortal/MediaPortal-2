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

using System.Drawing;
using MediaPortal.Presentation.Properties;
using MediaPortal.Control.InputManager;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.MpfElements;
using Presentation.SkinEngine.SkinManagement;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class ContentPresenter : FrameworkElement
  {
    #region Private fields

    private Property _contentProperty;
    private Property _contentTemplateProperty;
    private Property _contentTemplateSelectorProperty;
    FrameworkElement _contentCache;

    #endregion

    #region Ctor

    public ContentPresenter()
    {
      Init();
    }

    void Init()
    {
      _contentProperty = new Property(typeof(FrameworkElement), null);
      _contentTemplateProperty = new Property(typeof(DataTemplate), null);
      _contentTemplateSelectorProperty = new Property(typeof(DataTemplateSelector), null);

      _contentProperty.Attach(OnContentChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ContentPresenter p = source as ContentPresenter;
      Content = copyManager.GetCopy(p.Content);
      ContentTemplateSelector = copyManager.GetCopy(p.ContentTemplateSelector);

      // Don't take part in the outer copying process for the ContentTemplate property here -
      // we need a finished copied template here. As the template has no references to its
      // containing instance, it is safe to do a self-contained deep copy of it.
      ContentTemplate = MpfCopyManager.DeepCopy(p.ContentTemplate);
    }

    #endregion

    void OnContentChanged(Property property)
    {
      _contentCache = _contentProperty.GetValue() as FrameworkElement;
      Content.VisualParent = this;
      Content.SetWindow(Window);
    }

    public Property ContentProperty
    {
      get { return _contentProperty; }
    }

    public FrameworkElement Content
    {
      get { return _contentCache; }
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

    public Property ContentTemplateSelectorProperty
    {
      get { return _contentTemplateSelectorProperty; }
    }

    public DataTemplateSelector ContentTemplateSelector
    {
      get { return _contentTemplateSelectorProperty.GetValue() as DataTemplateSelector; }
      set { _contentTemplateSelectorProperty.SetValue(value); }
    }

    public override void Measure(SizeF availableSize)
    {
      float marginWidth = (Margin.Left + Margin.Right) * SkinContext.Zoom.Width;
      float marginHeight = (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height;
      _desiredSize = new SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);
      
      // Width / Height is not set.
      if (Width == 0)
        _desiredSize.Width = availableSize.Width - marginWidth;
      if (Height == 0)
        _desiredSize.Height = availableSize.Height - marginHeight;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      // Calculate how much is available for the child
      SizeF childSize = new SizeF(_desiredSize.Width, _desiredSize.Height);

      // It can not be less than zero in any dimension
      if (childSize.Width < 0) 
        childSize.Width = 0;
      if (childSize.Height < 0) 
        childSize.Height = 0;

      // Do we have a child
      if (Content != null)
      {
        // Measure the child
        Content.Measure(childSize);

        // Next lines added by Albert78, 20.5.08
        if (Width == 0)
          _desiredSize.Width = Content.DesiredSize.Width;
        if (Height == 0)
          _desiredSize.Height = Content.DesiredSize.Height;
      }

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }

      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);
      _desiredSize.Width += marginWidth;
      _desiredSize.Height += marginHeight;

      _availableSize = new SizeF(availableSize.Width, availableSize.Height);
    }

    public override void Arrange(RectangleF finalRect)
    {
      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);
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

      if (Content != null)
      {
        PointF location = new PointF(layoutRect.X, layoutRect.Y);
        SizeF size = new SizeF(Content.DesiredSize.Width, Content.DesiredSize.Height);
        ArrangeChild(Content, ref location, layoutRect.Width, layoutRect.Height);
        Content.Arrange(new RectangleF(location, size));
      }

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      IsArrangeValid = true;
      Initialize();
      InitializeTriggers();
      _isLayoutInvalid = false;
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
      if (!IsFocusScope) return;
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

    public override UIElement FindElement(IFinder finder)
    {
      UIElement found = base.FindElement(finder);
      if (found != null) return found;
      if (Content != null)
      {
        found = Content.FindElement(finder);
        return found;
      }
      return null;
    }

    public override void Reset()
    {
      base.Reset();
      if (Content != null)
        Content.Reset();
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

    #region Focus prediction

    public override FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Content).PredictFocusUp(focusedFrameworkElement, ref key, strict);
    }

    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Content).PredictFocusDown(focusedFrameworkElement, ref key, strict);
    }

    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Content).PredictFocusLeft(focusedFrameworkElement, ref key, strict);
    }

    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Content).PredictFocusRight(focusedFrameworkElement, ref key, strict);
    }

    #endregion

    public override void SetWindow(Window window)
    {
      base.SetWindow(window);
      if (Content != null)
      {
        Content.SetWindow(window);
      }
    }
  }
}
