#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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

using SkinEngine;


namespace SkinEngine.Controls.Visuals
{
  public class ItemsControl : Control
  {
    Property _itemsSourceProperty;
    Property _itemTemplateProperty;
    Property _itemTemplateSelectorProperty;
    Property _itemContainerStyleProperty;
    Property _itemContainerStyleSelectorProperty;

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
    }

    public override object Clone()
    {
      return new ItemsControl();
    }

    void Init()
    {
      _itemsSourceProperty = new Property(null);
      _itemTemplateProperty = new Property(null);
      _itemTemplateSelectorProperty = new Property(null);
      _itemContainerStyleProperty = new Property(null);
      _itemContainerStyleSelectorProperty = new Property(null);
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
  }
}
