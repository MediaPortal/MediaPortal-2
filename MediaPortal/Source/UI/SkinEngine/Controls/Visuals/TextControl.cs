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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Settings;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.Exceptions;
using SlimDX;
using Font = MediaPortal.UI.SkinEngine.Fonts.Font;
using FontBufferAsset = MediaPortal.UI.SkinEngine.ContentManagement.TextBufferAsset;
using MediaPortal.Utilities.DeepCopy;

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
    protected AbstractTextInputHandler _textInputHandler = null;

    // Use to avoid change handlers during text updates
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

      _hasFocusProperty.Attach(OnHasFocusChanged);
    }

    void Detach()
    {
      _textProperty.Detach(OnTextChanged);
      _colorProperty.Detach(OnColorChanged);
      _preferredTextLengthProperty.Detach(OnPreferredTextLengthChanged);
      _textAlignProperty.Detach(OnTextAlignChanged);

      _hasFocusProperty.Attach(OnHasFocusChanged);
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

    void OnHasFocusChanged(AbstractProperty prop, object oldValue)
    {
      AbstractTextInputHandler oldTextInputHandler = _textInputHandler;
      _textInputHandler = HasFocus ? CreateTextInputHandler() : null;
      if (oldTextInputHandler != null)
        oldTextInputHandler.Dispose();
    }

    void OnTextChanged(AbstractProperty prop, object oldValue)
    {
      string text = Text ?? string.Empty;
      // The skin is setting the text, so update the caret
      if (!_editText)
        CaretIndex = text.Length;
      if (_asset != null)
        _asset.Text = text;
    }

    void OnPreferredTextLengthChanged(AbstractProperty prop, object oldValue)
    {
      InvalidateLayout();
      InvalidateParentLayout();
    }

    protected void ClearAsset()
    {
      TextBufferAsset asset = _asset;
      _asset = null;
      if (asset != null)
        asset.Free(true);
    }

    protected AbstractTextInputHandler CreateTextInputHandler()
    {
      AppSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<AppSettings>();
      SimplePropertyDataDescriptor textPropertyDataDescriptor;
      SimplePropertyDataDescriptor caretIndexDataDescriptor;
      if (!SimplePropertyDataDescriptor.CreateSimplePropertyDataDescriptor(
          this, "Text", out textPropertyDataDescriptor) ||
          !SimplePropertyDataDescriptor.CreateSimplePropertyDataDescriptor(
          this, "CaretIndex", out caretIndexDataDescriptor))
        throw new FatalException("One of the properties 'Text' or 'CaretIndex' was not found");
      return settings.CellPhoneInputStyle ?
          (AbstractTextInputHandler) new CellPhoneTextInputHandler(this, textPropertyDataDescriptor, caretIndexDataDescriptor) :
          new DefaultTextInputHandler(this, textPropertyDataDescriptor, caretIndexDataDescriptor);
    }

    protected override void OnFontChanged(AbstractProperty prop, object oldValue)
    {
      ClearAsset();
    }

    public override void OnKeyPreview(ref Key key)
    {
      AbstractTextInputHandler textInputHandler = _textInputHandler;
      if (textInputHandler != null)
      {
        _editText = true;
        textInputHandler.HandleInput(ref key);
        _editText = false;
      }
      base.OnKeyPreview(ref key);
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
        _asset = ContentManager.GetTextBuffer(GetFontFamilyOrInherited(), GetFontSizeOrInherited(), true);
        _asset.Text = Text;
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
      AllocFont();

      SizeF childSize = _asset == null ? SizeF.Empty : new SizeF(_asset.TextWidth(Text), _asset.TextHeight(1));

      if (PreferredTextLength.HasValue && _asset != null)
        // We use the "W" character as the character which needs the most space in X-direction
        childSize.Width = PreferredTextLength.Value * _asset.TextWidth("W");

      return childSize;
    }

    public override void DoRender(RenderContext localRenderContext)
    {
      base.DoRender(localRenderContext);

      AllocFont();
      if (_asset == null)
        return;

      Font.Align align = Font.Align.Left;
      if (TextAlign == HorizontalAlignmentEnum.Right)
        align = Font.Align.Right;
      else if (TextAlign == HorizontalAlignmentEnum.Center)
        align = Font.Align.Center;

      Color4 color = ColorConverter.FromColor(Color);
      color.Alpha *= (float) localRenderContext.Opacity;

      _asset.Render(_innerRect, align, color, false, localRenderContext.ZOrder, TextScrollMode.None, 0.0f, localRenderContext.Transform);
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (_asset != null)
        _asset.Free(true);
      _asset = null;
    }
  }
}

