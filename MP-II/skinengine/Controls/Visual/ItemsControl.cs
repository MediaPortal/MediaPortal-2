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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Collections;
using SkinEngine;
using SkinEngine.Controls.Panels;

namespace SkinEngine.Controls.Visuals
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
    Panel _itemsHostPanel;

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
      return new ItemsControl(this);
    }

    void Init()
    {
      _itemsSourceProperty = new Property(null);
      _itemTemplateProperty = new Property(null);
      _itemTemplateSelectorProperty = new Property(null);
      _itemContainerStyleProperty = new Property(null);
      _itemContainerStyleSelectorProperty = new Property(null);
      _itemsPanelProperty = new Property(null);
      _currentItem = new Property(null);
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
        p.Attach(new PropertyChangedHandler(OnItemsSourcePropChanged));
      }
      else if (ItemsSource is ItemsCollection)
      {
        ItemsCollection coll = (ItemsCollection)ItemsSource;
        coll.Changed += new ItemsCollection.ItemsChangedHandler(OnCollectionChanged);
      }
      _prepare = true;
      Invalidate();
    }

    void OnCollectionChanged(bool refreshAll)
    {
      _prepare = true;
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
      Invalidate();
    }
    void OnItemTemplateChanged(Property property)
    {
      _prepare = true;
      Invalidate();
    }
    void OnItemsPanelChanged(Property property)
    {
      _templateApplied = false;
      _prepare = true;
      Invalidate();
    }
    void OnItemContainerStyleChanged(Property property)
    {
      _prepare = true;
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

    #region item generation
    /// <summary>
    /// Prepares this instance.
    /// </summary>
    /// <returns></returns>
    bool Prepare()
    {
      if (ItemsSource == null) return false;
      if (ItemsPanel == null) return false;
      if (ItemContainerStyle == null) return false;
      if (ItemTemplate == null) return false;
      Trace.WriteLine("ItemsControl.Prepare()");
      ItemsPresenter presenter = FindElementType(typeof(ItemsPresenter)) as ItemsPresenter;
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


      int index = 0;
      FrameworkElement focusedContainer=null;
      UIElementCollection children = new UIElementCollection(null);
      IEnumerator enumer = ItemsSource.GetEnumerator();
      while (enumer.MoveNext())
      {
        FrameworkElement container = ItemContainerStyle.Get();
        container.VisualParent = _itemsHostPanel;
        container.Name = String.Format("ItemsControl.{0} #{1}", container, index++);
        FrameworkElement newItem = (FrameworkElement)ItemTemplate.LoadContent();
        newItem.VisualParent = container;
        newItem.Context = enumer.Current;
        container.Context = enumer.Current;
        if (enumer.Current is ListItem)
        {
          if (((ListItem)enumer.Current).Selected)
          {
            focusedContainer = container;
          }
        }
        ContentPresenter cpresenter = container.FindElementType(typeof(ContentPresenter)) as ContentPresenter;
        if (cpresenter != null)
        {
          cpresenter.Content = newItem;
        }
        children.Add(container);
      }
      children.SetParent(_itemsHostPanel);
      _itemsHostPanel.SetChildren(children);
      _itemsHostPanel.Invalidate();

      
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
        focusedContainer.HasFocus = true;
      }

      return true;
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

  }
}
