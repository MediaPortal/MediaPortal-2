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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Controls.Visuals
{
  public class FrameworkElement : UIElement
  {
    Property _widthProperty;
    Property _heightProperty;

    Property _acutalWidthProperty;
    Property _actualHeightProperty;

    bool _mouseOver = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkElement"/> class.
    /// </summary>
    public FrameworkElement()
    {
      Init();
    }

    public FrameworkElement(FrameworkElement el)
      : base((UIElement)el)
    {
      Init();
      Width = el.Width;
      Height = el.Height;
      ActualWidth = el.ActualWidth;
      ActualHeight = el.ActualHeight;
    }
    void Init()
    {
      _widthProperty = new Property((double)0.0f);
      _heightProperty = new Property((double)0.0f);


      _acutalWidthProperty = new Property((double)0.0f);
      _actualHeightProperty = new Property((double)0.0f);


      _widthProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _heightProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }
    /// <summary>
    /// Called when a property value has been changed
    /// Since all UIElement properties are layout properties
    /// we're simply calling Invalidate() here to invalidate the layout
    /// </summary>
    /// <param name="property">The property.</param>
    void OnPropertyChanged(Property property)
    {
      Invalidate();
    }

    #region properties
    /// <summary>
    /// Gets or sets the width property.
    /// </summary>
    /// <value>The width property.</value>
    public Property WidthProperty
    {
      get
      {
        return _widthProperty;
      }
      set
      {
        _widthProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <value>The width.</value>
    public double Width
    {
      get
      {
        return (double)_widthProperty.GetValue();
      }
      set
      {
        _widthProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the height property.
    /// </summary>
    /// <value>The height property.</value>
    public Property HeightProperty
    {
      get
      {
        return _heightProperty;
      }
      set
      {
        _heightProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    public double Height
    {
      get
      {
        return (double)_heightProperty.GetValue();
      }
      set
      {
        _heightProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the width property.
    /// </summary>
    /// <value>The width property.</value>
    public Property ActualWidthProperty
    {
      get
      {
        return _acutalWidthProperty;
      }
      set
      {
        _acutalWidthProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <value>The width.</value>
    public double ActualWidth
    {
      get
      {
        return (double)_acutalWidthProperty.GetValue();
      }
      set
      {
        _acutalWidthProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the height property.
    /// </summary>
    /// <value>The height property.</value>
    public Property ActualHeightProperty
    {
      get
      {
        return _actualHeightProperty;
      }
      set
      {
        _actualHeightProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    public double ActualHeight
    {
      get
      {
        return (double)_actualHeightProperty.GetValue();
      }
      set
      {
        _actualHeightProperty.SetValue(value);
      }
    }
    #endregion


    public override void OnMouseMove(float x, float y)
    {
      if (x >= ActualPosition.X && x <= ActualPosition.X + ActualWidth)
      {
        if (y >= ActualPosition.Y && y <= ActualPosition.Y + ActualHeight)
        {
          if (!_mouseOver)
          {
            _mouseOver = true;
            FireEvent("OnMouseEnter");
          }
          return;
        }
      }
      if (_mouseOver)
      {
        _mouseOver = false;
        FireEvent("OnMouseLeave");
      }
    }
  }
}
