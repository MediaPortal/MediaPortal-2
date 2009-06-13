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

using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.ContentManagement;
using SlimDX;
using Font = MediaPortal.SkinEngine.Fonts.Font;
using FontRender = MediaPortal.SkinEngine.ContentManagement.FontRender;
using FontBufferAsset = MediaPortal.SkinEngine.ContentManagement.FontBufferAsset;
using FontManager = MediaPortal.SkinEngine.Fonts.FontManager;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class TextControl : Control
  {
    #region Protected fields

    protected Property _caretIndexProperty;
    protected Property _textProperty;
    protected Property _colorProperty;
    protected Property _preferredTextLengthProperty;
    protected Property _textAlignProperty;
    protected FontBufferAsset _asset;
    protected FontRender _renderer;
    protected int _fontSizeCache;
    protected bool _performLayout;

    // Are we editing the text?
    bool _editText = false; 

    #endregion

    #region Ctor

    public TextControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _caretIndexProperty = new Property(typeof(int), 0);
      _textProperty = new Property(typeof(string), "");
      _colorProperty = new Property(typeof(Color), Color.Black);

      _preferredTextLengthProperty = new Property(typeof(int?), null);
      _textAlignProperty = new Property(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Left);

      // Yes, we can have focus
      Focusable = true;
    }

    void Attach()
    {
      _textProperty.Attach(OnTextChanged);
      _colorProperty.Attach(OnColorChanged);
      _preferredTextLengthProperty.Attach(OnPreferredTextLengthChanged);
      _textAlignProperty.Attach(OnTextAlignChanged);
    }

    void Detach()
    {
      _textProperty.Detach(OnTextChanged);
      _colorProperty.Detach(OnColorChanged);
      _preferredTextLengthProperty.Detach(OnPreferredTextLengthChanged);
      _textAlignProperty.Detach(OnTextAlignChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      TextControl tc = (TextControl) source;
      Text = copyManager.GetCopy(tc.Text);
      Color = copyManager.GetCopy(tc.Color);
      CaretIndex = copyManager.GetCopy(tc.CaretIndex);
      Attach();
    }

    #endregion

    void OnColorChanged(Property prop, object oldValue)
    {
      if (Screen != null)
        Screen.Invalidate(this);
    }

    void OnTextAlignChanged(Property prop, object oldValue)
    {
      if (Screen != null)
        Screen.Invalidate(this);
    }

    void OnTextChanged(Property prop, object oldValue)
    {
      // The skin is setting the text, also update the caret
      if (!_editText)
        CaretIndex = Text.Length;
      if (Screen != null)
        Screen.Invalidate(this);
    }

    void OnPreferredTextLengthChanged(Property prop, object oldValue)
    {
      Invalidate();
    }

    protected override void OnFontChanged(Property prop, object oldValue)
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

    public override void OnKeyPreview(ref Key key)
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
          CaretIndex--;
        }
        key = Key.None;
      }
      else if (key == Key.Left)
      {
        if (CaretIndex > 0)
        {
          CaretIndex--;
          // Only consume the key if we can move the cared - else the key can be used by
          // the focus management, for example
          key = Key.None;
        }
      }
      else if (key == Key.Right)
      {
        if (CaretIndex < Text.Length)
        {
          CaretIndex++;
          // Only consume the key if we can move the cared - else the key can be used by
          // the focus management, for example
          key = Key.None;
        }
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
      else if (key.IsPrintableKey)
      {
        Text = Text.Insert(CaretIndex, key.RawCode.Value.ToString());
        CaretIndex++;
        key = Key.None;
      }

      _editText = false;
    }

    public Property PreferredTextLengthProperty
    {
      get { return _preferredTextLengthProperty; }
    }

    /// <summary>
    /// Gets or sets the preferred length of the text in this <see cref="TextControl"/>.
    /// </summary>
    public int? PreferredTextLength
    {
      get { return (int?) _preferredTextLengthProperty.GetValue(); }
      set { _preferredTextLengthProperty.SetValue(value); }
    }

    public Property CaretIndexProperty
    {
      get { return _caretIndexProperty; }
    }

    public int CaretIndex
    {
      get { return (int) _caretIndexProperty.GetValue(); }
      set { _caretIndexProperty.SetValue(value); }
    }

    public Property TextProperty
    {
      get { return _textProperty; }
    }

    public string Text
    {
      get { return (string) _textProperty.GetValue(); }
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

    public Property TextAlignProperty
    {
      get { return _textAlignProperty; }
    }

    public HorizontalAlignmentEnum TextAlign
    {
      get { return (HorizontalAlignmentEnum) _textAlignProperty.GetValue(); }
      set { _textAlignProperty.SetValue(value); }
    }

    void AllocFont()
    {
      if (_asset == null)
      {
        // We want to select the font based on the maximum zoom height (fullscreen)
        // This means that the font will be scaled down in windowed mode, but look
        // good in full screen. 
        Font font = FontManager.GetScript(GetFontFamilyOrInherited(), (int) (_fontSizeCache * SkinContext.MaxZoomHeight));
        if (font != null)
          _asset = ContentManager.GetFont(font);
      }

      if (_renderer == null)
        _renderer = new FontRender(_asset.Font);
    }

    public override void Measure(ref SizeF totalSize)
    {
      RemoveMargin(ref totalSize);
      InitializeTriggers();

      _fontSizeCache = GetFontSizeOrInherited();
      AllocFont();

      SizeF childSize;
      if (_asset != null)
        childSize = new SizeF(_asset.Font.Width(Text, _fontSizeCache) * SkinContext.Zoom.Width,
            _asset.Font.LineHeight(_fontSizeCache) * SkinContext.Zoom.Height);
      else
        childSize = new SizeF();

      _desiredSize = new SizeF((float) Width * SkinContext.Zoom.Width, (float) Height * SkinContext.Zoom.Height);

      if (double.IsNaN(Width))
      {
        if (PreferredTextLength.HasValue)
          // We use the "W" character as the character which needs the most space in X-direction
          _desiredSize.Width = PreferredTextLength.Value*_asset.Font.Width("W", _fontSizeCache);
        else
          _desiredSize.Width = childSize.Width;
      }

      if (double.IsNaN(Height))
        _desiredSize.Height = childSize.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();

      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("TextControl.Measure: {0} returns {1}x{2}", this.Name, (int)totalSize.Width, (int)totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("TextControl.Arrange: {0} X {1} Y {2} W {3} H {4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));

      RemoveMargin(ref finalRect);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

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

      float x = ActualPosition.X;
      float y = ActualPosition.Y;
      float w = (float) ActualWidth;
      float h = (float) ActualHeight;
      if (_finalLayoutTransform != null)
      {
        GraphicsDevice.TransformWorld *= _finalLayoutTransform.Matrix;

        _finalLayoutTransform.InvertXY(ref x, ref y);
        _finalLayoutTransform.InvertXY(ref w, ref h);
      }
      Rectangle rect = new Rectangle((int) x, (int) y, (int) w, (int) h);
      Font.Align align = Font.Align.Left;
      if (TextAlign == HorizontalAlignmentEnum.Right)
        align = Font.Align.Right;
      else if (TextAlign == HorizontalAlignmentEnum.Center)
        align = Font.Align.Center;

      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix = Matrix.Translation(-rect.X, -rect.Y, 0);
      m.Matrix *= Matrix.Scaling(SkinContext.Zoom.Width, SkinContext.Zoom.Height, 1);
      m.Matrix *= Matrix.Translation(rect.X, rect.Y, 0);
      SkinContext.AddTransform(m);
      color.Alpha *= (float) SkinContext.Opacity;
      color.Alpha *= (float) Opacity;

      _renderer.Draw(Text, rect, ActualPosition.Z, align, _fontSizeCache, color, false, out totalWidth);
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

      float y = ActualPosition.Y;
      float x = ActualPosition.X;
      float w = (float) ActualWidth;
      float h = (float) ActualHeight;
      if (_finalLayoutTransform != null)
      {
        GraphicsDevice.TransformWorld *= _finalLayoutTransform.Matrix;

        _finalLayoutTransform.InvertXY(ref x, ref y);
        _finalLayoutTransform.InvertXY(ref w, ref h);
      }
      Rectangle rect = new Rectangle((int) x, (int) y, (int) w, (int) h);
      Font.Align align = Font.Align.Left;
      if (TextAlign == HorizontalAlignmentEnum.Right)
        align = Font.Align.Right;
      else if (TextAlign == HorizontalAlignmentEnum.Center)
        align = Font.Align.Center;

      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix = Matrix.Translation(-rect.X, -rect.Y, 0);
      m.Matrix *= Matrix.Scaling(SkinContext.Zoom.Width, SkinContext.Zoom.Height, 1);
      m.Matrix *= Matrix.Translation(rect.X, rect.Y, 0);
      SkinContext.AddTransform(m);
      color.Alpha *= (float) SkinContext.Opacity;
      color.Alpha *= (float) Opacity;
      _asset.Draw(Text, rect, align, _fontSizeCache, color, false, out totalWidth);
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
  }
}

