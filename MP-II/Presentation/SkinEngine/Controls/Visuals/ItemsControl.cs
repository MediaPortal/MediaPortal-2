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
using System.Diagnostics;
using System.Collections;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Presentation.Collections;
using Presentation.SkinEngine.Controls.Panels;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Represents a control that can be used to present a collection of items.
  /// http://msdn2.microsoft.com/en-us/library/system.windows.controls.itemscontrol.aspx
  /// </summary>
  public class ItemsControl : Control
  {
    Property _itemsSourceProperty;
    Property _itemTemplateProperty;
    Property _itemTemplateSelectorProperty;
    Property _itemContainerStyleProperty;
    Property _itemContainerStyleSelectorProperty;
    Property _itemsPanelProperty;
    Property _currentItem;
    bool _prepare;
    bool _templateApplied;
    protected Panel _itemsHostPanel;

    #region ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemsControl"/> class.
    /// </summary>
    public ItemsControl()
    {
      Init();
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemsControl"/> class.
    /// </summary>
    /// <param name="c">The c.</param>
    public ItemsControl(ItemsControl c)
      : base(c)
    {
      Init();
      ItemsSource = c.ItemsSource;
      ItemTemplate = c.ItemTemplate;
      ItemTemplateSelector = c.ItemTemplateSelector;
      ItemContainerStyle = c.ItemContainerStyle;
      ItemContainerStyleSelector = c.ItemContainerStyleSelector;
      ItemsPanel = c.ItemsPanel;
      _prepare = false;
    }

    public override object Clone()
    {
      ItemsControl result = new ItemsControl(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    void Init()
    {
      _itemsSourceProperty = new Property(typeof(IEnumerable), null);
      _itemTemplateProperty = new Property(typeof(DataTemplate), null);
      _itemTemplateSelectorProperty = new Property(typeof(DataTemplateSelector), null);
      _itemContainerStyleProperty = new Property(typeof(Style), null);
      _itemContainerStyleSelectorProperty = new Property(typeof(StyleSelector), null);
      _itemsPanelProperty = new Property(typeof(ItemsPanelTemplate), null);
      _currentItem = new Property(typeof(object), null);
      _itemsSourceProperty.Attach(new PropertyChangedHandler(OnItemsSourceChanged));
      _itemTemplateProperty.Attach(new PropertyChangedHandler(OnItemTemplateChanged));
      _itemsPanelProperty.Attach(new PropertyChangedHandler(OnItemsPanelChanged));
      _itemContainerStyleProperty.Attach(new PropertyChangedHandler(OnItemContainerStyleChanged));

    }
    #endregion

    #region event handlers

    void OnItemsSourceChanged(Property property)
    {
      if (ItemsSource is Property)
      {
        Property p = (Property)ItemsSource;
        p.Attach(OnItemsSourcePropChanged);
      }
      else if (ItemsSource is ItemsCollection)
      {
        ItemsCollection coll = (ItemsCollection)ItemsSource;
        coll.Changed += OnCollectionChanged;
      }
      _prepare = true;
      Invalidate();
      if (Window!=null) Window.Invalidate(this);
    }

    void OnCollectionChanged(bool refreshAll)
    {
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
    }

    void OnHasFocusChanged(Property property)
    {
      if (HasFocus)
      {
        SetFocusOnFirstItem();
      }
    }
    void OnItemsSourcePropChanged(Property property)
    {
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
      Invalidate();
    }
    void OnItemTemplateChanged(Property property)
    {
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
      Invalidate();
    }
    void OnItemsPanelChanged(Property property)
    {
      _templateApplied = false;
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
      Invalidate();
    }
    void OnItemContainerStyleChanged(Property property)
    {
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
      Invalidate();
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the template that defines the panel that controls the layout of items. This is a dependency property.
    /// </summary>
    /// <value>The items panel property.</value>
    public Property ItemsPanelProperty
    {
      get
      {
        return _itemsPanelProperty;
      }
      set
      {
        _itemsPanelProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the template that defines the panel that controls the layout of items. This is a dependency property.
    /// </summary>
    /// <value>The items panel.</value>
    public ItemsPanelTemplate ItemsPanel
    {
      get
      {
        return _itemsPanelProperty.GetValue() as ItemsPanelTemplate;
      }
      set
      {
        _itemsPanelProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets a collection used to generate the content of the ItemsControl. This is a dependency property.
    /// </summary>
    /// <value>The items source property.</value>
    public Property ItemsSourceProperty
    {
      get
      {
        return _itemsSourceProperty;
      }
    }

    /// <summary>
    /// Gets or sets a collection used to generate the content of the ItemsControl. This is a dependency property.
    /// </summary>
    /// <value>The items source.</value>
    public IEnumerable ItemsSource
    {
      get
      {
        return _itemsSourceProperty.GetValue() as IEnumerable;
      }
      set
      {
        _itemsSourceProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the Style that is applied to the container element generated for each item. This is a dependency property.
    /// </summary>
    /// <value>The item container style property.</value>
    public Property ItemContainerStyleProperty
    {
      get
      {
        return _itemContainerStyleProperty;
      }
      set
      {
        _itemContainerStyleProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the Style that is applied to the container element generated for each item. This is a dependency property.
    /// </summary>
    /// <value>The item container style.</value>
    public Style ItemContainerStyle
    {
      get
      {
        return _itemContainerStyleProperty.GetValue() as Style;
      }
      set
      {
        _itemContainerStyleProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets custom style-selection logic for a style that can be applied to each generated container element. This is a dependency property.
    /// </summary>
    /// <value>The item container style selector property.</value>
    public Property ItemContainerStyleSelectorProperty
    {
      get
      {
        return _itemContainerStyleSelectorProperty;
      }
      set
      {
        _itemContainerStyleSelectorProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets custom style-selection logic for a style that can be applied to each generated container element. This is a dependency property.
    /// </summary>
    /// <value>The item container style selector.</value>
    public StyleSelector ItemContainerStyleSelector
    {
      get
      {
        return _itemContainerStyleSelectorProperty.GetValue() as StyleSelector;
      }
      set
      {
        _itemContainerStyleSelectorProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the DataTemplate used to display each item. This is a dependency property.
    /// </summary>
    /// <value>The item template property.</value>
    public Property ItemTemplateProperty
    {
      get
      {
        return _itemTemplateProperty;
      }
      set
      {
        _itemTemplateProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the DataTemplate used to display each item. This is a dependency property.
    /// </summary>
    /// <value>The item template.</value>
    public DataTemplate ItemTemplate
    {
      get
      {
        return _itemTemplateProperty.GetValue() as DataTemplate;
      }
      set
      {
        _itemTemplateProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the custom logic for choosing a template used to display each item. This is a dependency property.
    /// </summary>
    /// <value>The item template selector property.</value>
    public Property ItemTemplateSelectorProperty
    {
      get
      {
        return _itemTemplateSelectorProperty;
      }
      set
      {
        _itemTemplateSelectorProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the custom logic for choosing a template used to display each item. This is a dependency property.
    /// </summary>
    /// <value>The item template selector.</value>
    public DataTemplateSelector ItemTemplateSelector
    {
      get
      {
        return _itemTemplateSelectorProperty.GetValue() as DataTemplateSelector;
      }
      set
      {
        _itemTemplateSelectorProperty.SetValue(value);
      }
    }

    public Property CurrentItemProperty
    {
      get
      {
        return _currentItem;
      }
      set
      {
        _currentItem = value;
      }
    }

    /// <summary>
    /// Gets or sets the item template selector.
    /// </summary>
    /// <value>The item template selector.</value>
    public object CurrentItem
    {
      get
      {
        return _currentItem.GetValue();
      }
      set
      {
        _currentItem.SetValue(value);
      }
    }
    #endregion

    public override void Reset()
    {
      Trace.WriteLine("Reset:" + this.Name);
      base.Reset();
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
      if (_itemsHostPanel != null)
      {
        _itemsHostPanel.Reset();
        _itemsHostPanel.SetChildren(new UIElementCollection(_itemsHostPanel));
      }
    }
    protected virtual ItemsPresenter FindItemsPresenter()
    {
      return FindElementType(typeof(ItemsPresenter)) as ItemsPresenter;
    }
    #region item generation
    /// <summary>
    /// Prepares this instance.
    /// </summary>
    /// <returns></returns>
    bool Prepare()
    {
      if (ItemsSource == null) return false;
      if (ItemsPanel == null) return false;
      if (TemplateControl == null)
      {
        if (ItemContainerStyle == null) return false;
        if (ItemTemplate == null) return false;
      }
      IEnumerator enumer = ItemsSource.GetEnumerator();
      if (enumer.MoveNext() == false) return true;
      enumer.Reset();
      if (this is TreeViewItem)
      {
        TreeViewItem item = (TreeViewItem)this;
        if (!item.IsExpanded) return true;
      }
      DateTime dtStart = DateTime.Now;
      ItemsPresenter presenter = FindItemsPresenter();
      if (presenter == null) return false;
      if (!_templateApplied)
      {
        presenter.ApplyTemplate(ItemsPanel);
        _itemsHostPanel = null;
        _templateApplied = true;
      }

      if (_itemsHostPanel == null)
      {
        _itemsHostPanel = presenter.FindItemsHost() as Panel;
      }
      if (_itemsHostPanel == null) return false;

      int itemCount = _itemsHostPanel.Children.Count;
      int focusedIndex = -1;
      FrameworkElement focusedItem = null;
      for (int i = 0; i < _itemsHostPanel.Children.Count; ++i)
      {
        focusedItem = _itemsHostPanel.Children[i].FindFocusedItem() as FrameworkElement;
        if (focusedItem != null)
        {
          focusedIndex = i;
          break;
        }
      }
      _itemsHostPanel.Children.Clear();
      int index = 0;
      FrameworkElement focusedContainer = null;
      UIElementCollection children = new UIElementCollection(null);
      while (enumer.MoveNext())
      {
        if (this is ListView)
        {
          ListViewItem container = new ListViewItem();
          container.Context = enumer.Current;
          container.Style = ItemContainerStyle;
          container.ContentTemplate = ItemTemplate;
          container.ContentTemplateSelector = ItemTemplateSelector;
          container.Content = (FrameworkElement)ItemTemplate.LoadContent(Window);
          container.VisualParent = _itemsHostPanel;
          //container.Name = String.Format("ItemsControl.{0} #{1}", container, index++);
          if (enumer.Current is ListItem)
          {
            if (((ListItem)enumer.Current).Selected)
            {
              focusedContainer = container;
            }
          }
          children.Add(container);
        }
        else if (this is TreeView)
        {
          TreeViewItem container = new TreeViewItem();
          container.Context = enumer.Current;
          container.Style = ItemContainerStyle;
          container.TemplateControl = new ItemsPresenter();
          container.TemplateControl.Margin = new SlimDX.Vector4(64, 0, 0, 0);
          container.TemplateControl.VisualParent = container; 
          container.ItemsPanel = ItemsPanel;
          if (enumer.Current is ListItem)
          {
            ListItem listItem = (ListItem)enumer.Current;
            container.ItemsSource = listItem.SubItems;
          }
          container.Name = String.Format("{0}.{1}", this.Name, index++);
          //container.TemplateControl.Name = "itemspresenter for childs of :" + container.Name;

          container.Style = ItemContainerStyle;
          container.HeaderTemplateSelector = this.ItemTemplateSelector;
          container.HeaderTemplate = ItemTemplate;
          FrameworkElement element = container.Style.Get(Window);
          element.Context = enumer.Current;
          ContentPresenter headerContentPresenter = element.FindElementType(typeof(ContentPresenter)) as ContentPresenter;
          headerContentPresenter.Content = (FrameworkElement)container.HeaderTemplate.LoadContent(Window);

          container.Header = (FrameworkElement)element;

          ItemsPresenter p = container.Header.FindElementType(typeof(ItemsPresenter)) as ItemsPresenter;
          if (p != null) p.IsVisible = false;

          if (enumer.Current is ListItem)
          {
            if (((ListItem)enumer.Current).Selected)
            {
              focusedContainer = container;
            }
          }
          children.Add(container);
        }
        else
        {
          _itemsHostPanel.IsItemsHost = false;
          TreeViewItem container = new TreeViewItem();
          TreeViewItem item = (TreeViewItem)this;
          //container.Name = String.Format("{0}.{1}", item.Name, index++);
          container.Context = enumer.Current;
          //container.Style = ItemContainerStyle;
          container.ItemsPanel = ItemsPanel;
          container.Style = this.Style;
          container.HeaderTemplateSelector = item.HeaderTemplateSelector;
          container.HeaderTemplate = item.HeaderTemplate;
          FrameworkElement element = container.Style.Get(Window);
          //container.TemplateControl.Name = "itemspresenter for childs of :" + container.Name;
          element.Context = enumer.Current;
          ContentPresenter headerContentPresenter = element.FindElementType(typeof(ContentPresenter)) as ContentPresenter;
          headerContentPresenter.Content = (FrameworkElement)container.HeaderTemplate.LoadContent(Window);

          container.TemplateControl = new ItemsPresenter();
          container.TemplateControl.Margin = new SlimDX.Vector4(64, 0, 0, 0);
          container.TemplateControl.VisualParent = container; 
          container.Header = (FrameworkElement)element;
          ItemsPresenter p = container.Header.FindElementType(typeof(ItemsPresenter)) as ItemsPresenter;
          if (p != null) p.IsVisible = false;

          if (enumer.Current is ListItem)
          {
            ListItem listItem = (ListItem)enumer.Current;
            container.ItemsSource = listItem.SubItems;
          }

          if (enumer.Current is ListItem)
          {
            if (((ListItem)enumer.Current).Selected)
            {
              focusedContainer = container;
            }
          }
          children.Add(container);
        }
      }
      children.SetParent(_itemsHostPanel);
      //if (!(this is TreeView))
      //_itemsHostPanel.Name = "ItemsPanel of :" + this.Name;
      _itemsHostPanel.SetChildren(children);
      _itemsHostPanel.Invalidate();

      if (this is ListView)
      {
        if (focusedItem != null)
        {
          IScrollInfo info = _itemsHostPanel as IScrollInfo;
          if (info != null)
          {
            info.ResetScroll();
          }
          //        result = true;
          _itemsHostPanel.UpdateLayout();
          focusedItem.HasFocus = false;
          if (_itemsHostPanel.Children.Count <= focusedIndex)
          {
            float x = (float)_itemsHostPanel.Children[0].ActualPosition.X;
            float y = (float)_itemsHostPanel.Children[0].ActualPosition.Y;
            _itemsHostPanel.OnMouseMove(x, y);
          }
          else
          {
            float x = (float)focusedItem.ActualPosition.X;
            float y = (float)focusedItem.ActualPosition.Y;
            _itemsHostPanel.OnMouseMove(x, y);
          }
        }
        else if (focusedContainer != null)
        {
          _itemsHostPanel.UpdateLayout();
          focusedContainer.OnMouseMove((float)focusedContainer.ActualPosition.X, (float)focusedContainer.ActualPosition.Y);
        }
      }
      TimeSpan ts = DateTime.Now - dtStart;
      Trace.WriteLine(String.Format("ItemsControl.Prepare:{0} {1} msec {2}", this.Name, ts.TotalMilliseconds, _itemsHostPanel.Children.Count));
      return true;
    }
    public override void UpdateLayout()
    {
      DoUpdateItems();
      base.UpdateLayout();
    }
    public bool DoUpdateItems()
    {
      if (_prepare)
      {
        if (Prepare())
        {
          _prepare = false;
        }
      }
      return false;
    }

    public override void DoRender()
    {
      DoUpdateItems();
      base.DoRender();
    }
    public override bool HasFocus
    {
      get
      {
        return (FindFocusedItem() != null);
      }
      set
      {
        if (value)
        {
          if (!HasFocus)
            SetFocusOnFirstItem();
        }
        else
        {
          UIElement element = FindFocusedItem();
          if (element != null)
            element.HasFocus = false;
        }
      }
    }
    public void SetFocusOnFirstItem()
    {
      ItemsPresenter presenter = FindElementType(typeof(ItemsPresenter)) as ItemsPresenter;
      if (presenter != null)
      {
        Panel panel = presenter.FindItemsHost() as Panel;
        if (panel != null)
        {
          if (panel.Children != null && panel.Children.Count > 0)
          {
            FrameworkElement element = (FrameworkElement)panel.Children[0];
            element.OnMouseMove((float)element.ActualPosition.X, (float)element.ActualPosition.Y);
          }
        }
      }
    }
    #endregion

    public override void Allocate()
    {
      base.Allocate();
      _prepare = true;
    }

    public override void Update()
    {
      base.Update();
      DoUpdateItems();
    }
  }
}
