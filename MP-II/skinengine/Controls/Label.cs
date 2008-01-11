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
using System.Drawing;
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using SkinEngine.Fonts;
using SkinEngine.Properties;
using Font = SkinEngine.Fonts.Font;

namespace SkinEngine.Controls
{
  public class Label : Control
  {
    #region variables

    private Property _label;
    private Property _scroll;
    private FontBufferAsset _font;
    private Font.Align _align = Font.Align.Left;
    private DateTime _timer = DateTime.MinValue;
    float _autoWidth;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Label"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public Label(Control parent)
      : base(parent)
    {
      _label = new Property("");
      _scroll = new Property(false);
    }

    /// <summary>
    /// Gets or sets the alignment.
    /// </summary>
    /// <value>The align.</value>
    public Font.Align Align
    {
      get { return _align; }
      set { _align = value; }
    }

    /// <summary>
    /// Gets or sets the scroll property.
    /// </summary>
    /// <value>The scroll.</value>
    public Property ScrollProperty
    {
      get { return _scroll; }
      set { _scroll = value; }
    }

    public bool Scroll
    {
      get { return (bool)_scroll.GetValue(); }
      set { _scroll.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the font asset for this label.
    /// </summary>
    /// <value>The font asset.</value>
    public FontBufferAsset FontAsset
    {
      get { return _font; }
      set { _font = value; }
    }

    /// <summary>
    /// Gets or sets the text property.
    /// </summary>
    /// <value>The text.</value>
    public string Text
    {
      get
      {
        object obj = _label.GetValue();
        if (obj == null)
        {
          return "";
        }
        return obj.ToString();
      }
      set { _label.SetValue(value); }
    }

    public Property LabelProperty
    {
      get { return _label; }
      set { _label = value; }
    }

    /// <summary>
    /// Renders the label
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public override void Render(uint timePassed)
    {
      if (_font == null)
      {
        return;
      }


      if (!IsVisible)
      {
        if (!IsAnimating)
        {
          return;
        }
      }
      base.Render(timePassed);

      RectangleF rect = new RectangleF(0, 0, 700, 100);
      Vector3 pos = Position;
      rect.X = (int)pos.X;
      rect.Y = (int)pos.Y;
      rect.Width = (int)_width;
      if (rect.Width == 0)
        rect.Width = 800;
      rect.Height = (int)Height;
      if (rect.Height < _font.Font.LineHeight * 1.2f)
      {
        rect.Height = _font.Font.LineHeight * 1.2f;
      }
      rect.Y -= (_font.Font.LineHeight - _font.Font.Base);
      float size = _font.Font.Size;

      GraphicsDevice.Device.Transform.World = SkinContext.FinalMatrix.Matrix;
      float alpha = Color.Alpha * AlphaMask.X;
      if (SkinContext.TemporaryTransform != null)
      {
        GraphicsDevice.Device.Transform.World *= SkinContext.TemporaryTransform.Matrix;
        alpha *= SkinContext.TemporaryTransform.Alpha.X;
      }
      ColorValue color = new ColorValue(Color.Red, Color.Green, Color.Blue, alpha * SkinContext.FinalMatrix.Alpha.X);

      float totalWidth;
      _font.Draw(Text, rect, Align, size, color, Scroll, out totalWidth);
      if (_width == 0 && totalWidth != _autoWidth)
      {
        _autoWidth = totalWidth;
        //DoLayout();
      }
    }

    public override void Reset()
    {
      Dependency depend = _label as Dependency;
      if (depend != null)
      {
        depend.Reset();
      }
      base.Reset();
    }
  }
}
