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
      _itemsSourceProperty.Attach(new PropertyChangedHandler(OnItemsChanged));
      _itemTemplateProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _itemsPanelProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _itemContainerStyleProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    void OnItemsChanged(Property property)
    {
      if (ItemsSource is Property)
      {
        Property p = (Property)ItemsSource;
        p.Attach(new PropertyChangedHandler(OnPropertyChanged));
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
      Prepare();
    }
    void OnPropertyChanged(Property property)
    {
      _prepare = true;
      Invalidate();
    }

    /// <summary>
    /// Gets or sets the items panel property.
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
    /// Gets or sets the items panel.
    /// </summary>
    /// <value>The items panel.</value>
    public Panel ItemsPanel
    {
      get
      {
        return _itemsPanelProperty.GetValue() as Panel;
      }
      set
      {
        _itemsPanelProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets the items source property.
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
    /// Gets or sets the items source.
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
    /// Gets or sets the item container style property.
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
    /// Gets or sets the item container style.
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
    /// Gets or sets the item container style selector property.
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
    /// Gets or sets the item container style selector.
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
    /// Gets or sets the item template property.
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
    /// Gets or sets the item template.
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
    /// Gets or sets the item template selector property.
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
    /// Gets or sets the item template selector.
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

    bool Prepare()
    {
      if (ItemsSource == null) return false;
      if (ItemsPanel == null) return false;
      if (ItemContainerStyle == null) return false;
      if (ItemTemplate == null) return false;
      if (ItemTemplate.VisualTree == null) return false;
      Trace.WriteLine("ItemsControl.Prepare()");

      int itemCount = ItemsPanel.Children.Count;
      int focusedIndex = -1;
      FrameworkElement focusedItem = null;
      for (int i = 0; i < ItemsPanel.Children.Count; ++i)
      {
        focusedItem = ItemsPanel.Children[i].FindFocusedItem() as FrameworkElement;
        if (focusedItem != null)
        {
          focusedIndex = i;
          break;
        }
      }

      ItemsPanel.Children.Clear();
      IEnumerator enumer = ItemsSource.GetEnumerator();
      while (enumer.MoveNext())
      {
        FrameworkElement container = ItemContainerStyle.Get();
        container.VisualParent = ItemsPanel;
        FrameworkElement newItem = (FrameworkElement)ItemTemplate.VisualTree.Clone();
        newItem.VisualParent = container;
        newItem.Context = enumer.Current;
        container.Context = enumer.Current;
        ContentPresenter presenter = container.FindElementType(typeof(ContentPresenter)) as ContentPresenter;
        if (presenter != null)
        {
          presenter.Content = newItem;
        }
        ItemsPanel.Children.Add(container);
      }
      //bool result = false;
      ItemsPanel.Invalidate();
      if (focusedItem != null)
      {
        IScrollInfo info = ItemsPanel as IScrollInfo;
        if (info != null)
        {
          info.Reset();
        }
//        result = true;
        ItemsPanel.UpdateLayout();
        focusedItem.HasFocus = false;
        if (ItemsPanel.Children.Count <= focusedIndex)
        {
          float x = (float)ItemsPanel.Children[0].ActualPosition.X;
          float y = (float)ItemsPanel.Children[0].ActualPosition.Y;
          ItemsPanel.OnMouseMove(x, y);
        }
        else
        {
          float x = (float)ItemsPanel.Children[focusedIndex].ActualPosition.X;
          float y = (float)ItemsPanel.Children[focusedIndex].ActualPosition.Y;
          ItemsPanel.OnMouseMove(x, y);
        }
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
  }
}
