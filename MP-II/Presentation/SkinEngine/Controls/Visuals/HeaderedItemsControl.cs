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
using MediaPortal.Presentation.Properties;
using MediaPortal.Control.InputManager;
using Presentation.SkinEngine;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.MpfElements;
using MediaPortal.Presentation.Collections;
using Presentation.SkinEngine.SkinManagement;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class HeaderedItemsControl : ItemsControl
  {
    #region Private fields

    private Property _headerProperty;
    private Property _headerTemplateProperty;
    private Property _headerTemplateSelectorProperty;
    SizeF _baseDesiredSize;
    bool _wasExpanded = false;
    public string TempName;

    #endregion

    #region Ctor

    public HeaderedItemsControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _headerProperty = new Property(typeof(FrameworkElement), null);
      _headerTemplateProperty = new Property(typeof(DataTemplate), null);
      _headerTemplateSelectorProperty = new Property(typeof(DataTemplateSelector), null);
    }

    void Attach()
    {
      _headerProperty.Attach(OnContentChanged);
    }

    void Detach()
    {
      _headerProperty.Detach(OnContentChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      HeaderedItemsControl c = source as HeaderedItemsControl;
      Header = copyManager.GetCopy(c.Header);
      HeaderTemplateSelector = copyManager.GetCopy(c.HeaderTemplateSelector);
      HeaderTemplate = copyManager.GetCopy(c.HeaderTemplate);
      Attach();
    }

    #endregion

    #region Event handlers

    void OnContentChanged(Property property)
    {
      Header.VisualParent = this;
      Header.SetWindow(Window);
    }

    #endregion

    #region Public properties

    public Property HeaderProperty
    {
      get { return _headerProperty; }
    }

    public FrameworkElement Header
    {
      get { return _headerProperty.GetValue() as FrameworkElement; }
      set { _headerProperty.SetValue(value); }
    }

    public Property HeaderTemplateProperty
    {
      get { return _headerTemplateProperty; }
    }

    public DataTemplate HeaderTemplate
    {
      get { return _headerTemplateProperty.GetValue() as DataTemplate; }
      set { _headerTemplateProperty.SetValue(value); }
    }

    public Property HeaderTemplateSelectorProperty
    {
      get { return _headerTemplateSelectorProperty; }
    }

    public DataTemplateSelector HeaderTemplateSelector
    {
      get { return _headerTemplateSelectorProperty.GetValue() as DataTemplateSelector; }
      set { _headerTemplateSelectorProperty.SetValue(value); }
    }

    public bool IsExpanded
    {
      get
      {
        if (Header == null)
        {
          _wasExpanded = false;
          return false;
        }
        CheckBox expander = VisualTreeHelper.FindElement(Header, "Expander") as CheckBox;
        if (Header == null)
        {
          _wasExpanded = false;
          return false;
        }
        if (_wasExpanded != expander.IsChecked)
        {
          if (_wasExpanded)
          {
            if (_itemsHostPanel != null)
            {
              _itemsHostPanel.SetChildren(new UIElementCollection(_itemsHostPanel));
            }
          }
          Invalidate();
        }
        _wasExpanded = expander.IsChecked;
        return _wasExpanded;
      }
    }

    #endregion

    #region Measure&arrange

    public override void Measure(SizeF availableSize)
    {
      MediaPortal.Presentation.Collections.ListItem listItem = (MediaPortal.Presentation.Collections.ListItem)Context;
      //      string name = listItem.Label("Name").Evaluate(null, null);
      //      Trace.WriteLine(String.Format("TreeView Item:Measure '{0}' {1}x{2} expanded:{3}", name, availableSize.Width, availableSize.Height, IsExpanded));

      _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
      if (Header != null)
      {
        Header.Measure(new SizeF(availableSize.Width, 0));
        if (!_wasExpanded)
        {
          _desiredSize = Header.DesiredSize;

          //          Trace.WriteLine(String.Format("TreeView Item:Measure '{0}' returns header:{1}x{2} not expanded",
          //              name, Header.DesiredSize.Width, Header.DesiredSize.Height));
          return;
        }
      }
      base.Measure(new SizeF(availableSize.Width, 0));
      _baseDesiredSize = new SizeF(_desiredSize.Width, _desiredSize.Height);
      if (Header != null)
      {
        _desiredSize.Height += Header.DesiredSize.Height;
      }
      _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
      //      Trace.WriteLine(String.Format("TreeView Item:Measure '{0}' returns header:{1}x{2} base:{3}x{4}",
      //          name, Header.DesiredSize.Width, Header.DesiredSize.Height,
      //        _baseDesiredSize.Width, _baseDesiredSize.Height));
    }

    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.Left * SkinContext.Zoom.Width);
      layoutRect.Y += (float)(Margin.Top * SkinContext.Zoom.Height);
      layoutRect.Width -= (float)((Margin.Left + Margin.Right) * SkinContext.Zoom.Width);
      layoutRect.Height -= (float)((Margin.Left + Margin.Right) * SkinContext.Zoom.Height);
      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      PointF p = layoutRect.Location;

      MediaPortal.Presentation.Collections.ListItem listItem = (MediaPortal.Presentation.Collections.ListItem)Context;
      //      string name = listItem.Label("Name").Evaluate(null, null);
      //      Trace.WriteLine(String.Format("TreeView Item:Arrange {0} ({1},{2}) {2}x{3}", name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));


      if (Header != null)
      {
        //ArrangeChild(Header, ref p, layoutRect.Width, layoutRect.Height);
        Header.Arrange(new RectangleF(p, Header.DesiredSize));
        if (!_wasExpanded)
        {

          _finalLayoutTransform = SkinContext.FinalLayoutTransform;
          IsArrangeValid = true;
          Initialize();
          InitializeTriggers();
          _isLayoutInvalid = false;
          if (!finalRect.IsEmpty)
          {
            if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
              _performLayout = true;
            _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
          }
          return;
        }
        p.Y += Header.DesiredSize.Height;

      }
      if (_wasExpanded)
      {
        //        Trace.WriteLine(String.Format("TreeView Item:Arrange {0} childs at({1},{2})", name, (int)p.X, (int)p.Y));
        base.Arrange(new RectangleF(p, _baseDesiredSize));
      }
    }

    protected void ArrangeChild(FrameworkElement child, ref System.Drawing.PointF p, double widthPerCell, double heightPerCell)
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
    #endregion

    #region Rendering

    public override void DoRender()
    {
      lock (_headerProperty)
      {
        if (Header != null)
        {
          SkinContext.AddOpacity(this.Opacity);
          Header.DoRender();
          SkinContext.RemoveOpacity();

        }
        if (IsExpanded)
        {
          SkinContext.AddOpacity(this.Opacity);
          base.DoRender();
          SkinContext.RemoveOpacity();
        }
      }
    }

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      if (Header != null)
      {
        Header.BuildRenderTree();
      }
      if (IsExpanded)
      {
        base.DoBuildRenderTree();
      }
    }

    public override void DestroyRenderTree()
    {
      if (Header != null)
      {
        Header.DestroyRenderTree();
      }
      base.DestroyRenderTree();
    }

    #endregion

    public override void FireUIEvent(UIEvent eventType, UIElement source)
    {
      if (Header != null)
        Header.FireUIEvent(eventType, source);
    }

    #region Input handling

    public override void OnMouseMove(float x, float y)
    {
      if (!IsFocusScope) return;
      if (Header != null)
      {
        Header.OnMouseMove(x, y);
      }
      if (IsExpanded)
      {
        base.OnMouseMove(x, y);
      }
    }

    public override void OnKeyPressed(ref MediaPortal.Control.InputManager.Key key)
    {
      lock (_headerProperty)
      {
        if (Header != null)
        {
          Header.OnKeyPressed(ref key);
        }
        if (IsExpanded)
        {
          base.OnKeyPressed(ref key);
        }
      }
    }

    #endregion

    public override UIElement FindElement(IFinder finder)
    {
      UIElement found = base.FindElement(finder);
      if (found != null) return found;
      if (Header != null)
      {
        found = Header.FindElement(finder);
        return found;
      }
      return null;
    }

    public override void Reset()
    {
      if (Header != null)
        Header.Reset();
      base.Reset();
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (Header != null)
      {
        Header.Deallocate();
      }
    }

    public override void Allocate()
    {
      base.Allocate();
      if (Header != null)
      {
        Header.Allocate();
      }
    }

    #region Focus prediction

    public override FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (IsExpanded)
      {
        FrameworkElement element = base.PredictFocusUp(focusedFrameworkElement, ref key, strict);
        if (element != null) return element;
      }
      return (Header).PredictFocusUp(focusedFrameworkElement, ref key, strict);
    }

    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (IsExpanded)
      {
        FrameworkElement element = base.PredictFocusDown(focusedFrameworkElement, ref key, strict);
        if (element != null) return element;
      }
      return (Header).PredictFocusDown(focusedFrameworkElement, ref key, strict);
    }

    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (IsExpanded && base.FindElement(FocusFinder.Instance) != null)
      {
        FrameworkElement element = base.PredictFocusLeft(focusedFrameworkElement, ref key, strict);
        if (element != null) return element;
      }
      return (Header).PredictFocusLeft(focusedFrameworkElement, ref key, strict);
    }

    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (IsExpanded && base.FindElement(FocusFinder.Instance) != null)
      {
        FrameworkElement element = base.PredictFocusRight(focusedFrameworkElement, ref key, strict);
        if (element != null) return element;
      }
      return (Header).PredictFocusRight(focusedFrameworkElement, ref key, strict);
    }

    #endregion

    public override void SetWindow(Window window)
    {
      base.SetWindow(window);
      if (Header != null)
      {
        Header.SetWindow(window);
      }
    }

    protected override FrameworkElement PrepareItemContainer(object dataItem)
    {
      _itemsHostPanel.IsItemsHost = false;
      TreeViewItem container = new TreeViewItem();
      container.Style = ItemContainerStyle;
      container.ItemContainerStyle = ItemContainerStyle; // TreeItems also have to build containers...
      container.Context = dataItem;
      container.ItemsPanel = ItemsPanel;
      container.HeaderTemplateSelector = HeaderTemplateSelector;
      container.HeaderTemplate = HeaderTemplate;
      FrameworkElement containerTemplateControl = ItemContainerStyle.Get();
      containerTemplateControl.Context = dataItem;
      ContentPresenter headerContentPresenter = containerTemplateControl.FindElement(new TypeFinder(typeof(ContentPresenter))) as ContentPresenter;
      headerContentPresenter.Content = (FrameworkElement)container.HeaderTemplate.LoadContent();

      container.TemplateControl = new ItemsPresenter();
      container.TemplateControl.Margin = new Thickness(64, 0, 0, 0);
      container.TemplateControl.VisualParent = container;
      container.Header = containerTemplateControl;
      ItemsPresenter p = container.Header.FindElement(new TypeFinder(typeof(ItemsPresenter))) as ItemsPresenter;
      if (p != null) p.IsVisible = false;

      if (dataItem is ListItem)
      {
        ListItem listItem = (ListItem)dataItem;
        container.ItemsSource = listItem.SubItems;
      }
      return container;
    }
  }
}
