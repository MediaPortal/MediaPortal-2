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

namespace SkinEngine.Controls.Visuals
{
  public class Button : FrameworkElement
  {
    Property _controlTemplateProperty;

    public Button()
    {
      _controlTemplateProperty = new Property(null);
      IsFocusable = true;
      _controlTemplateProperty.Attach(new PropertyChangedHandler(OnControlTemplateChanged));
    }

    void OnControlTemplateChanged(Property property)
    {
      Invalidate();
    }

    /// <summary>
    /// Gets or sets the control template property.
    /// </summary>
    /// <value>The control template property.</value>
    public Property ControlTemplateProperty
    {
      get
      {
        return _controlTemplateProperty;
      }
      set
      {
        _controlTemplateProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the control template.
    /// </summary>
    /// <value>The control template.</value>
    public UIElement ControlTemplate
    {
      get
      {
        return _controlTemplateProperty.GetValue() as UIElement;
      }
      set
      {
        _controlTemplateProperty.SetValue(value);
      }
    }

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(System.Drawing.Size availableSize)
    {
      base.Measure(availableSize);
      _desiredSize = new System.Drawing.Size((int)Width, (int)Height);
      if (Width == 0) _desiredSize.Width = (int)availableSize.Width;
      if (Height == 0) _desiredSize.Height = (int)availableSize.Height;

      if (ControlTemplate != null)
      {
        ControlTemplate.Measure(_desiredSize);
        _desiredSize = ControlTemplate.DesiredSize;
      }
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.Rectangle finalRect)
    {
      if (ControlTemplate != null)
      {
        ControlTemplate.Arrange(finalRect);
      }
      base.Arrange(finalRect);
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      if (ControlTemplate != null)
      {
        ControlTemplate.DoRender();
      }
      base.DoRender();
    }
  }
}
