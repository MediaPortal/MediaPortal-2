#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.DirectX11;
using SharpDX;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  public class TextBuffer2D : IDisposable
  {
    #region Consts

    protected const int FADE_SIZE = 15;

    #endregion

    #region Protected fields

    // Immutable properties
    protected float _fontSize;
    protected TextLayout _textLayout;
    protected TextFormat _textFormat;

    // State
    protected string _text;
    protected bool _textChanged;
    protected SizeF _lastTextSize;
    protected float _lastTextBoxWidth;
    protected bool _lastWrap;
    protected bool _kerning;
    protected int[] _textLines;

    // Scrolling
    protected Vector2 _scrollPos;
    protected Vector2 _scrollWrapOffset;
    protected DateTime _lastTimeUsed;
    protected DateTime _scrollInitialized;
    protected string _fontName;
    protected FontWeight _fontWeight;
    protected FontStyle _fontStyle;

    #endregion

    #region Ctor

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBuffer"/> class.
    /// </summary>
    /// <param name="fontName">The name of the font to use.</param>
    /// <param name="fontWeight">Font weight.</param>
    /// <param name="fontStyle">Font style.</param>
    /// <param name="fontSize">The font size.</param>
    public TextBuffer2D(string fontName, FontWeight fontWeight, FontStyle fontStyle, float fontSize)
    {
      _fontName = fontName;
      _fontWeight = fontWeight;
      _fontStyle = fontStyle;
      _fontSize = fontSize;
      SetFont();

      _kerning = true;
      _lastTimeUsed = DateTime.MinValue;
      _lastTextSize = new SizeF();
      ResetScrollPosition();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the brush used for rendering the text.
    /// </summary>
    public Brush TextBrush
    {
      get; set;
    }

    /// <summary>
    /// Gets the font size. This may be slightly different from the size stored in Font.
    /// </summary>
    public float FontSize
    {
      get { return _fontSize; }
    }

    /// <summary>
    /// Gets or sets the text to be rendered.
    /// </summary>
    public string Text
    {
      get { return _text; }
      set
      {
        if (_text == value)
          return;
        _textChanged = true;
        _text = value;
        CreateTextLayout();
        ResetScrollPosition();
      }
    }

    protected void CreateTextLayout()
    {
      if (_textLayout != null)
        _textLayout.Dispose();
      _textLayout= new TextLayout(GraphicsDevice11.Instance.FactoryDW, _text, _textFormat, 4096, 4096);
    }

    /// <summary>
    /// Gets the current kerning setting.
    /// </summary>
    public bool Kerning
    {
      get { return _kerning; }
    }

    /// <summary>
    /// Gets the width of a string as if it was rendered by this resource.
    /// </summary>
    /// <param name="text">String to evaluate</param>
    /// <returns>The width of the text in graphics device units (pixels)</returns>
    public float TextWidth(string text)
    {
      using (var textLayout = new TextLayout(GraphicsDevice11.Instance.FactoryDW, text, _textFormat, 4096, 4096))
      {
        return textLayout.Metrics.WidthIncludingTrailingWhitespace;
      }
    }

    /// <summary>
    /// Gets the width of a string as if it was rendered by this resource.
    /// </summary>
    /// <param name="text">String to evaluate</param>
    /// <param name="totalWidth"></param>
    /// <returns>The width of the text in graphics device units (pixels)</returns>
    public Size2F TextSize(string text, float totalWidth = float.NaN)
    {
      if (float.IsNaN(totalWidth))
        totalWidth = 4096;
      using (var textLayout = new TextLayout(GraphicsDevice11.Instance.FactoryDW, text, _textFormat, totalWidth, 4096))
      {
        return new Size2F(textLayout.Metrics.WidthIncludingTrailingWhitespace, textLayout.Metrics.Height);
      }
    }

    /// <summary>
    /// Gets the height of a number of text lines if rendered by this resource.
    /// </summary>
    /// <param name="lineCount">The number of lines to measure.</param>
    /// <returns>The height of the text in graphics device units (pixels)</returns>
    public float TextHeight(int lineCount)
    {
      if (string.IsNullOrEmpty(Text))
        return 0f;

      string[] lines = GetLines(0, false);
      float totalHeight = 0f;
      foreach (string line in lines)
      {
        using (var textLayout = new TextLayout(GraphicsDevice11.Instance.FactoryDW, line, _textFormat, 4096, 4096))
        {
          totalHeight += textLayout.Metrics.Height;
        }
      }
      return totalHeight;
    }

    /// <summary>
    /// Gets the height of text rendered with this resource.
    /// </summary>
    public float LineHeight
    {
      get
      {
        LineSpacingMethod lineSpacingMethod;
        float lineSpacing;
        float baseLine;
        _textFormat.GetLineSpacing(out lineSpacingMethod, out lineSpacing, out baseLine);
        // TODO: check correct value
        return lineSpacing;
      }
    }

    public override string ToString()
    {
      return Text;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Sets a new font for this buffer.
    /// </summary>
    /// <param name="fontName">The name of the font to use.</param>
    /// <param name="fontWeight">Font weight.</param>
    /// <param name="fontStyle">Font style.</param>
    /// <param name="fontSize">The font size.</param>
    public void SetFont(string fontName, FontWeight fontWeight, FontStyle fontStyle, float fontSize)
    {
      _fontName = fontName;
      _fontWeight = fontWeight;
      _fontStyle = fontStyle;
      _fontSize = fontSize;
      SetFont();
    }

    protected void SetFont()
    {
      DisposeFont();
      _textFormat = new TextFormat(GraphicsDevice11.Instance.FactoryDW, _fontName, _fontWeight, _fontStyle, _fontSize);
      _textChanged = true;
    }


    public string[] GetLines(float maxWidth, bool wrap)
    {
      return wrap ? WrapText(maxWidth) : _text.Split(Environment.NewLine.ToCharArray());
    }

    /// <summary>
    /// Allocates or re-alocates this resource.
    /// </summary>
    /// <param name="boxWidth"></param>
    /// <param name="wrap"></param>
    public void Allocate(float boxWidth, bool wrap)
    {
      if (String.IsNullOrEmpty(_text))
      {
        return;
      }

      // Get text quads
      //string[] lines = GetLines(boxWidth, wrap);
      //PositionColoredTextured[] verts = _font.CreateText(lines, _fontSize, true, out _lastTextSize, out _textLines);

      //// Re-use existing buffer if necessary
      //_buffer.Set(ref verts, PrimitiveType.TriangleList);

      // Preserve state
      _lastTextBoxWidth = boxWidth;
      _lastWrap = wrap;
      _textChanged = false;
    }
    /// <summary>
    /// Wraps the text of this label to the specified <paramref name="maxWidth"/> and returns the wrapped
    /// text parts. Wrapping is performed on a word-by-word basis.
    /// </summary>
    /// <param name="maxWidth">Maximum available width before the text should be wrapped.</param>
    /// <returns>An array of strings holding the wrapped text lines.</returns>
    public string[] WrapText(float maxWidth)
    {
      //if (string.IsNullOrEmpty(_text))
      return new string[0];

      //IList<string> result = new List<string>();
      //foreach (string para in _text.Split(Environment.NewLine.ToCharArray()))
      //{
      //  int paraLength = para.Length;
      //  int nextIndex = 0;
      //  float lineWidth = 0;
      //  int lineStartIndex = 0; // Start index of the current line to be examined

      //  // Split paragraphs into lines that will fit into maxWidth
      //  while (nextIndex < paraLength)
      //  {
      //    int sectionIndex = nextIndex;
      //    // Iterate over leading spaces
      //    while (nextIndex < paraLength && char.IsWhiteSpace(para[nextIndex]))
      //      ++nextIndex;
      //    int lastIndex = nextIndex; // Remember index to avoid busy loops if not a single character fits
      //    if (nextIndex < paraLength)
      //    {
      //      // Find length of next word
      //      int wordIndex = nextIndex;
      //      while (nextIndex < paraLength && !char.IsWhiteSpace(para[nextIndex]))
      //        ++nextIndex;
      //      // Does the word fit into the space?
      //      // Remember to take into account the additional width required if this was the last word on the line.
      //      float cx = PartialTextWidth(para, sectionIndex, nextIndex - 1, _fontSize, Kerning);
      //      float extension = CharWidthExtension(para, nextIndex - 1, _fontSize);

      //      if (lineWidth + cx + extension > maxWidth)
      //      {
      //        // If the word is longer than a line width wrap it by letter
      //        if (cx + extension > maxWidth)
      //        {
      //          // Double check without leading whitespace
      //          if (PartialTextWidth(para, wordIndex, nextIndex - 1, _fontSize, Kerning) + extension > maxWidth)
      //          {
      //            // Don't wrap whitespace
      //            while (sectionIndex < para.Length && char.IsWhiteSpace(para[sectionIndex]))
      //            {
      //              lineWidth += TextWidth(para, sectionIndex, sectionIndex, _fontSize, Kerning);
      //              ++sectionIndex;
      //            }
      //            // See how many characters of the word we can fit on this line
      //            lineWidth += TextWidth(para, sectionIndex, sectionIndex, _fontSize, Kerning);
      //            while (lineWidth < maxWidth)
      //            {
      //              lineWidth += TextWidth(para, sectionIndex, sectionIndex, _fontSize, Kerning);
      //              ++sectionIndex;
      //            }
      //            // Prepare for next line
      //            wordIndex = sectionIndex;
      //            nextIndex = wordIndex;
      //          }
      //        }
      //        // Start new line						
      //        if (sectionIndex != lineStartIndex)
      //          result.Add(para.Substring(lineStartIndex, sectionIndex - lineStartIndex));
      //        lineStartIndex = wordIndex;
      //        lineWidth = _font.TextWidth(para, wordIndex, nextIndex - 1, _fontSize, Kerning);
      //      }
      //      else
      //        lineWidth += cx;
      //      if (nextIndex >= paraLength)
      //      {
      //        // End of paragraphs
      //        result.Add(para.Substring(lineStartIndex, nextIndex - lineStartIndex));
      //        lineStartIndex = nextIndex;
      //      }
      //    }
      //    if (nextIndex == lastIndex)
      //      break;
      //  }
      //  // If no words found add an empty line to preserve text formatting
      //  if (lineStartIndex == 0)
      //    result.Add("");
      //}
      //return result.ToArray();
    }

    /// <summary>
    /// Standard draw method for this text.
    /// </summary>
    /// <param name="textBox">The box where the text should be drawn.</param>
    /// <param name="horzAlignment">The horizontal alignment.</param>
    /// <param name="vertAlignment">The vertical alignment.</param>
    /// <param name="color">The color.</param>
    /// <param name="wrap">If <c>true</c> then text will be word-wrapped to fit the <paramref name="textBox"/>.</param>
    /// <param name="fade">If <c>true</c> then text will be faded at the edge of the <paramref name="textBox"/>.</param>
    /// <param name="zOrder">A value indicating the depth (and thus position in the visual heirachy) that this element should be rendered at.</param>
    /// <param name="scrollMode">Text scrolling behaviour.</param>
    /// <param name="scrollSpeed">Text scrolling speed in units (pixels at original skin size) per second.</param>
    /// <param name="scrollDelay">Text scrolling delay in seconds.</param>
    /// <param name="finalTransform">The final combined layout/render-transform.</param>
    //public void Render(RectangleF textBox, HorizontalTextAlignEnum horzAlignment, VerticalTextAlignEnum vertAlignment, Color4 color,
    //    bool wrap, bool fade, float zOrder, TextScrollEnum scrollMode, float scrollSpeed, float scrollDelay, Matrix finalTransform)
    //{
    //  if (wrap != _lastWrap || _textChanged || (wrap && textBox.Width != _lastTextBoxWidth))
    //  {
    //    Allocate(textBox.Width, wrap);
    //  }

    //  // Update scrolling
    //  TextScrollEnum actualScrollMode = scrollMode;
    //  if (scrollMode != TextScrollEnum.None && _lastTimeUsed != DateTime.MinValue)
    //    actualScrollMode = UpdateScrollPosition(textBox, scrollMode, scrollSpeed, scrollDelay);

    //  // Prepare horizontal alignment info for shader. X is position offset, Y is multiplyer for line width.
    //  Vector4 alignParam;
    //  switch (horzAlignment)
    //  {
    //    case HorizontalTextAlignEnum.Center:
    //      alignParam = new Vector4(textBox.Width / 2.0f, -0.5f, zOrder, 1.0f);
    //      break;
    //    case HorizontalTextAlignEnum.Right:
    //      alignParam = new Vector4(textBox.Width, -1.0f, zOrder, 1.0f);
    //      break;
    //    //case TextAlignEnum.Left:
    //    default:
    //      alignParam = new Vector4(0.0f, 0.0f, zOrder, 1.0f);
    //      break;
    //  }
    //  // Do vertical alignment by adjusting yPosition
    //  float yPosition = 0.0f;
    //  switch (vertAlignment)
    //  {
    //    case VerticalTextAlignEnum.Bottom:
    //      yPosition = Math.Max(textBox.Height - _lastTextSize.Height, 0.0f);
    //      break;
    //    case VerticalTextAlignEnum.Center:
    //      yPosition += Math.Max((textBox.Height - _lastTextSize.Height) / 2.0f, 0.0f);
    //      break;
    //    //case TextAlignEnum.Top:
    //    // Do nothing
    //  }

    //  // Do we need to add fading edges?
    //  //Vector4 fadeBorder;
    //  //if (fade && CalculateFadeBorder(actualScrollMode, textBox, horzAlignment, out fadeBorder))
    //  //{
    //  //  _effect = ContentManager.Instance.GetEffect(EFFECT_FONT_FADE);
    //  //  _effect.Parameters[PARAM_FADE_BORDER] = fadeBorder;
    //  //}
    //  //else
    //  //  _effect = ContentManager.Instance.GetEffect(EFFECT_FONT);

    //  //// Render
    //  //_effect.Parameters[PARAM_COLOR] = color;
    //  //_effect.Parameters[PARAM_ALIGNMENT] = alignParam;
    //  //_effect.Parameters[PARAM_SCROLL_POSITION] = new Vector4(_scrollPos.X, _scrollPos.Y + yPosition, 0.0f, 0.0f);
    //  //_effect.Parameters[PARAM_TEXT_RECT] = new Vector4(textBox.Left, textBox.Top, textBox.Width, textBox.Height);
    //  DoRender(finalTransform);

    //  //// Because text wraps around before it is complete scrolled off the screen we may need to render a second copy 
    //  //// to create the desired wrapping effect
    //  //if (scrollMode != TextScrollEnum.None)
    //  //{
    //  //  if (!float.IsNaN(_scrollWrapOffset.X))
    //  //  {
    //  //    _effect.Parameters[PARAM_SCROLL_POSITION] = new Vector4(_scrollPos.X + _scrollWrapOffset.X, _scrollPos.Y, 0.0f, 0.0f);
    //  //    DoRender(finalTransform);
    //  //  }
    //  //  else if (!float.IsNaN(_scrollWrapOffset.Y))
    //  //  {
    //  //    _effect.Parameters[PARAM_SCROLL_POSITION] = new Vector4(_scrollPos.X, _scrollPos.Y + _scrollWrapOffset.Y, 0.0f, 0.0f);
    //  //    DoRender(finalTransform);
    //  //  }
    //  //}
    //  _lastTimeUsed = SkinContext.FrameRenderingStartTime;
    //}

    /// <summary>
    /// Simplified render method to draw the text with a given text offset. This can be used for text edit controls where the text is
    /// shifted horizontally against its textbox.
    /// </summary>
    /// <param name="textBox">The box where the text should be drawn.</param>
    /// <param name="horzAlignment">The horizontal alignment.</param>
    /// <param name="vertAlignment">The vertical alignment.</param>
    /// <param name="offsetX">Horizontal offset of the text in relation to its text box. A negative offset will make the text start left of its
    /// normal position.</param>
    /// <param name="color">The color.</param>
    /// <param name="zOrder">A value indicating the depth (and thus position in the visual heirachy) that this element should be rendered at.</param>
    /// <param name="finalTransform">The final combined layout/render-transform.</param>
    public void Render(RectangleF textBox, RenderContext localRenderContext)

    //public void Render(RectangleF textBox, HorizontalTextAlignEnum horzAlignment, VerticalTextAlignEnum vertAlignment, float offsetX,
    //    Color4 color, float zOrder, Matrix finalTransform)
    {
      if (_lastWrap || _textChanged)
      {
        Allocate(textBox.Width, false);
      }

      // Prepare horizontal alignment info for shader. X is position offset, Y is multiplyer for line width.
      //Vector4 alignParam;
      //switch (horzAlignment)
      //{
      //  case HorizontalTextAlignEnum.Center:
      //    alignParam = new Vector4(textBox.Width / 2.0f, -0.5f, zOrder, 1.0f);
      //    break;
      //  case HorizontalTextAlignEnum.Right:
      //    alignParam = new Vector4(textBox.Width, -1.0f, zOrder, 1.0f);
      //    break;
      //  //case TextAlignEnum.Left:
      //  default:
      //    alignParam = new Vector4(0.0f, 0.0f, zOrder, 1.0f);
      //    break;
      //}
      //// Do vertical alignment by adjusting yPosition
      //float yPosition = 0.0f;
      //switch (vertAlignment)
      //{
      //  case VerticalTextAlignEnum.Bottom:
      //    yPosition = Math.Max(textBox.Height - _lastTextSize.Height, 0.0f);
      //    break;
      //  case VerticalTextAlignEnum.Center:
      //    yPosition += Math.Max((textBox.Height - _lastTextSize.Height) / 2.0f, 0.0f);
      //    break;
      //  //case TextAlignEnum.Top:
      //  // Do nothing
      //}

      //// No fading
      //_effect = ContentManager.Instance.GetEffect(EFFECT_FONT);

      // Render
      var brush = TextBrush;
      // _textLayout can be null if no Text has been set before.
      if (brush != null && _textLayout != null) 
      {
        GraphicsDevice11.Instance.Context2D1.DrawTextLayout(localRenderContext.OccupiedTransformedBounds.TopLeft, _textLayout, brush, localRenderContext);
      }
      _lastTimeUsed = SkinContext.FrameRenderingStartTime;
    }

    #endregion

    #region Protected methods

    protected void DoRender(Matrix finalTransform)
    {
      //_effect.StartRender(_font.Texture, finalTransform);
      //_buffer.Render(0);
      //_effect.EndRender();
    }

    protected TextScrollEnum UpdateScrollPosition(RectangleF textBox, TextScrollEnum mode, float speed, float scrollDelay)
    {
      if ((SkinContext.FrameRenderingStartTime - _scrollInitialized).TotalSeconds < scrollDelay)
        return TextScrollEnum.None;

      float dif = speed * (float)SkinContext.FrameRenderingStartTime.Subtract(_lastTimeUsed).TotalSeconds;

      if (mode == TextScrollEnum.Auto)
      {
        if (_lastWrap && _lastTextSize.Height > textBox.Height)
          mode = TextScrollEnum.Up;
        else if (_textLines.Length == 1 && _lastTextSize.Width > textBox.Width)
          mode = TextScrollEnum.Left;
        else
          return TextScrollEnum.None;
      }

      switch (mode)
      {
        case TextScrollEnum.Left:
          _scrollPos.X -= dif;
          if (_scrollPos.X + _lastTextSize.Width < textBox.Width / 2.0f)
          {
            _scrollWrapOffset.X = _scrollPos.X;
            _scrollPos.X = textBox.Width + 4;
            _scrollWrapOffset.X -= _scrollPos.X;
          }
          else if (_scrollWrapOffset.X + _scrollPos.X + _lastTextSize.Width < 0.0f)
            _scrollWrapOffset.X = float.NaN;
          break;
        case TextScrollEnum.Right:
          _scrollPos.X += dif;
          if (_scrollPos.X > textBox.Width / 2.0f)
          {
            _scrollWrapOffset.X = _scrollPos.X;
            _scrollPos.X = -_lastTextSize.Width - 4;
            _scrollWrapOffset.X -= _scrollPos.X;
          }
          else if (_scrollWrapOffset.X + _scrollPos.X > textBox.Width)
            _scrollWrapOffset.X = float.NaN;
          break;
        case TextScrollEnum.Down:
          _scrollPos.Y += dif;
          if (_scrollPos.Y > textBox.Height / 2.0f)
          {
            _scrollWrapOffset.Y = _scrollPos.Y;
            _scrollPos.Y = -_lastTextSize.Height - 4;
            _scrollWrapOffset.Y -= _scrollPos.Y;
          }
          else if (_scrollWrapOffset.Y + _scrollPos.Y > textBox.Height)
            _scrollWrapOffset.Y = float.NaN;
          break;
        //case TextScrollEnum.Up:
        default:
          _scrollPos.Y -= dif;
          if (_scrollPos.Y + _lastTextSize.Height < textBox.Height / 2.0f)
          {
            _scrollWrapOffset.Y = _scrollPos.Y;
            _scrollPos.Y = textBox.Height + 4;
            _scrollWrapOffset.Y -= _scrollPos.Y;
          }
          else if (_scrollWrapOffset.Y + _scrollPos.Y + _lastTextSize.Height < 0.0f)
            _scrollWrapOffset.Y = float.NaN;
          break;
      }
      return mode;
    }

    public void ResetScrollPosition()
    {
      _scrollPos = new Vector2(0.0f, 0.0f);
      _scrollWrapOffset = new Vector2(float.NaN, float.NaN);
      _scrollInitialized = DateTime.Now;
    }

    #endregion

    #region Protected methods

    protected bool CalculateFadeBorder(TextScrollEnum scrollMode, RectangleF textBox, HorizontalTextAlignEnum horzAlign, out Vector4 fadeBorder)
    {
      fadeBorder = new Vector4(0.0001f, 0.0001f, 0.0001f, 0.0001f);
      bool dofade = false;
      if (scrollMode == TextScrollEnum.Left || scrollMode == TextScrollEnum.Right)
      {
        fadeBorder.X = FADE_SIZE; // Fade on left edge
        fadeBorder.Z = FADE_SIZE; // Fade on right edge
        dofade = true;
      }
      else if (_lastTextSize.Width > textBox.Width)
      {
        if (horzAlign == HorizontalTextAlignEnum.Right || horzAlign == HorizontalTextAlignEnum.Center)
          fadeBorder.X = FADE_SIZE; // Fade on left edge
        if (horzAlign == HorizontalTextAlignEnum.Left || horzAlign == HorizontalTextAlignEnum.Center)
          fadeBorder.Z = FADE_SIZE; // Fade on right edge
        dofade = true;
      }
      if (scrollMode == TextScrollEnum.Up || scrollMode == TextScrollEnum.Down)
      {
        fadeBorder.Y = FADE_SIZE; // Fade on top edge
        fadeBorder.W = FADE_SIZE; // Fade on bottom edge
        dofade = true;
      }
      else if (_lastTextSize.Height > textBox.Height + _fontSize / 4)
      {
        fadeBorder.W = FADE_SIZE; // Fade on bottom edge
        dofade = true;
      }
      return dofade;
    }

    void DisposeFont()
    {
      if (_textFormat != null)
        _textFormat.Dispose();
      if (_textLayout != null)
        _textLayout.Dispose();
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      DisposeFont();
    }

    #endregion
  }
}
