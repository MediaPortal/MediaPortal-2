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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localization;
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
  public class Label : Control
  {
    #region Private fields

    Property _contentProperty;
    Property _colorProperty;
    Property _scrollProperty;
    Property _wrapProperty;
    FontBufferAsset _asset;
    FontRender _renderer;
    IResourceString _resourceString;

    #endregion

    #region Ctor

    public Label()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _contentProperty = new Property(typeof(string), "");
      _colorProperty = new Property(typeof(Color), Color.White);
      _scrollProperty = new Property(typeof(bool), false);
      _wrapProperty = new Property(typeof(bool), false);

      HorizontalAlignment = HorizontalAlignmentEnum.Left;
    }

    void Attach()
    {
      _contentProperty.Attach(OnContentChanged);
      _scrollProperty.Attach(OnScrollChanged);
      _wrapProperty.Attach(OnWrapChanged);
      _colorProperty.Attach(OnColorChanged);
    }

    void Detach()
    {
      _contentProperty.Detach(OnContentChanged);
      _scrollProperty.Detach(OnScrollChanged);
      _wrapProperty.Detach(OnWrapChanged);
      _colorProperty.Detach(OnColorChanged);
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

      _resourceString = LocalizationHelper.CreateResourceString(Content);
      Attach();
    }

    #endregion

    void OnColorChanged(Property prop)
    {
      if (Screen != null) 
        Screen.Invalidate(this);
    }

    void OnContentChanged(Property prop)
    {
      _resourceString = Content == null ? null : LocalizationHelper.CreateResourceString(Content);
      Invalidate();
    }

    void OnScrollChanged(Property prop)
    {
      if (Screen != null) 
        Screen.Invalidate(this);
    }

    void OnWrapChanged(Property prop)
    {
      Invalidate();
    }

    protected override void OnFontChanged(Property prop)
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

    public Property ContentProperty
    {
      get { return _contentProperty; }
    }

    public string Content
    {
      get { return _contentProperty.GetValue() as string; }
      set { _contentProperty.SetValue(value); }
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

    public Property ScrollProperty
    {
      get { return _scrollProperty; }
    }

    public bool Scroll
    {
      get { return (bool) _scrollProperty.GetValue(); }
      set { _scrollProperty.SetValue(value); }
    }

    public Property WrapProperty
    {
      get { return _wrapProperty; }
    }

    public bool Wrap
    {
      get { return (bool) _wrapProperty.GetValue(); }
      set { _wrapProperty.SetValue(value); }
    }

    void AllocFont()
    {
      if (_asset == null)
      {
        // Get default values if not set
        if (FontSize == 0)
          FontSize = FontManager.DefaultFontSize;
        if (FontFamily == string.Empty)
          FontFamily = FontManager.DefaultFontFamily;

        // We want to select the font based on the maximum zoom height (fullscreen)
        // This means that the font will be scaled down in windowed mode, but look
        // good in full screen. 
        Font font = FontManager.GetScript(FontFamily, (int)(FontSize * SkinContext.MaxZoomHeight));
        if (font != null)
          _asset = ContentManager.GetFont(font);
      }

      if (_renderer == null)
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
          nextIndex = _asset.Font.CalculateMaxSubstring(paragraph, FontSize, startIndex, maxWidth);
          if (findWordBoundaries && nextIndex < paragraph.Length && !char.IsWhiteSpace(paragraph[nextIndex]))
          {
            int wordBoundary = paragraph.LastIndexOf(' ', nextIndex - 1);
            while (wordBoundary > startIndex && char.IsWhiteSpace(paragraph[wordBoundary - 1]))
              wordBoundary--;
            if (wordBoundary > startIndex && wordBoundary < nextIndex)
              nextIndex = wordBoundary;
          }
          result.Add(paragraph.Substring(startIndex, nextIndex - startIndex));
        }
      }
      return result.ToArray();
    }

    public override void Measure(ref SizeF totalSize)
    {
      SizeF childSize;
      
      InitializeTriggers();
      AllocFont();

      // Measure the text
      if (_resourceString != null && _asset != null)
      {
        float height = _asset.Font.LineHeight(FontSize);
        float width;
        if (double.IsNaN(Width) || !Wrap)
          width = _asset.Font.Width(_resourceString.Evaluate(), FontSize);
        else
        { // If Width property set and Wrap property set, we need to calculate the number of necessary text lines
          string[] lines = WrapText((float) Width, true);
          width = 0; // Not used, as Width overrides our calculated width
          height *= lines.Length;
        }
        childSize = new SizeF(width * SkinContext.Zoom.Width, height * SkinContext.Zoom.Height);
      }
      else
        childSize = new SizeF();

      _desiredSize = new SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);

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

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();

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

      float x = ActualPosition.X;
      float y = ActualPosition.Y + 0.1f * FontSize * SkinContext.Zoom.Height;
      float w = (float)ActualWidth;
      float h = (float)ActualHeight;
      if (_finalLayoutTransform != null)
      {
        GraphicsDevice.TransformWorld *= _finalLayoutTransform.Matrix;

        _finalLayoutTransform.InvertXY(ref x, ref y);
        _finalLayoutTransform.InvertXY(ref w, ref h);
      }
      RectangleF rect = new RectangleF(x, y, w, h);
      Font.Align align = Font.Align.Left;
      if (HorizontalAlignment == HorizontalAlignmentEnum.Right)
        align = Font.Align.Right;
      else if (HorizontalAlignment == HorizontalAlignmentEnum.Center)
        align = Font.Align.Center;

      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix = Matrix.Translation(-rect.X, -rect.Y, 0);
      m.Matrix *= Matrix.Scaling(SkinContext.Zoom.Width, SkinContext.Zoom.Height, 1);
      m.Matrix *= Matrix.Translation(rect.X, rect.Y, 0);
      SkinContext.AddTransform(m);

      Color4 color = ColorConverter.FromColor(Color);
      color.Alpha *= (float) SkinContext.Opacity;
      color.Alpha *= (float) Opacity;
      
      if (_resourceString != null)
      {
        bool scroll = Scroll && !Wrap;
        string[] lines = Wrap ? WrapText(_finalRect.Width, true) : new string[] {_resourceString.Evaluate() };

        float totalWidth;
        foreach (string line in lines)
        {
          _renderer.Draw(line, rect, ActualPosition.Z, align, FontSize * 0.9f, color, scroll, out totalWidth);
          rect.Y += FontSize * SkinContext.Zoom.Height;
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
      Color4 color = ColorConverter.FromColor(Color);

      base.DoRender();


      // The characters fits the textbox exactly, so to get some room between the top of the characters 
      // and the inner rectangle. Move the text down (10% of font size) also reduce the font size to 90%
      // of the value. Otherwise we will be outside of the inner rectangle.

      float y = _finalRect.Y + 0.1f * FontSize * SkinContext.Zoom.Height;
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
      if (HorizontalAlignment == HorizontalAlignmentEnum.Right)
        align = Font.Align.Right;
      else if (HorizontalAlignment == HorizontalAlignmentEnum.Center)
        align = Font.Align.Center;

      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix = Matrix.Translation(-rect.X, -rect.Y, 0);
      m.Matrix *= Matrix.Scaling(SkinContext.Zoom.Width, SkinContext.Zoom.Height, 1);
      m.Matrix *= Matrix.Translation(rect.X, rect.Y, 0);
      SkinContext.AddTransform(m);
      color.Alpha *= (float) SkinContext.Opacity;
      color.Alpha *= (float) Opacity;

      if (_resourceString != null)
      {
        bool scroll = Scroll && !Wrap;
        string[] lines = Wrap ? WrapText(_finalRect.Width, true) : new string[] { _resourceString.Evaluate() };

        float totalWidth;
        foreach (string line in lines)
        {
          _asset.Draw(line, rect, align, FontSize * 0.9f, color, scroll, out totalWidth);
          rect.Y += FontSize * SkinContext.Zoom.Height;
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

