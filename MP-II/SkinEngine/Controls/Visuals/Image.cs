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

using System;
using System.Drawing;
using System.Diagnostics;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.ContentManagement;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public enum StretchDirection { UpOnly, DownOnly, Both };

  public enum Stretch
  {
    //The content preserves its original size.
    None,

    //The content is resized to fill the destination dimensions. The aspect ratio is not preserved.
    Fill,

    //The content is resized to fit in the destination dimensions while it preserves its native aspect ratio.
    Uniform,

    //The content is resized to fill the destination dimensions while it preserves its native aspect ratio. 
    //If the aspect ratio of the destination rectangle differs from the source, the source content is 
    //clipped to fit in the destination dimensions (zoom-in)
    UniformToFill
  };

  public class Image : Control
  {
    #region Private fields

    Property _fallbackSourceProperty;
    Property _imageSourceProperty;
    Property _stretchDirectionProperty;
    Property _thumbnailProperty;
    private VertextBufferAsset _image;
    private VertextBufferAsset _fallbackImage;
    Property _stretchProperty;
    TextureRender _renderImage;
    TextureRender _renderFallback;

    float _u, _v, _uoff, _voff, _w, _h;
    Vector2 _pos;
    bool _performImageLayout;

    #endregion

    #region Ctor

    public Image()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _imageSourceProperty = new Property(typeof(string), null);
      _fallbackSourceProperty = new Property(typeof(string), null);
      _stretchDirectionProperty = new Property(typeof(StretchDirection), StretchDirection.Both);
      _thumbnailProperty = new Property(typeof(bool), false);
      _stretchProperty = new Property(typeof(Stretch), Stretch.None);
    }

    void Attach()
    {
      _imageSourceProperty.Attach(OnImageChanged);
      _stretchDirectionProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _imageSourceProperty.Detach(OnImageChanged);
      _stretchDirectionProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Image i = (Image) source;
      Source = copyManager.GetCopy(i.Source);
      FallbackSource = copyManager.GetCopy(i.FallbackSource);
      StretchDirection = copyManager.GetCopy(i.StretchDirection);
      Thumbnail = copyManager.GetCopy(i.Thumbnail);
      Attach();
    }

    #endregion

    /// <summary>
    /// Called when a property changed. 
    /// Simply sets a variable to indicate a layout needs to be performed
    /// </summary>
    /// <param name="property">The property.</param>
    void OnPropertyChanged(Property property)
    {
      _performImageLayout = true;
      if (Screen != null) Screen.Invalidate(this);
    }

    /// <summary>
    /// Called when the imagesource has been changed
    /// Simply invalidates the image, the renderer will automaticly create a new one
    /// with the new image source
    /// </summary>
    /// <param name="property">The property.</param>
    void OnImageChanged(Property property)
    {
      if (_image != null)
      {
        _image.Free(true);
        ContentManager.Remove(_image);
        _image = null;

        if (_renderImage != null)
        {
          _renderImage.Free();
          _renderImage = null;
        }
      }
      _performImageLayout = true;
      if (Screen != null) Screen.Invalidate(this);
    }

    public Property StretchProperty
    {
      get { return _stretchProperty; }
    }

    public Stretch Stretch
    {
      get { return (Stretch)_stretchProperty.GetValue(); }
      set { _stretchProperty.SetValue(value); }
    }

    public Property ThumbnailProperty
    {
      get { return _thumbnailProperty; }
    }

    public bool Thumbnail
    {
      get { return (bool)_thumbnailProperty.GetValue(); }
      set { _thumbnailProperty.SetValue(value); }
    }

    public Property SourceProperty
    {
      get { return _imageSourceProperty; }
      set { _imageSourceProperty = value; }
    }

    public string Source
    {
      get { return (string)_imageSourceProperty.GetValue(); }
      set { _imageSourceProperty.SetValue(value); }
    }

    public Property FallbackSourceProperty
    {
      get { return _fallbackSourceProperty; }
    }

    public string FallbackSource
    {
      get { return (string)_fallbackSourceProperty.GetValue(); }
      set { _fallbackSourceProperty.SetValue(value); }
    }

    public Property StretchDirectionProperty
    {
      get { return _stretchDirectionProperty; }
    }

    public StretchDirection StretchDirection
    {
      get { return (StretchDirection)_stretchDirectionProperty.GetValue(); }
      set { _stretchDirectionProperty.SetValue(value); }
    }

    public override void Measure(ref SizeF totalSize)
    {
      InitializeTriggers();
      Deallocate();
      float w = (float)Width * SkinContext.Zoom.Width;
      float h = (float)Height * SkinContext.Zoom.Height;

      if (_image == null && Source != null)
      {
        _image = ContentManager.Load(Source, Thumbnail);
        _image.Texture.UseThumbNail = Thumbnail;
        _image.Texture.Allocate();

        if (SkinContext.UseBatching)
          _renderImage = new TextureRender(_image.Texture);
      }

      if (_fallbackImage == null && FallbackSource != null)
      {
        _fallbackImage = ContentManager.Load(FallbackSource, Thumbnail);
        _fallbackImage.Texture.UseThumbNail = Thumbnail;
        _fallbackImage.Texture.Allocate();

        if (SkinContext.UseBatching)
          _renderFallback = new TextureRender(_fallbackImage.Texture);
      }

      if (_image != null)
      {
        if (Double.IsNaN(w))
          w = _image.Texture.Width * SkinContext.Zoom.Width;
        if (Double.IsNaN(h))
          h = _image.Texture.Height * SkinContext.Zoom.Height;
      }
      else if (_fallbackImage != null)
      {
        if (Double.IsNaN(w))
          w = _fallbackImage.Texture.Width * SkinContext.Zoom.Width;
        if (Double.IsNaN(h))
          h = _fallbackImage.Texture.Height * SkinContext.Zoom.Height;
      }

      _desiredSize = new SizeF(w, h);


      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }

      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("Image.Measure :{0} returns {1}x{2}", this.Name, (int)totalSize.Width, (int)totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("Image.Arrange :{0} X {1},Y {2} W {3}xH {4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));
      ComputeInnerRectangle(ref finalRect);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      if (_image != null)
      {
        PerformLayout(_image);
      }

      if (_fallbackImage != null)
      {
        PerformLayout(_fallbackImage);
      }

      if (Screen != null)
        Screen.Invalidate(this);
    }

    public override void DoBuildRenderTree()
    {
      if (_hidden) 
        return;

      if (!IsVisible) 
        return;
      if (!IsEnabled && Opacity == 0.0) 
        return;

      SkinContext.AddOpacity(this.Opacity);
      float opacity = (float)SkinContext.Opacity;
      float posx = _pos.X + (float)ActualPosition.X;
      float posy = _pos.Y + (float)ActualPosition.Y;
      if (_renderImage != null)
      {
        _renderImage.Draw(posx, posy, ActualPosition.Z, _w, _h, _uoff, _voff, _u, _v, opacity, opacity, opacity, opacity);
        if (_renderImage.Texture.IsAllocated)
        {
          SkinContext.RemoveOpacity();
          return;
        }
      }
      if (_renderFallback != null)
      {
        _renderFallback.Draw(posx, posy, ActualPosition.Z, _w, _h, _uoff, _voff, _u, _v, opacity, opacity, opacity, opacity);
      }
      SkinContext.RemoveOpacity();
    }

    public override void DestroyRenderTree()
    {
      if (_renderImage != null)
      {
        _renderImage.Free();
        _renderImage = null;
      }
      if (_renderFallback != null)
      {
        _renderFallback.Free();
        _renderFallback = null;
      }
    }

    public override void DoRender()
    {
      if (!IsEnabled && Opacity == 0.0)
        return;

      base.DoRender();
      SkinContext.AddOpacity(this.Opacity);
      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix = Matrix.Translation(new Vector3((float)ActualPosition.X, (float)ActualPosition.Y, (float)ActualPosition.Z));
      SkinContext.AddTransform(m);
      //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
      float opacity = (float)SkinContext.Opacity;
      if (_image != null)
      {
        _image.Draw(_pos.X, _pos.Y, ActualPosition.Z, _w, _h, _uoff, _voff, _u, _v, opacity, opacity, opacity, opacity);
        if (_image.Texture.IsAllocated)
        {
          SkinContext.RemoveTransform();
          SkinContext.RemoveOpacity();
          return;
        }
      }
      if (_fallbackImage != null)
      {
        _fallbackImage.Draw(_pos.X, _pos.Y, ActualPosition.Z, _w, _h, _uoff, _voff, _u, _v, opacity, opacity, opacity, opacity);
      }
      SkinContext.RemoveTransform();
      SkinContext.RemoveOpacity();
    }


    void PerformLayout(VertextBufferAsset asset)
    {
      if (asset != null && asset.Texture.IsAllocated)
      {
        Vector3 imgScale = new Vector3(1, 1, 1);
        if (Width > 0.0f)
        {
          if (asset.Texture.Width > 0)
          {
            imgScale.X = (float)(ActualWidth / (float)asset.Texture.Width * SkinContext.Zoom.Width);
          }
        }
        if (Height > 0.0f)
        {
          if (asset.Texture.Height > 0)
          {
            imgScale.Y = (float)(ActualHeight / (float)asset.Texture.Height * SkinContext.Zoom.Height);
          }
        }

        Vector2 pos = new Vector2(0, 0);
        float height = (float)ActualHeight;
        float width = (float)ActualWidth;
        float pixelRatio = 1.0f;
        float fSourceFrameRatio = asset.Texture.Width / (float)asset.Texture.Height;
        float fOutputFrameRatio = fSourceFrameRatio / pixelRatio;
        if (this.Stretch == Stretch.Uniform || this.Stretch == Stretch.UniformToFill)
        {

          if (this.Stretch == Stretch.Uniform)
          {
            float fNewWidth = width;
            float fNewHeight = fNewWidth / fOutputFrameRatio;

            // make sure the height is not larger than the maximum
            if (fNewHeight > ActualHeight)
            {
              fNewHeight = height;
              fNewWidth = fNewHeight * fOutputFrameRatio;
            }

            // this shouldnt happen, but just make sure that everything still fits onscreen
            if (fNewWidth > ActualWidth || fNewHeight > ActualHeight)
            {
              fNewWidth = (float)ActualWidth;
              fNewHeight = (float)ActualHeight;
            }

            width = fNewWidth;
            height = fNewHeight;

          }
        }

        //center image
        pos.X += ((((float)ActualWidth) - width) / 2.0f);
        pos.Y += ((((float)ActualHeight) - height) / 2.0f);

        float iSourceX = 0;
        float iSourceY = 0;
        float iSourceWidth = asset.Texture.Width;
        float iSourceHeight = asset.Texture.Height;
        if (Stretch == Stretch.UniformToFill)
        {
          float imageWidth = asset.Texture.Width;
          float imageHeight = asset.Texture.Height;
          // suggested by ziphnor
          //float fOutputFrameRatio = fSourceFrameRatio / pixelRatio;
          float fSourcePixelRatio = fSourceFrameRatio / (imageWidth / imageHeight);
          float fCroppedOutputFrameRatio = fSourcePixelRatio * (imageWidth / imageHeight) / pixelRatio;

          // calculate AR compensation (see http://www.iki.fi/znark/video/conversion)
          // assume that the movie is widescreen first, so use full height
          float fVertBorder = 0;
          float fNewHeight = height;
          float fNewWidth = fNewHeight * fOutputFrameRatio;
          float fHorzBorder = (fNewWidth - width) / 2.0f;
          float fFactor = fNewWidth / imageWidth;
          fFactor *= pixelRatio;
          fHorzBorder = fHorzBorder / fFactor;

          if ((int)fNewWidth < width)
          {
            fHorzBorder = 0;
            fNewWidth = width;
            fNewHeight = fNewWidth / fOutputFrameRatio;
            fVertBorder = (fNewHeight - height) / 2.0f;
            fFactor = fNewWidth / imageWidth;
            fFactor *= pixelRatio;
            fVertBorder = fVertBorder / fFactor;
          }
          iSourceX = fHorzBorder;
          iSourceY = fVertBorder;
          iSourceWidth = (imageWidth - 2.0f * fHorzBorder);
          iSourceHeight = (imageHeight - 2.0f * fVertBorder);
        }
        // x-offset in texture
        float uoffs = iSourceX / (float)asset.Texture.Width;

        // y-offset in texture
        float voffs = iSourceY / (float)asset.Texture.Height;

        // width copied from texture
        float u = iSourceWidth / (float)asset.Texture.Width;

        // height copied from texture
        float v = iSourceHeight / (float)asset.Texture.Height;


        if (uoffs < 0 || uoffs > 1)
          uoffs = 0;
        if (u < 0 || u > 1)
          u = 1;
        if (v < 0 || v > 1)
          v = 1;
        if (u + uoffs > 1)
        {
          uoffs = 0;
          u = 1;
        }

        _u = u;
        _v = v;
        _uoff = uoffs;
        _voff = voffs;
        _w = width;
        _h = height;
        _pos = pos;
      }
      else
        Trace.WriteLine("Not Allocated");
    }

    public override void Deallocate()
    {
      if (_image != null)
      {
        _image.Free(true);
        ContentManager.Remove(_image);
        _image = null;
      }

      if (_fallbackImage != null)
      {
        _fallbackImage.Free(true);
        ContentManager.Remove(_fallbackImage);
        _fallbackImage = null;
      }

      if (_renderImage != null)
      {
        _renderImage.Free();
        _renderImage = null;
      }
      if (_renderFallback != null)
      {
        _renderFallback.Free();
        _renderFallback = null;
      }
      base.Deallocate();
    }

    public override void BecomesHidden()
    {
      if (_renderImage != null)
        _renderImage.Free();
      if (_renderFallback != null)
        _renderFallback.Free();
    }

    public override void BecomesVisible()
    {

      if (_renderImage != null)
        _renderImage.Alloc();
      if (_renderFallback != null)
        _renderFallback.Alloc();
    }

    public override void Update()
    {
      base.Update();
      if (_hidden == false)
      {
        if (_performImageLayout)
        {
          DoBuildRenderTree();
          _performImageLayout = false;
        }
      }
    }
  }
}
