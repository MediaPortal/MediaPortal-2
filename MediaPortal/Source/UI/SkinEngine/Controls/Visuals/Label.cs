#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Windows;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  // TODO: We don't notice font changes if font is declared on a parent element, so add a virtual font change handler in parent
  public class Label : Control
  {
    public const double DEFAULT_SCROLL_SPEED = 20.0;
    public const double DEFAULT_SCROLL_DELAY = 2.0;

    #region Protected fields

    protected AbstractProperty _contentProperty;
    protected AbstractProperty _colorProperty;
    protected AbstractProperty _scrollProperty;
    protected AbstractProperty _scrollSpeedProperty;
    protected AbstractProperty _scrollDelayProperty;
    protected AbstractProperty _wrapProperty;
    protected AbstractProperty _lineHeightProperty;
    protected AbstractProperty _textTrimmingProperty;
    protected string _resourceString;
    private TextBuffer2D _asset;
    private SolidColorBrush _textBrush;

    #endregion

    #region Ctor

    public Label()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _contentProperty = new SProperty(typeof(string), null);
      _colorProperty = new SProperty(typeof(Color), Color.DarkViolet);
      _scrollProperty = new SProperty(typeof(TextBuffer2D.TextScrollEnum), TextBuffer2D.TextScrollEnum.None);
      _scrollSpeedProperty = new SProperty(typeof(double), DEFAULT_SCROLL_SPEED);
      _scrollDelayProperty = new SProperty(typeof(double), DEFAULT_SCROLL_DELAY);
      _wrapProperty = new SProperty(typeof(bool), false);
      _lineHeightProperty = new SProperty(typeof(double), Double.NaN);
      _textTrimmingProperty = new SProperty(typeof(TextTrimming), TextTrimming.None);

      HorizontalAlignment = HorizontalAlignmentEnum.Left;
      InitializeResourceString();
    }

    void Attach()
    {
      _contentProperty.Attach(OnContentChanged);
      _wrapProperty.Attach(OnLayoutPropertyChanged);
      _lineHeightProperty.Attach(OnLayoutPropertyChanged);
      _textTrimmingProperty.Attach(OnLayoutPropertyChanged);
      _scrollProperty.Attach(OnLayoutPropertyChanged);
      _scrollSpeedProperty.Attach(OnLayoutPropertyChanged);
      _scrollDelayProperty.Attach(OnLayoutPropertyChanged);
      _colorProperty.Attach(OnColorPropertyChanged);

      HorizontalAlignmentProperty.Attach(OnLayoutPropertyChanged);
      VerticalAlignmentProperty.Attach(OnLayoutPropertyChanged);
      FontFamilyProperty.Attach(OnFontChanged);
      FontSizeProperty.Attach(OnFontChanged);
      FontWeightProperty.Attach(OnFontChanged);
      FontStyleProperty.Attach(OnFontChanged);

      HorizontalContentAlignmentProperty.Attach(OnLayoutPropertyChanged);
      VerticalContentAlignmentProperty.Attach(OnLayoutPropertyChanged);
    }

    void Detach()
    {
      _contentProperty.Detach(OnContentChanged);
      _wrapProperty.Detach(OnLayoutPropertyChanged);
      _lineHeightProperty.Detach(OnLayoutPropertyChanged);
      _textTrimmingProperty.Detach(OnLayoutPropertyChanged);
      _scrollProperty.Detach(OnLayoutPropertyChanged);
      _scrollSpeedProperty.Detach(OnLayoutPropertyChanged);
      _scrollDelayProperty.Detach(OnLayoutPropertyChanged);
      _colorProperty.Detach(OnColorPropertyChanged);

      HorizontalAlignmentProperty.Detach(OnLayoutPropertyChanged);
      VerticalAlignmentProperty.Detach(OnLayoutPropertyChanged);
      FontFamilyProperty.Detach(OnFontChanged);
      FontSizeProperty.Detach(OnFontChanged);
      FontWeightProperty.Detach(OnFontChanged);
      FontStyleProperty.Detach(OnFontChanged);

      HorizontalContentAlignmentProperty.Detach(OnLayoutPropertyChanged);
      VerticalContentAlignmentProperty.Detach(OnLayoutPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Label l = (Label)source;
      Content = l.Content;
      HorizontalAlignment = l.HorizontalAlignment;
      VerticalAlignment = l.VerticalAlignment;
      Color = l.Color;
      Scroll = l.Scroll;
      ScrollDelay = l.ScrollDelay;
      ScrollSpeed = l.ScrollSpeed;
      Wrap = l.Wrap;
      LineHeight = l.LineHeight;
      TextTrimming = l.TextTrimming;
      InitializeResourceString();
      Attach();
    }

    #endregion

    #region Property change handlers

    void OnContentChanged(AbstractProperty prop, object oldValue)
    {
      InitializeResourceString();
      ReAllocFont();
      InvalidateLayout(true, false);
    }

    void OnLayoutPropertyChanged(AbstractProperty prop, object oldValue)
    {
      ReAllocFont();
      InvalidateLayout(true, false);
    }

    protected void OnFontChanged(AbstractProperty prop, object oldValue)
    {
      InvalidateLayout(true, false);
      ReAllocFont();
    }

    private void OnColorPropertyChanged(AbstractProperty property, object oldvalue)
    {
      if (_textBrush != null)
      {
        _textBrush.Color = Color;
      }
    }

    #endregion

    protected void InitializeResourceString()
    {
      string content = Content;
      _resourceString = string.IsNullOrEmpty(content) ? string.Empty :
          LocalizationHelper.CreateResourceString(content).Evaluate();
    }

    #region Public properties

    public AbstractProperty ContentProperty
    {
      get { return _contentProperty; }
    }

    public string Content
    {
      get { return (string)_contentProperty.GetValue(); }
      set { _contentProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the color of the label.
    /// </summary>
    public Color Color
    {
      get { return (Color)_colorProperty.GetValue(); }
      set { _colorProperty.SetValue(value); }
    }

    public AbstractProperty ColorProperty
    {
      get { return _colorProperty; }
    }

    /// <summary>
    /// Determines how to scroll text.
    /// </summary>
    public TextBuffer2D.TextScrollEnum Scroll
    {
      get { return (TextBuffer2D.TextScrollEnum)_scrollProperty.GetValue(); }
      set { _scrollProperty.SetValue(value); }
    }

    public AbstractProperty ScrollProperty
    {
      get { return _scrollProperty; }
    }

    /// <summary>
    /// Sets the delay in seconds before scrolling starts.
    /// </summary>
    public Double ScrollDelay
    {
      get { return (double)_scrollDelayProperty.GetValue(); }
      set { _scrollDelayProperty.SetValue(value); }
    }

    public AbstractProperty ScrollDelayProperty
    {
      get { return _scrollDelayProperty; }
    }

    /// <summary>
    /// Gets or sets the scroll speed for text in skin units per second (1 unit = 1 pixel at native skin resolution).
    /// <see cref="Scroll"/> must also be set for this to have an effect.
    /// </summary>
    public double ScrollSpeed
    {
      get { return (double)_scrollSpeedProperty.GetValue(); }
      set { _scrollSpeedProperty.SetValue(value); }
    }

    public AbstractProperty ScrollSpeedProperty
    {
      get { return _scrollSpeedProperty; }
    }

    /// <summary>
    /// Gets or sets whether content text should be horizontally wrapped when it longer than can fit on a single line.
    /// </summary>
    public bool Wrap
    {
      get { return (bool)_wrapProperty.GetValue(); }
      set { _wrapProperty.SetValue(value); }
    }

    public AbstractProperty WrapProperty
    {
      get { return _wrapProperty; }
    }

    /// <summary>
    /// Gets or sets whether content text should be trimmed if it does not fit within the available space and any remaining text replaced with an ellipsis (...) 
    /// </summary>
    public double LineHeight
    {
      get { return (double)_lineHeightProperty.GetValue(); }
      set { _lineHeightProperty.SetValue(value); }
    }

    public AbstractProperty LineHeightProperty
    {
      get { return _lineHeightProperty; }
    }

    /// <summary>
    /// Gets or sets whether content text should be trimmed if it does not fit within the available space and any remaining text replaced with an ellipsis (...) 
    /// </summary>
    public TextTrimming TextTrimming
    {
      get { return (TextTrimming)_textTrimmingProperty.GetValue(); }
      set { _textTrimmingProperty.SetValue(value); }
    }

    public AbstractProperty TextTrimmingProperty
    {
      get { return _textTrimmingProperty; }
    }

    #endregion

    void ReAllocFont()
    {
      DeAllocFont();
      AllocFont();
    }

    private void DeAllocFont()
    {
      TryDispose(ref _asset);
      TryDispose(ref _textBrush);
    }

    void AllocFont()
    {
      // HACK: avoid NREs during style load time
      if (GraphicsDevice11.Instance.FactoryDW == null)
        return;

      if (_asset == null)
      {
        _asset = new TextBuffer2D(GetFontFamilyOrInherited(), GetFontWeightOrInherited(), GetFontStyleOrInherited(), GetFontSizeOrInherited(), LineHeight);
        _asset.Text = _resourceString;
      }
      if (_textBrush == null)
      {
        _textBrush = new SolidColorBrush(GraphicsDevice11.Instance.Context2D1, Color);
      }
    }

    protected override Size2F CalculateInnerDesiredSize(Size2F totalSize)
    {
      base.CalculateInnerDesiredSize(totalSize); // Needs to be called in each sub class of Control, see comment in Control.CalculateInnerDesiredSize()
      // Measure the text
      float totalWidth = totalSize.Width; // Attention: totalWidth is cleaned up by SkinContext.Zoom
      if (!double.IsNaN(Width))
        totalWidth = (float)Width;
      if (float.IsNaN(totalWidth))
        totalWidth = 4096;
      Size2F size = new Size2F { Width = 0 };

      ReAllocFont(); // Make sure to recreate asset to match current font metrics
      if (_asset == null)
        return size;

      return _asset.TextSize(_resourceString, Wrap, totalWidth);
    }

    public override void RenderOverride(RenderContext localRenderContext)
    {
      base.RenderOverride(localRenderContext);

      AllocFont();

      // TODO: add alignment handling to TextBuffer2D
      TextBuffer2D.HorizontalTextAlignEnum horzAlign = TextBuffer2D.HorizontalTextAlignEnum.Left;
      if (HorizontalContentAlignment == HorizontalAlignmentEnum.Right)
        horzAlign = TextBuffer2D.HorizontalTextAlignEnum.Right;
      else if (HorizontalContentAlignment == HorizontalAlignmentEnum.Center)
        horzAlign = TextBuffer2D.HorizontalTextAlignEnum.Center;

      TextBuffer2D.VerticalTextAlignEnum vertAlign = TextBuffer2D.VerticalTextAlignEnum.Top;
      if (VerticalContentAlignment == VerticalAlignmentEnum.Bottom)
        vertAlign = TextBuffer2D.VerticalTextAlignEnum.Bottom;
      else if (VerticalContentAlignment == VerticalAlignmentEnum.Center)
        vertAlign = TextBuffer2D.VerticalTextAlignEnum.Center;

      Color4 color = ColorConverter.FromColor(Color);
      color.Alpha *= (float)localRenderContext.Opacity;

      _asset.TextBrush = _textBrush;

      _asset.Render(_innerRect, Wrap, Scroll, (float)ScrollSpeed, (float)ScrollDelay, localRenderContext);
    }

    public override void Deallocate()
    {
      base.Deallocate();
      DeAllocFont();
    }

    public override void Dispose()
    {
      Deallocate();
      base.Dispose();
    }
  }
}

