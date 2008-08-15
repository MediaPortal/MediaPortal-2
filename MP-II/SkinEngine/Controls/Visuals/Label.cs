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
using System.Drawing;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localisation;
using SlimDX;
using Font = MediaPortal.SkinEngine.Fonts.Font;
using FontRender = MediaPortal.SkinEngine.Fonts.FontRender;
using FontBufferAsset = MediaPortal.SkinEngine.Fonts.FontBufferAsset;
using FontManager = MediaPortal.SkinEngine.Fonts.FontManager;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class Label : Control
  {
    #region Private fields

    Property _textProperty;
    Property _colorProperty;
    Property _scrollProperty;
    FontBufferAsset _asset;
    FontRender _renderer;
    StringId _label;
    bool _update = false;

    #endregion

    #region Ctor

    public Label()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _textProperty = new Property(typeof(string), "");
      _colorProperty = new Property(typeof(Color), Color.White);
      _scrollProperty = new Property(typeof(bool), false);

      HorizontalAlignment = HorizontalAlignmentEnum.Left;
    }

    void Attach()
    {
      _textProperty.Attach(OnTextChanged);
      _scrollProperty.Attach(OnScrollChanged);
      _colorProperty.Attach(OnColorChanged);
    }

    void Detach()
    {
      _textProperty.Detach(OnTextChanged);
      _scrollProperty.Detach(OnScrollChanged);
      _colorProperty.Detach(OnColorChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Label l = source as Label;
      Text = copyManager.GetCopy(l.Text);
      Color = copyManager.GetCopy(l.Color);
      Scroll = copyManager.GetCopy(l.Scroll);

      _label = new StringId(Text);
      Attach();
    }

    #endregion

    void OnColorChanged(Property prop)
    {
      _update = true;
      if (Screen != null) Screen.Invalidate(this);
    }

    void OnTextChanged(Property prop)
    {
      _label = new StringId(Text);
      _update = true;
      if (Screen != null) Screen.Invalidate(this);
      // Invalidate();
    }

    void OnScrollChanged(Property prop)
    {
      _update = true;
      if (Screen != null) Screen.Invalidate(this);
    }

    protected override void OnFontChanged(Property prop)
    {
      if (_asset != null)
      {
        _asset.Free(true);
        ContentManager.Remove(_asset);
      }

      _asset = null;
      _update = true;
      if (Screen != null) Screen.Invalidate(this);
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

    public Property ScrollProperty
    {
      get { return _scrollProperty; }
    }

    public bool Scroll
    {
      get { return (bool) _scrollProperty.GetValue(); }
      set { _scrollProperty.SetValue(value); }
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

      // Measure the text
      if (_label != null && _asset != null)
      {
        childSize = new SizeF(_asset.Font.Width(_label.ToString(), FontSize) * SkinContext.Zoom.Width,
                         _asset.Font.LineHeight(FontSize) * SkinContext.Zoom.Height);
      }

      _desiredSize = new SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);

      if (Double.IsNaN(Width))
        _desiredSize.Width = childSize.Width;

      if (Double.IsNaN(Height))
        _desiredSize.Height = childSize.Height;

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

      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("Label.measure :{0} returns {1}x{2}", _label.ToString(), (int)totalSize.Width, (int)totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect, float zOrder)
    {
      //Trace.WriteLine(String.Format("Label.Arrange :{0} X {1},Y {2},Z {3} W {4}xH {5}", _label.ToString(), (int)finalRect.X, (int)finalRect.Y, zOrder, (int)finalRect.Width, (int)finalRect.Height));

      ComputeInnerRectangle(ref finalRect);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, zOrder);
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
      IsArrangeValid = true;
      IsInvalidLayout = false;
      _update = true;

      if (Screen != null)
        Screen.Invalidate(this);
    }

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      if (_asset == null) return;
      //AllocFont();
      ColorValue color = ColorConverter.FromColor(this.Color);

      base.DoRender();
      //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
      float totalWidth;

      float x = (float)ActualPosition.X;
      float y = (float)ActualPosition.Y;
      float w = (float)ActualWidth;
      float h = (float)ActualHeight;
      if (_finalLayoutTransform != null)
      {
        GraphicsDevice.TransformWorld *= _finalLayoutTransform.Matrix;

        _finalLayoutTransform.InvertXY(ref x, ref y);
        _finalLayoutTransform.InvertXY(ref w, ref h);
      }
      RectangleF rect = new RectangleF(x, y, w, h);
      SkinEngine.Fonts.Font.Align align = SkinEngine.Fonts.Font.Align.Left;
      if (HorizontalAlignment == HorizontalAlignmentEnum.Right)
        align = SkinEngine.Fonts.Font.Align.Right;
      else if (HorizontalAlignment == HorizontalAlignmentEnum.Center)
        align = SkinEngine.Fonts.Font.Align.Center;

      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix = Matrix.Translation((float)-rect.X, (float)-rect.Y, 0);
      m.Matrix *= Matrix.Scaling(SkinContext.Zoom.Width, SkinContext.Zoom.Height, 1);
      m.Matrix *= Matrix.Translation((float)rect.X, (float)rect.Y, 0);
      SkinContext.AddTransform(m);
      color.Alpha *= (float)SkinContext.Opacity;
      color.Alpha *= (float)this.Opacity;
      if (_label != null)
        _renderer.Draw(_label.ToString(), rect, ActualPosition.Z, align, FontSize, color, Scroll, out totalWidth);
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
      if (SkinContext.UseBatching == false)
      {
        if (_asset == null) 
          return;
        ColorValue color = ColorConverter.FromColor(this.Color);

        base.DoRender();
        float totalWidth;
   
        float x = _finalRect.X;
        float y = _finalRect.Y;
        float w = _finalRect.Width;
        float h = _finalRect.Height;

        if (_finalLayoutTransform != null)
        {
          GraphicsDevice.TransformWorld *= _finalLayoutTransform.Matrix;

          _finalLayoutTransform.InvertXY(ref x, ref y);
          _finalLayoutTransform.InvertXY(ref w, ref h);
        }
        System.Drawing.RectangleF rect = new System.Drawing.RectangleF(x, y, w, h);
        SkinEngine.Fonts.Font.Align align = SkinEngine.Fonts.Font.Align.Left;
        if (HorizontalAlignment == HorizontalAlignmentEnum.Right)
          align = SkinEngine.Fonts.Font.Align.Right;
        else if (HorizontalAlignment == HorizontalAlignmentEnum.Center)
          align = SkinEngine.Fonts.Font.Align.Center;

        ExtendedMatrix m = new ExtendedMatrix();
        m.Matrix = Matrix.Translation(-rect.X, -rect.Y, 0);
        m.Matrix *= Matrix.Scaling(SkinContext.Zoom.Width, SkinContext.Zoom.Height, 1);
        m.Matrix *= Matrix.Translation(rect.X, rect.Y, 0);
        SkinContext.AddTransform(m);
        color.Alpha *= (float)SkinContext.Opacity;
        color.Alpha *= (float)this.Opacity;

        if (_label != null)
        {
          _asset.Draw(_label.ToString(), rect, align, FontSize, color, Scroll, out totalWidth);
        }
        SkinContext.RemoveTransform();
      }
      else
      {
        if (_asset == null) return;
        ColorValue color = ColorConverter.FromColor(this.Color);

        base.DoRender();
        float totalWidth;
        float x = (float)ActualPosition.X;
        float y = (float)ActualPosition.Y;
        float w = (float)ActualWidth;
        float h = (float)ActualHeight;
        if (_finalLayoutTransform != null)
        {
          GraphicsDevice.TransformWorld *= _finalLayoutTransform.Matrix;

          _finalLayoutTransform.InvertXY(ref x, ref y);
          _finalLayoutTransform.InvertXY(ref w, ref h);
        }
        Rectangle rect = new Rectangle((int)x, (int)y, (int)w, (int)h);
        SkinEngine.Fonts.Font.Align align = SkinEngine.Fonts.Font.Align.Left;
        if (HorizontalAlignment == HorizontalAlignmentEnum.Right)
          align = SkinEngine.Fonts.Font.Align.Right;
        else if (HorizontalAlignment == HorizontalAlignmentEnum.Center)
          align = SkinEngine.Fonts.Font.Align.Center;

        ExtendedMatrix m = new ExtendedMatrix();
        m.Matrix = Matrix.Translation((float)-rect.X, (float)-rect.Y, 0);
        m.Matrix *= Matrix.Scaling(SkinContext.Zoom.Width, SkinContext.Zoom.Height, 1);
        m.Matrix *= Matrix.Translation((float)rect.X, (float)rect.Y, 0);
        SkinContext.AddTransform(m);
        color.Alpha *= (float)SkinContext.Opacity;
        color.Alpha *= (float)this.Opacity;
        if (_label != null)
          _renderer.Draw(_label.ToString(), rect, ActualPosition.Z, align, FontSize, color, Scroll, out totalWidth);
        SkinContext.RemoveTransform();
      }
    }

    public override void Deallocate()
    {
      //Trace.WriteLine("lbl Deallocate:" + Text);
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
     // Trace.WriteLine("lbl BecomesHidden:" + Text);
      if (_renderer != null)
        _renderer.Free();
    }

    public override void BecomesVisible()
    {
      //Trace.WriteLine("lbl BecomesVisible:" + Text);
      if (_renderer != null)
      {
        _renderer.Alloc();
        DoBuildRenderTree();
      }
    }
    
    public override void Update()
    {
      base.Update();
      if (_hidden == false)
      {
        if (_update && _renderer != null)
        {
          DoBuildRenderTree();
        }
      }
      _update = false;
    }
  }
}

