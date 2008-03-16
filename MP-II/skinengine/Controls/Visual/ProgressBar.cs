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
using System.Collections.Generic;
using System.Text;
using System.Drawing.Drawing2D;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Brushes;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using SkinEngine;
using SkinEngine.DirectX;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Control.InputManager;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = SlimDX.Matrix;

namespace SkinEngine.Controls.Visuals
{
  public class ProgressBar : Control
  {
    Property _valueProperty;
    FrameworkElement _partIndicator;

    public ProgressBar()
    {
      Init();
    }

    public ProgressBar(ProgressBar b)
      : base(b)
    {
      Init(); ;
      Value = b.Value;
    }

    public override object Clone()
    {
      return new ProgressBar(this);
    }

    void Init()
    {
      Focusable = false;
      _valueProperty = new Property(0.0f);
      _valueProperty.Attach(new PropertyChangedHandler(OnValueChanged));
    }

    void OnValueChanged(Property property)
    {
      if (_partIndicator != null)
      {
        double w = this.ActualWidth;
        w /= 100.0f;
        w *= (double)(this.Value);
        _partIndicator.Width = (double)w;

      }
    }


    /// <summary>
    /// Gets or sets the progress value property.
    /// </summary>
    /// <value>The progress value property.</value>
    public Property ValueProperty
    {
      get
      {
        return _valueProperty;
      }
      set
      {
        _valueProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the progress value.
    /// </summary>
    /// <value>The progress value.</value>
    public float Value
    {
      get
      {
        return (float)_valueProperty.GetValue();
      }
      set
      {
        _valueProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      if (_partIndicator == null)
        _partIndicator = FindElement("PART_Indicator") as FrameworkElement;
      base.DoRender();
    }

  }
}

