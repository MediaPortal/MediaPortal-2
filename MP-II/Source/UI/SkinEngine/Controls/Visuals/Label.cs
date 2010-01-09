#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.UI.SkinEngine.ContentManagement;
using SlimDX;
using Font = MediaPortal.UI.SkinEngine.Fonts.Font;
using FontRender = MediaPortal.UI.SkinEngine.ContentManagement.FontRender;
using FontBufferAsset = MediaPortal.UI.SkinEngine.ContentManagement.FontBufferAsset;
using FontManager = MediaPortal.UI.SkinEngine.Fonts.FontManager;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class Label : Control
  {
    #region Private fields

    protected AbstractProperty _contentProperty;
    protected AbstractProperty _colorProperty;
    protected AbstractProperty _scrollProperty;
    protected AbstractProperty _wrapProperty;
    protected AbstractProperty _textAlignProperty;
    protected AbstractProperty _maxDesiredWidthProperty;
    protected FontBufferAsset _asset;
    protected FontRender _renderer;
    protected IResourceString _resourceString;
    private int _fontSizeCache;

    #endregion

    #region Ctor

    public Label()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _contentProperty = new SProperty(typeof(string), "");
      _colorProperty = new SProperty(typeof(Color), Color.White);
      _scrollProperty = new SProperty(typeof(bool), false);
      _wrapProperty = new SProperty(typeof(bool), false);
      _textAlignProperty = new SProperty(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Left);
      _maxDesiredWidthProperty = new SProperty(typeof(double), double.NaN);

      HorizontalAlignment = HorizontalAlignmentEnum.Left;
    }

    void Attach()
    {
      _contentProperty.Attach(OnContentChanged);
      _scrollProperty.Attach(OnRenderAttributeChanged);
      _wrapProperty.Attach(OnLayoutPropertyChanged);
      _colorProperty.Attach(OnRenderAttributeChanged);
      _textAlignProperty.Attach(OnRenderAttributeChanged);
      _maxDesiredWidthProperty.Attach(OnLayoutPropertyChanged);
    }

    void Detach()
    {
      _contentProperty.Detach(OnContentChanged);
      _scrollProperty.Detach(OnRenderAttributeChanged);
      _wrapProperty.Detach(OnLayoutPropertyChanged);
      _colorProperty.Detach(OnRenderAttributeChanged);
      _textAlignProperty.Detach(OnRenderAttributeChanged);
      _maxDesiredWidthProperty.Detach(OnLayoutPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Label l = (Label) source;
      Content = copyManager.GetCopy(l.Content);
      Color = copyManager.GetCopy(l.Color);
      Scroll = copyManager.GetCopy(l.Scroll);
      Wrap = copyManager.GetCopy(l.Wrap);
      MaxDesiredWidth = copyManager.GetCopy(l.MaxDesiredWidth);

      _resourceString = LocalizationHelper.CreateResourceString(Content);
      Attach();
    }

    #endregion

    void OnContentChanged(AbstractProperty prop, object oldValue)
    {
      _resourceString = Content == null ? null : LocalizationHelper.CreateResourceString(Content);
      Invalidate();
    }

    void OnRenderAttributeChanged(AbstractProperty prop, object oldValue)
    {
      if (Screen != null)
        Screen.Invalidate(this);
    }

    void OnLayoutPropertyChanged(AbstractProperty prop, object oldValue)
    {
      Invalidate();
    }

    protected override void OnFontChanged(AbstractProperty prop, object oldValue)
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

    public AbstractProperty ContentProperty
    {
      get { return _contentProperty; }
    }

    public string Content
    {
      get { return _contentProperty.GetValue() as string; }
      set { _contentProperty.SetValue(value); }
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

    public AbstractProperty ScrollProperty
    {
      get { return _scrollProperty; }
    }

    public bool Scroll
    {
      get { return (bool) _scrollProperty.GetValue(); }
      set { _scrollProperty.SetValue(value); }
    }

    public AbstractProperty WrapProperty
    {
      get { return _wrapProperty; }
    }

    public bool Wrap
    {
      get { return (bool) _wrapProperty.GetValue(); }
      set { _wrapProperty.SetValue(value); }
    }

    public AbstractProperty MaxDesiredWidthProperty
    {
      get { return _maxDesiredWidthProperty; }
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
      if (_renderer == null && _asset != null && _asset.Font != null)
        _renderer = new FontRender(_asset.Font);
    }

    /// <summary>
    /// Wraps the text of this label to the specified <paramref name="maxWidth"/> and returns the wrapped
    /// text parts.
    /// </summary>
    /// <param name="maxWidth">Maximum available width until the text should be wrapped.</param>
    /// <param name="findWordBoundaries">If set to <c>true</c>, this method will wrap the text
    /// at word boundaries. Else, it will wrap at the last character index which fits into the specified
    /// <paramref name="maxWidth"/>.</param>
    protected string[] WrapText(float maxWidth, bool findWordBoundaries)
    {
      string text = _resourceString.Evaluate();
      if (string.IsNullOrEmpty(text))
        return new string[0];
      IList<string> result = new List<string>();
      foreach (string para in text.Replace("\r\n", "\n").Split('\n'))
      {
        string paragraph = para.Trim();
        for (int nextIndex = 0; nextIndex < paragraph.Length; )
        {
          while (char.IsWhiteSpace(paragraph[nextIndex]))
            nextIndex++;
          int startIndex = nextIndex;
          nextIndex = _asset.Font.CalculateMaxSubstring(paragraph, _fontSizeCache, startIndex, maxWidth);
          if (findWordBoundaries && nextIndex < paragraph.Length)
          {
            int lastFitWordBoundary = paragraph.LastIndexOf(' ', nextIndex);
            while (lastFitWordBoundary > startIndex && char.IsWhiteSpace(paragraph[lastFitWordBoundary - 1]))
              lastFitWordBoundary--;
            if (lastFitWordBoundary > startIndex)
              nextIndex = lastFitWordBoundary;
          }
          result.Add(paragraph.Substring(startIndex, nextIndex - startIndex));
        }
      }
      return result.ToArray();
    }

    public override void Measure(ref SizeF totalSize)
    {
      RemoveMargin(ref totalSize);
      InitializeTriggers();

      _fontSizeCache = GetFontSizeOrInherited();
      AllocFont();

      SizeF childSize;
      // Measure the text
      if (_resourceString != null && _asset != null)
      {
        float height = _asset.Font.LineHeight(_fontSizeCache);
        float width;
        float totalWidth; // Attention: totalWidth is cleaned up by SkinContext.Zoom
        if (double.IsNaN(Width))
          if ((Scroll || Wrap) && !double.IsNaN(MaxDesiredWidth))
            // MaxDesiredWidth will only be evaluated if either Scroll or Wrap is set
            totalWidth = (float) MaxDesiredWidth;
          else
            // No size constraints
            totalWidth = totalSize.Width / SkinContext.Zoom.Width;
        else
          // Width: highest priority
          totalWidth = (float) Width;
        if (Wrap)
        { // If Width property set and Wrap property set, we need to calculate the number of necessary text lines
          string[] lines = WrapText(totalWidth, true);
          width = 0;
          foreach (string line in lines)
            width = Math.Max(width, _asset.Font.Width(line, _fontSizeCache));
          height *= lines.Length;
        }
        else if (float.IsNaN(totalWidth) || !Scroll)
          width = _asset.Font.Width(_resourceString.Evaluate(), _fontSizeCache);
        else
          width = totalWidth;

        childSize = new SizeF(width * SkinContext.Zoom.Width, height * SkinContext.Zoom.Height);
      }
      else
        childSize = new SizeF();

      _desiredSize = new SizeF((float) Width * SkinContext.Zoom.Width, (float) Height * SkinContext.Zoom.Height);

      if (double.IsNaN(Width))
        _desiredSize.Width = childSize.Width;

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

      //Trace.WriteLine(String.Format("Label.Measure: {0} returns {1}x{2}", _label.ToString(), (int)totalSize.Width, (int)totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("Label.Arrange: {0} X {1},Y {2} W {3} H {4}", _label.ToString(), (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));

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
      if (!IsVisible) 
        return;
      if (_asset == null) 
        return;

      base.DoRender();

      // The characters fit the textbox exactly, so to get some room between the top of the characters 
      // and the inner rectangle, move the text down (10% of font size) also reduce the font size to 90%
      // of the value. Otherwise we will be outside of the inner rectangle.

      float lineHeight = _asset.Font.LineHeight(_fontSizeCache);
      float x = ActualPosition.X;
      float y = ActualPosition.Y + 0.05f * lineHeight * SkinContext.Zoom.Height;
      float w = (float) ActualWidth;
      float h = (float) ActualHeight;
      if (_finalLayoutTransform != null)
      {
        GraphicsDevice.TransformWorld *= _finalLayoutTransform.Matrix;

        _finalLayoutTransform.InvertXY(ref x, ref y);
        _finalLayoutTransform.InvertXY(ref w, ref h);
      }
      RectangleF rect = new RectangleF(x, y, w, h);
      Font.Align align = Font.Align.Left;
      if (TextAlign == HorizontalAlignmentEnum.Right)
        align = Font.Align.Right;
      else if (TextAlign == HorizontalAlignmentEnum.Center)
        align = Font.Align.Center;

      ExtendedMatrix m = new ExtendedMatrix
        {
            Matrix = Matrix.Translation(-rect.X, -rect.Y, 0)
        };
      m.Matrix *= Matrix.Scaling(SkinContext.Zoom.Width, SkinContext.Zoom.Height, 1);
      m.Matrix *= Matrix.Translation(rect.X, rect.Y, 0);
      SkinContext.AddTransform(m);

      Color4 color = ColorConverter.FromColor(Color);
      color.Alpha *= (float) SkinContext.Opacity;
      color.Alpha *= (float) Opacity;
      
      if (_resourceString != null)
      {
        bool scroll = Scroll && !Wrap;
        string[] lines = Wrap ? WrapText(_finalRect.Width / SkinContext.Zoom.Width, true) : new string[] { _resourceString.Evaluate() };

        foreach (string line in lines)
        {
          float totalWidth;
          _renderer.Draw(line, rect, ActualPosition.Z, align, _fontSizeCache * 0.9f, color, scroll, out totalWidth);
          rect.Y += lineHeight;
        }
      }

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

      base.DoRender();

      float lineHeight = _asset.Font.LineHeight(_fontSizeCache);

      // The characters fits the textbox exactly, so to get some room between the top of the characters 
      // and the inner rectangle. Move the text down (10% of font size) also reduce the font size to 90%
      // of the value. Otherwise we will be outside of the inner rectangle.

      float y = _finalRect.Y + 0.05f * lineHeight * SkinContext.Zoom.Height;
      float x = _finalRect.X;
      float w = _finalRect.Width;
      float h = _finalRect.Height;

      if (_finalLayoutTransform != null)
      {
        GraphicsDevice.TransformWorld *= _finalLayoutTransform.Matrix;

        _finalLayoutTransform.InvertXY(ref x, ref y);
        _finalLayoutTransform.InvertXY(ref w, ref h);
      }
      RectangleF rect = new RectangleF(x, y, w, h);
      Font.Align align = Font.Align.Left;
      if (TextAlign == HorizontalAlignmentEnum.Right)
        align = Font.Align.Right;
      else if (TextAlign == HorizontalAlignmentEnum.Center)
        align = Font.Align.Center;

      ExtendedMatrix m = new ExtendedMatrix
        {
            Matrix = Matrix.Translation(-rect.X, -rect.Y, 0)
        };
      m.Matrix *= Matrix.Scaling(SkinContext.Zoom.Width, SkinContext.Zoom.Height, 1);
      m.Matrix *= Matrix.Translation(rect.X, rect.Y, 0);
      SkinContext.AddTransform(m);

      Color4 color = ColorConverter.FromColor(Color);
      color.Alpha *= (float) SkinContext.Opacity;
      color.Alpha *= (float) Opacity;

      if (_resourceString != null)
      {
        bool scroll = Scroll && !Wrap;
        string[] lines = Wrap ? WrapText(_finalRect.Width / SkinContext.Zoom.Width, true) : new string[] { _resourceString.Evaluate() };

        foreach (string line in lines)
        {
          float totalWidth;
          _asset.Draw(line, rect, align, _fontSizeCache * 0.9f, color, scroll, out totalWidth);
          rect.Y += lineHeight;
        }
      }
      SkinContext.RemoveTransform();
    }

    public override void Deallocate()
    {
      //Trace.WriteLine("lbl Deallocate:" + Content);
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
     // Trace.WriteLine("lbl BecomesHidden:" + Content);
      if (_renderer != null)
        _renderer.Free();
    }

    public override void BecomesVisible()
    {
      //Trace.WriteLine("lbl BecomesVisible:" + Content);
      if (_renderer != null)
      {
        _renderer.Alloc();
        DoBuildRenderTree();
      }
    }
    
    public override void Update()
    {
      //Trace.WriteLine("Label.Update");
      base.Update();
      if (!_hidden)
        DoBuildRenderTree();

      // If the text is scrolling, then we must keep on building the render tree
      // Otherwise it won't scroll.
      if (Scroll && Screen != null)
          Screen.Invalidate(this);
    }
  }
}

