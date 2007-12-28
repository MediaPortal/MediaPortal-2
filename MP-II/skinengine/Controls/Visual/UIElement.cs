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
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Visuals
{
  public class UIElement : Visual
  {
    Property _nameProperty;
    Property _isFocusableProperty;
    Property _visibleProperty;
    protected Size _desiredSize;
    protected Size _availableSize;
    Property _acutalPositionProperty;
    Property _positionProperty;
    Property _dockProperty;
    Property _marginProperty;
    bool _isArrangeValid;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIElement"/> class.
    /// </summary>
    public UIElement()
    {
      _nameProperty = new Property("");
      _isFocusableProperty = new Property(false);
      _visibleProperty = new Property((bool)true);
      _acutalPositionProperty = new Property(new Vector3(0, 0, 1));
      _positionProperty = new Property(new Vector3(0, 0, 1));
      _dockProperty = new Property(Dock.Top);
      _marginProperty = new Property(new Vector4(0, 0, 0, 0));

      _visibleProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _positionProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _dockProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _marginProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));

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

    /// <summary>
    /// Gets or sets the actual position property.
    /// </summary>
    /// <value>The actual position property.</value>
    public Property ActualPositionProperty
    {
      get
      {
        return _acutalPositionProperty;
      }
      set
      {
        _acutalPositionProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the actual position.
    /// </summary>
    /// <value>The actual position.</value>
    public Vector3 ActualPosition
    {
      get
      {
        return (Vector3)_acutalPositionProperty.GetValue();
      }
      set
      {
        _acutalPositionProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the name property.
    /// </summary>
    /// <value>The name property.</value>
    public Property NameProperty
    {
      get
      {
        return _nameProperty;
      }
      set
      {
        _nameProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get
      {
        return _nameProperty.GetValue() as string;
      }
      set
      {
        _nameProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the is focusable property.
    /// </summary>
    /// <value>The is focusable property.</value>
    public Property IsFocusableProperty
    {
      get
      {
        return _isFocusableProperty;
      }
      set
      {
        _isFocusableProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the is focusable.
    /// </summary>
    /// <value>The is focusable.</value>
    public bool IsFocusable
    {
      get
      {
        return (bool)_isFocusableProperty.GetValue() ;
      }
      set
      {
        _isFocusableProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the position property.
    /// </summary>
    /// <value>The position property.</value>
    public Property PositionProperty
    {
      get
      {
        return _positionProperty;
      }
      set
      {
        _positionProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the position.
    /// </summary>
    /// <value>The position.</value>
    public Vector3 Position
    {
      get
      {
        return (Vector3)_positionProperty.GetValue();
      }
      set
      {
        _positionProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the dock property.
    /// </summary>
    /// <value>The dock property.</value>
    public Property DockProperty
    {
      get
      {
        return _dockProperty;
      }
      set
      {
        _dockProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the dock.
    /// </summary>
    /// <value>The dock.</value>
    public Dock Dock
    {
      get
      {
        return (Dock)_dockProperty.GetValue();
      }
      set
      {
        _dockProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the is visible property.
    /// </summary>
    /// <value>The is visible property.</value>
    public Property IsVisibleProperty
    {
      get
      {
        return _visibleProperty;
      }
      set
      {
        _visibleProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is visible.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is visible; otherwise, <c>false</c>.
    /// </value>
    public bool IsVisible
    {
      get
      {
        return (bool)_visibleProperty.GetValue();
      }
      set
      {
        _visibleProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the margin property.
    /// </summary>
    /// <value>The margin property.</value>
    public Property MarginProperty
    {
      get
      {
        return _marginProperty;
      }
      set
      {
        _marginProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the margin.
    /// </summary>
    /// <value>The margin.</value>
    public Vector4 Margin
    {
      get
      {
        return (Vector4)_marginProperty.GetValue();
      }
      set
      {
        _marginProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this UIElement has been layout
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this UIElement is arrange valid; otherwise, <c>false</c>.
    /// </value>
    public bool IsArrangeValid
    {
      get
      {
        return _isArrangeValid;
      }
      set
      {
        _isArrangeValid = value;
      }
    }

    /// <summary>
    /// Gets desired size
    /// </summary>
    /// <value>The desired size.</value>
    public Size DesiredSize
    {
      get
      {
        return _desiredSize;
      }
    }
    /// <summary>
    /// Gets the size for brush.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public virtual void GetSizeForBrush(out double width, out double height)
    {
      width = 0.0;
      height = 0.0;
    }

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements. </param>
    /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
    public virtual void Measure(Size availableSize)
    {
      _availableSize = availableSize;
    }

    /// <summary>
    /// Arranges the UI element 
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public virtual void Arrange(Rectangle finalRect)
    {
      IsArrangeValid = true;
    }

    /// <summary>
    /// Invalidates the layout of this uielement.
    /// If dimensions change, it will invalidate the parent visual so 
    /// the parent will re-layout itself and its children
    /// </summary>
    public virtual void Invalidate()
    {
      if (!IsArrangeValid) return;
      if (_availableSize.Width > 0 && _availableSize.Height > 0)
      {
        System.Drawing.Size sizeOld = _desiredSize;
        System.Drawing.Size availsizeOld = _availableSize;
        Measure(_availableSize);
        _availableSize = availsizeOld;
        if (_desiredSize == sizeOld)
        {
          Arrange(new Rectangle((int)ActualPosition.X, (int)ActualPosition.Y, _desiredSize.Width, _desiredSize.Height));
          return;
        }
      }
      if (VisualParent != null)
      {
        VisualParent.Invalidate();
      }
      else
      {
        FrameworkElement element = this as FrameworkElement;
        if (element == null)
        {
          Measure(new Size((int)SkinContext.Width, (int)SkinContext.Height));
          Arrange(new Rectangle(0, 0, (int)SkinContext.Width, (int)SkinContext.Height));
        }
        else
        {
          int w = (int)element.Width;
          int h = (int)element.Height;
          if (w == 0) w = (int)SkinContext.Width;
          if (h == 0) h = (int)SkinContext.Height;
          Measure(new Size(w, h));
          Arrange(new Rectangle((int)element.Position.X, (int)element.Position.Y, w, h));
        }
      }
    }
  }
}
