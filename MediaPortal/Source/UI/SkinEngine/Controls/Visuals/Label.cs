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

using System;
using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX;
using Font = MediaPortal.UI.SkinEngine.Fonts.Font;
using FontBufferAsset = MediaPortal.UI.SkinEngine.ContentManagement.TextBufferAsset;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class Label : Control
  {
    #region Private & protected fields

    protected AbstractProperty _contentProperty;
    protected AbstractProperty _colorProperty;
    protected AbstractProperty _scrollProperty;
    protected AbstractProperty _scrollSpeedProperty;
    protected AbstractProperty _wrapProperty;
    protected AbstractProperty _horizontalContentAlignmentProperty;
    protected AbstractProperty _maxDesiredWidthProperty;
    protected FontBufferAsset _asset;
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
      _contentProperty = new SProperty(typeof(string), string.Empty);
      _colorProperty = new SProperty(typeof(Color), Color.White);
      _scrollProperty = new SProperty(typeof(TextScrollMode), TextScrollMode.None);
      _scrollSpeedProperty = new SProperty(typeof(double), 20.0);
      _wrapProperty = new SProperty(typeof(bool), false);
      _horizontalContentAlignmentProperty = new SProperty(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Left);
      _maxDesiredWidthProperty = new SProperty(typeof(double), double.NaN);

      HorizontalAlignment = HorizontalAlignmentEnum.Left;
      InitializeResourceString();
    }

    void Attach()
    {
      _contentProperty.Attach(OnContentChanged);
      _wrapProperty.Attach(OnLayoutPropertyChanged);
      _scrollProperty.Attach(OnLayoutPropertyChanged);
      _maxDesiredWidthProperty.Attach(OnLayoutPropertyChanged);
    }

    void Detach()
    {
      _contentProperty.Detach(OnContentChanged);
      _wrapProperty.Detach(OnLayoutPropertyChanged);
      _scrollProperty.Detach(OnLayoutPropertyChanged);
      _maxDesiredWidthProperty.Detach(OnLayoutPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Label l = (Label) source;
      Content = l.Content;
      Color = l.Color;
      Scroll = l.Scroll;
      Wrap = l.Wrap;
      MaxDesiredWidth = l.MaxDesiredWidth;

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
      InvalidateLayout();
    }

    void OnLayoutPropertyChanged(AbstractProperty prop, object oldValue)
    {
      if (_asset != null)
        _asset.ResetScrollPosition();
      InvalidateLayout();
    }

    protected override void OnFontChanged(AbstractProperty prop, object oldValue)
    {
      ClearAsset();
    }

    #endregion

    protected void ClearAsset()
    {
      TextBufferAsset asset = _asset;
      _asset = null;
      if (asset != null)
        asset.Free(true);
    }

    protected void InitializeResourceString()
    {
      string content = Content;
      _resourceString = string.IsNullOrEmpty(content) ? string.Empty : LocalizationHelper.CreateResourceString(content).Evaluate();
    }

    #region Public properties

    /// <summary>
    /// Gets or sets the content text for the Label.
    /// </summary>
    public string Content
    {
      get { return _contentProperty.GetValue() as string; }
      set { _contentProperty.SetValue(value); }
    }

    public AbstractProperty ContentProperty
    {
      get { return _contentProperty; }
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
    public TextScrollMode Scroll
    {
      get { return (TextScrollMode) _scrollProperty.GetValue(); }
      set { _scrollProperty.SetValue(value); }
    }

    public AbstractProperty ScrollProperty
    {
      get { return _scrollProperty; }
    }

    /// <summary>
    /// Gets or sets the scroll speed for text in skin units (1 unit = 1 pixel at native skin resolution).
    /// <see cref="Scroll"/> must also be set for this to have an effect.
    /// TODO: Skin units per what? Second?
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

    /// <summary>
    /// Will be evaluated if <see cref="Scroll"/> or <see cref="Wrap"/> is set to give a maximum width of this label.
    /// This property differs from the <see cref="FrameworkElement.Width"/> property, as this label doesn't always occupy
    /// the whole maximum width.
    /// </summary>
    public double MaxDesiredWidth
    {
      get { return (double) _maxDesiredWidthProperty.GetValue(); }
      set { _maxDesiredWidthProperty.SetValue(value); }
    }

    public AbstractProperty MaxDesiredWidthProperty
    {
      get { return _maxDesiredWidthProperty; }
    }

    /// <summary>
    /// Gets or sets the horizontal text alignment.
    /// </summary>
    public HorizontalAlignmentEnum HorizontalContentAlignment
    {
      get { return (HorizontalAlignmentEnum) _horizontalContentAlignmentProperty.GetValue(); }
      set { _horizontalContentAlignmentProperty.SetValue(value); }
    }

    public AbstractProperty HorizontalContentAlignmentProperty
    {
      get { return _horizontalContentAlignmentProperty; }
    }

    #endregion

    void AllocFont()
    {
      if (_asset == null)
      {
        _asset = ContentManager.GetTextBuffer(GetFontFamilyOrInherited(), GetFontSizeOrInherited(), true);
        _asset.Text = _resourceString;
      }
    }

    protected override SizeF CalculateDesiredSize(SizeF totalSize)
    {
      AllocFont();
      if (_asset == null)
        return SizeF.Empty;

      // Measure the text
      SizeF size = new SizeF();
      float totalWidth; // Attention: totalWidth is cleaned up by SkinContext.Zoom
      if (double.IsNaN(Width))
        if ((Scroll != TextScrollMode.None || Wrap) && !double.IsNaN(MaxDesiredWidth))
          // MaxDesiredWidth will only be evaluated if either Scroll or Wrap is set
          totalWidth = (float) MaxDesiredWidth;
        else
          // No size constraints
          totalWidth = totalSize.Width;
      else
        // Width: highest priority
        totalWidth = (float) Width;
      if (Wrap)
      { // If Width property set and Wrap property set, we need to calculate the number of necessary text lines
        string[] lines = _asset.WrapText(totalWidth);
        size.Width = 0;
        foreach (string line in lines)
          size.Width = Math.Max(size.Width, _asset.TextWidth(line));
        size.Height = _asset.TextHeight(Math.Max(lines.Length, 1));
      }
      else
      {
        size.Width = _asset.TextWidth(_resourceString);
        if (!float.IsNaN(totalWidth))
          size.Width = Math.Min(size.Width, totalWidth);
        size.Height = _asset.TextHeight(1);
      }
      // Add one pixel to compensate rounding errors. Avoids that the label scrolls although there is enough space.
      size.Width += 1;
      size.Height += 1;
      return size;
    }

    public override void DoRender(RenderContext localRenderContext)
    {
      base.DoRender(localRenderContext);

      AllocFont();
      if (_asset == null)
        return;

      Font.Align align = Font.Align.Left;
      if (HorizontalContentAlignment == HorizontalAlignmentEnum.Right)
        align = Font.Align.Right;
      else if (HorizontalContentAlignment == HorizontalAlignmentEnum.Center)
        align = Font.Align.Center;

      Color4 color = ColorConverter.FromColor(Color);
      color.Alpha *= (float) localRenderContext.Opacity;

      _asset.Render(_innerRect, align, color, Wrap, localRenderContext.ZOrder, 
        Scroll, (float) ScrollSpeed, localRenderContext.Transform);
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

