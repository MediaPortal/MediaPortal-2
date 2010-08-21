#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using SlimDX;
using Font = MediaPortal.UI.SkinEngine.Fonts.Font;
using FontBufferAsset = MediaPortal.UI.SkinEngine.ContentManagement.FontBufferAsset;
using FontManager = MediaPortal.UI.SkinEngine.Fonts.FontManager;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class TextControl : Control
  {
    #region Protected fields

    protected AbstractProperty _caretIndexProperty;
    protected AbstractProperty _textProperty;
    protected AbstractProperty _colorProperty;
    protected AbstractProperty _preferredTextLengthProperty;
    protected AbstractProperty _textAlignProperty;
    protected FontBufferAsset _asset;
    protected int _fontSizeCache;

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
      _caretIndexProperty = new SProperty(typeof(int), 0);
      _textProperty = new SProperty(typeof(string), string.Empty);
      _colorProperty = new SProperty(typeof(Color), Color.Black);

      _preferredTextLengthProperty = new SProperty(typeof(int?), null);
      _textAlignProperty = new SProperty(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Left);

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
      Text = tc.Text;
      Color = tc.Color;
      CaretIndex = tc.CaretIndex;
      Attach();
    }

    #endregion

    void OnColorChanged(AbstractProperty prop, object oldValue)
    {
    }

    void OnTextAlignChanged(AbstractProperty prop, object oldValue)
    {
    }

    void OnTextChanged(AbstractProperty prop, object oldValue)
    {
      // The skin is setting the text, also update the caret
      if (!_editText)
        CaretIndex = Text.Length;
    }

    void OnPreferredTextLengthChanged(AbstractProperty prop, object oldValue)
    {
      InvalidateLayout();
      InvalidateParentLayout();
    }

    protected override void OnFontChanged(AbstractProperty prop, object oldValue)
    {
      if (_asset != null)
      {
        _asset.Free(true);
        ContentManager.Remove(_asset);
      }

      _asset = null;
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

    public AbstractProperty PreferredTextLengthProperty
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

    public AbstractProperty CaretIndexProperty
    {
      get { return _caretIndexProperty; }
    }

    public int CaretIndex
    {
      get { return (int) _caretIndexProperty.GetValue(); }
      set { _caretIndexProperty.SetValue(value); }
    }

    public AbstractProperty TextProperty
    {
      get { return _textProperty; }
    }

    public string Text
    {
      get { return (string) _textProperty.GetValue(); }
      set { _textProperty.SetValue(value); }
    }

    public AbstractProperty ColorProperty
    {
      get { return _colorProperty; }
    }

    public Color Color
    {
      get { return (Color) _colorProperty.GetValue(); }
      set { _colorProperty.SetValue(value); }
    }

    public AbstractProperty TextAlignProperty
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
    }

    public override void OnKeyPressed(ref Key key)
    {
      base.OnKeyPressed(ref key);
      if (HasFocus && key == Key.Ok)
      {
        key = Key.None;
        Screen screen = Screen;
        if (screen != null)
          screen.ShowVirtualKeyboard(_textProperty, new VirtualKeyboardSettings
            {
                ElementArrangeBounds = ActualBounds,
                TextStyle = VirtualKeyboardTextStyle.None
            });
      }
    }

    protected override SizeF CalculateDesiredSize(SizeF totalSize)
    {
      _fontSizeCache = GetFontSizeOrInherited();
      AllocFont();

      SizeF childSize = _asset == null ? new SizeF() :
          new SizeF(_asset.Font.Width(Text, _fontSizeCache), _asset.Font.LineHeight(_fontSizeCache));

      if (PreferredTextLength.HasValue && _asset != null)
        // We use the "W" character as the character which needs the most space in X-direction
        childSize.Width = PreferredTextLength.Value * _asset.Font.Width("W", _fontSizeCache);

      return childSize;
    }

    public override void DoRender(RenderContext localRenderContext)
    {
      if (_asset == null)
        return;
      Color4 color = ColorConverter.FromColor(Color);

      base.DoRender(localRenderContext);
      float totalWidth;

      float y = ActualPosition.Y;
      float x = ActualPosition.X;
      float w = (float) ActualWidth;
      float h = (float) ActualHeight;

      Rectangle rect = new Rectangle((int) x, (int) y, (int) w, (int) h);
      Font.Align align = Font.Align.Left;
      if (TextAlign == HorizontalAlignmentEnum.Right)
        align = Font.Align.Right;
      else if (TextAlign == HorizontalAlignmentEnum.Center)
        align = Font.Align.Center;

      color.Alpha *= (float) localRenderContext.Opacity;
      _asset.Draw(Text, rect, align, _fontSizeCache, color, false, out totalWidth, localRenderContext.Transform);
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
    }
  }
}

