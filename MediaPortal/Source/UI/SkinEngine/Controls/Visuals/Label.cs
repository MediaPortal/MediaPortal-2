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
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;

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
    protected TextBuffer _asset = null;
    protected string _resourceString;

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
      _scrollProperty = new SProperty(typeof(TextScrollEnum), TextScrollEnum.None);
      _scrollSpeedProperty = new SProperty(typeof(double), DEFAULT_SCROLL_SPEED);
      _scrollDelayProperty = new SProperty(typeof(double), DEFAULT_SCROLL_DELAY);
      _wrapProperty = new SProperty(typeof(bool), false);

      HorizontalAlignment = HorizontalAlignmentEnum.Left;
      InitializeResourceString();
    }

    void Attach()
    {
      _contentProperty.Attach(OnContentChanged);
      _wrapProperty.Attach(OnLayoutPropertyChanged);
      _scrollProperty.Attach(OnLayoutPropertyChanged);

      HorizontalAlignmentProperty.Attach(OnLayoutPropertyChanged);
      VerticalAlignmentProperty.Attach(OnLayoutPropertyChanged);
      FontFamilyProperty.Attach(OnFontChanged);
      FontSizeProperty.Attach(OnFontChanged);

      HorizontalContentAlignmentProperty.Attach(OnLayoutPropertyChanged);
      VerticalContentAlignmentProperty.Attach(OnLayoutPropertyChanged);
    }

    void Detach()
    {
      _contentProperty.Detach(OnContentChanged);
      _wrapProperty.Detach(OnLayoutPropertyChanged);
      _scrollProperty.Detach(OnLayoutPropertyChanged);

      HorizontalAlignmentProperty.Detach(OnLayoutPropertyChanged);
      VerticalAlignmentProperty.Detach(OnLayoutPropertyChanged); 
      FontFamilyProperty.Detach(OnFontChanged);
      FontSizeProperty.Detach(OnFontChanged);

      HorizontalContentAlignmentProperty.Detach(OnLayoutPropertyChanged);
      VerticalContentAlignmentProperty.Detach(OnLayoutPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Label l = (Label) source;
      Content = l.Content;
      HorizontalAlignment = l.HorizontalAlignment;
      VerticalAlignment = l.VerticalAlignment;
      Color = l.Color;
      Scroll = l.Scroll;
      ScrollDelay = l.ScrollDelay;
      Wrap = l.Wrap;

      InitializeResourceString();
      Attach();
    }

    #endregion

    #region Property change handlers

    void OnContentChanged(AbstractProperty prop, object oldValue)
    {
      InitializeResourceString();
      if (_asset != null)
        _asset.Text = _resourceString;
      InvalidateLayout(true, false);
    }

    void OnLayoutPropertyChanged(AbstractProperty prop, object oldValue)
    {
      AllocFont();
      if (_asset != null)
        _asset.ResetScrollPosition();
      InvalidateLayout(true, false);
    }

    protected void OnFontChanged(AbstractProperty prop, object oldValue)
    {
      InvalidateLayout(true, false);
      if (_asset != null)
        _asset.SetFont(GetFontFamilyOrInherited(), GetFontSizeOrInherited());
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
      get { return (string) _contentProperty.GetValue(); }
      set { _contentProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the color of the label.
    /// </summary>
    public Color Color
    {
      get { return (Color) _colorProperty.GetValue(); }
      set { _colorProperty.SetValue(value); }
    }

    public AbstractProperty ColorProperty
    {
      get { return _colorProperty; }
    }

    /// <summary>
    /// Determines how to scroll text.
    /// </summary>
    public TextScrollEnum Scroll
    {
      get { return (TextScrollEnum) _scrollProperty.GetValue(); }
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
      get { return (double) _scrollDelayProperty.GetValue(); }
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
      get { return (double) _scrollSpeedProperty.GetValue(); }
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
      get { return (bool) _wrapProperty.GetValue(); }
      set { _wrapProperty.SetValue(value); }
    }

    public AbstractProperty WrapProperty
    {
      get { return _wrapProperty; }
    }

    #endregion

    void AllocFont()
    {
      if (_asset == null)
        _asset = new TextBuffer(GetFontFamilyOrInherited(), GetFontSizeOrInherited()) {Text = _resourceString};
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      base.CalculateInnerDesiredSize(totalSize); // Needs to be called in each sub class of Control, see comment in Control.CalculateInnerDesiredSize()
      AllocFont();

      // Measure the text
      float totalWidth = totalSize.Width; // Attention: totalWidth is cleaned up by SkinContext.Zoom
      if (!double.IsNaN(Width))
        totalWidth = (float) Width;

      SizeF size = SizeF.Empty;
      string[] lines = _asset.GetLines(totalWidth, Wrap);
      size.Width = 0;
      foreach (string line in lines)
        size.Width = Math.Max(size.Width, _asset.TextWidth(line));
      size.Height = _asset.TextHeight(Math.Max(lines.Length, 1));

      // Add one pixel to compensate rounding errors. Stops the label scrolling even though there is enough space.
      size.Width += 1;
      size.Height += 1;
      return size;
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

      _asset.Render(_innerRect, horzAlign, vertAlign, color, Wrap, true, localRenderContext.ZOrder, 
        Scroll, (float) ScrollSpeed, (float) ScrollDelay, localRenderContext.Transform);
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (_asset != null)
      {
        _asset.Dispose();
        _asset = null;
      }
    }

    public override void Dispose()
    {
      Deallocate();
      base.Dispose();
    }
  }
}

