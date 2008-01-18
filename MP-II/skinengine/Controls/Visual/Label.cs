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
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Core;
using MediaPortal.Core.Properties;
using MediaPortal.Core.Localisation;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using Font = SkinEngine.Fonts.Font;
using FontBufferAsset = SkinEngine.Fonts.FontBufferAsset;
using FontManager = SkinEngine.Fonts.FontManager;

namespace SkinEngine.Controls.Visuals
{
  public class Label : Control
  {
    Property _textProperty;
    Property _colorProperty;
    Property _scrollProperty;
    Property _fontProperty;
    FontBufferAsset _asset;
    StringId _label;
    public Label()
    {
      Init();
      HorizontalAlignment = HorizontalAlignmentEnum.Left;
    }

    public Label(Label lbl)
      : base(lbl)
    {
      Init();
      Text = lbl.Text;
      Color = lbl.Color;
      Scroll = lbl.Scroll;
      Font = lbl.Font;
      _label = new StringId(Text);
    }
    void Init()
    {
      _textProperty = new Property("");
      _colorProperty = new Property(Color.White);
      _scrollProperty = new Property(false);
      _fontProperty = new Property("");
      _fontProperty.Attach(new PropertyChangedHandler(OnFontChanged));
      _textProperty.Attach(new PropertyChangedHandler(OnTextChanged));
    }

    public override object Clone()
    {
      return new Label(this);
    }

    void OnTextChanged(Property prop)
    {
      _label = new StringId(Text);
      Invalidate();
    }
    void OnFontChanged(Property prop)
    {
      _asset = null;
      Font font = FontManager.GetScript(Font);
      if (font != null)
      {
        _asset = ContentManager.GetFont(font);
      }
    }

    public Property FontProperty
    {
      get
      {
        return _fontProperty;
      }
      set
      {
        _fontProperty = value;
      }
    }

    public string Font
    {
      get
      {
        return _fontProperty.GetValue() as string;
      }
      set
      {
        _fontProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the text property.
    /// </summary>
    /// <value>The text property.</value>
    public Property TextProperty
    {
      get
      {
        return _textProperty;
      }
      set
      {
        _textProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    /// <value>The text.</value>
    public string Text
    {
      get
      {
        return _textProperty.GetValue() as string;
      }
      set
      {
        _textProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the color property.
    /// </summary>
    /// <value>The color property.</value>
    public Property ColorProperty
    {
      get
      {
        return _colorProperty;
      }
      set
      {
        _colorProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    /// <value>The color.</value>
    public Color Color
    {
      get
      {
        return (Color)_colorProperty.GetValue();
      }
      set
      {
        _colorProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the scroll property.
    /// </summary>
    /// <value>The scroll.</value>
    public Property ScrollProperty
    {
      get { return _scrollProperty; }
      set { _scrollProperty = value; }
    }

    public bool Scroll
    {
      get { return (bool)_scrollProperty.GetValue(); }
      set { _scrollProperty.SetValue(value); }
    }

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(System.Drawing.SizeF availableSize)
    {
      _desiredSize = new System.Drawing.SizeF((float)Width, (float)Height);
      System.Drawing.SizeF size = new System.Drawing.SizeF(32, 32);
      if (_asset != null)
      {
        float h = _asset.Font.LineHeight;// *1.2f;
        //h -= (_asset.Font.LineHeight - _asset.Font.Base);
        size = new SizeF((float)availableSize.Width, (float)(h));
        if (availableSize.Width == 0)
          size.Width = _asset.Font.AverageWidth * _label.ToString().Length;
      }
      if (Width <= 0)
        _desiredSize.Width = ((float)size.Width) - (float)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = ((float)size.Height) - (float)(Margin.Y + Margin.Z);

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _desiredSize.Width += (float)(Margin.X + Margin.W);
      _desiredSize.Height += (float)(Margin.Y + Margin.Z);
      _originalSize = _desiredSize;


      _availableSize = new SizeF(availableSize.Width, availableSize.Height);
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      System.Drawing.RectangleF layoutRect = new System.Drawing.RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);

      layoutRect.X += (float)(Margin.X);
      layoutRect.Y += (float)(Margin.Y);
      layoutRect.Width -= (float)(Margin.X + Margin.W);
      layoutRect.Height -= (float)(Margin.Y + Margin.Z);
      ActualPosition = new Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      if (!IsArrangeValid)
      {
        IsArrangeValid = true;
        InitializeBindings();
        InitializeTriggers();
      }
      _isLayoutInvalid = false;
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      if (_asset == null) return;
      ColorValue color = ColorConverter.FromColor(this.Color);

      base.DoRender();
      //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
      GraphicsDevice.TransformWorld *= _finalLayoutTransform.Matrix;
      float totalWidth;
      float size = _asset.Font.Size;
      float x = (float)ActualPosition.X;
      float y = (float)ActualPosition.Y;
      float w = (float)ActualWidth;
      float h = (float)ActualHeight;
      _finalLayoutTransform.InvertXY(ref x, ref y);
      _finalLayoutTransform.InvertXY(ref w, ref h);
      System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)x, (int)y, (int)w, (int)h);
      SkinEngine.Fonts.Font.Align align = SkinEngine.Fonts.Font.Align.Left;
      if (HorizontalAlignment == HorizontalAlignmentEnum.Right)
        align = SkinEngine.Fonts.Font.Align.Right;
      else if (HorizontalAlignment == HorizontalAlignmentEnum.Center)
        align = SkinEngine.Fonts.Font.Align.Center;

      if (rect.Height < _asset.Font.LineHeight * 1.2f)
      {
        rect.Height = (int)(_asset.Font.LineHeight * 1.2f);
      }
      if (VerticalAlignment == VerticalAlignmentEnum.Center)
      {
        rect.Y = (int)(y + (h - _asset.Font.LineHeight) / 2.0);
      }

      //rect.Y -= (int)(_asset.Font.LineHeight - _asset.Font.Base);

      _asset.Draw(_label.ToString(), rect, align, size, color, Scroll, out totalWidth);
    }
  }
}

