#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Drawing;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Controls.Brushes;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Settings;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.Exceptions;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;
using SlimDX.Direct3D9;
using Brush = MediaPortal.UI.SkinEngine.Controls.Brushes.Brush;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public enum TextCursorState
  {
    Visible,
    Hidden
  }

  // TODO: We don't notice font changes if font is declared on a parent element
  public class TextControl : Control
  {
    #region Consts

    public TimeSpan CURSOR_BLINK_INTERVAL = TimeSpan.FromSeconds(0.5);
    public TimeSpan INFINITE_TIMESPAN = TimeSpan.FromMilliseconds(-1);

    /// <summary>
    /// Thickness of the text cursor in pixels.
    /// </summary>
    public int CURSOR_THICKNESS = 2;

    public const char PASSWORD_CHAR = '*';

    #endregion

    #region Protected fields

    protected AbstractProperty _caretIndexProperty;
    protected AbstractProperty _textProperty;
    protected AbstractProperty _internalTextProperty;
    protected AbstractProperty _colorProperty;
    protected AbstractProperty _preferredTextLengthProperty;
    protected AbstractProperty _textAlignProperty;
    protected AbstractProperty _isPasswordProperty;
    protected TextBuffer _asset;
    protected AbstractTextInputHandler _textInputHandler = null;

    // Use to avoid change handlers during text updates
    protected bool _internalUpdate = false; 

    // Virtual position
    protected float _virtualPosition = 0; // Negative values mean the text starts before the text control

    // Text cursor
    protected bool _cursorShapeInvalid = true;
    protected bool _cursorBrushInvalid = true;
    protected Timer _cursorBlinkTimer = null;
    protected TextCursorState _cursorState;
    protected PrimitiveBuffer _cursorContext = null;
    protected Brush _cursorBrush = null;

    #endregion

    #region Ctor

    public TextControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _cursorBlinkTimer = new Timer(CursorBlinkHandler);
      _textProperty = new SProperty(typeof(string), string.Empty);
      _isPasswordProperty = new SProperty(typeof(bool), false);
      _caretIndexProperty = new SProperty(typeof(int), 0);
      _internalTextProperty = new SProperty(typeof(string), string.Empty);
      _colorProperty = new SProperty(typeof(Color), Color.Black);

      _preferredTextLengthProperty = new SProperty(typeof(int?), null);
      _textAlignProperty = new SProperty(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Left);

      _cursorState = TextCursorState.Hidden;

      // Yes, we can have focus
      Focusable = true;
    }

    void Attach()
    {
      _textProperty.Attach(OnTextChanged);
      _isPasswordProperty.Attach(OnTextChanged);
      _internalTextProperty.Attach(OnInternalTextChanged);
      _caretIndexProperty.Attach(OnCaretIndexChanged);
      _colorProperty.Attach(OnColorChanged);
      _preferredTextLengthProperty.Attach(OnCompleteLayoutGetsInvalid);

      HasFocusProperty.Attach(OnHasFocusChanged);
      FontFamilyProperty.Attach(OnFontChanged);
      FontSizeProperty.Attach(OnFontChanged);
    }

    void Detach()
    {
      _textProperty.Detach(OnTextChanged);
      _isPasswordProperty.Detach(OnTextChanged);
      _internalTextProperty.Detach(OnInternalTextChanged);
      _caretIndexProperty.Detach(OnCaretIndexChanged);
      _colorProperty.Detach(OnColorChanged);
      _preferredTextLengthProperty.Detach(OnCompleteLayoutGetsInvalid);

      HasFocusProperty.Detach(OnHasFocusChanged);
      FontFamilyProperty.Detach(OnFontChanged);
      FontSizeProperty.Detach(OnFontChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      TextControl tc = (TextControl) source;
      Text = tc.Text;
      InternalText = Text;
      CaretIndex = tc.CaretIndex;
      Color = tc.Color;
      PreferredTextLength = tc.PreferredTextLength;
      IsPassword = tc.IsPassword;
      Attach();
      CheckTextCursor();
    }

    #endregion

    void CursorBlinkHandler(object state)
    {
      ToggleTextCursorState();
    }

    void OnHasFocusChanged(AbstractProperty prop, object oldValue)
    {
      AbstractTextInputHandler oldTextInputHandler = _textInputHandler;
      _textInputHandler = HasFocus ? CreateTextInputHandler() : null;
      if (oldTextInputHandler != null)
        oldTextInputHandler.Dispose();
      CheckTextCursor();
    }

    void OnTextChanged(AbstractProperty prop, object oldValue)
    {
      string text = Text ?? string.Empty;
      if (!_internalUpdate)
        // The skin is setting the text, so update the caret
        CaretIndex = text.Length;
      InternalText = text;
    }

    void OnCaretIndexChanged(AbstractProperty prop, object oldValue)
    {
      _cursorShapeInvalid = true;
      if (CursorState == TextCursorState.Visible)
        // A cursor movement makes the text cursor visible at once
        SetupCursor();
    }

    void OnColorChanged(AbstractProperty prop, object oldValue)
    {
      _cursorBrushInvalid = true;
    }

    void OnInternalTextChanged(AbstractProperty prop, object oldValue)
    {
      string text = InternalText ?? string.Empty;
      _internalUpdate = true;
      try
      {
        Text = text;
      }
      finally
      {
        _internalUpdate = false;
      }
      if (_asset == null)
        AllocFont();
      else
        _asset.Text = GetVisibleText(text);
      if (CursorState == TextCursorState.Visible)
        // A text change makes the text cursor visible at once
        SetupCursor();
    }

    protected string GetVisibleText(string clearText)
    {
      return IsPassword ? ToPassword(clearText) : clearText;
    }

    protected string ToPassword(string clearText)
    {
      const string replacedString = "";
      return replacedString.PadRight(clearText.Length, PASSWORD_CHAR);
    }

    protected string VisibleText
    {
      get { return GetVisibleText(Text); }
    }

    protected void ToggleTextCursorState()
    {
      CursorState = CursorState == TextCursorState.Visible ? TextCursorState.Hidden : TextCursorState.Visible;
    }

    protected void CheckTextCursor()
    {
      if (HasFocus)
        SetupCursor();
      else
        DisableCursor();
    }

    /// <summary>
    /// Shows the text cursor and initializes the text cursor timer.
    /// </summary>
    protected void SetupCursor()
    {
      CursorState = TextCursorState.Visible;
      _cursorBlinkTimer.Change(CURSOR_BLINK_INTERVAL, CURSOR_BLINK_INTERVAL);
    }

    /// <summary>
    /// Hides the text cursor.
    /// </summary>
    protected void DisableCursor()
    {
      CursorState = TextCursorState.Hidden;
      _cursorBlinkTimer.Change(INFINITE_TIMESPAN, INFINITE_TIMESPAN);
    }

    protected AbstractTextInputHandler CreateTextInputHandler()
    {
      AppSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<AppSettings>();
      SimplePropertyDataDescriptor internalTextPropertyDataDescriptor;
      SimplePropertyDataDescriptor caretIndexDataDescriptor;
      if (!SimplePropertyDataDescriptor.CreateSimplePropertyDataDescriptor(
          this, "InternalText", out internalTextPropertyDataDescriptor) ||
          !SimplePropertyDataDescriptor.CreateSimplePropertyDataDescriptor(
          this, "CaretIndex", out caretIndexDataDescriptor))
        throw new FatalException("One of the properties 'InternalText' or 'CaretIndex' was not found");
      return settings.CellPhoneInputStyle ?
          (AbstractTextInputHandler) new CellPhoneTextInputHandler(this, internalTextPropertyDataDescriptor, caretIndexDataDescriptor) :
          new DefaultTextInputHandler(this, internalTextPropertyDataDescriptor, caretIndexDataDescriptor);
    }

    protected void OnFontChanged(AbstractProperty prop, object oldValue)
    {
      InvalidateLayout(true, true);
      if (_asset == null)
        AllocFont();
      else
        _asset.SetFont(GetFontFamilyOrInherited(), GetFontSizeOrInherited());
    }

    public override void OnKeyPreview(ref Key key)
    {
      AbstractTextInputHandler textInputHandler = _textInputHandler;
      if (textInputHandler != null && IsEnabled)
        textInputHandler.HandleInput(ref key);
      base.OnKeyPreview(ref key);
    }

    /// <summary>
    /// Gets the current state of the text cursor (visible/invisible).
    /// </summary>
    public TextCursorState CursorState
    {
      get { return _cursorState; }
      internal set { _cursorState = value; }
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

    public AbstractProperty IsPasswordProperty
    {
      get { return _isPasswordProperty; }
    }

    public bool IsPassword
    {
      get { return (bool) _isPasswordProperty.GetValue(); }
      set { _isPasswordProperty.SetValue(value); }
    }

    public AbstractProperty InternalTextProperty
    {
      get { return _internalTextProperty; }
    }

    public string InternalText
    {
      get { return (string) _internalTextProperty.GetValue(); }
      set { _internalTextProperty.SetValue(value); }
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
        _asset = new TextBuffer(GetFontFamilyOrInherited(), GetFontSizeOrInherited()) { Text = VisibleText };
      }
    }

    void DeallocateCursor()
    {
      if (_cursorContext != null)
        PrimitiveBuffer.DisposePrimitiveBuffer(ref _cursorContext);
    }

    void UpdateCursorShape(RectangleF cursorBounds, float zPos)
    {
      DeallocateCursor();
      Color4 col = ColorConverter.FromColor(Color);
      int color = col.ToArgb();

      PositionColoredTextured[] verts = PositionColoredTextured.CreateQuad_Fan(
          cursorBounds.Left - 0.5f, cursorBounds.Top - 0.5f, cursorBounds.Right - 0.5f, cursorBounds.Bottom - 0.5f,
          0, 0, 0, 0, zPos, color);

      if (_cursorBrushInvalid && _cursorBrush != null)
      {
        _cursorBrush.Deallocate();
        _cursorBrush.Dispose();
        _cursorBrush = null;
      }
      if (_cursorBrush == null)
        _cursorBrush = new SolidColorBrush {Color = Color};
      _cursorBrushInvalid = false;

      _cursorBrush.SetupBrush(this, ref verts, zPos, false);
      PrimitiveBuffer.SetPrimitiveBuffer(ref _cursorContext, ref verts, PrimitiveType.TriangleFan);

      _cursorShapeInvalid = false;
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
                TextStyle = IsPassword ? VirtualKeyboardTextStyle.PasswordText : VirtualKeyboardTextStyle.None
            });
      }
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      base.CalculateInnerDesiredSize(totalSize); // Needs to be called in each sub class of Control, see comment in Control.CalculateInnerDesiredSize()
      AllocFont();

      SizeF childSize = _asset == null ? SizeF.Empty : new SizeF(_asset.TextWidth(VisibleText ?? string.Empty), _asset.TextHeight(1));

      if (PreferredTextLength.HasValue && _asset != null)
        // We use the "W" character as the character which needs the most space in X-direction
        childSize.Width = PreferredTextLength.Value * _asset.TextWidth("W");

      return childSize;
    }

    protected void Bound(ref float value, float min, float max)
    {
      if (value < min)
        value = min;
      if (value > max)
        value = max;
    }

    public override void RenderOverride(RenderContext localRenderContext)
    {
      base.RenderOverride(localRenderContext);
      AllocFont();

      HorizontalTextAlignEnum horzAlign = HorizontalTextAlignEnum.Left;
      if (HorizontalContentAlignment == HorizontalAlignmentEnum.Right)
        horzAlign = HorizontalTextAlignEnum.Right;
      else if (HorizontalContentAlignment == HorizontalAlignmentEnum.Center)
        horzAlign = HorizontalTextAlignEnum.Center;

      VerticalTextAlignEnum vertAlign = VerticalTextAlignEnum.Top;
      if (VerticalContentAlignment == VerticalAlignmentEnum.Bottom)
        vertAlign = VerticalTextAlignEnum.Bottom;
      else if (VerticalContentAlignment == VerticalAlignmentEnum.Center)
        vertAlign = VerticalTextAlignEnum.Center;

      Color4 color = ColorConverter.FromColor(Color);
      color.Alpha *= (float) localRenderContext.Opacity;

      // Update text cursor
      if ((_cursorShapeInvalid || _cursorBrushInvalid) && CursorState == TextCursorState.Visible)
      {
        string textBeforeCaret = VisibleText;
        textBeforeCaret = string.IsNullOrEmpty(textBeforeCaret) ? string.Empty : textBeforeCaret.Substring(0, CaretIndex);
        float caretX = _asset.TextWidth(textBeforeCaret);
        float textHeight = _asset.TextHeight(1);
        float textInsetY;
        switch (vertAlign)
        {
          case VerticalTextAlignEnum.Bottom:
            textInsetY = _innerRect.Height - textHeight;
            break;
          case VerticalTextAlignEnum.Center:
            textInsetY = (_innerRect.Height - textHeight) / 2;
            break;
          default: // VerticalTextAlignEnum.Top
            textInsetY = 0;
            break;
        }
        if (_virtualPosition + caretX < 10)
          _virtualPosition = _innerRect.Width / 3 - caretX;
        if (_virtualPosition + caretX > _innerRect.Width - 10)
          _virtualPosition = _innerRect.Width * 2/3 - caretX;
        Bound(ref _virtualPosition, -caretX, 0);
        RectangleF cursorBounds = new RectangleF(_innerRect.X + _virtualPosition + caretX, _innerRect.Y + textInsetY, CURSOR_THICKNESS, textHeight);
        UpdateCursorShape(cursorBounds, localRenderContext.ZOrder);
      }

      // Render text
      _asset.Render(_innerRect, horzAlign, vertAlign, _virtualPosition, color, localRenderContext.ZOrder, localRenderContext.Transform);

      // Render text cursor
      if (_cursorBrush != null && CursorState == TextCursorState.Visible)
      {
        _cursorBrush.BeginRenderBrush(_cursorContext, localRenderContext);
        _cursorContext.Render(0);
        _cursorBrush.EndRender();
      }
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (_asset != null)
      {
        _asset.Dispose();
        _asset = null;
      }
      DeallocateCursor();
    }

    public override void Dispose()
    {
      Deallocate();
      _cursorBlinkTimer.Dispose();
      base.Dispose();
    }
  }
}

