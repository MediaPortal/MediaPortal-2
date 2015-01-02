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
using MediaPortal.UI.SkinEngine.MpfElements;
using SharpDX;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  public class TextBuffer2D : IDisposable
  {
    #region Consts

    protected const float FADE_SIZE = 15f;

    #endregion

    #region Protected fields

    // Immutable properties
    protected float _fontSize;
    protected TextLayout _textLayout;
    protected TextFormat _textFormat;

    // State
    protected string _text;
    protected bool _textChanged;
    protected bool _lastWrap;
    protected RectangleF _lastTextBox;

    protected string _fontName;
    protected FontWeight _fontWeight;
    protected FontStyle _fontStyle;
    protected Brush _opacityBrush;

    // Scrolling
    protected Vector2 _scrollPos;
    protected Vector2 _scrollWrapOffset;
    protected DateTime _lastTimeUsed;
    protected DateTime _scrollInitialized;
    protected TextScrollEnum _lastScrollDirection;

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

      _lastTimeUsed = DateTime.MinValue;
      ResetScrollPosition();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the brush used for rendering the text.
    /// </summary>
    public Brush TextBrush
    {
      get;
      set;
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

      var totalWidth = _lastTextBox.Width;
      var totalHeight = _lastTextBox.Height;

      if (!_lastWrap)
      {
        totalWidth = 4096;
        totalHeight = 4096;
      }
      _textLayout = new TextLayout(GraphicsDevice11.Instance.FactoryDW, _text, _textFormat, totalWidth, totalHeight);
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
    /// <param name="wrap">Text may wrap around when it is larger than <see cref="totalWidth"/></param>
    /// <param name="totalWidth"></param>
    /// <returns>The width of the text in graphics device units (pixels)</returns>
    public Size2F TextSize(string text, bool wrap = false, float totalWidth = float.NaN)
    {
      // If we won't wrap text or have no size limitations we consider full skin resolution as available space (TODO: check screen original size?)
      if (!wrap || float.IsNaN(totalWidth))
        totalWidth = SkinContext.SkinResources.SkinWidth;
      var totalHeight = SkinContext.SkinResources.SkinHeight;
      using (var textLayout = new TextLayout(GraphicsDevice11.Instance.FactoryDW, text, _textFormat, totalWidth, totalHeight))
      {
        var textWidth = textLayout.Metrics.WidthIncludingTrailingWhitespace;
        var textHeight = textLayout.Metrics.Height;
        // If there should be no wrap, then we calculate only one single line height
        if (!wrap)
          textHeight /= textLayout.Metrics.LineCount;

        return new Size2F(textWidth, textHeight);
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

    public bool IsAllocated
    {
      get { return _textLayout != null; }
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
    public void Allocate(RectangleF boxWidth, bool wrap)
    {
      if (String.IsNullOrEmpty(_text))
      {
        return;
      }

      // Preserve state
      _lastTextBox = boxWidth;
      _lastWrap = wrap;

      CreateTextLayout();

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
    /// Simplified draw method for this text.
    /// <para>
    /// The <see cref="TextBrush"/> has to be set before and will be used for text rendering. Usually this is a SolidColorBrush with desired color, but could also be
    /// some kind of GradientBrush for text rendering effects.
    /// </para>
    /// </summary>
    /// <param name="textBox">The box where the text should be drawn.</param>
    /// <param name="localRenderContext">RenderContext to apply transformations.</param>
    public void Render(RectangleF textBox, RenderContext localRenderContext)
    {
      Render(textBox, 0f, 0f, localRenderContext);
    }

    /// <summary>
    /// Draw method that supports scrolling of text if it is larger than the target <paramref name="textBox"/>.
    /// <para>
    /// The <see cref="TextBrush"/> has to be set before and will be used for text rendering. Usually this is a SolidColorBrush with desired color, but could also be
    /// some kind of GradientBrush for text rendering effects.
    /// </para>
    /// </summary>
    /// <param name="textBox">The box where the text should be drawn.</param>
    /// <param name="wrap">If <c>true</c> text is allowed to wrap into multiple lines.</param>
    /// <param name="scrollMode">Text scrolling behaviour.</param>
    /// <param name="scrollSpeed">Text scrolling speed in units (pixels at original skin size) per second.</param>
    /// <param name="scrollDelay">Text scrolling delay in seconds.</param>
    /// <param name="localRenderContext">RenderContext to apply transformations.</param>
    public void Render(RectangleF textBox, bool wrap, TextScrollEnum scrollMode, float scrollSpeed, float scrollDelay, RenderContext localRenderContext)
    {
      // Update scrolling
      var actualScrolling = scrollMode;
      if (scrollMode != TextScrollEnum.None && _lastTimeUsed != DateTime.MinValue)
        actualScrolling = UpdateScrollPosition(textBox, scrollMode, scrollSpeed, scrollDelay);

      Render(textBox, wrap, _scrollPos.X, _scrollPos.Y, actualScrolling, localRenderContext);
    }

    /// <summary>
    /// Draw method that supports virtual scrolling of text. If it is larger than the target <paramref name="textBox"/>, the position can be shifted by the given
    /// offsets (<see cref="offsetX"/> and <see cref="offsetY"/>). When using this method, no fading of text will be used.
    /// <para>
    /// The <see cref="TextBrush"/> has to be set before and will be used for text rendering. Usually this is a SolidColorBrush with desired color, but could also be
    /// some kind of GradientBrush for text rendering effects.
    /// </para>
    /// </summary>
    /// <param name="textBox">The box where the text should be drawn.</param>
    /// <param name="offsetX">Text rendering offset in x direction.</param>
    /// <param name="offsetY">Text rendering offset in y direction.</param>
    /// <param name="localRenderContext">RenderContext to apply transformations.</param>
    public void Render(RectangleF textBox, float offsetX, float offsetY, RenderContext localRenderContext)
    {
      Render(textBox, false, offsetX, offsetY, TextScrollEnum.None, localRenderContext);
    }

    protected void Render(RectangleF textBox, bool wrap, float offsetX, float offsetY, TextScrollEnum scrollMode, RenderContext localRenderContext)
    {
      if (_lastWrap != wrap || _textChanged)
      {
        Allocate(textBox, wrap);
      }

      // _textLayout can be null if no Text has been set before.
      var brush = TextBrush;
      if (brush != null && _textLayout != null)
      {
        Size2F totalTextSize = new Size2F(_textLayout.Metrics.Width, _textLayout.Metrics.Height);
        bool usingMask = false;
        bool textLargerThanTextbox = totalTextSize.Width > textBox.Width || totalTextSize.Height > textBox.Height;
        bool hasManualOffsets = scrollMode == TextScrollEnum.None && (offsetX != 0f || offsetY != 0f);
        if (textLargerThanTextbox || hasManualOffsets)
        {
          if (_opacityBrush == null || _lastTextBox != textBox || _lastScrollDirection != scrollMode)
          {
            _lastTextBox = textBox;
            _lastScrollDirection = scrollMode;
            DependencyObject.TryDispose(ref _opacityBrush);

            // Two different case for opacity mask handling:
            // 1) Text has offsets and might be clipped into viewport, then we use solid color to avoid fading
            // 2) Text is larger than text box, then we use a LinearGradientBrush to fade out side / bottom
            if (hasManualOffsets)
            {
              _opacityBrush = new SolidColorBrush(GraphicsDevice11.Instance.Context2D1, Color.Black);
            }
            else
            {
              // Horizontal from left to right
              Vector2 startPoint;
              Vector2 endPoint;
              float gradientFadeOffset;

              switch (scrollMode)
              {
                default:
                case TextScrollEnum.Left:
                  startPoint = new Vector2(0, 0);
                  endPoint = new Vector2(1, 0);
                  gradientFadeOffset = 1f - (FADE_SIZE / textBox.Width);
                  break;
                case TextScrollEnum.Right:
                  startPoint = new Vector2(1, 0);
                  endPoint = new Vector2(0, 0);
                  gradientFadeOffset = 1f - (FADE_SIZE / textBox.Width);
                  break;
                case TextScrollEnum.Up:
                  startPoint = new Vector2(0, 0);
                  endPoint = new Vector2(0, 1);
                  gradientFadeOffset = 1f - (FADE_SIZE / textBox.Height);
                  break;
                case TextScrollEnum.Down:
                  startPoint = new Vector2(0, 1);
                  endPoint = new Vector2(0, 0);
                  gradientFadeOffset = 1f - (FADE_SIZE / textBox.Height);
                  break;
              }

              LinearGradientBrushProperties properties = new LinearGradientBrushProperties { StartPoint = startPoint, EndPoint = endPoint };

              GradientStopCollection gradientStopCollection = new GradientStopCollection(GraphicsDevice11.Instance.Context2D1, new[]
              {
                new GradientStop { Position = 0, Color = Color.Black },
                new GradientStop { Position = gradientFadeOffset, Color = Color.Black }, // Transform into relative position, so it will render 15 pixel width
                new GradientStop { Position = 1, Color = Color.Transparent }
              });
              _opacityBrush = new LinearGradientBrush(GraphicsDevice11.Instance.Context2D1, properties, gradientStopCollection);
            }

            // Calculate actual gradient positions from transformed bounds
            var bounds = localRenderContext.OccupiedTransformedBounds;
            Matrix3x2 transform = Matrix.Identity;
            transform *= Matrix3x2.Scaling(bounds.Width, bounds.Height);
            transform *= Matrix3x2.Translation(bounds.X, bounds.Y);
            _opacityBrush.Transform = transform;
          }

          usingMask = true;
          var layerParams1 = new LayerParameters1
          {
            ContentBounds = localRenderContext.OccupiedTransformedBounds,
            LayerOptions = LayerOptions1.None,
            MaskAntialiasMode = AntialiasMode.PerPrimitive,
            MaskTransform = localRenderContext.Transform,
            Opacity = 1f,
            OpacityBrush = _opacityBrush
          };

          GraphicsDevice11.Instance.Context2D1.PushLayer(layerParams1, null);
        }

        // Render
        GraphicsDevice11.Instance.Context2D1.DrawTextLayout(new Vector2(textBox.X + offsetX, textBox.Y + offsetY), _textLayout, brush, localRenderContext);

        if (usingMask)
        {
          GraphicsDevice11.Instance.Context2D1.PopLayer();
        }
      }

      _lastTimeUsed = SkinContext.FrameRenderingStartTime;
    }

    #endregion

    #region Protected methods

    protected TextScrollEnum UpdateScrollPosition(RectangleF textBox, TextScrollEnum mode, float speed, float scrollDelay)
    {
      float dif = speed * (float)SkinContext.FrameRenderingStartTime.Subtract(_lastTimeUsed).TotalSeconds;

      if (mode == TextScrollEnum.Auto)
      {
        if (_textLayout.Metrics.Height > textBox.Height)
          mode = TextScrollEnum.Up;
        else if (_textLayout.Metrics.LineCount == 1 && _textLayout.Metrics.Width > textBox.Width)
          mode = TextScrollEnum.Left;
        else
          return TextScrollEnum.None;
      }

      if ((SkinContext.FrameRenderingStartTime - _scrollInitialized).TotalSeconds < scrollDelay)
        return mode;

      switch (mode)
      {
        case TextScrollEnum.Left:
          _scrollPos.X -= dif;
          if (_scrollPos.X + _textLayout.Metrics.Width < textBox.Width / 2.0f)
          {
            _scrollWrapOffset.X = _scrollPos.X;
            _scrollPos.X = textBox.Width + 4;
            _scrollWrapOffset.X -= _scrollPos.X;
          }
          else if (_scrollWrapOffset.X + _scrollPos.X + _textLayout.Metrics.Width < 0.0f)
            _scrollWrapOffset.X = float.NaN;
          break;
        case TextScrollEnum.Right:
          _scrollPos.X += dif;
          if (_scrollPos.X > textBox.Width / 2.0f)
          {
            _scrollWrapOffset.X = _scrollPos.X;
            _scrollPos.X = -_textLayout.Metrics.Width - 4;
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
            _scrollPos.Y = -_textLayout.Metrics.Height - 4;
            _scrollWrapOffset.Y -= _scrollPos.Y;
          }
          else if (_scrollWrapOffset.Y + _scrollPos.Y > textBox.Height)
            _scrollWrapOffset.Y = float.NaN;
          break;
        //case TextScrollEnum.Up:
        default:
          _scrollPos.Y -= dif;
          if (_scrollPos.Y + _textLayout.Metrics.Height < textBox.Height / 2.0f)
          {
            _scrollWrapOffset.Y = _scrollPos.Y;
            _scrollPos.Y = textBox.Height + 4;
            _scrollWrapOffset.Y -= _scrollPos.Y;
          }
          else if (_scrollWrapOffset.Y + _scrollPos.Y + _textLayout.Metrics.Height < 0.0f)
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
      else if (_textLayout.Metrics.Width > textBox.Width)
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
      else if (_textLayout.Metrics.Height > textBox.Height + _fontSize / 4)
      {
        fadeBorder.W = FADE_SIZE; // Fade on bottom edge
        dofade = true;
      }
      return dofade;
    }

    void DisposeFont()
    {
      DependencyObject.TryDispose(ref _textFormat);
      DependencyObject.TryDispose(ref _textLayout);
      DependencyObject.TryDispose(ref _opacityBrush);
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
