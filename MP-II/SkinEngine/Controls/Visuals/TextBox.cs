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
using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.Controls.Brushes;
using SlimDX;
using Font = MediaPortal.SkinEngine.Fonts.Font;
using FontRender = MediaPortal.SkinEngine.ContentManagement.FontRender;
using FontBufferAsset = MediaPortal.SkinEngine.ContentManagement.FontBufferAsset;
using FontManager = MediaPortal.SkinEngine.Fonts.FontManager;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class TextBox : Control
  {
    #region Private fields

    Property _caretIndexProperty;
    Property _textProperty;
    Property _textWrappingProperty;
    Property _colorProperty;
    FontBufferAsset _asset;
    FontRender _renderer;

    // If we are editing the text.
    bool _editText = false; 

    #endregion

    #region Ctor

    public TextBox()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _caretIndexProperty = new Property(typeof(int), 0);
      _textProperty = new Property(typeof(string), "");
      _colorProperty = new Property(typeof(Color), Color.Black);

      _textWrappingProperty = new Property(typeof(string), "");

      // Yes, we can have focus
      Focusable = true;

      // Set-up default fill
      SolidColorBrush background = new SolidColorBrush();
      background.Color = Color.White;
      Background = background;

      // Set-up default border
      SolidColorBrush border = new SolidColorBrush();
      border.Color = Color.Black;
      BorderBrush = border;

      HorizontalAlignment = HorizontalAlignmentEnum.Left;
    }

    void Attach()
    {
      _textProperty.Attach(OnTextChanged);
      _colorProperty.Attach(OnColorChanged);
    }

    void Detach()
    {
      _textProperty.Detach(OnTextChanged);
      _colorProperty.Detach(OnColorChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      TextBox t = (TextBox) source;
      Text = copyManager.GetCopy(t.Text);
      Color = copyManager.GetCopy(t.Color);
      TextWrapping = copyManager.GetCopy(t.TextWrapping);
      CaretIndex = copyManager.GetCopy(t.CaretIndex);
      Attach();
    }

    #endregion

    void OnColorChanged(Property prop)
    {
      if (Screen != null) 
        Screen.Invalidate(this);
    }

    void OnTextChanged(Property prop)
    {
      // The skin is setting the text, also update the caret
      if (!_editText)
      {
        CaretIndex = Text.Length;
      }
      if (Screen != null)
        Screen.Invalidate(this);
    }

    protected override void OnFontChanged(Property prop)
    {
      if (_asset != null)
      {
        _asset.Free(true);
        ContentManager.Remove(_asset);
      }

      _asset = null;
      if (Screen != null) 
        Screen.Invalidate(this);
    }

    // We need to override this one, so we can subscribe to raw data.
    public override bool HasFocus
    {
      get { return base.HasFocus; }
      set
      {
        base.HasFocus = value;
        IInputManager manager = ServiceScope.Get<IInputManager>();
        
        // We now have focus, so set that we need raw data
        manager.NeedRawKeyData = value;
      }
    }

    public Property CaretIndexProperty
    {
      get { return _caretIndexProperty; }
    }

    public int CaretIndex
    {
      get { return (int)_caretIndexProperty.GetValue(); }
      set { _caretIndexProperty.SetValue(value); }
    }

    public Property TextWrappingProperty
    {
      get { return _textWrappingProperty; }
    }

    public string TextWrapping
    {
      get { return _textWrappingProperty.GetValue() as string; }
      set { _textWrappingProperty.SetValue(value); }
    }

    public Property TextProperty
    {
      get { return _textProperty; }
    }

    public string Text
    {
      get { return _textProperty.GetValue() as string; }
      set { _textProperty.SetValue(value); }
    }

    public Property ColorProperty
    {
      get { return _colorProperty; }
    }

    public Color Color
    {
      get { return (Color) _colorProperty.GetValue(); }
      set { _colorProperty.SetValue(value); }
    }

    void AllocFont()
    {
      if (_asset == null)
      {
        // Get default values if not set
        if (FontSize == 0)
          FontSize = FontManager.DefaultFontSize;
        if (FontFamily == string.Empty)
          FontFamily = FontManager.DefaultFontFamily;

        // We want to select the font based on the maximum zoom height (fullscreen)
        // This means that the font will be scaled down in windowed mode, but look
        // good in full screen. 
        Font font = FontManager.GetScript(FontFamily, (int)(FontSize * SkinContext.MaxZoomHeight));
        if (font != null)
        {
          _asset = ContentManager.GetFont(font);
        }
      }

      if (_renderer == null)
        _renderer = new FontRender(_asset.Font);
    }

    public override void Measure(ref SizeF totalSize)
    {
      SizeF childSize = new SizeF();
     
      InitializeTriggers();
      AllocFont();

      if (_asset != null)
      {
        childSize = new SizeF(_asset.Font.Width(Text, FontSize) * SkinContext.Zoom.Width,
                 _asset.Font.LineHeight(FontSize) * SkinContext.Zoom.Height);
      }
      _desiredSize = new SizeF((float) Width * SkinContext.Zoom.Width, (float) Height * SkinContext.Zoom.Height);

      if (Double.IsNaN(Width))
        _desiredSize.Width = childSize.Width;

      if (Double.IsNaN(Height))
        _desiredSize.Height = childSize.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("TextBox.Measure: {0} returns {1}x{2}", this.Name, (int)totalSize.Width, (int)totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("TextBox.Arrange: {0} X {1} Y {2} W {3} H {4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));

      ComputeInnerRectangle(ref finalRect);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      IsInvalidLayout = false;
    
      if (Screen != null)
        Screen.Invalidate(this);
    }

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      if (_asset == null) return;
      AllocFont();
      Color4 color = ColorConverter.FromColor(Color);

      base.DoRender();
      float totalWidth;

      // The characters fits the textbox exactly, so to get some room between the top of the characters 
      // and the inner rectangle. Move the text down (10% of font size) also reduce the font size to 90%
      // of the value. Otherwise we will be outside of the inner rectangle.
      float x = ActualPosition.X;
      float y = ActualPosition.Y + 0.1f * FontSize * SkinContext.Zoom.Height;
      float w = (float)ActualWidth;
      float h = (float)ActualHeight;
      if (_finalLayoutTransform != null)
      {
        GraphicsDevice.TransformWorld *= _finalLayoutTransform.Matrix;

        _finalLayoutTransform.InvertXY(ref x, ref y);
        _finalLayoutTransform.InvertXY(ref w, ref h);
      }
      Rectangle rect = new Rectangle((int)x, (int)y, (int)w, (int)h);
      Font.Align align = Font.Align.Left;
      if (HorizontalAlignment == HorizontalAlignmentEnum.Right)
        align = Font.Align.Right;
      else if (HorizontalAlignment == HorizontalAlignmentEnum.Center)
        align = Font.Align.Center;

      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix = Matrix.Translation(-rect.X, -rect.Y, 0);
      m.Matrix *= Matrix.Scaling(SkinContext.Zoom.Width, SkinContext.Zoom.Height, 1);
      m.Matrix *= Matrix.Translation(rect.X, rect.Y, 0);
      SkinContext.AddTransform(m);
      color.Alpha *= (float) SkinContext.Opacity;
      color.Alpha *= (float) Opacity;

      _renderer.Draw(Text, rect, ActualPosition.Z, align, FontSize * 0.9f, color, false, out totalWidth);
      SkinContext.RemoveTransform();
    }

    public override void DestroyRenderTree()
    {
      if (_renderer != null)
        _renderer.Free();
      _renderer = null;
    }

    public override void DoRender()
    {
      if (_asset == null)
        return;
      Color4 color = ColorConverter.FromColor(Color);

      base.DoRender();
      float totalWidth;

      // The characters fits the textbox exactly, so to get some room between the top of the characters 
      // and the inner rectangle. Move the text down (10% of font size) also reduce the font size to 90%
      // of the value. Otherwise we will be outside of the inner rectangle.
 
      float y = ActualPosition.Y + 0.1f * FontSize * SkinContext.Zoom.Height;
      float x = ActualPosition.X;
      float w = (float)ActualWidth;
      float h = (float)ActualHeight;
      if (_finalLayoutTransform != null)
      {
        GraphicsDevice.TransformWorld *= _finalLayoutTransform.Matrix;

        _finalLayoutTransform.InvertXY(ref x, ref y);
        _finalLayoutTransform.InvertXY(ref w, ref h);
      }
      Rectangle rect = new Rectangle((int) x, (int) y, (int) w, (int) h);
      Font.Align align = Font.Align.Left;
      if (HorizontalAlignment == HorizontalAlignmentEnum.Right)
        align = Font.Align.Right;
      else if (HorizontalAlignment == HorizontalAlignmentEnum.Center)
        align = Font.Align.Center;

      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix = Matrix.Translation(-rect.X, -rect.Y, 0);
      m.Matrix *= Matrix.Scaling(SkinContext.Zoom.Width, SkinContext.Zoom.Height, 1);
      m.Matrix *= Matrix.Translation(rect.X, rect.Y, 0);
      SkinContext.AddTransform(m);
      color.Alpha *= (float) SkinContext.Opacity;
      color.Alpha *= (float) Opacity;
      _asset.Draw(Text, rect, align, FontSize * 0.9f, color, false, out totalWidth);
      SkinContext.RemoveTransform();
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (_asset != null)
      {
        ContentManager.Remove(_asset);
        _asset.Free(true);
        _asset = null;
      }
      if (_renderer != null)
        _renderer.Free();
      _renderer = null;
    }

    public override void BecomesHidden()
    {
      if (_renderer != null)
        _renderer.Free();
    }

    public override void BecomesVisible()
    {

      if (_renderer != null)
      {
        _renderer.Alloc();
        DoBuildRenderTree();
      }
    }
    
    public override void Update()
    {
      base.Update();
      if (!_hidden)
        DoBuildRenderTree();
    }

    public override void OnKeyPressed(ref Key key)
    {
      if (!HasFocus) 
        return;
      if (key == Key.None)
        return;
     
      _editText = true;
      if (key == Key.BackSpace)
      {
        if (CaretIndex > 0)
        {
          Text = Text.Remove(CaretIndex - 1, 1);
          CaretIndex = CaretIndex - 1;
        }
        key = Key.None;
      }
      else if (key == Key.Left)
      {
        if (CaretIndex > 0)
          CaretIndex = CaretIndex - 1;
        key = Key.None;
      }
      else if (key == Key.Right)
      {
        if (CaretIndex < Text.Length)
          CaretIndex = CaretIndex + 1;
        key = Key.None;
      }
      else if (key == Key.Home)
      {
        CaretIndex = 0;
        key = Key.None;
      }
      else if (key == Key.End)
      {
        CaretIndex = Text.Length;
        key = Key.None;
      } 
      else if (key != Key.Up && 
               key != Key.Down &&
               key != Key.Enter)
      {
        Text = Text.Insert(CaretIndex, key.Name);
        CaretIndex = CaretIndex + 1;
        key = Key.None;
      }

      _editText = false;
    }
  }
}

